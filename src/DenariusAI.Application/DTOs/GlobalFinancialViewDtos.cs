namespace DenariusAI.Application.DTOs;

/// <summary>
/// Describes the direction of a deterministic financial metric change.
/// </summary>
public enum FinancialMetricDirection
{
    /// <summary>The metric is lower than its comparison value.</summary>
    Decrease,
    /// <summary>The metric is unchanged.</summary>
    Unchanged,
    /// <summary>The metric is higher than its comparison value.</summary>
    Increase
}

/// <summary>
/// Represents a current financial metric and its comparison-period change.
/// </summary>
/// <param name="Key">Stable metric identifier.</param>
/// <param name="Label">User-facing metric label.</param>
/// <param name="CurrentValue">Metric value for the selected period or end date.</param>
/// <param name="ComparisonValue">Metric value for the comparison period or end date.</param>
/// <param name="AbsoluteChange">Current value minus comparison value.</param>
/// <param name="PercentageChange">Percentage variation, or null when the comparison value is zero.</param>
/// <param name="Direction">Direction of the change.</param>
/// <param name="IsPercentage">Whether the metric value itself represents a percentage.</param>
public sealed record GlobalFinancialMetricDto(
    string Key,
    string Label,
    decimal CurrentValue,
    decimal ComparisonValue,
    decimal AbsoluteChange,
    decimal? PercentageChange,
    FinancialMetricDirection Direction,
    bool IsPercentage = false);

/// <summary>
/// Represents one deterministic executive finding in the global financial view.
/// </summary>
/// <param name="Tone">Semantic tone used by the interface.</param>
/// <param name="Title">Short user-facing finding title.</param>
/// <param name="Detail">Deterministic detail explaining the finding.</param>
/// <param name="TargetArea">Specialised analysis area related to the finding.</param>
public sealed record GlobalFinancialFindingDto(string Tone, string Title, string Detail, string TargetArea);

/// <summary>
/// Represents one monthly point in the global financial evolution series.
/// </summary>
/// <param name="Year">Calendar year.</param>
/// <param name="Month">Calendar month.</param>
/// <param name="Income">Income recorded during the month.</param>
/// <param name="Expenses">Expenses recorded during the month.</param>
/// <param name="Savings">Income minus expenses for the month.</param>
/// <param name="SavingsRate">Savings as a percentage of income, or null when income is zero.</param>
/// <param name="NetWorth">Net worth at the end of the month.</param>
public sealed record GlobalFinancialTrendDto(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal? SavingsRate,
    decimal NetWorth);

/// <summary>
/// Contains the authoritative data required by the global financial view.
/// </summary>
/// <param name="From">Selected period start.</param>
/// <param name="To">Selected period end.</param>
/// <param name="ComparisonFrom">Comparison period start.</param>
/// <param name="ComparisonTo">Comparison period end.</param>
/// <param name="Currency">Currency used by the analysis.</param>
/// <param name="NetWorth">Net worth metric.</param>
/// <param name="LiquidBalance">Liquid bank and cash balance metric.</param>
/// <param name="Savings">Savings metric.</param>
/// <param name="SavingsRate">Savings-rate metric.</param>
/// <param name="Income">Income metric.</param>
/// <param name="Expenses">Expenses metric.</param>
/// <param name="Trend">Monthly evolution ending in the selected period.</param>
/// <param name="Findings">Deterministic executive findings.</param>
/// <param name="UnreconciledMovements">Unreconciled movements in the selected period.</param>
/// <param name="HasActivity">Whether income or expenses exist in the selected period.</param>
/// <param name="HasComparisonActivity">Whether income or expenses exist in the comparison period.</param>
public sealed record GlobalFinancialViewDto(
    DateOnly From,
    DateOnly To,
    DateOnly ComparisonFrom,
    DateOnly ComparisonTo,
    string Currency,
    GlobalFinancialMetricDto NetWorth,
    GlobalFinancialMetricDto LiquidBalance,
    GlobalFinancialMetricDto Savings,
    GlobalFinancialMetricDto SavingsRate,
    GlobalFinancialMetricDto Income,
    GlobalFinancialMetricDto Expenses,
    IReadOnlyList<GlobalFinancialTrendDto> Trend,
    IReadOnlyList<GlobalFinancialFindingDto> Findings,
    int UnreconciledMovements,
    bool HasActivity,
    bool HasComparisonActivity);
