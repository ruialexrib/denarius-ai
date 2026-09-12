using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads classified income, expense and asset-account flow data for financial analysis.
/// </summary>
/// <param name="dbContext">Application database context.</param>
public sealed class IncomeExpenseFlowRepository(DenariusDbContext dbContext) : IIncomeExpenseFlowRepository
{
    /// <inheritdoc />
    public async Task<IncomeExpenseFlowDataDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var days = to.DayNumber - from.DayNumber + 1;
        var comparisonTo = from.AddDays(-1);
        var comparisonFrom = comparisonTo.AddDays(-(days - 1));

        var currentIncome = await AmountAsync(from, to, FinancialGroupKind.Income, cancellationToken);
        var currentExpenses = await AmountAsync(from, to, FinancialGroupKind.Expense, cancellationToken);
        var previousIncome = await AmountAsync(comparisonFrom, comparisonTo, FinancialGroupKind.Income, cancellationToken);
        var previousExpenses = await AmountAsync(comparisonFrom, comparisonTo, FinancialGroupKind.Expense, cancellationToken);

        var incomeGroups = await BuildBreakdownAsync(from, to, comparisonFrom, comparisonTo, FinancialGroupKind.Income, groupByCategory: false, currentIncome, cancellationToken);
        var incomeCategories = await BuildBreakdownAsync(from, to, comparisonFrom, comparisonTo, FinancialGroupKind.Income, groupByCategory: true, currentIncome, cancellationToken);
        var expenseGroups = await BuildBreakdownAsync(from, to, comparisonFrom, comparisonTo, FinancialGroupKind.Expense, groupByCategory: false, currentExpenses, cancellationToken);
        var expenseCategories = await BuildBreakdownAsync(from, to, comparisonFrom, comparisonTo, FinancialGroupKind.Expense, groupByCategory: true, currentExpenses, cancellationToken);
        var accountFlows = await BuildAccountFlowsAsync(from, to, comparisonFrom, comparisonTo, cancellationToken);
        var largestMovements = await BuildLargestMovementsAsync(from, to, cancellationToken);
        var trend = await BuildTrendAsync(to, cancellationToken);

        return new(
            from,
            to,
            comparisonFrom,
            comparisonTo,
            currentIncome,
            currentExpenses,
            previousIncome,
            previousExpenses,
            incomeGroups,
            incomeCategories,
            expenseGroups,
            expenseCategories,
            accountFlows,
            trend,
            largestMovements);
    }

    /// <summary>
    /// Returns active journal lines inside a date interval.
    /// </summary>
    /// <param name="from">Interval start.</param>
    /// <param name="to">Interval end.</param>
    /// <returns>Filtered journal lines.</returns>
    private IQueryable<JournalEntryLine> ActiveLines(DateOnly from, DateOnly to) =>
        dbContext.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntry.Status == JournalEntryStatus.Active
                && line.JournalEntry.Date >= from
                && line.JournalEntry.Date <= to);

    /// <summary>
    /// Filters classified lines by financial kind.
    /// </summary>
    /// <param name="from">Interval start.</param>
    /// <param name="to">Interval end.</param>
    /// <param name="kind">Income or expense.</param>
    /// <returns>Classified journal lines.</returns>
    private IQueryable<JournalEntryLine> ClassifiedLines(DateOnly from, DateOnly to, FinancialGroupKind kind) =>
        ActiveLines(from, to).Where(line =>
            (line.Category != null
                ? line.Category.FinancialGroup.Kind
                : line.Account.Category!.FinancialGroup.Kind) == kind);

    /// <summary>
    /// Calculates a deterministic income or expense total.
    /// </summary>
    /// <param name="from">Interval start.</param>
    /// <param name="to">Interval end.</param>
    /// <param name="kind">Income or expense.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>The calculated amount.</returns>
    private Task<decimal> AmountAsync(
        DateOnly from,
        DateOnly to,
        FinancialGroupKind kind,
        CancellationToken cancellationToken)
    {
        var query = ClassifiedLines(from, to, kind);
        return kind == FinancialGroupKind.Income
            ? query.SumAsync(line => line.Credit - line.Debit, cancellationToken)
            : query.SumAsync(line => line.Debit - line.Credit, cancellationToken);
    }

    /// <summary>
    /// Builds a current/comparison classification breakdown.
    /// </summary>
    /// <param name="from">Selected period start.</param>
    /// <param name="to">Selected period end.</param>
    /// <param name="comparisonFrom">Comparison period start.</param>
    /// <param name="comparisonTo">Comparison period end.</param>
    /// <param name="kind">Income or expense.</param>
    /// <param name="groupByCategory">Whether to group by category instead of financial group.</param>
    /// <param name="currentTotal">Current total used for weight calculation.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Classification breakdown ordered by current amount.</returns>
    private async Task<IReadOnlyList<FlowBreakdownDto>> BuildBreakdownAsync(
        DateOnly from,
        DateOnly to,
        DateOnly comparisonFrom,
        DateOnly comparisonTo,
        FinancialGroupKind kind,
        bool groupByCategory,
        decimal currentTotal,
        CancellationToken cancellationToken)
    {
        var current = await BreakdownRowsAsync(from, to, kind, groupByCategory, cancellationToken);
        var previous = await BreakdownRowsAsync(comparisonFrom, comparisonTo, kind, groupByCategory, cancellationToken);
        var previousById = previous.ToDictionary(item => item.Id);

        return current
            .Select(item =>
            {
                var previousAmount = previousById.GetValueOrDefault(item.Id)?.Amount ?? 0m;
                decimal? change = previousAmount == 0m
                    ? null
                    : decimal.Round((item.Amount - previousAmount) / Math.Abs(previousAmount) * 100m, 1);
                var weight = currentTotal == 0m
                    ? 0m
                    : decimal.Round(item.Amount / currentTotal * 100m, 1);
                return new FlowBreakdownDto(item.Id, item.Name, item.Amount, previousAmount, weight, change);
            })
            .OrderByDescending(item => item.CurrentAmount)
            .ToList();
    }

    /// <summary>
    /// Reads classification rows for one period.
    /// </summary>
    /// <param name="from">Period start.</param>
    /// <param name="to">Period end.</param>
    /// <param name="kind">Income or expense.</param>
    /// <param name="groupByCategory">Whether to group by category.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Raw grouped amounts.</returns>
    private async Task<IReadOnlyList<BreakdownRow>> BreakdownRowsAsync(
        DateOnly from,
        DateOnly to,
        FinancialGroupKind kind,
        bool groupByCategory,
        CancellationToken cancellationToken)
    {
        var lines = ClassifiedLines(from, to, kind);
        if (groupByCategory)
        {
            var rows = await lines
                .GroupBy(line => new
                {
                    Id = line.CategoryId ?? line.Account.CategoryId!.Value,
                    Name = line.Category != null ? line.Category.Name : line.Account.Category!.Name
                })
                .Select(group => new BreakdownRow(
                    group.Key.Id,
                    group.Key.Name,
                    kind == FinancialGroupKind.Income
                        ? group.Sum(line => line.Credit - line.Debit)
                        : group.Sum(line => line.Debit - line.Credit)))
                .ToListAsync(cancellationToken);
            return rows;
        }

        return await lines
            .GroupBy(line => new
            {
                Id = line.Category != null ? line.Category.FinancialGroupId : line.Account.Category!.FinancialGroupId,
                Name = line.Category != null ? line.Category.FinancialGroup.Name : line.Account.Category!.FinancialGroup.Name
            })
            .Select(group => new BreakdownRow(
                group.Key.Id,
                group.Key.Name,
                kind == FinancialGroupKind.Income
                    ? group.Sum(line => line.Credit - line.Debit)
                    : group.Sum(line => line.Debit - line.Credit)))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Builds account inflow/outflow comparisons for asset accounts.
    /// </summary>
    /// <param name="from">Selected period start.</param>
    /// <param name="to">Selected period end.</param>
    /// <param name="comparisonFrom">Comparison period start.</param>
    /// <param name="comparisonTo">Comparison period end.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Account flows ordered by absolute current net flow.</returns>
    private async Task<IReadOnlyList<AccountFlowDto>> BuildAccountFlowsAsync(
        DateOnly from,
        DateOnly to,
        DateOnly comparisonFrom,
        DateOnly comparisonTo,
        CancellationToken cancellationToken)
    {
        var current = await AccountRowsAsync(from, to, cancellationToken);
        var previous = await AccountRowsAsync(comparisonFrom, comparisonTo, cancellationToken);
        var previousById = previous.ToDictionary(item => item.Id);

        return current
            .Select(item => new AccountFlowDto(
                item.Id,
                item.Name,
                item.Inflows,
                item.Outflows,
                item.Inflows - item.Outflows,
                previousById.TryGetValue(item.Id, out var comparison)
                    ? comparison.Inflows - comparison.Outflows
                    : 0m))
            .OrderByDescending(item => Math.Abs(item.NetFlow))
            .ToList();
    }

    /// <summary>
    /// Reads debit and credit flows for non-income/non-expense accounts.
    /// </summary>
    /// <param name="from">Period start.</param>
    /// <param name="to">Period end.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Raw account-flow rows.</returns>
    private Task<List<AccountFlowRow>> AccountRowsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        ActiveLines(from, to)
            .Where(line => line.Account.AccountType != AccountType.Income
                && line.Account.AccountType != AccountType.Expense)
            .GroupBy(line => new { line.AccountId, line.Account.Name })
            .Select(group => new AccountFlowRow(
                group.Key.AccountId,
                group.Key.Name,
                group.Sum(line => line.Debit),
                group.Sum(line => line.Credit)))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Gets the largest classified income and expense movements in the selected period.
    /// </summary>
    /// <param name="from">Period start.</param>
    /// <param name="to">Period end.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Largest movements ordered by amount.</returns>
    private async Task<IReadOnlyList<FlowMovementHighlightDto>> BuildLargestMovementsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var rows = await ActiveLines(from, to)
            .Where(line =>
                (line.Category != null
                    ? line.Category.FinancialGroup.Kind
                    : line.Account.Category!.FinancialGroup.Kind) == FinancialGroupKind.Income
                || (line.Category != null
                    ? line.Category.FinancialGroup.Kind
                    : line.Account.Category!.FinancialGroup.Kind) == FinancialGroupKind.Expense)
            .Select(line => new
            {
                EntryId = line.JournalEntryId,
                line.JournalEntry.Date,
                line.JournalEntry.Description,
                Category = line.Category != null ? line.Category.Name : line.Account.Category!.Name,
                Kind = line.Category != null ? line.Category.FinancialGroup.Kind : line.Account.Category!.FinancialGroup.Kind,
                IncomeAmount = line.Credit - line.Debit,
                ExpenseAmount = line.Debit - line.Credit
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(item => new FlowMovementHighlightDto(
                item.EntryId,
                item.Date,
                item.Description,
                item.Category,
                item.Kind == FinancialGroupKind.Income ? "Income" : "Expense",
                item.Kind == FinancialGroupKind.Income ? item.IncomeAmount : item.ExpenseAmount))
            .OrderByDescending(item => item.Amount)
            .Take(8)
            .ToList();
    }

    /// <summary>
    /// Builds a trailing twelve-month income, expense and balance series.
    /// </summary>
    /// <param name="to">End date of the analysis.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Monthly trend points.</returns>
    private async Task<IReadOnlyList<IncomeExpenseFlowTrendDto>> BuildTrendAsync(
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var firstMonth = new DateOnly(to.Year, to.Month, 1).AddMonths(-11);
        var result = new List<IncomeExpenseFlowTrendDto>();
        for (var month = firstMonth; month <= new DateOnly(to.Year, to.Month, 1); month = month.AddMonths(1))
        {
            var monthTo = month.AddMonths(1).AddDays(-1);
            if (monthTo > to)
                monthTo = to;
            var income = await AmountAsync(month, monthTo, FinancialGroupKind.Income, cancellationToken);
            var expenses = await AmountAsync(month, monthTo, FinancialGroupKind.Expense, cancellationToken);
            result.Add(new(month.Year, month.Month, income, expenses, income - expenses));
        }
        return result;
    }

    /// <summary>
    /// Represents a raw classification row.
    /// </summary>
    /// <param name="Id">Classification identifier.</param>
    /// <param name="Name">Classification name.</param>
    /// <param name="Amount">Calculated amount.</param>
    private sealed record BreakdownRow(Guid Id, string Name, decimal Amount);

    /// <summary>
    /// Represents raw account inflow and outflow data.
    /// </summary>
    /// <param name="Id">Account identifier.</param>
    /// <param name="Name">Account name.</param>
    /// <param name="Inflows">Debit total.</param>
    /// <param name="Outflows">Credit total.</param>
    private sealed record AccountFlowRow(Guid Id, string Name, decimal Inflows, decimal Outflows);
}
