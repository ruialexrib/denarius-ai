using System.Security.Claims;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Api;
using DenariusAI.Web.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace DenariusAI.IntegrationTests.Api;

/// <summary>Runs the production HTTP pipeline with ephemeral data and test-only endpoint probes.</summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Replaces external persistence and startup side effects without bypassing HTTP security.</summary>
    /// <param name="builder">The application host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("HttpsRedirection:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IApplicationInitializer>();
            services.AddSingleton<IApplicationInitializer, TestInitializer>();
            services.RemoveAll<DbContextOptions<DenariusDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DenariusDbContext>>();
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContext<DenariusDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
            services.AddControllersWithViews().AddApplicationPart(typeof(ApiProbeController).Assembly);
        });
    }

    /// <summary>Creates an HTTPS client that exposes redirects to assertions.</summary>
    /// <returns>An isolated HTTP client.</returns>
    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    /// <summary>Protects a synthetic access ticket using the production bearer handler configuration.</summary>
    /// <param name="role">The role to include in the ticket.</param>
    /// <param name="expired">Whether the ticket is already expired.</param>
    /// <returns>An opaque protected access token.</returns>
    public string CreateAccessToken(string role = "User", bool expired = false)
    {
        var options = Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>()
            .Get(ApiFoundation.AuthenticationScheme);
        return options.BearerTokenProtector.Protect(Ticket(ApiFoundation.AuthenticationScheme, role, expired));
    }

    /// <summary>Protects a synthetic MVC cookie to verify separation from bearer authentication.</summary>
    /// <returns>The cookie header value.</returns>
    public string CreateMvcCookie()
    {
        var options = Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(Ticket(IdentityConstants.ApplicationScheme, "User", false))}";
    }

    /// <summary>Creates a ticket with no real user or financial information.</summary>
    /// <param name="scheme">The ticket authentication scheme.</param>
    /// <param name="role">The synthetic role claim.</param>
    /// <param name="expired">Whether the ticket is expired.</param>
    /// <returns>The ticket used by a real authentication handler in tests.</returns>
    private static AuthenticationTicket Ticket(string scheme, string role, bool expired) => new(
        new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "api-fixture"), new Claim(ClaimTypes.Role, role)], scheme)),
        new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(expired ? -1 : 5)
        }, scheme);

    /// <summary>Replaces migrations and seed operations only through test service registration.</summary>
    private sealed class TestInitializer : IApplicationInitializer
    {
        /// <summary>Leaves test persistence initialization to the ephemeral provider.</summary>
        /// <param name="application">The application under test.</param>
        /// <returns>An already completed task.</returns>
        public Task InitializeAsync(WebApplication application) => Task.CompletedTask;
    }
}
