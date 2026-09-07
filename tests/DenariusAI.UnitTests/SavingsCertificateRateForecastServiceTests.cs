using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>Tests deterministic Savings Certificate reference-rate forecasting.</summary>
public sealed class SavingsCertificateRateForecastServiceTests
{
    /// <summary>Verifies that fewer than six monthly observations do not produce a forecast.</summary>
    [Fact]
    public void Forecast_WithFewerThanSixObservations_IsUnavailable()
    {
        var service = new SavingsCertificateRateForecastService();
        var result = service.Forecast(CreateHistory(5));

        Assert.False(result.Available);
        Assert.Null(result.Forecast);
        Assert.NotNull(result.Message);
    }

    /// <summary>Verifies that sufficient monthly history produces an ordered next-month confidence interval.</summary>
    [Fact]
    public void Forecast_WithSufficientHistory_ReturnsNextMonth()
    {
        var service = new SavingsCertificateRateForecastService();
        var history = CreateHistory(12);

        var result = service.Forecast(history);

        Assert.True(result.Available);
        Assert.Equal("ARIMA(0,1,0) com deriva", result.Model);
        Assert.NotNull(result.Forecast);
        Assert.Equal(history.Max(item => item.Date).AddMonths(1), result.Forecast!.Date);
        Assert.True(result.Forecast.LowerRate <= result.Forecast.GrossRate);
        Assert.True(result.Forecast.GrossRate <= result.Forecast.UpperRate);
        Assert.NotNull(result.MeanAbsoluteError);
    }

    /// <summary>Verifies that a constant rate remains constant in the next-month point forecast.</summary>
    [Fact]
    public void Forecast_WithConstantHistory_PreservesRate()
    {
        var service = new SavingsCertificateRateForecastService();
        var history = Enumerable.Range(0, 12)
            .Select(index => new SavingsCertificateRateObservationDto(new DateOnly(2025, 10, 1).AddMonths(index), "F", 2.5m))
            .ToArray();

        var result = service.Forecast(history);

        Assert.True(result.Available);
        Assert.Equal(2.5m, result.Forecast!.GrossRate);
        Assert.Equal(2.5m, result.Forecast.LowerRate);
        Assert.Equal(2.5m, result.Forecast.UpperRate);
    }

    /// <summary>Creates a deterministic monthly rate history with a small upward drift.</summary>
    /// <param name="count">Number of monthly observations.</param>
    /// <returns>The generated reference-rate observations.</returns>
    private static IReadOnlyCollection<SavingsCertificateRateObservationDto> CreateHistory(int count)
        => Enumerable.Range(0, count)
            .Select(index => new SavingsCertificateRateObservationDto(new DateOnly(2025, 10, 1).AddMonths(index), "F", 2m + index * 0.05m))
            .ToArray();
}
