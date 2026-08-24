using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

public sealed class BudgetRepository(DenariusDbContext dbContext) : Repository<Budget>(dbContext), IBudgetRepository
{
    public Task<Budget?> GetByPeriodAsync(int year, int month, CancellationToken cancellationToken = default) =>
        Set.Include(budget => budget.Lines).ThenInclude(line => line.Category)
            .SingleOrDefaultAsync(budget => budget.Year == year && budget.Month == month, cancellationToken);

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
