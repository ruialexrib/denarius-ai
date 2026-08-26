using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for PersistenceAndServiceTests.
/// </summary>
public sealed class PersistenceAndServiceTests
{
    [Fact]
    public async Task AnalyticsAggregatesFilteredPeriodAndBuildsTrend()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var service = new AnalyticsService(new AnalyticsRepository(context));

        var analytics = await service.GetAsync(new AnalyticsFilterDto(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)));

        Assert.True(analytics.Income > 0m);
        Assert.True(analytics.Expenses > 0m);
        Assert.NotEmpty(analytics.Categories);
        Assert.NotEmpty(analytics.Groups);
        Assert.Single(analytics.Trend);
        Assert.Equal(analytics.Income - analytics.Expenses, analytics.Savings);
    }

    [Fact]
    public async Task DashboardCombinesAccountsBudgetMonthlyResultsAndReconciliation()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var expenseCategory = await context.Categories.FirstAsync(item => item.FinancialGroup.Kind == FinancialGroupKind.Expense);
        var budget = new Budget { Year = 2026, Month = 7 };
        budget.Lines.Add(new BudgetLine { CategoryId = expenseCategory.Id, Amount = 100m });
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        var unitOfWork = new UnitOfWork(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
        var dashboardService = new DashboardService(
            new AccountService(unitOfWork),
            new JournalEntryService(unitOfWork),
            new BudgetService(unitOfWork),
            new ReconciliationService(unitOfWork));

        var dashboard = await dashboardService.GetAsync(2026, 7);

        Assert.Equal(2026, dashboard.Year);
        Assert.Equal(7, dashboard.Month);
        Assert.True(dashboard.TotalAssets > 0m);
        Assert.True(dashboard.Income > 0m);
        Assert.True(dashboard.Expenses > 0m);
        Assert.True(dashboard.Budgeted > 0m);
        Assert.NotEmpty(dashboard.Categories);
        Assert.Equal(12, dashboard.Evolution.Count);
        Assert.Equal(Enumerable.Range(1, 12), dashboard.Evolution.Select(item => item.Month));
        Assert.Equal(12, dashboard.BudgetEvolution.Count);
        Assert.Equal(Enumerable.Range(1, 12), dashboard.BudgetEvolution.Select(item => item.Month));
    }

    [Fact]
    public async Task GenericRepositoryPersistsAndFindsEntities()
    {
        await using var context = CreateContext();
        var repository = new Repository<Account>(context);
        var account = NewAccount("Conta principal", AccountType.BankAccount);

        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        Assert.True(await repository.ExistsAsync(item => item.Id == account.Id));
        Assert.Equal(account.Name, (await repository.GetByIdAsync(account.Id))?.Name);
    }

    [Fact]
    public async Task JournalServicePersistsBalancedEntryAndUpdatesBalance()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var service = new JournalEntryService(unitOfWork);
        var request = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Água", null, null,
        [
            new JournalEntryLineInput(expense.Id, 35m, 0m),
            new JournalEntryLineInput(bank.Id, 0m, 35m)
        ]);

        var created = await service.CreateAsync(request, "user-id");

        Assert.Equal(35m, created.TotalDebit);
        Assert.Equal(35m, created.TotalCredit);
        Assert.Single(context.JournalEntries);
        Assert.Equal(-35m, await unitOfWork.Accounts.GetBalanceAsync(bank.Id));
        Assert.Equal(35m, await unitOfWork.Accounts.GetBalanceAsync(expense.Id));
    }

    [Fact]
    public async Task JournalServiceRejectsUnbalancedEntryWithoutPersistingAnything()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var service = new JournalEntryService(unitOfWork);
        var request = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Inválido", null, null,
        [
            new JournalEntryLineInput(expense.Id, 40m, 0m),
            new JournalEntryLineInput(bank.Id, 0m, 35m)
        ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "user-id"));

        Assert.Empty(context.JournalEntries);
        Assert.Empty(context.JournalEntryLines);
    }

    [Fact]
    public async Task JournalServiceListsGetsAndUpdatesAnActiveEntry()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var service = new JournalEntryService(unitOfWork);
        var created = await service.CreateAsync(new(new DateOnly(2026, 8, 24), "Água", "FT-1", null,
        [
            new(expense.Id, 35m, 0m, "Consumo"),
            new(bank.Id, 0m, 35m)
        ]), "user-id");

        var summaries = await service.ListAsync();
        var details = await service.GetAsync(created.Id);
        Assert.Single(summaries);
        Assert.Equal(35m, summaries[0].TotalDebit);
        Assert.Equal("FT-1", details?.Reference);

        await service.UpdateAsync(created.Id, new(new DateOnly(2026, 8, 25), "Água corrigida", "FT-2", "Nota",
        [
            new(expense.Id, 40m, 0m),
            new(bank.Id, 0m, 40m)
        ]), "editor-id");

        details = await service.GetAsync(created.Id);
        Assert.Equal("Água corrigida", details?.Description);
        Assert.Equal(40m, details?.TotalDebit);
        Assert.Equal(2, await context.JournalEntryLines.CountAsync());
        Assert.Equal(-40m, await unitOfWork.Accounts.GetBalanceAsync(bank.Id));
    }

    [Fact]
    public async Task JournalServiceRejectsSameAccountAndDifferentCurrencies()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var service = new JournalEntryService(unitOfWork);
        var sameAccount = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Inválido", null, null,
        [new(bank.Id, 10m, 0m), new(bank.Id, 0m, 10m)]);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(sameAccount, "user-id"));

        expense.Currency = "USD";
        await context.SaveChangesAsync();
        var mixedCurrencies = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Inválido", null, null,
        [new(expense.Id, 10m, 0m), new(bank.Id, 0m, 10m)]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(mixedCurrencies, "user-id"));
        Assert.Empty(context.JournalEntries);
    }

    [Fact]
    public async Task CancelledJournalEntryCannotBeEdited()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var service = new JournalEntryService(unitOfWork);
        var request = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Água", null, null,
        [new(expense.Id, 35m, 0m), new(bank.Id, 0m, 35m)]);
        var created = await service.CreateAsync(request, "user-id");
        await service.CancelAsync(created.Id, "user-id");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(created.Id, request, "user-id"));
    }

    [Fact]
    public async Task InactiveAccountRejectsWholeJournalEntry()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        expense.IsActive = false;
        await context.SaveChangesAsync();
        var service = new JournalEntryService(unitOfWork);
        var request = new CreateJournalEntryRequest(new DateOnly(2026, 8, 24), "Inválido", null, null,
        [
            new JournalEntryLineInput(expense.Id, 35m, 0m),
            new JournalEntryLineInput(bank.Id, 0m, 35m)
        ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "user-id"));
        Assert.Empty(context.JournalEntries);
    }

    [Fact]
    public void BudgetExecutionHandlesZeroBudget()
    {
        var withoutBudget = new BudgetExecutionItemDto(Guid.NewGuid(), "Água", 0m, 35m);
        var withBudget = new BudgetExecutionItemDto(Guid.NewGuid(), "Água", 40m, 35m);

        Assert.Null(withoutBudget.ExecutionPercentage);
        Assert.Equal(87.50m, withBudget.ExecutionPercentage);
        Assert.Equal(-5m, withBudget.Variance);
    }

    [Fact]
    public async Task BudgetServiceSavesPartialPagesAndCalculatesActualFromAccountCategory()
    {
        await using var context = CreateContext();
        var accounts = new AccountRepository(context);
        var unitOfWork = new UnitOfWork(context, accounts, new JournalEntryRepository(context), new BudgetRepository(context));
        var group = new FinancialGroup { Name = "Despesas Correntes", Kind = FinancialGroupKind.Expense, IsActive = true };
        var water = new Category { Name = "Água", FinancialGroup = group, IsActive = true };
        var energy = new Category { Name = "Eletricidade", FinancialGroup = group, IsActive = true };
        var bank = NewAccount("Banco", AccountType.BankAccount);
        var expense = NewAccount("Despesas", AccountType.Expense);
        expense.Category = water;
        context.AddRange(group, water, energy, bank, expense);
        await context.SaveChangesAsync();
        var service = new BudgetService(unitOfWork);

        await service.SaveAsync(2026, 8, [new(water.Id, 40m), new(energy.Id, 70m)], "planner-id");
        var budgetId = (await context.Budgets.SingleAsync()).Id;
        await new JournalEntryService(unitOfWork).CreateAsync(new(new DateOnly(2026, 9, 12), "Fatura de água", null, null,
        [new(expense.Id, 35m, 0m), new(bank.Id, 0m, 35m)], budgetId), "user-id");
        var execution = await service.GetExecutionAsync(2026, 8);
        var waterExecution = Assert.Single(execution, item => item.CategoryId == water.Id);
        Assert.Equal(40m, waterExecution.Budgeted);
        Assert.Equal(35m, waterExecution.Actual);
        Assert.Equal("Despesas Correntes", waterExecution.FinancialGroupName);

        await service.SaveAsync(2026, 8, [new(water.Id, 45m)], "editor-id");
        execution = await service.GetExecutionAsync(2026, 8);
        Assert.Equal(45m, Assert.Single(execution, item => item.CategoryId == water.Id).Budgeted);
        Assert.Equal(70m, Assert.Single(execution, item => item.CategoryId == energy.Id).Budgeted);

        await service.SaveAsync(2026, 8, [new(water.Id, 0m)], "editor-id");
        Assert.Equal(0m, Assert.Single(await service.GetExecutionAsync(2026, 8), item => item.CategoryId == water.Id).Budgeted);
        Assert.Single(context.Budgets);
        Assert.Single(context.BudgetLines);
    }

    [Fact]
    public async Task BudgetServiceRejectsIncomeAndNegativeLines()
    {
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
        var group = new FinancialGroup { Name = "Rendimentos", Kind = FinancialGroupKind.Income, IsActive = true };
        var category = new Category { Name = "Salário", FinancialGroup = group, IsActive = true };
        context.AddRange(group, category);
        await context.SaveChangesAsync();
        var service = new BudgetService(unitOfWork);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(2026, 8, [new(category.Id, -1m)], "user-id"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(2026, 8, [new(category.Id, 100m)], "user-id"));
        Assert.Empty(context.Budgets);
    }

    [Fact]
    public void ReconciliationCanBeAppliedAndUndone()
    {
        var reconciliation = new Reconciliation();
        reconciliation.MarkReconciled("user-id", DateTimeOffset.UtcNow);
        Assert.Equal(ReconciliationStatus.Reconciled, reconciliation.Status);
        Assert.NotNull(reconciliation.ReconciledAt);

        reconciliation.MarkUnreconciled();
        Assert.Equal(ReconciliationStatus.Unreconciled, reconciliation.Status);
        Assert.Null(reconciliation.ReconciledAt);
    }

    [Fact]
    public async Task ReconciliationServiceListsBankMovementsAndPersistsAudit()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var journals = new JournalEntryService(unitOfWork);
        var reconciliation = new ReconciliationService(unitOfWork);
        var created = await journals.CreateAsync(new(new DateOnly(2026, 8, 20), "Eletricidade", "REF-20", null,
        [new(expense.Id, 60m, 0m), new(bank.Id, 0m, 60m)]), "creator-id");

        var pending = await reconciliation.ListAsync(bank.Id, status: ReconciliationStatus.Unreconciled);
        Assert.Single(pending);
        Assert.Equal("Conta principal", pending[0].AccountNames);
        Assert.Equal(60m, pending[0].Credit);

        await reconciliation.ReconcileAsync(created.Id, "reconciler-id");
        var reconciled = await reconciliation.ListAsync(status: ReconciliationStatus.Reconciled);
        Assert.Single(reconciled);
        Assert.Equal("reconciler-id", reconciled[0].ReconciledBy);
        Assert.NotNull(reconciled[0].ReconciledAt);

        await reconciliation.UndoAsync(created.Id, "reviewer-id");
        Assert.Single(await reconciliation.ListAsync(status: ReconciliationStatus.Unreconciled));
        Assert.Empty(await reconciliation.ListAsync(status: ReconciliationStatus.Reconciled));
    }

    [Fact]
    public async Task ReconciliationRejectsMovementWithoutBankAccount()
    {
        await using var context = CreateContext();
        var accounts = new AccountRepository(context);
        var unitOfWork = new UnitOfWork(context, accounts, new JournalEntryRepository(context), new BudgetRepository(context));
        var cash = NewAccount("Dinheiro", AccountType.Cash);
        var expense = NewAccount("Compras", AccountType.Expense);
        await context.Accounts.AddRangeAsync(cash, expense);
        await context.SaveChangesAsync();
        var created = await new JournalEntryService(unitOfWork).CreateAsync(new(new DateOnly(2026, 8, 21), "Compras", null, null,
        [new(expense.Id, 20m, 0m), new(cash.Id, 0m, 20m)]), "user-id");

        var service = new ReconciliationService(unitOfWork);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReconcileAsync(created.Id, "user-id"));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task ReconciliationFiltersByDatesSearchAndValidatesRange()
    {
        await using var context = CreateContext();
        var (unitOfWork, bank, expense) = await CreateUnitOfWorkWithAccountsAsync(context);
        var journals = new JournalEntryService(unitOfWork);
        await journals.CreateAsync(new(new DateOnly(2026, 7, 10), "Água julho", "JUL", null, [new(expense.Id, 30m, 0m), new(bank.Id, 0m, 30m)]), "user-id");
        await journals.CreateAsync(new(new DateOnly(2026, 8, 10), "Água agosto", "AGO", null, [new(expense.Id, 35m, 0m), new(bank.Id, 0m, 35m)]), "user-id");
        var service = new ReconciliationService(unitOfWork);

        Assert.Single(await service.ListAsync(from: new DateOnly(2026, 8, 1), to: new DateOnly(2026, 8, 31)));
        Assert.Single(await service.ListAsync(search: "JUL"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(from: new DateOnly(2026, 9, 1), to: new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public async Task MonthlySummaryCalculatesIncomeExpensesAndIgnoresCancelledEntries()
    {
        await using var context = CreateContext();
        var accounts = new AccountRepository(context);
        var journals = new JournalEntryRepository(context);
        var unitOfWork = new UnitOfWork(context, accounts, journals, new BudgetRepository(context));
        var expenseGroup = new FinancialGroup { Name = "Despesas", Kind = FinancialGroupKind.Expense };
        var incomeGroup = new FinancialGroup { Name = "Rendimentos", Kind = FinancialGroupKind.Income };
        var expenseCategory = new Category { Name = "Água", FinancialGroup = expenseGroup, IsActive = true };
        var incomeCategory = new Category { Name = "Salário", FinancialGroup = incomeGroup, IsActive = true };
        var bank = NewAccount("Banco", AccountType.BankAccount);
        var expense = NewAccount("Água", AccountType.Expense);
        var income = NewAccount("Salário", AccountType.Income);
        context.AddRange(expenseGroup, incomeGroup, expenseCategory, incomeCategory, bank, expense, income);
        await context.SaveChangesAsync();
        var service = new JournalEntryService(unitOfWork);
        var expenseEntry = await service.CreateAsync(new CreateJournalEntryRequest(new DateOnly(2026, 8, 10), "Água", null, null,
        [new(expense.Id, 35m, 0m, CategoryId: expenseCategory.Id), new(bank.Id, 0m, 35m)]), "user-id");
        await service.CreateAsync(new CreateJournalEntryRequest(new DateOnly(2026, 8, 1), "Salário", null, null,
        [new(bank.Id, 1000m, 0m), new(income.Id, 0m, 1000m, CategoryId: incomeCategory.Id)]), "user-id");

        var summary = await service.GetMonthlySummaryAsync(2026, 8);
        Assert.Equal(1000m, summary.Income);
        Assert.Equal(35m, summary.Expenses);
        Assert.Equal(965m, summary.Result);

        await service.CancelAsync(expenseEntry.Id, "user-id");
        summary = await service.GetMonthlySummaryAsync(2026, 8);
        Assert.Equal(0m, summary.Expenses);
    }

    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(IUnitOfWork UnitOfWork, Account Bank, Account Expense)> CreateUnitOfWorkWithAccountsAsync(DenariusDbContext context)
    {
        var accounts = new AccountRepository(context);
        var journals = new JournalEntryRepository(context);
        var budgets = new BudgetRepository(context);
        var unitOfWork = new UnitOfWork(context, accounts, journals, budgets);
        var bank = NewAccount("Conta principal", AccountType.BankAccount);
        var expense = NewAccount("Água", AccountType.Expense);
        await context.Accounts.AddRangeAsync(bank, expense);
        await context.SaveChangesAsync();
        return (unitOfWork, bank, expense);
    }

    private static Account NewAccount(string name, AccountType type) => new()
    {
        Name = name,
        AccountType = type,
        Currency = "EUR",
        IsActive = true
    };
}
