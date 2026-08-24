namespace DenariusAI.Application.Abstractions.Services;

public sealed record FinancialDataResetResult(int Accounts, int JournalEntries, int Reconciliations, int Budgets);

public interface IFinancialDataResetService
{
    Task<FinancialDataResetResult> ResetAsync(CancellationToken cancellationToken = default);
}
