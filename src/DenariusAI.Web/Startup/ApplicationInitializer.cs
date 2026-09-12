using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Startup;

/// <summary>Preserves the migration and seed sequence used by the web application.</summary>
public sealed class ApplicationInitializer : IApplicationInitializer
{
    /// <summary>Completes migrations, administrator seeding and demonstration initialization in order.</summary>
    /// <param name="application">The application providing startup services.</param>
    /// <returns>A task representing initialization completion.</returns>
    public async Task InitializeAsync(WebApplication application)
    {
        await ApplyDatabaseMigrationsAsync(application);
        await SeedAdministratorAsync(application);
        await EnsureInitialDemonstrationDataAsync(application);
    }

    /// <summary>Applies pending database migrations before accepting requests.</summary>
    /// <param name="application">The application providing startup services.</param>
    /// <returns>A task representing initialization completion.</returns>
    private static async Task ApplyDatabaseMigrationsAsync(WebApplication application)
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

    /// <summary>Ensures the application roles and configured initial administrator exist.</summary>
    /// <param name="application">The application providing startup services.</param>
    /// <returns>A task representing initialization completion.</returns>
    /// <exception cref="InvalidOperationException">The configured administrator could not be created.</exception>
    private static async Task SeedAdministratorAsync(WebApplication application)
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

        logger.LogInformation("Initial administrator created successfully.");
    }

    /// <summary>Initializes demonstration data and demonstration users when required.</summary>
    /// <param name="application">The application providing startup services.</param>
    /// <returns>A task representing initialization completion.</returns>
    private static async Task EnsureInitialDemonstrationDataAsync(WebApplication application)
    {
        await using var scope = application.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemonstrationDataSeed");
        var service = scope.ServiceProvider.GetRequiredService<DenariusAI.Application.Abstractions.Services.IDemonstrationDataService>();

        var result = await service.EnsureInitialDemonstrationDataAsync();
        if (result.Loaded)
            logger.LogInformation("Demonstration data automatically loaded on first initialization: {Accounts} accounts, {JournalEntries} journal entries, {Budgets} budgets.", result.Accounts, result.JournalEntries, result.Budgets);

        await service.EnsureUsersAsync();
    }

}
