using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

public sealed class GroupAndCategoryServiceTests
{
    [Fact]
    public async Task GroupCanBeCreatedAndUpdated()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var service = new FinancialGroupService(unit);
        var id = await service.CreateAsync(new("Despesas pessoais", "Descrição", FinancialGroupKind.Expense, 10), "user-id");
        await service.UpdateAsync(id, new("Despesas regulares", "Atualizada", FinancialGroupKind.Expense, 2), "user-id");
        var result = await service.GetAsync(id);
        Assert.Equal("Despesas regulares", result?.Name); Assert.Equal(2, result?.SortOrder); Assert.NotNull(context.FinancialGroups.Single().UpdatedAt);
    }

    [Fact]
    public async Task DuplicateGroupNameIsRejected()
    {
        await using var context = CreateContext(); var service = new FinancialGroupService(CreateUnit(context));
        await service.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 2), "user-id"));
    }

    [Fact]
    public async Task GroupWithActiveCategoriesCannotBeDeactivated()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var groupService = new FinancialGroupService(unit); var categoryService = new CategoryService(unit);
        var groupId = await groupService.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        await categoryService.CreateAsync(new(groupId, "Água", null, 1), "user-id");
        await Assert.ThrowsAsync<InvalidOperationException>(() => groupService.SetActiveAsync(groupId, false, "user-id"));
    }

    [Fact]
    public async Task CategoryCanBeDeactivatedThenItsGroupCanBeDeactivated()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var groupService = new FinancialGroupService(unit); var categoryService = new CategoryService(unit);
        var groupId = await groupService.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        var categoryId = await categoryService.CreateAsync(new(groupId, "Água", null, 1), "user-id");
        await categoryService.SetActiveAsync(categoryId, false, "user-id"); await groupService.SetActiveAsync(groupId, false, "user-id");
        Assert.False((await groupService.GetAsync(groupId))!.IsActive); Assert.False((await categoryService.GetAsync(categoryId))!.IsActive);
    }

    [Fact]
    public async Task UsedCategoryCannotMoveToAnotherGroup()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var groupService = new FinancialGroupService(unit); var categoryService = new CategoryService(unit);
        var firstGroup = await groupService.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        var secondGroup = await groupService.CreateAsync(new("Extra", null, FinancialGroupKind.Expense, 2), "user-id");
        var categoryId = await categoryService.CreateAsync(new(firstGroup, "Água", null, 1), "user-id");
        context.Accounts.Add(new Account { Name = "Água", AccountType = AccountType.Expense, Currency = "EUR", CategoryId = categoryId }); await context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => categoryService.UpdateAsync(categoryId, new(secondGroup, "Água", null, 1), "user-id"));
    }

    /// <summary>
    /// Verifies that movement usage is based only on journal entry lines and not account default categories.
    /// </summary>
    [Fact]
    public async Task CategoryUsageReportsOnlyJournalMovementReferences()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var groupService = new FinancialGroupService(unit); var categoryService = new CategoryService(unit);
        var groupId = await groupService.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        var usedCategoryId = await categoryService.CreateAsync(new(groupId, "Supermercado", null, 1), "user-id");
        var unusedCategoryId = await categoryService.CreateAsync(new(groupId, "Cinema", null, 2), "user-id");
        var accountDefaultOnlyCategoryId = await categoryService.CreateAsync(new(groupId, "Combustível", null, 3), "user-id");

        var bankAccount = new Account { Name = "Banco", AccountType = AccountType.Asset, Currency = "EUR" };
        var expenseAccount = new Account { Name = "Compras", AccountType = AccountType.Expense, Currency = "EUR" };
        var defaultCategoryAccount = new Account { Name = "Combustível", AccountType = AccountType.Expense, Currency = "EUR", CategoryId = accountDefaultOnlyCategoryId };
        context.Accounts.AddRange(bankAccount, expenseAccount, defaultCategoryAccount); await context.SaveChangesAsync();

        var entry = new JournalEntry(new DateOnly(2026, 9, 6), "Compra");
        entry.AddLine(expenseAccount.Id, 25m, 0m, categoryId: usedCategoryId);
        entry.AddLine(bankAccount.Id, 0m, 25m);
        context.JournalEntries.Add(entry); await context.SaveChangesAsync();

        var usedCategoryIds = await new CategoryUsageService(unit).GetUsedInJournalMovementsAsync([usedCategoryId, unusedCategoryId, accountDefaultOnlyCategoryId]);

        Assert.Contains(usedCategoryId, usedCategoryIds);
        Assert.DoesNotContain(unusedCategoryId, usedCategoryIds);
        Assert.DoesNotContain(accountDefaultOnlyCategoryId, usedCategoryIds);
    }

    [Fact]
    public async Task CategoryCannotBeReactivatedUnderInactiveGroup()
    {
        await using var context = CreateContext(); var unit = CreateUnit(context); var groupService = new FinancialGroupService(unit); var categoryService = new CategoryService(unit);
        var groupId = await groupService.CreateAsync(new("Despesas", null, FinancialGroupKind.Expense, 1), "user-id");
        var categoryId = await categoryService.CreateAsync(new(groupId, "Água", null, 1), "user-id");
        await categoryService.SetActiveAsync(categoryId, false, "user-id"); await groupService.SetActiveAsync(groupId, false, "user-id");
        await Assert.ThrowsAsync<InvalidOperationException>(() => categoryService.SetActiveAsync(categoryId, true, "user-id"));
    }

    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static UnitOfWork CreateUnit(DenariusDbContext context) => new(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
}
