using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing journal entries in the database.
/// </summary>
/// <param name="dbContext">The database context used for data access.</param>
public sealed class JournalEntryRepository(DenariusDbContext dbContext) : Repository<JournalEntry>(dbContext), IJournalEntryRepository
{
    /// <summary>
    /// Retrieves a list of journal entry summaries ordered by date and creation time.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of journal entry summary DTOs.</returns>
    public async Task<IReadOnlyList<JournalEntrySummaryDto>> ListSummariesAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt)
            .Select(entry => new JournalEntrySummaryDto(
                entry.Id,
                entry.Date,
                entry.Description,
                entry.Reference,
                entry.Lines.Sum(line => (decimal?)line.Debit) ?? 0m,
                entry.Lines.Sum(line => (decimal?)line.Credit) ?? 0m,
                entry.Status,
                entry.Reconciliation == null ? ReconciliationStatus.Unreconciled : entry.Reconciliation.Status,
                entry.Budget == null ? null : entry.Budget.Year,
                entry.Budget == null ? null : entry.Budget.Month,
                entry.BudgetId,
                entry.Lines.Any(line => line.Account.AccountType != AccountType.Income && line.Account.AccountType != AccountType.Expense && line.Debit > 0) &&
                entry.Lines.Any(line => line.Account.AccountType != AccountType.Income && line.Account.AccountType != AccountType.Expense && line.Credit > 0) ? "Transferência" :
                entry.Lines.Any(line => line.Account.AccountType != AccountType.Income && line.Account.AccountType != AccountType.Expense && line.Debit > 0) ? "Entrada" :
                entry.Lines.Any(line => line.Account.AccountType != AccountType.Income && line.Account.AccountType != AccountType.Expense && line.Credit > 0) ? "Saída" : "Transferência"))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieves a journal entry with all its related details including lines, accounts, categories, reconciliation, and budget information.
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The journal entry with details, or null if not found.</returns>
    public Task<JournalEntry?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(entry => entry.Lines).ThenInclude(line => line.Account)
            .Include(entry => entry.Lines).ThenInclude(line => line.Category)
            .Include(entry => entry.Reconciliation)
            .Include(entry => entry.Budget)
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    /// <summary>
    /// Retrieves a filtered list of journal entries for reconciliation purposes.
    /// </summary>
    /// <param name="accountId">Optional account identifier to filter by.</param>
    /// <param name="from">Optional start date for filtering.</param>
    /// <param name="to">Optional end date for filtering.</param>
    /// <param name="status">Optional reconciliation status to filter by.</param>
    /// <param name="search">Optional search term to filter by description or reference.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of reconciliation item DTOs.</returns>
    public async Task<IReadOnlyList<ReconciliationItemDto>> ListForReconciliationAsync(Guid? accountId, DateOnly? from, DateOnly? to, ReconciliationStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var bankingTypes = new[] { AccountType.BankAccount, AccountType.Savings, AccountType.TermDeposit };
        var query = Set.AsNoTracking().Where(entry => entry.Status == JournalEntryStatus.Active &&
            entry.Lines.Any(line => bankingTypes.Contains(line.Account.AccountType) && (!accountId.HasValue || line.AccountId == accountId.Value)));
        if (from.HasValue) query = query.Where(entry => entry.Date >= from.Value);
        if (to.HasValue) query = query.Where(entry => entry.Date <= to.Value);
        if (status.HasValue) query = status == ReconciliationStatus.Reconciled
            ? query.Where(entry => entry.Reconciliation != null && entry.Reconciliation.Status == ReconciliationStatus.Reconciled)
            : query.Where(entry => entry.Reconciliation == null || entry.Reconciliation.Status == ReconciliationStatus.Unreconciled);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(entry => entry.Description.Contains(term) || (entry.Reference != null && entry.Reference.Contains(term)));
        }
        return await query.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt)
            .Select(entry => new ReconciliationItemDto(
                entry.Id, entry.Date, entry.Description, entry.Reference,
                string.Join(", ", entry.Lines.Where(line => bankingTypes.Contains(line.Account.AccountType) && (!accountId.HasValue || line.AccountId == accountId.Value)).Select(line => line.Account.Name)),
                entry.Lines.Where(line => bankingTypes.Contains(line.Account.AccountType) && (!accountId.HasValue || line.AccountId == accountId.Value)).Sum(line => (decimal?)line.Debit) ?? 0m,
                entry.Lines.Where(line => bankingTypes.Contains(line.Account.AccountType) && (!accountId.HasValue || line.AccountId == accountId.Value)).Sum(line => (decimal?)line.Credit) ?? 0m,
                entry.Reconciliation == null ? ReconciliationStatus.Unreconciled : entry.Reconciliation.Status,
                entry.Reconciliation == null ? null : entry.Reconciliation.ReconciledAt,
                entry.Reconciliation == null ? null : entry.Reconciliation.ReconciledBy))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Calculates the total amount for a specific financial group kind within a date range.
    /// </summary>
    /// <param name="from">The start date of the period.</param>
    /// <param name="to">The end date of the period.</param>
    /// <param name="kind">The financial group kind (Income or Expense).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The total amount for the specified financial group kind.</returns>
    public Task<decimal> GetAmountByGroupKindAsync(DateOnly from, DateOnly to, FinancialGroupKind kind, CancellationToken cancellationToken = default)
    {
        var lines = DbContext.JournalEntryLines.AsNoTracking().Where(line =>
            line.JournalEntry.Status == JournalEntryStatus.Active && line.JournalEntry.Date >= from && line.JournalEntry.Date <= to &&
            ((line.Category != null && line.Category.FinancialGroup.Kind == kind) ||
             (line.CategoryId == null && line.Account.Category != null && line.Account.Category.FinancialGroup.Kind == kind)));
        return kind == FinancialGroupKind.Income
            ? lines.SumAsync(line => line.Credit - line.Debit, cancellationToken)
            : lines.SumAsync(line => line.Debit - line.Credit, cancellationToken);
    }

    /// <inheritdoc />
    public Task<decimal> GetAmountByBudgetAndGroupKindAsync(int year, int month, FinancialGroupKind kind, CancellationToken cancellationToken = default)
    {
        var lines = DbContext.JournalEntryLines.AsNoTracking().Where(line =>
            line.JournalEntry.Status == JournalEntryStatus.Active &&
            line.JournalEntry.Budget != null && line.JournalEntry.Budget.Year == year && line.JournalEntry.Budget.Month == month &&
            ((line.Category != null && line.Category.FinancialGroup.Kind == kind) ||
             (line.CategoryId == null && line.Account.Category != null && line.Account.Category.FinancialGroup.Kind == kind)));
        return kind == FinancialGroupKind.Income
            ? lines.SumAsync(line => line.Credit - line.Debit, cancellationToken)
            : lines.SumAsync(line => line.Debit - line.Credit, cancellationToken);
    }

    /// <summary>
    /// Generates a classification statement for a specific financial group or category, showing all transactions and running balance.
    /// </summary>
    /// <param name="groupId">Optional financial group identifier to filter by.</param>
    /// <param name="categoryId">Optional category identifier to filter by.</param>
    /// <param name="kind">The financial group kind (Income or Expense) for balance calculation.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of classification statement line DTOs with running balances.</returns>
    public async Task<IReadOnlyList<ClassificationStatementLineDto>> GetClassificationStatementAsync(Guid? groupId, Guid? categoryId, FinancialGroupKind kind, CancellationToken cancellationToken = default)
    {
        var query = DbContext.JournalEntryLines.AsNoTracking().Where(line => line.JournalEntry.Status == JournalEntryStatus.Active);
        if (categoryId.HasValue)
            query = query.Where(line => line.CategoryId == categoryId.Value || (line.CategoryId == null && line.Account.CategoryId == categoryId.Value));
        else if (groupId.HasValue)
            query = query.Where(line => (line.Category != null && line.Category.FinancialGroupId == groupId.Value)
                || (line.CategoryId == null && line.Account.Category != null && line.Account.Category.FinancialGroupId == groupId.Value));

        var lines = await query.OrderBy(line => line.JournalEntry.Date)
            .ThenBy(line => line.JournalEntry.CreatedAt)
            .ThenBy(line => line.JournalEntryId)
            .ThenBy(line => line.Id)
            .Select(line => new
            {
                line.JournalEntryId,
                LineId = line.Id,
                line.JournalEntry.Date,
                line.JournalEntry.CreatedAt,
                line.JournalEntry.Description,
                line.JournalEntry.Reference,
                AccountName = line.Account.Name,
                CategoryName = line.Category != null ? line.Category.Name : line.Account.Category!.Name,
                line.Debit,
                line.Credit
            })
            .ToListAsync(cancellationToken);

        var balance = 0m;
        var statement = new List<ClassificationStatementLineDto>(lines.Count);
        foreach (var line in lines)
        {
            balance += kind == FinancialGroupKind.Income ? line.Credit - line.Debit : line.Debit - line.Credit;
            statement.Add(new ClassificationStatementLineDto(line.JournalEntryId, line.LineId, line.Date, line.CreatedAt,
                line.Description, line.Reference, line.AccountName, line.CategoryName, line.Debit, line.Credit, balance));
        }
        return statement;
    }
}
