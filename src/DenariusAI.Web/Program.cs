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
builder.Services.AddSingleton(new DenariusAI.Web.Models.ApplicationInfo(
    Version: typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.2.0",
    Description: "O controlo do seu futuro financeiro começa aqui."));
builder.Services.AddHealthChecks().AddDbContextCheck<DenariusDbContext>("sqlserver");

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);
await SeedAdministratorAsync(app);
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

static async Task SeedDemonstrationUsersAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DenariusDbContext>();
    if (!await dbContext.JournalEntries.AnyAsync(entry => entry.CreatedBy == "demo-seed")) return;

    var service = scope.ServiceProvider.GetRequiredService<DenariusAI.Application.Abstractions.Services.IDemonstrationDataService>();
    await service.EnsureUsersAsync();
}

public partial class Program;
