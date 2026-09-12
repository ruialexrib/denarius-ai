namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents one classified amount in the income and expense flow analysis.
/// </summary>
/// <param name="Id">Classification identifier.</param>
/// <param name="Name">Classification name.</param>
/// <param name="CurrentAmount">Amount in the selected period.</param>
/// <param name="PreviousAmount">Amount in the preceding equivalent period.</param>
/// <param name="Weight">Share of the relevant current-period total.</param>
/// <param name="ChangePercentage">Percentage change versus the previous period, or null when the previous value is zero.</param>
public sealed record FlowBreakdownDto(
    Guid Id,
    string Name,
    decimal CurrentAmount,
    decimal PreviousAmount,
    decimal Weight,
    decimal? ChangePercentage);

/// <summary>
/// Represents money entering and leaving a non-income/non-expense account.
/// </summary>
/// <param name="Id">Account identifier.</param>
/// <param name="Name">Account name.</param>
/// <param name="Inflows">Debits recorded in the selected period.</param>
/// <param name="Outflows">Credits recorded in the selected period.</param>
/// <param name="NetFlow">Inflows minus outflows in the selected period.</param>
/// <param name="PreviousNetFlow">Net flow in the preceding equivalent period.</param>
public sealed record AccountFlowDto(
    Guid Id,
    string Name,
    decimal Inflows,
    decimal Outflows,
    decimal NetFlow,
    decimal PreviousNetFlow);

/// <summary>
/// Represents one monthly income/expense flow point.
/// </summary>
/// <param name="Year">Calendar year.</param>
/// <param name="Month">Calendar month.</param>
/// <param name="Income">Income recorded in the month.</param>
/// <param name="Expenses">Expenses recorded in the month.</param>
/// <param name="Balance">Income minus expenses.</param>
public sealed record IncomeExpenseFlowTrendDto(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Balance);

/// <summary>
/// Represents one material movement included in the executive flow analysis.
/// </summary>
/// <param name="EntryId">Journal entry identifier.</param>
/// <param name="Date">Movement date.</param>
/// <param name="Description">Journal entry description.</param>
/// <param name="Category">Classification category when available.</param>
/// <param name="Kind">Income or Expense.</param>
/// <param name="Amount">Movement amount.</param>
public sealed record FlowMovementHighlightDto(
    Guid EntryId,
    DateOnly Date,
    string Description,
    string? Category,
    string Kind,
    decimal Amount);

/// <summary>
/// Represents one deterministic finding about income, expenses or account flows.
/// </summary>
/// <param name="Tone">Semantic display tone.</param>
/// <param name="Title">Short finding title.</param>
/// <param name="Detail">Calculated explanation.</param>
public sealed record IncomeExpenseFlowFindingDto(string Tone, string Title, string Detail);

/// <summary>
/// Contains raw deterministic data produced by the flow repository before executive interpretation.
/// </summary>
/// <param name="From">Selected period start.</param>
/// <param name="To">Selected period end.</param>
/// <param name="ComparisonFrom">Comparison period start.</param>
/// <param name="ComparisonTo">Comparison period end.</param>
/// <param name="Income">Selected-period income.</param>
/// <param name="Expenses">Selected-period expenses.</param>
/// <param name="PreviousIncome">Comparison-period income.</param>
/// <param name="PreviousExpenses">Comparison-period expenses.</param>
/// <param name="IncomeGroups">Income grouped by financial group for both periods.</param>
/// <param name="IncomeCategories">Income grouped by category for both periods.</param>
/// <param name="ExpenseGroups">Expenses grouped by financial group for both periods.</param>
/// <param name="ExpenseCategories">Expenses grouped by category for both periods.</param>
/// <param name="AccountFlows">Selected and comparison account net flows.</param>
/// <param name="Trend">Recent monthly trend.</param>
/// <param name="LargestMovements">Largest classified movements in the selected period.</param>
public sealed record IncomeExpenseFlowDataDto(
    DateOnly From,
    DateOnly To,
    DateOnly ComparisonFrom,
    DateOnly ComparisonTo,
    decimal Income,
    decimal Expenses,
    decimal PreviousIncome,
    decimal PreviousExpenses,
    IReadOnlyList<FlowBreakdownDto> IncomeGroups,
    IReadOnlyList<FlowBreakdownDto> IncomeCategories,
    IReadOnlyList<FlowBreakdownDto> ExpenseGroups,
    IReadOnlyList<FlowBreakdownDto> ExpenseCategories,
    IReadOnlyList<AccountFlowDto> AccountFlows,
    IReadOnlyList<IncomeExpenseFlowTrendDto> Trend,
    IReadOnlyList<FlowMovementHighlightDto> LargestMovements);

/// <summary>
/// Contains the complete deterministic analysis shown on the Income, Expenses and Flows page.
/// </summary>
/// <param name="Data">Authoritative detailed flow data.</param>
/// <param name="IncomeMetric">Income comparison metric.</param>
/// <param name="ExpenseMetric">Expense comparison metric.</param>
/// <param name="BalanceMetric">Period balance comparison metric.</param>
/// <param name="Findings">Prioritised deterministic findings.</param>
public sealed record IncomeExpenseFlowAnalysisDto(
    IncomeExpenseFlowDataDto Data,
    GlobalFinancialMetricDto IncomeMetric,
    GlobalFinancialMetricDto ExpenseMetric,
    GlobalFinancialMetricDto BalanceMetric,
    IReadOnlyList<IncomeExpenseFlowFindingDto> Findings);
