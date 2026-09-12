using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies deterministic persistence queries for the income, expense and flow analysis.
/// </summary>
public sealed class IncomeExpenseFlowRepositoryTests
{
    /// <summary>
    /// Verifies income, expense, category weights, account flows and cancelled-entry exclusion.
    /// </summary>
    [Fact]
    public async Task GetAsyncBuildsClassifiedFlowsAndExcludesCancelledEntries()
    {
        await using var context = CreateContext();
        var incomeGroup = new FinancialGroup { Name = "Rendimentos", Kind = FinancialGroupKind.Income };
        var expenseGroup = new FinancialGroup { Name = "Despesas", Kind = FinancialGroupKind.Expense };
        var salary = new Category { Name = "Salário", FinancialGroup = incomeGroup };
        var food = new Category { Name = "Alimentação", FinancialGroup = expenseGroup };
        var bank = new Account { Name = "Conta à Ordem", AccountType = AccountType.BankAccount };
        var incomeAccount = new Account { Name = "Rendimentos", AccountType = AccountType.Income, Category = salary };
        var expenseAccount = new Account { Name = "Alimentação", AccountType = AccountType.Expense, Category = food };
        context.AddRange(incomeGroup, expenseGroup, salary, food, bank, incomeAccount, expenseAccount);
        await context.SaveChangesAsync();

        var income = new JournalEntry(new(2026, 8, 5), "Ordenado");
        income.AddLine(bank.Id, 2000m, 0m);
        income.AddLine(incomeAccount.Id, 0m, 2000m, categoryId: salary.Id);
        var expense = new JournalEntry(new(2026, 8, 10), "Supermercado");
        expense.AddLine(expenseAccount.Id, 500m, 0m, categoryId: food.Id);
        expense.AddLine(bank.Id, 0m, 500m);
        var previousExpense = new JournalEntry(new(2026, 7, 10), "Supermercado julho");
        previousExpense.AddLine(expenseAccount.Id, 400m, 0m, categoryId: food.Id);
        previousExpense.AddLine(bank.Id, 0m, 400m);
        var cancelled = new JournalEntry(new(2026, 8, 15), "Despesa anulada");
        cancelled.AddLine(expenseAccount.Id, 900m, 0m, categoryId: food.Id);
        cancelled.AddLine(bank.Id, 0m, 900m);
        cancelled.Cancel("test", DateTimeOffset.UtcNow);
        context.AddRange(income, expense, previousExpense, cancelled);
        await context.SaveChangesAsync();

        var result = await new IncomeExpenseFlowRepository(context)
            .GetAsync(new(2026, 8, 1), new(2026, 8, 31));

        Assert.Equal(2000m, result.Income);
        Assert.Equal(500m, result.Expenses);
        Assert.Equal(400m, result.PreviousExpenses);
        var expenseCategory = Assert.Single(result.ExpenseCategories);
        Assert.Equal("Alimentação", expenseCategory.Name);
        Assert.Equal(100m, expenseCategory.Weight);
        Assert.Equal(25m, expenseCategory.ChangePercentage);
        var incomeCategory = Assert.Single(result.IncomeCategories);
        Assert.Equal("Salário", incomeCategory.Name);
        var account = Assert.Single(result.AccountFlows);
        Assert.Equal(1500m, account.NetFlow);
        Assert.Equal(-400m, account.PreviousNetFlow);
        Assert.DoesNotContain(result.LargestMovements, item => item.Description == "Despesa anulada");
    }

    /// <summary>
    /// Creates an isolated in-memory application database.
    /// </summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => new(
        new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
