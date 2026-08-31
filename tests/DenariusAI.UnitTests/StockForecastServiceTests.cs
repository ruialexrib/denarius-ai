using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>Tests deterministic stock time-series forecasting.</summary>
public sealed class StockForecastServiceTests
{
    /// <summary>Verifies that an undersized history does not produce misleading forecasts.</summary>
    [Fact]
    public void Forecast_WithFewerThanSixtyObservations_IsUnavailable()
    {
        var service = new StockForecastService();
        var result = service.Forecast(CreateHistory(59));

        Assert.False(result.IsAvailable);
        Assert.Empty(result.Points);
        Assert.NotNull(result.Message);
    }

    /// <summary>Verifies that the ARIMA result contains every requested horizon and ordered confidence bounds.</summary>
    [Fact]
    public void Forecast_WithSufficientHistory_ReturnsThirtySixtyAndNinetyDays()
    {
        var service = new StockForecastService();
        var result = service.Forecast(CreateHistory(120));

        Assert.True(result.IsAvailable);
        Assert.Equal([30, 60, 90], result.Points.Select(x => x.Days));
        Assert.All(result.Points, point =>
        {
            Assert.True(point.LowerPrice <= point.Price);
            Assert.True(point.Price <= point.UpperPrice);
        });
        Assert.NotNull(result.ValidationMaePercent);
    }

    /// <summary>Creates a deterministic upward price history.</summary>
    /// <param name="count">The number of daily observations.</param>
    /// <returns>The generated observations.</returns>
    private static IReadOnlyCollection<StockPriceObservationDto> CreateHistory(int count)
        => Enumerable.Range(0, count)
            .Select(index => new StockPriceObservationDto(new DateOnly(2025, 1, 1).AddDays(index), 100m + index * 0.25m))
            .ToArray();
}
