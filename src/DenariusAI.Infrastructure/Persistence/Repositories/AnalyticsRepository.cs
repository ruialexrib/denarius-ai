using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for retrieving analytics data from the database.
/// </summary>
/// <param name="dbContext">The database context.</param>
public sealed class AnalyticsRepository(DenariusDbContext dbContext) : IAnalyticsRepository
{
    /// <summary>
    /// Gets analytics data based on the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for analytics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the analytics data.</returns>
    public async Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default)
    {
        var current = FilterLines(filter, filter.From, filter.To);
        var days = filter.To.DayNumber - filter.From.DayNumber + 1;
        var previousTo = filter.From.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(days - 1));
        var previous = FilterLines(filter, previousFrom, previousTo);

        var income = await AmountAsync(current, FinancialGroupKind.Income, cancellationToken);
        var expenses = await AmountAsync(current, FinancialGroupKind.Expense, cancellationToken);
        var previousIncome = await AmountAsync(previous, FinancialGroupKind.Income, cancellationToken);
        var previousExpenses = await AmountAsync(previous, FinancialGroupKind.Expense, cancellationToken);

        var expenseLines = current.Where(line => line.CategoryId != null && line.Category!.FinancialGroup.Kind == FinancialGroupKind.Expense);
        var groupRows = await expenseLines.GroupBy(line => new { Id = line.Category!.FinancialGroupId, Name = line.Category.FinancialGroup.Name })
            .Select(group => new { group.Key.Id, group.Key.Name, Amount = group.Sum(line => line.Debit - line.Credit) }).ToListAsync(cancellationToken);
        var groups = groupRows.OrderByDescending(item => item.Amount).Select(item => new AnalyticsBreakdownDto(item.Id, item.Name, item.Amount)).ToList();
        var categoryRows = await expenseLines.GroupBy(line => new { Id = line.CategoryId!.Value, Name = line.Category!.Name })
            .Select(group => new { group.Key.Id, group.Key.Name, Amount = group.Sum(line => line.Debit - line.Credit) }).ToListAsync(cancellationToken);
        var categories = categoryRows.OrderByDescending(item => item.Amount).Select(item => new AnalyticsBreakdownDto(item.Id, item.Name, item.Amount)).ToList();
        var accountRows = await current.Where(line => line.Account.AccountType != AccountType.Income && line.Account.AccountType != AccountType.Expense)
            .GroupBy(line => new { line.AccountId, line.Account.Name }).Select(group => new { Id = group.Key.AccountId, group.Key.Name, Amount = group.Sum(line => line.Debit - line.Credit) })
            .ToListAsync(cancellationToken);
        var accounts = accountRows.OrderByDescending(item => item.Amount).Select(item => new AnalyticsBreakdownDto(item.Id, item.Name, item.Amount)).ToList();
        var certificates = await dbContext.SavingsCertificates.AsNoTracking().Where(item => item.InvestmentDate <= filter.To)
            .OrderBy(item => item.InvestmentDate).Select(item => new SavingsCertificateSummaryDto(item.Id, item.InvestmentDate,
                item.SeriesNumber, item.Description, item.InvestmentValue, item.Rate, item.CurrentValue, item.NextCapitalization)).ToListAsync(cancellationToken);

        var trend = new List<AnalyticsTrendDto>();
        var month = new DateOnly(filter.From.Year, filter.From.Month, 1);
        var finalMonth = new DateOnly(filter.To.Year, filter.To.Month, 1);
        while (month <= finalMonth)
        {
            var monthTo = month.AddMonths(1).AddDays(-1);
            if (monthTo > filter.To) monthTo = filter.To;
            var monthly = FilterLines(filter, month < filter.From ? filter.From : month, monthTo);
            trend.Add(new(month.Year, month.Month,
                await AmountAsync(monthly, FinancialGroupKind.Income, cancellationToken),
                await AmountAsync(monthly, FinancialGroupKind.Expense, cancellationToken),
                await NetWorthAsync(monthTo, cancellationToken)));
            month = month.AddMonths(1);
        }

        return new(income, expenses, previousIncome, previousExpenses, await NetWorthAsync(filter.To, cancellationToken),
            certificates.Sum(item => item.CurrentValue), certificates.Sum(item => item.Yield), certificates, groups, categories, accounts, trend);
    }

    /// <summary>
    /// Filters journal entry lines based on the specified criteria and date range.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <returns>A queryable collection of filtered journal entry lines.</returns>
    private IQueryable<JournalEntryLine> FilterLines(AnalyticsFilterDto filter, DateOnly from, DateOnly to) =>
        dbContext.JournalEntryLines.AsNoTracking().Where(line => line.JournalEntry.Status == JournalEntryStatus.Active && line.JournalEntry.Date >= from && line.JournalEntry.Date <= to
            && (!filter.AccountId.HasValue || line.JournalEntry.Lines.Any(item => item.AccountId == filter.AccountId.Value))
            && (!filter.CategoryId.HasValue || (line.CategoryId ?? line.Account.CategoryId) == filter.CategoryId)
            && (!filter.GroupId.HasValue || (line.Category != null ? line.Category.FinancialGroupId : line.Account.Category!.FinancialGroupId) == filter.GroupId));

    /// <summary>
    /// Calculates the total amount for a specific financial group kind from the provided query.
    /// </summary>
    /// <param name="query">The journal entry lines query.</param>
    /// <param name="kind">The financial group kind (Income or Expense).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total amount.</returns>
    private static Task<decimal> AmountAsync(IQueryable<JournalEntryLine> query, FinancialGroupKind kind, CancellationToken cancellationToken) => kind == FinancialGroupKind.Income
        ? query.Where(line => (line.Category != null ? line.Category.FinancialGroup.Kind : line.Account.Category!.FinancialGroup.Kind) == kind).SumAsync(line => line.Credit - line.Debit, cancellationToken)
        : query.Where(line => (line.Category != null ? line.Category.FinancialGroup.Kind : line.Account.Category!.FinancialGroup.Kind) == kind).SumAsync(line => line.Debit - line.Credit, cancellationToken);

    /// <summary>
    /// Calculates the net worth as of the specified date.
    /// </summary>
    /// <param name="to">The date to calculate net worth.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the net worth value.</returns>
    private async Task<decimal> NetWorthAsync(DateOnly to, CancellationToken cancellationToken)
    {
        var assets = await dbContext.Accounts.AsNoTracking().Where(account => account.AccountType != AccountType.Income && account.AccountType != AccountType.Expense)
            .Select(account => new { account.InitialBalance, Movement = account.JournalEntryLines.Where(line => line.JournalEntry.Status == JournalEntryStatus.Active && line.JournalEntry.Date <= to).Sum(line => (decimal?)(line.Debit - line.Credit)) ?? 0m })
            .ToListAsync(cancellationToken);
        var certificates = await dbContext.SavingsCertificates.AsNoTracking().Where(item => item.InvestmentDate <= to).SumAsync(item => item.CurrentValue, cancellationToken);
        return assets.Sum(item => item.InitialBalance + item.Movement) + certificates;
    }
}
