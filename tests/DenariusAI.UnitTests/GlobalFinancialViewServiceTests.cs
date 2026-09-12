using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>
/// Verifies deterministic comparison and materiality rules for the global financial view.
/// </summary>
public sealed class GlobalFinancialViewServiceTests
{
    /// <summary>
    /// Verifies that the immediately preceding equivalent interval is used and that authoritative metrics are compared.
    /// </summary>
    [Fact]
    public async Task GetAsyncUsesEquivalentPreviousPeriodAndCalculatesMetrics()
    {
        var current = Report(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            income: 2000m,
            expenses: 1200m,
            netWorth: 1500m,
            bankBalance: 1000m,
            unreconciled: 2);
        var previous = Report(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            income: 1500m,
            expenses: 1000m,
            netWorth: 1300m,
            bankBalance: 800m);
        var data = new StubServices(current, previous);
        var service = new GlobalFinancialViewService(data, data);

        var result = await service.GetAsync(new(2026, 8, 1), new(2026, 8, 31));

        Assert.Equal(new DateOnly(2026, 7, 1), result.ComparisonFrom);
        Assert.Equal(new DateOnly(2026, 7, 31), result.ComparisonTo);
        Assert.Equal(1500m, result.NetWorth.CurrentValue);
        Assert.Equal(200m, result.NetWorth.AbsoluteChange);
        Assert.Equal(15.4m, result.NetWorth.PercentageChange);
        Assert.Equal(1000m, result.LiquidBalance.CurrentValue);
        Assert.Equal(200m, result.LiquidBalance.AbsoluteChange);
        Assert.Equal(800m, result.Savings.CurrentValue);
        Assert.Equal(40m, result.SavingsRate.CurrentValue);
        Assert.Equal(2, result.UnreconciledMovements);
        Assert.Contains(result.Findings, item => item.Title == "Património em crescimento");
        Assert.Contains(result.Findings, item => item.Title == "Rendimentos aumentaram materialmente");
        Assert.Contains(result.Findings, item => item.Title == "Existem movimentos por reconciliar");
        Assert.Collection(data.ReportCalls,
            call => Assert.Equal((new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)), call),
            call => Assert.Equal((new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), call));
    }

    /// <summary>
    /// Verifies safe handling of zero comparison values and negative savings.
    /// </summary>
    [Fact]
    public async Task GetAsyncAvoidsUndefinedPercentageAndHighlightsNegativeSavings()
    {
        var current = Report(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            income: 0m,
            expenses: 100m,
            netWorth: 0m,
            bankBalance: 0m);
        var previous = Report(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            income: 0m,
            expenses: 0m,
            netWorth: 0m,
            bankBalance: 0m);
        var data = new StubServices(current, previous);
        var service = new GlobalFinancialViewService(data, data);

        var result = await service.GetAsync(new(2026, 8, 1), new(2026, 8, 31));

        Assert.Null(result.Expenses.PercentageChange);
        Assert.Equal(-100m, result.Savings.CurrentValue);
        Assert.Equal(0m, result.SavingsRate.CurrentValue);
        Assert.Contains(result.Findings, item => item.Title == "Poupança negativa no período");
    }

    /// <summary>
    /// Creates authoritative report data for one test period.
    /// </summary>
    /// <param name="from">Period start.</param>
    /// <param name="to">Period end.</param>
    /// <param name="income">Income total.</param>
    /// <param name="expenses">Expense total.</param>
    /// <param name="netWorth">Net worth at period end.</param>
    /// <param name="bankBalance">Bank balance at period end.</param>
    /// <param name="unreconciled">Unreconciled movement count.</param>
    /// <returns>Financial report data.</returns>
    private static FinancialReportDataDto Report(
        DateOnly from,
        DateOnly to,
        decimal income,
        decimal expenses,
        decimal netWorth,
        decimal bankBalance,
        int unreconciled = 0)
    {
        var savings = income - expenses;
        var rate = income == 0m ? 0m : decimal.Round(savings / income * 100m, 1);
        return new(
            from,
            to,
            "EUR",
            income,
            expenses,
            savings,
            rate,
            netWorth,
            [new(Guid.NewGuid(), "Conta", "BankAccount", 0m, bankBalance, "EUR", true)],
            [],
            [],
            [],
            [],
            new(unreconciled, 0, unreconciled, []));
    }

    /// <summary>
    /// Supplies fixed report data and records period requests for service tests.
    /// </summary>
    private sealed class StubServices(
        FinancialReportDataDto current,
        FinancialReportDataDto previous) : IFinancialReportDataService, IAnalyticsService
    {
        /// <summary>Gets the report periods requested by the service.</summary>
        public List<(DateOnly From, DateOnly To)> ReportCalls { get; } = [];

        /// <inheritdoc />
        public Task<FinancialReportDataDto> GetAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            ReportCalls.Add((from, to));
            return Task.FromResult(ReportCalls.Count == 1 ? current : previous);
        }

        /// <inheritdoc />
        public Task<AnalyticsDto> GetAsync(
            AnalyticsFilterDto filter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDto(
                current.Income,
                current.Expenses,
                previous.Income,
                previous.Expenses,
                current.NetWorth,
                [],
                [],
                [],
                [new(filter.To.Year, filter.To.Month, current.Income, current.Expenses, current.NetWorth)]));
    }
}
