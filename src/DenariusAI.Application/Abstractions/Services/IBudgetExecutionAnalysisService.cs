using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Builds deterministic budget and budget-execution analysis.
/// </summary>
public interface IBudgetExecutionAnalysisService
{
    /// <summary>
    /// Gets the budget execution analysis for one calendar month.
    /// </summary>
    /// <param name="year">Budget year.</param>
    /// <param name="month">Budget month.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The calculated budget execution analysis.</returns>
    Task<BudgetExecutionAnalysisDto> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
