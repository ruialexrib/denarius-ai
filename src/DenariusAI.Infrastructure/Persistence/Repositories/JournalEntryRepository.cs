using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

public sealed class JournalEntryRepository(DenariusDbContext dbContext) : Repository<JournalEntry>(dbContext), IJournalEntryRepository
{
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
                entry.BudgetId))
            .ToListAsync(cancellationToken);

    public Task<JournalEntry?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(entry => entry.Lines).ThenInclude(line => line.Account)
            .Include(entry => entry.Lines).ThenInclude(line => line.Category)
            .Include(entry => entry.Reconciliation)
            .Include(entry => entry.Budget)
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

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
}
