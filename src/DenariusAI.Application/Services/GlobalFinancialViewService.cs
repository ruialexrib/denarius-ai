using System.Globalization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

/// <summary>
/// Builds an executive global financial view from authoritative, pre-calculated financial data.
/// </summary>
/// <param name="reportDataService">Service that supplies authoritative report data.</param>
/// <param name="analyticsService">Service used to obtain the monthly evolution series.</param>
public sealed class GlobalFinancialViewService(
    IFinancialReportDataService reportDataService,
    IAnalyticsService analyticsService) : IGlobalFinancialViewService
{
    private const decimal MaterialPercentageChange = 10m;
    private const decimal MaterialNetWorthPercentageChange = 5m;
    private const decimal MaterialSavingsRatePointChange = 5m;
    private const decimal MaterialAccountAbsoluteChange = 100m;
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-PT");

    /// <inheritdoc />
    public async Task<GlobalFinancialViewDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from == default || to == default || from > to)
            throw new ArgumentException("O intervalo de datas é inválido.");

        var days = to.DayNumber - from.DayNumber + 1;
        var comparisonTo = from.AddDays(-1);
        var comparisonFrom = comparisonTo.AddDays(-(days - 1));

        var current = await reportDataService.GetAsync(from, to, cancellationToken);
        var comparison = await reportDataService.GetAsync(comparisonFrom, comparisonTo, cancellationToken);

        var currentLiquid = LiquidBalance(current);
        var comparisonLiquid = LiquidBalance(comparison);

        var netWorth = Metric("net-worth", "Património líquido", current.NetWorth, comparison.NetWorth);
        var liquidBalance = Metric("liquid-balance", "Saldo disponível", currentLiquid, comparisonLiquid);
        var savings = Metric("savings", "Poupança", current.Savings, comparison.Savings);
        var savingsRate = Metric("savings-rate", "Taxa de poupança", current.SavingsRate, comparison.SavingsRate, true);
        var income = Metric("income", "Rendimentos", current.Income, comparison.Income);
        var expenses = Metric("expenses", "Despesas", current.Expenses, comparison.Expenses);

        var trendFrom = new DateOnly(to.Year, to.Month, 1).AddMonths(-11);
        var trendAnalytics = await analyticsService.GetAsync(new(trendFrom, to), cancellationToken);
        var trend = trendAnalytics.Trend.Select(item => new GlobalFinancialTrendDto(
            item.Year,
            item.Month,
            item.Income,
            item.Expenses,
            item.Savings,
            item.Income == 0m ? null : decimal.Round(item.Savings / item.Income * 100m, 1),
            item.NetWorth)).ToList();

        var findings = BuildFindings(
            current,
            comparison,
            netWorth,
            savings,
            savingsRate,
            income,
            expenses);

        return new(
            from,
            to,
            comparisonFrom,
            comparisonTo,
            current.Currency,
            netWorth,
            liquidBalance,
            savings,
            savingsRate,
            income,
            expenses,
            trend,
            findings,
            current.Reconciliation.Unreconciled,
            current.Income != 0m || current.Expenses != 0m,
            comparison.Income != 0m || comparison.Expenses != 0m);
    }

    /// <summary>
    /// Calculates liquid resources from bank-account and cash balances at the report end date.
    /// </summary>
    /// <param name="data">Authoritative report data.</param>
    /// <returns>Total liquid balance.</returns>
    private static decimal LiquidBalance(FinancialReportDataDto data)
        => data.Accounts
            .Where(item => item.Type is nameof(AccountType.BankAccount) or nameof(AccountType.Cash))
            .Sum(item => item.BalanceAtEnd);

    /// <summary>
    /// Creates a deterministic comparison metric.
    /// </summary>
    /// <param name="key">Stable metric identifier.</param>
    /// <param name="label">User-facing metric label.</param>
    /// <param name="current">Current-period value.</param>
    /// <param name="comparison">Comparison-period value.</param>
    /// <param name="isPercentage">Whether the metric itself is a percentage.</param>
    /// <returns>The metric with calculated changes.</returns>
    private static GlobalFinancialMetricDto Metric(
        string key,
        string label,
        decimal current,
        decimal comparison,
        bool isPercentage = false)
    {
        var absolute = current - comparison;
        var percentage = comparison == 0m
            ? null
            : decimal.Round(absolute / Math.Abs(comparison) * 100m, 1);
        var direction = absolute > 0m
            ? FinancialMetricDirection.Increase
            : absolute < 0m
                ? FinancialMetricDirection.Decrease
                : FinancialMetricDirection.Unchanged;
        return new(key, label, current, comparison, absolute, percentage, direction, isPercentage);
    }

    /// <summary>
    /// Builds the prioritised deterministic findings shown before any AI interpretation.
    /// </summary>
    /// <param name="current">Selected-period report data.</param>
    /// <param name="comparison">Comparison-period report data.</param>
    /// <param name="netWorth">Net-worth metric.</param>
    /// <param name="savings">Savings metric.</param>
    /// <param name="savingsRate">Savings-rate metric.</param>
    /// <param name="income">Income metric.</param>
    /// <param name="expenses">Expense metric.</param>
    /// <returns>Prioritised executive findings.</returns>
    private static IReadOnlyList<GlobalFinancialFindingDto> BuildFindings(
        FinancialReportDataDto current,
        FinancialReportDataDto comparison,
        GlobalFinancialMetricDto netWorth,
        GlobalFinancialMetricDto savings,
        GlobalFinancialMetricDto savingsRate,
        GlobalFinancialMetricDto income,
        GlobalFinancialMetricDto expenses)
    {
        var findings = new List<GlobalFinancialFindingDto>();

        AddPercentageFinding(
            findings,
            netWorth,
            MaterialNetWorthPercentageChange,
            "Património em crescimento",
            "Património em redução",
            "Investimentos e Património Financeiro",
            positiveWhenIncrease: true);

        if (current.Savings < 0m)
        {
            findings.Add(new(
                "negative",
                "Poupança negativa no período",
                $"As despesas excederam os rendimentos em {Money(Math.Abs(current.Savings))}.",
                "Poupança e Liquidez"));
        }
        else if (current.Income != 0m && comparison.Income != 0m
            && savingsRate.AbsoluteChange >= MaterialSavingsRatePointChange)
        {
            findings.Add(new(
                "positive",
                "Taxa de poupança melhorou",
                $"A taxa de poupança aumentou {savingsRate.AbsoluteChange.ToString("N1", PortugueseCulture)} pontos percentuais face ao período anterior.",
                "Poupança e Liquidez"));
        }
        else if (current.Income != 0m && comparison.Income != 0m
            && savingsRate.AbsoluteChange <= -MaterialSavingsRatePointChange)
        {
            findings.Add(new(
                "negative",
                "Taxa de poupança deteriorou-se",
                $"A taxa de poupança diminuiu {Math.Abs(savingsRate.AbsoluteChange).ToString("N1", PortugueseCulture)} pontos percentuais face ao período anterior.",
                "Poupança e Liquidez"));
        }

        AddPercentageFinding(
            findings,
            expenses,
            MaterialPercentageChange,
            "Despesas aumentaram materialmente",
            "Despesas diminuíram materialmente",
            "Rendimentos, Despesas e Fluxos",
            positiveWhenIncrease: false);

        AddPercentageFinding(
            findings,
            income,
            MaterialPercentageChange,
            "Rendimentos aumentaram materialmente",
            "Rendimentos diminuíram materialmente",
            "Rendimentos, Despesas e Fluxos",
            positiveWhenIncrease: true);

        var accountFinding = BuildAccountFinding(current, comparison, netWorth.AbsoluteChange);
        if (accountFinding is not null)
            findings.Add(accountFinding);

        if (current.Reconciliation.Unreconciled > 0)
        {
            findings.Add(new(
                "warning",
                "Existem movimentos por reconciliar",
                $"{current.Reconciliation.Unreconciled} movimento(s) do período ainda não estão reconciliados e devem ser considerados na leitura dos dados.",
                "Rendimentos, Despesas e Fluxos"));
        }

        if (findings.Count == 0)
        {
            findings.Add(new(
                "info",
                "Sem alterações materiais detetadas",
                "Os indicadores comparáveis não ultrapassaram os limiares de materialidade desta visão executiva.",
                "Visão Financeira Global"));
        }

        return findings.Take(5).ToList();
    }

    /// <summary>
    /// Adds a finding when a percentage variation reaches the configured deterministic materiality threshold.
    /// </summary>
    /// <param name="findings">Destination findings collection.</param>
    /// <param name="metric">Metric being evaluated.</param>
    /// <param name="threshold">Absolute percentage-change threshold.</param>
    /// <param name="increaseTitle">Title used when the metric increases.</param>
    /// <param name="decreaseTitle">Title used when the metric decreases.</param>
    /// <param name="targetArea">Related specialised analysis area.</param>
    /// <param name="positiveWhenIncrease">Whether an increase is semantically positive.</param>
    private static void AddPercentageFinding(
        ICollection<GlobalFinancialFindingDto> findings,
        GlobalFinancialMetricDto metric,
        decimal threshold,
        string increaseTitle,
        string decreaseTitle,
        string targetArea,
        bool positiveWhenIncrease)
    {
        if (!metric.PercentageChange.HasValue || Math.Abs(metric.PercentageChange.Value) < threshold)
            return;

        var increased = metric.Direction == FinancialMetricDirection.Increase;
        var title = increased ? increaseTitle : decreaseTitle;
        var tone = increased == positiveWhenIncrease ? "positive" : "negative";
        findings.Add(new(
            tone,
            title,
            $"{metric.Label}: {Money(metric.CurrentValue)} ({SignedPercentage(metric.PercentageChange.Value)} face ao período anterior).",
            targetArea));
    }

    /// <summary>
    /// Identifies an account whose balance change explains a substantial share of the net-worth variation.
    /// </summary>
    /// <param name="current">Current report data.</param>
    /// <param name="comparison">Comparison report data.</param>
    /// <param name="netWorthChange">Net-worth absolute change.</param>
    /// <returns>An account finding when the deterministic relevance conditions are met; otherwise null.</returns>
    private static GlobalFinancialFindingDto? BuildAccountFinding(
        FinancialReportDataDto current,
        FinancialReportDataDto comparison,
        decimal netWorthChange)
    {
        if (netWorthChange == 0m)
            return null;

        var previousBalances = comparison.Accounts.ToDictionary(item => item.Id, item => item.BalanceAtEnd);
        var candidate = current.Accounts
            .Select(item => new
            {
                item.Name,
                Change = item.BalanceAtEnd - previousBalances.GetValueOrDefault(item.Id, item.InitialBalance)
            })
            .OrderByDescending(item => Math.Abs(item.Change))
            .FirstOrDefault();

        if (candidate is null
            || Math.Abs(candidate.Change) < MaterialAccountAbsoluteChange
            || Math.Abs(candidate.Change) < Math.Abs(netWorthChange) * .5m)
            return null;

        return new(
            candidate.Change >= 0m ? "positive" : "negative",
            "Uma conta explica grande parte da alteração patrimonial",
            $"{candidate.Name} variou {SignedMoney(candidate.Change)} entre as datas de comparação.",
            "Investimentos e Património Financeiro");
    }

    /// <summary>
    /// Formats a monetary value for user-facing deterministic findings.
    /// </summary>
    /// <param name="value">Monetary value.</param>
    /// <returns>Portuguese-formatted euro amount.</returns>
    private static string Money(decimal value) => $"{value.ToString("N2", PortugueseCulture)} €";

    /// <summary>
    /// Formats a signed monetary variation.
    /// </summary>
    /// <param name="value">Monetary variation.</param>
    /// <returns>Portuguese-formatted signed euro amount.</returns>
    private static string SignedMoney(decimal value)
        => $"{(value > 0m ? "+" : string.Empty)}{value.ToString("N2", PortugueseCulture)} €";

    /// <summary>
    /// Formats a signed percentage variation.
    /// </summary>
    /// <param name="value">Percentage variation.</param>
    /// <returns>Portuguese-formatted signed percentage.</returns>
    private static string SignedPercentage(decimal value)
        => $"{(value > 0m ? "+" : string.Empty)}{value.ToString("N1", PortugueseCulture)}%";
}
