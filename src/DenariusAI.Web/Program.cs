using DenariusAI.Web.Api;
using DenariusAI.Web.Startup;
using DenariusAI.Application;
using DenariusAI.Infrastructure;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;

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
builder.Services.AddApiFoundation();
builder.Services.AddSingleton<IApplicationInitializer, ApplicationInitializer>();
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

await app.Services.GetRequiredService<IApplicationInitializer>().InitializeAsync(app);

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<ApiBoundaryMiddleware>());

if (!app.Environment.IsDevelopment())
{
    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
        mvc => mvc.UseExceptionHandler("/Home/Error"));
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
app.UseRateLimiter();
app.UseAuthorization();
app.MapDenariusApi();
app.MapHealthChecks("/health", new HealthCheckOptions()).AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

/// <summary>Provides the web application entry point for HTTP integration tests.</summary>
public partial class Program;
