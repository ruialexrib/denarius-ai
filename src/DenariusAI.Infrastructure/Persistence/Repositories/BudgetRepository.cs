using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing budget entities and related operations.
/// </summary>
/// <param name="dbContext">The database context for data access operations.</param>
public sealed class BudgetRepository(DenariusDbContext dbContext) : Repository<Budget>(dbContext), IBudgetRepository
{
    /// <summary>
    /// Retrieves a budget for a specific year and month including its lines and categories.
    /// </summary>
    /// <param name="year">The year of the budget period.</param>
    /// <param name="month">The month of the budget period.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The budget for the specified period, or null if not found.</returns>
    public Task<Budget?> GetByPeriodAsync(int year, int month, CancellationToken cancellationToken = default) =>
        Set.Include(budget => budget.Lines).ThenInclude(line => line.Category)
            .SingleOrDefaultAsync(budget => budget.Year == year && budget.Month == month, cancellationToken);

    /// <summary>
    /// Retrieves the budget execution details for a specific year and month.
    /// Compares budgeted amounts with actual expenses for each category.
    /// </summary>
    /// <param name="year">The year of the budget execution period.</param>
    /// <param name="month">The month of the budget execution period.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of budget execution items containing budgeted and actual amounts per category.</returns>
    public async Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var budgetId = await Set.AsNoTracking().Where(budget => budget.Year == year && budget.Month == month)
            .Select(budget => (Guid?)budget.Id).SingleOrDefaultAsync(cancellationToken);
        return await DbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive && category.FinancialGroup.Kind == FinancialGroupKind.Expense)
            .OrderBy(category => category.FinancialGroup.SortOrder).ThenBy(category => category.SortOrder)
            .Select(category => new BudgetExecutionItemDto(
                category.Id,
                category.Name,
                DbContext.BudgetLines.Where(line => line.Budget.Year == year && line.Budget.Month == month && line.CategoryId == category.Id).Sum(line => (decimal?)line.Amount) ?? 0m,
                DbContext.JournalEntryLines.Where(line => budgetId.HasValue && line.JournalEntry.Status == JournalEntryStatus.Active && line.JournalEntry.BudgetId == budgetId &&
                    (line.CategoryId == category.Id || (line.CategoryId == null && line.Account.CategoryId == category.Id))).Sum(line => (decimal?)(line.Debit - line.Credit)) ?? 0m,
                category.FinancialGroupId,
                category.FinancialGroup.Name))
            .ToListAsync(cancellationToken);
    }
}
