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
}
