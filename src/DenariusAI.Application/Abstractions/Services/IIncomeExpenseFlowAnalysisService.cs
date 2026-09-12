using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Builds the deterministic Income, Expenses and Flows analysis.
/// </summary>
public interface IIncomeExpenseFlowAnalysisService
{
    /// <summary>
    /// Gets the executive flow analysis for the selected period.
    /// </summary>
    /// <param name="from">Selected period start.</param>
    /// <param name="to">Selected period end.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The calculated analysis.</returns>
    Task<IncomeExpenseFlowAnalysisDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
