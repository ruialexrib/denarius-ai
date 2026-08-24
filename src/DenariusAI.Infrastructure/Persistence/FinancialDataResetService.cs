using DenariusAI.Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

public sealed class FinancialDataResetService(DenariusDbContext dbContext) : IFinancialDataResetService
{
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
