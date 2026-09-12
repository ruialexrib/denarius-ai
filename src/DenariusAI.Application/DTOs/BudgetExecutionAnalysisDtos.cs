namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents one deterministic budget execution status.
/// </summary>
public enum BudgetExecutionStatus
{
    /// <summary>The category remains comfortably within its budget.</summary>
    WithinBudget,
    /// <summary>The category is approaching its budget limit.</summary>
    NearLimit,
    /// <summary>The category has exceeded its budget.</summary>
    OverBudget,
    /// <summary>Actual spending exists without a planned category amount.</summary>
    Unbudgeted,
    /// <summary>A planned category has not yet recorded execution.</summary>
    NoExecution
}

/// <summary>
/// Represents one analysed budget category.
/// </summary>
/// <param name="CategoryId">Category identifier.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="FinancialGroupId">Financial group identifier.</param>
/// <param name="FinancialGroupName">Financial group name.</param>
/// <param name="Budgeted">Planned amount.</param>
/// <param name="Actual">Executed amount.</param>
/// <param name="Variance">Actual minus planned amount.</param>
/// <param name="RelativeVariancePercentage">Variance relative to the planned amount, or null when no amount was planned.</param>
/// <param name="ExecutionPercentage">Execution percentage, or null when no amount was planned.</param>
/// <param name="Status">Deterministic execution status.</param>
/// <param name="StatusLabel">Portuguese user-facing status label.</param>
public sealed record BudgetExecutionCategoryAnalysisDto(
    Guid CategoryId,
    string CategoryName,
    Guid FinancialGroupId,
    string FinancialGroupName,
    decimal Budgeted,
    decimal Actual,
    decimal Variance,
    decimal? RelativeVariancePercentage,
    decimal? ExecutionPercentage,
    BudgetExecutionStatus Status,
    string StatusLabel);

/// <summary>
/// Represents one historical monthly budget execution point.
/// </summary>
/// <param name="Year">Calendar year.</param>
/// <param name="Month">Calendar month.</param>
/// <param name="Budgeted">Total planned expense.</param>
/// <param name="Actual">Total executed expense.</param>
/// <param name="Variance">Actual minus planned expense.</param>
/// <param name="ExecutionPercentage">Execution percentage when the month has a non-zero budget.</param>
public sealed record BudgetExecutionTrendDto(
    int Year,
    int Month,
    decimal Budgeted,
    decimal Actual,
    decimal Variance,
    decimal? ExecutionPercentage);

/// <summary>
/// Represents one prioritised deterministic budget finding.
/// </summary>
/// <param name="Tone">Semantic display tone.</param>
/// <param name="Title">Short finding title.</param>
/// <param name="Detail">Calculated explanation.</param>
/// <param name="CategoryId">Related category identifier when applicable.</param>
public sealed record BudgetExecutionFindingDto(
    string Tone,
    string Title,
    string Detail,
    Guid? CategoryId = null);

/// <summary>
/// Contains the complete deterministic monthly budget execution analysis.
/// </summary>
/// <param name="Year">Selected budget year.</param>
/// <param name="Month">Selected budget month.</param>
/// <param name="HasBudget">Whether a persisted budget exists for the selected period.</param>
/// <param name="TotalBudgeted">Total planned expense.</param>
/// <param name="TotalActual">Total executed expense.</param>
/// <param name="TotalVariance">Actual minus planned expense.</param>
/// <param name="ExecutionPercentage">Overall execution percentage, or null when the total budget is zero.</param>
/// <param name="OverBudgetAmount">Sum of category overspends and unbudgeted spending.</param>
/// <param name="OverBudgetCategoryCount">Number of categories over budget.</param>
/// <param name="NearLimitCategoryCount">Number of categories close to the budget limit.</param>
/// <param name="UnbudgetedCategoryCount">Number of categories with execution and no planned amount.</param>
/// <param name="Categories">Category-level execution analysis ordered by relevance.</param>
/// <param name="Trend">Recent monthly budget history.</param>
/// <param name="Findings">Prioritised deterministic findings.</param>
public sealed record BudgetExecutionAnalysisDto(
    int Year,
    int Month,
    bool HasBudget,
    decimal TotalBudgeted,
    decimal TotalActual,
    decimal TotalVariance,
    decimal? ExecutionPercentage,
    decimal OverBudgetAmount,
    int OverBudgetCategoryCount,
    int NearLimitCategoryCount,
    int UnbudgetedCategoryCount,
    IReadOnlyList<BudgetExecutionCategoryAnalysisDto> Categories,
    IReadOnlyList<BudgetExecutionTrendDto> Trend,
    IReadOnlyList<BudgetExecutionFindingDto> Findings);
