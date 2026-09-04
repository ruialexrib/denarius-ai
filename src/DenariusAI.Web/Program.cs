using DenariusAI.Application;
using DenariusAI.Infrastructure;
using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GoogleProfileImages")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(30); options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; });
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
        .SetApplicationName("DenariusAI");
}
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
var applicationAssembly = typeof(Program).Assembly;
var applicationVersion = applicationAssembly
    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
    .SingleOrDefault()?.InformationalVersion.Split('+')[0]
    ?? applicationAssembly.GetName().Version?.ToString(3)
    ?? "0.21.1";
builder.Services.AddSingleton(new DenariusAI.Web.Models.ApplicationInfo(
    Version: applicationVersion,
    Description: "O controlo do seu futuro financeiro começa aqui."));
builder.Services.AddHealthChecks().AddDbContextCheck<DenariusDbContext>("sqlserver");

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);
await SeedAdministratorAsync(app);
await ProvisionDemoGuestAsync(app);
await SeedDemonstrationUsersAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health", new HealthCheckOptions()).AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DenariusDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "Database migration failed during application startup.");
        throw;
    }
}

static async Task SeedAdministratorAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("IdentitySeed");
    var email = configuration["InitialAdmin:Email"];
    var password = configuration["InitialAdmin:Password"];
    var displayName = configuration["InitialAdmin:DisplayName"] ?? "Administrador";
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in ApplicationRoles.All)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogWarning("Initial administrator was not configured; no user was seeded.");
        return;
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var existingUser = await userManager.FindByEmailAsync(email);
    if (existingUser is not null) { if (!await userManager.IsInRoleAsync(existingUser, ApplicationRoles.Administrator)) await userManager.AddToRoleAsync(existingUser, ApplicationRoles.Administrator); return; }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        DisplayName = displayName
    };
    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        var errors = string.Join("; ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"Initial administrator could not be created: {errors}");
    }
    await userManager.AddToRoleAsync(user, ApplicationRoles.Administrator);

    logger.LogInformation("Initial administrator created for {Email}.", email);
}

/// <summary>
/// Creates or synchronizes the configured public demonstration guest when demo mode is enabled.
/// </summary>
/// <param name="application">The running web application whose services and configuration are used.</param>
static async Task ProvisionDemoGuestAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (!configuration.GetValue("DemoMode:Enabled", false)) return;

    var email = configuration["DemoMode:Email"];
    var password = configuration["DemoMode:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Demo mode requires DemoMode:Email and DemoMode:Password to be configured.");

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Convidado — Demo"
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException($"Demo guest could not be created: {string.Join("; ", createResult.Errors.Select(error => error.Code))}");
    }
    else if (!await userManager.CheckPasswordAsync(user, password))
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
        if (!resetResult.Succeeded)
            throw new InvalidOperationException($"Demo guest password could not be synchronized: {string.Join("; ", resetResult.Errors.Select(error => error.Code))}");
    }

    if (!await userManager.IsInRoleAsync(user, ApplicationRoles.User))
    {
        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.User);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException($"Demo guest role could not be assigned: {string.Join("; ", roleResult.Errors.Select(error => error.Code))}");
    }

    if (await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator))
    {
        var removeResult = await userManager.RemoveFromRoleAsync(user, ApplicationRoles.Administrator);
        if (!removeResult.Succeeded)
            throw new InvalidOperationException($"Demo guest administrator role could not be removed: {string.Join("; ", removeResult.Errors.Select(error => error.Code))}");
    }

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeed");
    logger.LogInformation("Demo guest account synchronized for {Email}.", email);
}

static async Task SeedDemonstrationUsersAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DenariusDbContext>();
    if (!await dbContext.JournalEntries.AnyAsync(entry => entry.CreatedBy == "demo-seed")) return;

    var service = scope.ServiceProvider.GetRequiredService<DenariusAI.Application.Abstractions.Services.IDemonstrationDataService>();
    await service.EnsureUsersAsync();
}

public partial class Program;