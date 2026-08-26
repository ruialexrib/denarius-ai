using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for AccountServiceTests.
/// </summary>
public sealed class AccountServiceTests
{
    [Fact]
    public async Task AccountCanBeCreatedUpdatedAndCurrencyIsNormalized()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var service = new AccountService(unit);
        var id = await service.CreateAsync(new("Conta principal", "Banco", AccountType.BankAccount, 125.50m, " eur ", null), "user-id");
        await service.UpdateAsync(id, new("Conta familiar", "Atualizada", AccountType.BankAccount, 150m, "EUR", null), "user-id");

        var result = await service.GetAsync(id);

        Assert.Equal("Conta familiar", result?.Name);
        Assert.Equal("Atualizada", result?.Description);
        Assert.Equal("EUR", result?.Currency);
        Assert.Equal(150m, result?.Balance);
    }

    [Fact]
    public async Task DuplicateAccountNameIsRejected()
    {
        await using var context = CreateContext(); var service = new AccountService(CreateUnit(context));
        await service.CreateAsync(new("Banco", null, AccountType.BankAccount, 0m, "EUR", null), "user-id");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new("Banco", null, AccountType.Savings, 0m, "EUR", null), "user-id"));
    }

    [Fact]
    public async Task CategoryMustMatchAccountType()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context);
        var groupId = await new FinancialGroupService(unit).CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        var categoryId = await new CategoryService(unit).CreateAsync(new(groupId, "Habitação", null, 1), "user-id");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new AccountService(unit)
            .CreateAsync(new("Conta bancária", null, AccountType.BankAccount, 0m, "EUR", categoryId), "user-id"));
    }

    [Fact]
    public async Task ListCalculatesAssetAndIncomeBalancesFromActiveEntries()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var accountService = new AccountService(unit);
        var bankId = await accountService.CreateAsync(new("Banco", null, AccountType.BankAccount, 100m, "EUR", null), "user-id");
        var incomeId = await accountService.CreateAsync(new("Salário", null, AccountType.Income, 0m, "EUR", null), "user-id");
        await new JournalEntryService(unit).CreateAsync(new(new DateOnly(2026, 8, 24), "Salário", null, null,
        [
            new(bankId, 50m, 0m),
            new(incomeId, 0m, 50m)
        ]), "user-id");

        var accounts = await accountService.ListAsync();

        Assert.Equal(150m, accounts.Single(item => item.Id == bankId).Balance);
        Assert.Equal(50m, accounts.Single(item => item.Id == incomeId).Balance);
    }

    [Fact]
    public async Task UsedAccountCannotChangeAccountingMeaning()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var service = new AccountService(unit);
        var bankId = await service.CreateAsync(new("Banco", null, AccountType.BankAccount, 0m, "EUR", null), "user-id");
        var expenseId = await service.CreateAsync(new("Despesa", null, AccountType.Expense, 0m, "EUR", null), "user-id");
        await new JournalEntryService(unit).CreateAsync(new(new DateOnly(2026, 8, 24), "Compra", null, null,
        [
            new(expenseId, 10m, 0m),
            new(bankId, 0m, 10m)
        ]), "user-id");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(bankId,
            new("Banco", null, AccountType.Savings, 0m, "EUR", null), "user-id"));
    }

    [Fact]
    public async Task AccountCannotReactivateWithInactiveCategory()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context);
        var groupId = await new FinancialGroupService(unit).CreateAsync(new("Património", null, FinancialGroupKind.Asset, 1), "user-id");
        var categoryService = new CategoryService(unit);
        var categoryId = await categoryService.CreateAsync(new(groupId, "Bancos", null, 1), "user-id");
        var service = new AccountService(unit);
        var accountId = await service.CreateAsync(new("Banco", null, AccountType.BankAccount, 0m, "EUR", categoryId), "user-id");
        await service.SetActiveAsync(accountId, false, "user-id");
        await categoryService.SetActiveAsync(categoryId, false, "user-id");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetActiveAsync(accountId, true, "user-id"));
    }

    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UnitOfWork CreateUnit(DenariusDbContext context) => new(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
}
