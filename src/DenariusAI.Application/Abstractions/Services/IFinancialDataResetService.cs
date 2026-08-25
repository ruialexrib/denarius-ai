namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Represents the result of a financial data reset operation.
/// </summary>
/// <param name="Accounts">The number of accounts that were reset.</param>
/// <param name="JournalEntries">The number of journal entries that were reset.</param>
/// <param name="Reconciliations">The number of reconciliations that were reset.</param>
/// <param name="Budgets">The number of budgets that were reset.</param>
public sealed record FinancialDataResetResult(int Accounts, int JournalEntries, int Reconciliations, int Budgets);

/// <summary>
/// Defines a service for resetting financial data in the system.
/// </summary>
public interface IFinancialDataResetService
{
    /// <summary>
    /// Resets all financial data asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the reset operation results.</returns>
    Task<FinancialDataResetResult> ResetAsync(CancellationToken cancellationToken = default);
}
