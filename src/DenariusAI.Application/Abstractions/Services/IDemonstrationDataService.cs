namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Represents the result of loading demonstration data into the system.
/// </summary>
/// <param name="Loaded">Indicates whether the demonstration data was successfully loaded.</param>
/// <param name="Accounts">The number of accounts created or loaded.</param>
/// <param name="JournalEntries">The number of journal entries created or loaded.</param>
/// <param name="Budgets">The number of budgets created or loaded.</param>
public sealed record DemonstrationDataLoadResult(bool Loaded, int Accounts, int JournalEntries, int Budgets);

/// <summary>
/// Service responsible for loading demonstration data into the system.
/// </summary>
public interface IDemonstrationDataService
{
    /// <summary>
    /// Asynchronously loads demonstration data into the system.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the load operation.</returns>
    Task<DemonstrationDataLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Ensures the two non-privileged users used by the demonstration scenario exist.</summary>
    Task EnsureUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the demonstration scenario is loaded exactly once for a brand-new installation, using an
    /// explicit persisted initialization marker rather than the presence of financial records to detect
    /// whether the automatic first-installation seeding has already occurred.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the load operation.</returns>
    Task<DemonstrationDataLoadResult> EnsureInitialDemonstrationDataAsync(CancellationToken cancellationToken = default);
}
