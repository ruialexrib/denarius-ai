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
        return await ExecutionQuery(budgetId, false).ToListAsync(cancellationToken);
    }

    /// <summary>Gets an import snapshot of actual income and expenses without counting pending rows.</summary>
    /// <param name="budgetId">The explicitly selected budget.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Category totals for an existing budget, otherwise an empty list.</returns>
    public async Task<IReadOnlyList<BudgetExecutionItemDto>> GetCategoryExecutionAsync(Guid budgetId, CancellationToken cancellationToken = default)
    {
        if (!await Set.AsNoTracking().AnyAsync(budget => budget.Id == budgetId, cancellationToken)) return [];
        return await ExecutionQuery(budgetId, true).ToListAsync(cancellationToken);
    }

    /// <summary>Builds the shared execution query using explicit budget association and active entries.</summary>
    /// <param name="budgetId">The budget identifier, or null for an unplanned period.</param>
    /// <param name="includeIncome">Whether income categories should also be returned.</param>
    /// <returns>A deterministic query with positive ordinary income and expense amounts.</returns>
    private IQueryable<BudgetExecutionItemDto> ExecutionQuery(Guid? budgetId, bool includeIncome) =>
        DbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive && (category.FinancialGroup.Kind == FinancialGroupKind.Expense ||
                (includeIncome && category.FinancialGroup.Kind == FinancialGroupKind.Income)))
            .OrderBy(category => category.FinancialGroup.SortOrder).ThenBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new BudgetExecutionItemDto(
                category.Id,
                category.Name,
                DbContext.BudgetLines.Where(line => budgetId.HasValue && line.BudgetId == budgetId && line.CategoryId == category.Id).Sum(line => (decimal?)line.Amount) ?? 0m,
                (DbContext.JournalEntryLines.Where(line => budgetId.HasValue && line.JournalEntry.Status == JournalEntryStatus.Active && line.JournalEntry.BudgetId == budgetId &&
                    (line.CategoryId == category.Id || (line.CategoryId == null && line.Account.CategoryId == category.Id))).Sum(line => (decimal?)(line.Debit - line.Credit)) ?? 0m) *
                    (category.FinancialGroup.Kind == FinancialGroupKind.Income ? -1m : 1m),
                category.FinancialGroupId,
                category.FinancialGroup.Name));
}
