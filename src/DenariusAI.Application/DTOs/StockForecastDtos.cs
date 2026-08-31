namespace DenariusAI.Application.DTOs;

/// <summary>Represents an observed dated stock price used by a forecasting model.</summary>
/// <param name="Date">The observation date.</param>
/// <param name="Price">The positive closing price.</param>
public sealed record StockPriceObservationDto(DateOnly Date, decimal Price);

/// <summary>Represents a time-series price forecast and its uncertainty interval.</summary>
/// <param name="Days">The calendar-day horizon.</param>
/// <param name="Date">The target calendar date.</param>
/// <param name="Price">The central forecast.</param>
/// <param name="LowerPrice">The lower 95 percent confidence bound.</param>
/// <param name="UpperPrice">The upper 95 percent confidence bound.</param>
public sealed record StockPriceForecastDto(int Days, DateOnly Date, decimal Price, decimal LowerPrice, decimal UpperPrice);

/// <summary>Contains the result of a validated ARIMA forecast.</summary>
/// <param name="IsAvailable">Whether the history supports a forecast.</param>
/// <param name="Model">The selected model description.</param>
/// <param name="ValidationMaePercent">The rolling validation mean absolute percentage error.</param>
/// <param name="Message">A user-facing explanation when no forecast is available.</param>
/// <param name="Points">The requested forecast horizons.</param>
public sealed record StockForecastResultDto(bool IsAvailable, string Model, decimal? ValidationMaePercent, string? Message, IReadOnlyList<StockPriceForecastDto> Points);
