using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

public sealed class AnalyticsService(IAnalyticsRepository repository) : IAnalyticsService
{
    public Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default)
    {
        if (filter.From == default || filter.To == default || filter.From > filter.To)
            throw new ArgumentException("O intervalo de datas é inválido.");
        return repository.GetAsync(filter, cancellationToken);
    }
}
