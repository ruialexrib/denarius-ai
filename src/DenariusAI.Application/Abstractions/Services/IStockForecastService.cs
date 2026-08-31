using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Calculates deterministic forecasts from an imported stock-price time series.</summary>
public interface IStockForecastService
{
    /// <summary>Builds 30, 60 and 90 calendar-day forecasts using a conservative ARIMA model.</summary>
    /// <param name="observations">The available dated closing prices.</param>
    /// <returns>The forecast result, including uncertainty and validation quality.</returns>
    StockForecastResultDto Forecast(IReadOnlyCollection<StockPriceObservationDto> observations);
}
