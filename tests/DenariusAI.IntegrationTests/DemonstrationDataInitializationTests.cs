using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the first-installation detection and automatic demonstration seeding behavior of
/// <see cref="DemonstrationDataService.EnsureInitialDemonstrationDataAsync"/>.
/// </summary>
public sealed class DemonstrationDataInitializationTests
{
    /// <summary>
    /// Verifies that a brand-new database (no persisted initialization marker) automatically receives the
    /// complete demonstration scenario exactly once.
    /// </summary>
    [Fact]
    public async Task FirstInitializationAutomaticallyLoadsDemonstrationScenario()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var result = await new DemonstrationDataService(context).EnsureInitialDemonstrationDataAsync();

        Assert.True(result.Loaded);
        Assert.Equal(5, result.Accounts);
        Assert.Equal(72, result.JournalEntries);
        Assert.Equal(8, result.Budgets);
        Assert.Equal(5, await context.Reminders.CountAsync());
        Assert.True(await context.ApplicationSettings.AnyAsync(setting => setting.Key == "System.InitialDemonstrationDataSeededAt"));
    }

    /// <summary>
    /// Verifies that an ordinary restart (initialization marker already present) does not reload or
    /// duplicate demonstration data, even though financial records are still present.
    /// </summary>
    [Fact]
    public async Task SubsequentStartupDoesNotReseedWhenAlreadyInitialized()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = new DemonstrationDataService(context);
        await service.EnsureInitialDemonstrationDataAsync();
        var accountsAfterFirstRun = await context.Accounts.CountAsync();

        var second = await service.EnsureInitialDemonstrationDataAsync();

        Assert.False(second.Loaded);
        Assert.Equal(accountsAfterFirstRun, await context.Accounts.CountAsync());
        Assert.Equal(1, await context.ApplicationSettings.CountAsync(setting => setting.Key == "System.InitialDemonstrationDataSeededAt"));
    }

    /// <summary>
    /// Verifies that once initialization has been recorded, deleting all financial records (for example
    /// through the explicit reset action) does not trigger an automatic reseed on the next startup, because
    /// detection relies on the persisted marker rather than on <c>Accounts.Any()</c> or similar checks.
    /// </summary>
    [Fact]
    public async Task DeletingFinancialRecordsAfterInitializationDoesNotTriggerAutomaticReseed()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = new DemonstrationDataService(context);
        await service.EnsureInitialDemonstrationDataAsync();

        await new FinancialDataResetService(context).ResetAsync();
        var result = await service.EnsureInitialDemonstrationDataAsync();

        Assert.False(result.Loaded);
        Assert.Empty(context.Accounts);
        Assert.Empty(context.JournalEntries);
    }

    /// <summary>
    /// Verifies that recreating the database from an empty volume (a fresh, unseeded context) reproduces
    /// the automatic demonstration scenario exactly once, mirroring the desired behavior after deleting and
    /// recreating a database volume.
    /// </summary>
    [Fact]
    public async Task RecreatingDatabaseFromEmptyVolumeLoadsScenarioAutomaticallyOnce()
    {
        await using var firstContext = CreateContext();
        await firstContext.Database.EnsureCreatedAsync();
        await new DemonstrationDataService(firstContext).EnsureInitialDemonstrationDataAsync();

        await using var recreatedContext = CreateContext();
        await recreatedContext.Database.EnsureCreatedAsync();
        var result = await new DemonstrationDataService(recreatedContext).EnsureInitialDemonstrationDataAsync();

        Assert.True(result.Loaded);
        Assert.Equal(5, await recreatedContext.Accounts.CountAsync());
    }

    /// <summary>
    /// Creates an isolated in-memory database context for a single test.
    /// </summary>
    /// <returns>A new database context backed by a uniquely named in-memory database.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
