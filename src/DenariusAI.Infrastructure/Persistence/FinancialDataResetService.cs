using DenariusAI.Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Service responsible for resetting financial data by removing all related entities from the database.
/// </summary>
/// <param name="dbContext">The database context used to access and manipulate financial data.</param>
public sealed class FinancialDataResetService(DenariusDbContext dbContext) : IFinancialDataResetService
{
    /// <summary>
    /// Resets all financial data by deleting accounts, journal entries, reconciliations, budgets, and related entities.
    /// Uses transactions for relational databases to ensure data consistency.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A result containing the count of deleted entities.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the reset operation does not complete successfully.</exception>
    public async Task<FinancialDataResetResult> ResetAsync(CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await DeleteFinancialDataAsync(cancellationToken);
        }

        FinancialDataResetResult? result = null;
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            // A retry must start with a clean tracker and repeat the whole transaction.
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            result = await DeleteFinancialDataAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return result ?? throw new InvalidOperationException("The financial data reset did not complete.");
    }

    /// <summary>
    /// Deletes all financial data entities from the database in the correct order to maintain referential integrity.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A result containing the count of deleted accounts, journal entries, reconciliations, and budgets.</returns>
    private async Task<FinancialDataResetResult> DeleteFinancialDataAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts.ToListAsync(cancellationToken);
        var entries = await dbContext.JournalEntries.ToListAsync(cancellationToken);
        var reconciliations = await dbContext.Reconciliations.ToListAsync(cancellationToken);
        var budgets = await dbContext.Budgets.ToListAsync(cancellationToken);
        var entryLines = await dbContext.JournalEntryLines.ToListAsync(cancellationToken);
        var budgetLines = await dbContext.BudgetLines.ToListAsync(cancellationToken);
        var savingsCertificates = await dbContext.SavingsCertificates.ToListAsync(cancellationToken);

        dbContext.RemoveRange(reconciliations);
        dbContext.RemoveRange(budgetLines);
        dbContext.RemoveRange(budgets);
        dbContext.RemoveRange(entryLines);
        dbContext.RemoveRange(entries);
        dbContext.RemoveRange(accounts);
        dbContext.RemoveRange(savingsCertificates);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(accounts.Count, entries.Count, reconciliations.Count, budgets.Count);
    }
}
