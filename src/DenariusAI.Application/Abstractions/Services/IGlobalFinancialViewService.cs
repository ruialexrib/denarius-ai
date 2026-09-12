using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Builds the deterministic executive data used by the global financial view.
/// </summary>
public interface IGlobalFinancialViewService
{
    /// <summary>
    /// Gets the global financial view for the selected interval and its preceding equivalent comparison period.
    /// </summary>
    /// <param name="from">Selected period start.</param>
    /// <param name="to">Selected period end.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The calculated global financial view.</returns>
    Task<GlobalFinancialViewDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
