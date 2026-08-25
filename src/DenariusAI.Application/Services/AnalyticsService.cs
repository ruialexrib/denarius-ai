using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>
/// Service responsible for handling analytics operations.
/// </summary>
/// <param name="repository">The analytics repository instance.</param>
public sealed class AnalyticsService(IAnalyticsRepository repository) : IAnalyticsService
{
    /// <summary>
    /// Retrieves analytics data based on the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter containing date range and other criteria.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the analytics data.</returns>
    /// <exception cref="ArgumentException">Thrown when the date range is invalid.</exception>
    public Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (filter.From == default || filter.To == default || filter.From > filter.To)
            throw new ArgumentException("O intervalo de datas é inválido.");
        return repository.GetAsync(filter, cancellationToken);
    }
}
