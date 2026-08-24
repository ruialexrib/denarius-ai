namespace DenariusAI.Application.Abstractions.Services;

public sealed record DemonstrationDataLoadResult(bool Loaded, int Accounts, int JournalEntries, int Budgets);

public interface IDemonstrationDataService
{
    Task<DemonstrationDataLoadResult> LoadAsync(CancellationToken cancellationToken = default);
}
