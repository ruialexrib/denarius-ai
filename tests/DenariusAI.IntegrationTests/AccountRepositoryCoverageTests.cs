using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies account repository balance and statement edge cases.</summary>
public sealed class AccountRepositoryCoverageTests
{
    /// <summary>Verifies income signs, active filtering, running balances, and cancelled-entry exclusion.</summary>
    [Fact]
    public async Task RepositoryCalculatesBalancesAndExcludesCancelledEntries()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var bank = NewAccount("Banco", AccountType.BankAccount, 100m, true);
        var income = NewAccount("Salário", AccountType.Income, 0m, true);
        var inactive = NewAccount("Antiga", AccountType.Cash, 50m, false);
        context.Accounts.AddRange(bank, income, inactive);
        var salary = NewEntry(new DateOnly(2026, 1, 2), "Salário", bank.Id, income.Id, 1000m);
        var cancelled = NewEntry(new DateOnly(2026, 1, 3), "Salário cancelado", bank.Id, income.Id, 500m);
        context.JournalEntries.AddRange(salary, cancelled);
        await context.SaveChangesAsync();
        cancelled.Cancel("test-user", DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
        var repository = new AccountRepository(context);

        Assert.Equal(1100m, await repository.GetBalanceAsync(bank.Id));
        Assert.Equal(1000m, await repository.GetBalanceAsync(income.Id));
        Assert.Equal(2, (await repository.ListWithBalancesAsync(activeOnly: true)).Count);
        var statement = Assert.Single(await repository.GetStatementAsync(bank.Id));
        Assert.Equal(1100m, statement.Balance);
        Assert.Equal("Salário", statement.Description);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.GetBalanceAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.GetStatementAsync(Guid.NewGuid()));
    }

    /// <summary>Creates an account for repository tests.</summary>
    /// <param name="name">Account name.</param><param name="type">Account type.</param><param name="initialBalance">Opening balance.</param><param name="active">Whether the account is active.</param>
    /// <returns>The new account.</returns>
    private static Account NewAccount(string name, AccountType type, decimal initialBalance, bool active) => new()
    { Name = name, AccountType = type, InitialBalance = initialBalance, Currency = "EUR", IsActive = active };

    /// <summary>Creates a balanced transfer between bank and income accounts.</summary>
    /// <param name="date">Movement date.</param><param name="description">Movement description.</param><param name="bankId">Bank account identifier.</param><param name="incomeId">Income account identifier.</param><param name="amount">Movement amount.</param>
    /// <returns>The balanced journal entry.</returns>
    private static JournalEntry NewEntry(DateOnly date, string description, Guid bankId, Guid incomeId, decimal amount)
    {
        var entry = new JournalEntry(date, description);
        entry.AddLine(bankId, amount, 0m);
        entry.AddLine(incomeId, 0m, amount);
        return entry;
    }
}
