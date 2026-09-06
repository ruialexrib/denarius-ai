using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using DenariusAI.Web.Controllers;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies the shared import snapshot against selected budgets and accounting signs.</summary>
public sealed class ImportBudgetExecutionTests
{
    /// <summary>Checks explicit budget association, cancellation, fallback categories, income signs and empty allocations.</summary>
    [Fact]
    public async Task SnapshotMatchesExpensesAndIncludesIncomeWithoutPendingRows()
    {
        await using var db = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var expenseGroup = new FinancialGroup { Name = "Expenses", Kind = FinancialGroupKind.Expense };
        var incomeGroup = new FinancialGroup { Name = "Income", Kind = FinancialGroupKind.Income };
        var expense = new Category { Name = "Food", FinancialGroup = expenseGroup };
        var income = new Category { Name = "Salary", FinancialGroup = incomeGroup };
        var empty = new Category { Name = "Empty", FinancialGroup = expenseGroup };
        var bank = new Account { Name = "Bank", AccountType = AccountType.BankAccount };
        var expenseAccount = new Account { Name = "Expense", AccountType = AccountType.Expense, Category = expense };
        var incomeAccount = new Account { Name = "Income", AccountType = AccountType.Income, Category = income };
        var budget = new Budget { Year = 2026, Month = 7 };
        var other = new Budget { Year = 2026, Month = 8 };
        db.AddRange(expense, income, empty, bank, expenseAccount, incomeAccount, budget, other);
        db.BudgetLines.Add(new BudgetLine { Budget = budget, Category = expense, Amount = 100m });
        await db.SaveChangesAsync();
        var active = Entry(bank.Id, expenseAccount.Id, budget.Id, expense.Id, 30m, false);
        var fallback = Entry(bank.Id, expenseAccount.Id, budget.Id, null, 5m, false);
        var salary = Entry(bank.Id, incomeAccount.Id, budget.Id, income.Id, 200m, true);
        var cancelled = Entry(bank.Id, expenseAccount.Id, budget.Id, expense.Id, 90m, false);
        var elsewhere = Entry(bank.Id, expenseAccount.Id, other.Id, expense.Id, 50m, false);
        db.AddRange(active, fallback, salary, cancelled, elsewhere);
        await db.SaveChangesAsync();
        cancelled.Cancel("test", DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        var repository = new BudgetRepository(db);
        var snapshot = (await repository.GetCategoryExecutionAsync(budget.Id)).ToDictionary(item => item.CategoryId);
        Assert.Equal(100m, snapshot[expense.Id].Budgeted);
        Assert.Equal(35m, snapshot[expense.Id].Actual);
        Assert.Equal(200m, snapshot[income.Id].Actual);
        Assert.Equal(0m, snapshot[income.Id].Budgeted);
        Assert.Equal(0m, snapshot[empty.Id].Actual);
        Assert.Equal(0m, snapshot[empty.Id].Budgeted);
        var standard = await repository.GetExecutionAsync(2026, 7);
        Assert.All(standard, item => Assert.Equal(item, snapshot[item.CategoryId]));
        Assert.DoesNotContain(standard, item => item.CategoryId == income.Id);
        Assert.Equal(50m, (await repository.GetCategoryExecutionAsync(other.Id)).Single(item => item.CategoryId == expense.Id).Actual);
        Assert.Empty(await repository.GetCategoryExecutionAsync(Guid.NewGuid()));
        var controller = new ReconciliationController(null!, null!, NullLogger<ReconciliationController>.Instance, db, null!, null!, repository);
        var review = new ReconciliationImportReviewViewModel { BankAccountId = bank.Id, BudgetId = budget.Id, BudgetYear = 1900, BudgetMonth = 1 };
        var invalid = Assert.IsType<ViewResult>(await controller.ConfirmImport(review, CancellationToken.None));
        Assert.Equal("ReviewImport", invalid.ViewName);
        Assert.Equal(35m, review.CategoryExecution[expense.Id].Actual);
        Assert.NotEmpty(review.Categories);
    }

    /// <summary>Creates an adjacent-month movement explicitly linked to the selected budget.</summary>
    /// <param name="bank">The bank account.</param>
    /// <param name="counter">The income or expense account.</param>
    /// <param name="budget">The selected budget.</param>
    /// <param name="category">The explicit category, or null to use the account fallback.</param>
    /// <param name="amount">The positive movement amount.</param>
    /// <param name="income">Whether the movement is an inflow.</param>
    /// <returns>A balanced unpersisted entry.</returns>
    private static JournalEntry Entry(Guid bank, Guid counter, Guid budget, Guid? category, decimal amount, bool income)
    {
        var entry = new JournalEntry(new DateOnly(2026, 8, 1), "Test movement");
        entry.AssignBudget(budget);
        entry.AddLine(bank, income ? amount : 0m, income ? 0m : amount);
        entry.AddLine(counter, income ? 0m : amount, income ? amount : 0m, categoryId: category);
        return entry;
    }
}
