using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Enums;

namespace DenariusAI.UnitTests;

/// <summary>Verifies deterministic savings and liquidity calculations.</summary>
public sealed class SavingsLiquidityAnalysisServiceTests
{
    /// <summary>Verifies savings comparison, EUR liquidity and currency separation.</summary>
    [Fact]
    public async Task GetAsyncSeparatesLiquidityAndCurrencies()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);
        var previousFrom = new DateOnly(2026, 8, 2);
        var previousTo = new DateOnly(2026, 8, 31);
        var current = Report(
            from,
            to,
            2000m,
            1500m,
            [
                new(Guid.NewGuid(), "Conta à Ordem", "BankAccount", 0m, 900m, "EUR", true),
                new(Guid.NewGuid(), "Poupança", "Savings", 0m, 600m, "EUR", true),
                new(Guid.NewGuid(), "Conta USD", "BankAccount", 0m, 300m, "USD", true)
            ],
            [new(Guid.NewGuid(), from, "F-1", "CAF", 1000m, 2.5m, 1020m, to.AddDays(15))],
            [new(2026, 9, 2000m, 1500m, 500m, 0m, 0m, 0m, [])]);
        var previous = Report(previousFrom, previousTo, 1800m, 1500m, [], [], []);
        var service = new SavingsLiquidityAnalysisService(new StubFinancialReportDataService(
            new Dictionary<(DateOnly From, DateOnly To), FinancialReportDataDto>
            {
                [(from, to)] = current,
                [(previousFrom, previousTo)] = previous
            }));

        var result = await service.GetAsync(from, to);

        Assert.Equal(500m, result.Savings);
        Assert.Equal(25m, result.SavingsRate);
        Assert.Equal(300m, result.PreviousSavings);
        Assert.Equal(1500m, result.EurImmediateLiquidity);
        Assert.Equal(600m, result.EurSavingsAccounts);
        Assert.Equal(1020m, result.SavingsCertificatesValue);
        Assert.Equal(1, result.NonEurLiquidAccountCount);
        Assert.Contains(result.Findings, item => item.Title == "Existem saldos noutras moedas");
    }

    /// <summary>Creates a report DTO for deterministic service tests.</summary>
    /// <param name="from">Period start.</param>
    /// <param name="to">Period end.</param>
    /// <param name="income">Income.</param>
    /// <param name="expenses">Expenses.</param>
    /// <param name="accounts">Account facts.</param>
    /// <param name="certificates">Savings Certificate facts.</param>
    /// <param name="months">Monthly facts.</param>
    /// <returns>Financial report data.</returns>
    private static FinancialReportDataDto Report(
        DateOnly from,
        DateOnly to,
        decimal income,
        decimal expenses,
        IReadOnlyList<FinancialReportAccountDto> accounts,
        IReadOnlyList<SavingsCertificateSummaryDto> certificates,
        IReadOnlyList<FinancialReportMonthDto> months) =>
        new(
            from,
            to,
            "EUR",
            income,
            expenses,
            income - expenses,
            income == 0m ? 0m : decimal.Round((income - expenses) / income * 100m, 1),
            0m,
            accounts,
            [],
            [],
            months,
            certificates,
            new(0, 0, 0, []));

    /// <summary>Supplies fixed financial-report data keyed by requested period.</summary>
    private sealed class StubFinancialReportDataService(
        IReadOnlyDictionary<(DateOnly From, DateOnly To), FinancialReportDataDto> reports)
        : IFinancialReportDataService
    {
        /// <inheritdoc />
        public Task<FinancialReportDataDto> GetAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(reports[(from, to)]);
    }
}

/// <summary>Verifies deterministic investment and financial-assets calculations.</summary>
public sealed class InvestmentPortfolioAnalysisServiceTests
{
    /// <summary>Verifies currency separation, stock gains and Savings Certificate consolidation.</summary>
    [Fact]
    public async Task GetAsyncDoesNotConvertDifferentCurrencies()
    {
        var asOf = new DateOnly(2026, 9, 12);
        var repository = new StubSpecializedRepository(
            [
                new(Guid.NewGuid(), "AAA", "EUR Position", "XETRA", "EUR", 10m, 10m, 12m, asOf, false, asOf.AddMonths(-6), 8m),
                new(Guid.NewGuid(), "BBB", "USD Position", "NYSE", "USD", 2m, 100m, 120m, asOf, false, asOf.AddMonths(-6), 90m)
            ],
            []);
        var certificate = new SavingsCertificateSummaryDto(
            Guid.NewGuid(), asOf.AddYears(-1), "F-1", "CAF", 1000m, 2m, 1050m, asOf.AddMonths(1));
        var analytics = new AnalyticsDto(
            0m, 0m, 0m, 0m, 0m, 1050m, 50m, [certificate], [], [], [], []);
        var service = new InvestmentPortfolioAnalysisService(repository, new StubAnalyticsService(analytics));

        var result = await service.GetAsync(asOf);

        Assert.Equal(120m, result.EurStockMarketValue);
        Assert.Equal(20m, result.EurStockGain);
        Assert.Equal(1170m, result.EurTrackedFinancialAssets);
        Assert.Equal(1, result.NonEurStockPositions);
        Assert.Contains(result.CurrencySummaries, item => item.Currency == "USD" && item.MarketValue == 240m);
        Assert.Contains(result.Findings, item => item.Title == "Exposição noutras moedas");
    }

    /// <summary>Supplies fixed analytics data.</summary>
    private sealed class StubAnalyticsService(AnalyticsDto data) : IAnalyticsService
    {
        /// <inheritdoc />
        public Task<AnalyticsDto> GetAsync(
            AnalyticsFilterDto filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(data);
    }

    /// <summary>Supplies fixed specialised-analysis persistence facts.</summary>
    private sealed class StubSpecializedRepository(
        IReadOnlyList<InvestmentStockFactDto> stocks,
        IReadOnlyList<InsurancePolicyFactDto> policies) : ISpecializedFinancialAnalysisRepository
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<InvestmentStockFactDto>> GetStocksAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(stocks);

        /// <inheritdoc />
        public Task<IReadOnlyList<InsurancePolicyFactDto>> GetInsurancePoliciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(policies);
    }
}

/// <summary>Verifies deterministic insurance-commitment calculations.</summary>
public sealed class FinancialCommitmentsAnalysisServiceTests
{
    /// <summary>Verifies known future premiums, overdue amounts and renewal detection without inferred premiums.</summary>
    [Fact]
    public async Task GetAsyncUsesOnlyExplicitlyRegisteredPremiums()
    {
        var asOf = new DateOnly(2026, 9, 12);
        var policyId = Guid.NewGuid();
        var policies = new[]
        {
            new InsurancePolicyFactDto(
                policyId,
                "Seguro Casa",
                "Seguradora",
                InsurancePolicyType.Home,
                InsurancePaymentFrequency.Monthly,
                asOf.AddYears(-1),
                null,
                asOf.AddDays(20),
                InsurancePolicyStatus.Active,
                [
                    new(Guid.NewGuid(), policyId, 50m, asOf.AddDays(-2), asOf.AddMonths(-1), asOf, false),
                    new(Guid.NewGuid(), policyId, 100m, asOf.AddDays(18), asOf, asOf.AddMonths(1), false)
                ])
        };
        var service = new FinancialCommitmentsAnalysisService(new StubSpecializedRepository([], policies));

        var result = await service.GetAsync(asOf);

        Assert.Equal(1, result.ActivePolicies);
        Assert.Equal(100m, result.ScheduledPremiums);
        Assert.Equal(50m, result.OutstandingAmount);
        Assert.Equal(1, result.OutstandingPremiums);
        Assert.Equal(1, result.RenewalsWithin90Days);
        Assert.Equal(1, result.Calendar.Count);
        Assert.Contains(result.Findings, item => item.Title == "Prémios vencidos por regularizar");
        Assert.DoesNotContain(result.Calendar, item => item.PremiumCount > 1);
    }

    /// <summary>Supplies fixed specialised-analysis persistence facts.</summary>
    private sealed class StubSpecializedRepository(
        IReadOnlyList<InvestmentStockFactDto> stocks,
        IReadOnlyList<InsurancePolicyFactDto> policies) : ISpecializedFinancialAnalysisRepository
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<InvestmentStockFactDto>> GetStocksAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(stocks);

        /// <inheritdoc />
        public Task<IReadOnlyList<InsurancePolicyFactDto>> GetInsurancePoliciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(policies);
    }
}
