using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(DenariusDbContext dbContext) : Repository<Account>(dbContext), IAccountRepository
{
    public async Task<IReadOnlyList<AccountDto>> ListWithBalancesAsync(bool activeOnly = false, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(account => !activeOnly || account.IsActive)
            .OrderBy(account => account.Name)
            .Select(account => new AccountDto(
                account.Id,
                account.Name,
                account.Description,
                account.AccountType,
                account.InitialBalance,
                account.AccountType == AccountType.Income
                    ? account.InitialBalance - (account.JournalEntryLines
                        .Where(line => line.JournalEntry.Status == JournalEntryStatus.Active)
                        .Sum(line => (decimal?)(line.Debit - line.Credit)) ?? 0m)
                    : account.InitialBalance + (account.JournalEntryLines
                        .Where(line => line.JournalEntry.Status == JournalEntryStatus.Active)
                        .Sum(line => (decimal?)(line.Debit - line.Credit)) ?? 0m),
                account.Currency,
                account.IsActive,
                account.CategoryId))
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await Set.AsNoTracking().Where(item => item.Id == accountId)
            .Select(item => new { item.InitialBalance, item.AccountType }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Account was not found.");
        var movement = await DbContext.JournalEntryLines.AsNoTracking()
            .Where(line => line.AccountId == accountId && line.JournalEntry.Status == JournalEntryStatus.Active)
            .SumAsync(line => line.Debit - line.Credit, cancellationToken);
        return account.AccountType == AccountType.Income
            ? account.InitialBalance - movement
            : account.InitialBalance + movement;
    }

    public async Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await Set.AsNoTracking()
            .Where(item => item.Id == accountId)
            .Select(item => new { item.InitialBalance, item.AccountType })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Account was not found.");

        var lines = await DbContext.JournalEntryLines.AsNoTracking()
            .Where(line => line.AccountId == accountId && line.JournalEntry.Status == JournalEntryStatus.Active)
            .OrderBy(line => line.JournalEntry.Date)
            .ThenBy(line => line.JournalEntry.CreatedAt)
            .ThenBy(line => line.JournalEntryId)
            .ThenBy(line => line.Id)
            .Select(line => new
            {
                line.JournalEntryId,
                LineId = line.Id,
                line.JournalEntry.Date,
                line.JournalEntry.CreatedAt,
                EntryDescription = line.JournalEntry.Description,
                line.JournalEntry.Reference,
                LineDescription = line.Description,
                CategoryName = line.Category == null ? null : line.Category.Name,
                line.Debit,
                line.Credit,
                ReconciliationStatus = line.JournalEntry.Reconciliation == null ? ReconciliationStatus.Unreconciled : line.JournalEntry.Reconciliation.Status
            })
            .ToListAsync(cancellationToken);

        var balance = account.InitialBalance;
        var statement = new List<AccountStatementLineDto>(lines.Count);
        foreach (var line in lines)
        {
            var movement = line.Debit - line.Credit;
            balance += account.AccountType == AccountType.Income ? -movement : movement;
            statement.Add(new AccountStatementLineDto(line.JournalEntryId, line.LineId, line.Date, line.CreatedAt,
                line.EntryDescription, line.Reference, line.LineDescription, line.CategoryName, line.Debit, line.Credit, balance, line.ReconciliationStatus));
        }

        return statement;
    }
}
