using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>
/// Provides read-only deterministic data for the income, expense and account-flow analysis.
/// </summary>
public interface IIncomeExpenseFlowRepository
{
    /// <summary>
    /// Gets classified income, expense and account-flow data for a selected period and its comparison period.
    /// </summary>
    /// <param name="from">Selected period start.</param>
    /// <param name="to">Selected period end.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>The deterministic flow data.</returns>
    Task<IncomeExpenseFlowDataDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
