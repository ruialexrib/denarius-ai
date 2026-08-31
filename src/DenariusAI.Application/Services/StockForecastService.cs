using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Produces conservative ARIMA(0,1,0) forecasts with drift and rolling validation.</summary>
public sealed class StockForecastService : IStockForecastService
{
    private static readonly int[] Horizons = [30, 60, 90];

    /// <inheritdoc />
    public StockForecastResultDto Forecast(IReadOnlyCollection<StockPriceObservationDto> observations)
    {
        var series = observations
            .Where(x => x.Price > 0)
            .GroupBy(x => x.Date)
            .Select(x => x.OrderByDescending(value => value.Date).First())
            .OrderBy(x => x.Date)
            .ToArray();

        if (series.Length < 60)
        {
            return new(false, "ARIMA(0,1,0) com deriva", null, "São necessárias pelo menos 60 cotações válidas para calcular a previsão.", []);
        }

        var logs = series.Select(x => Math.Log((double)x.Price)).ToArray();
        var differences = logs.Zip(logs.Skip(1), (previous, current) => current - previous).ToArray();
        var drift = differences.Average();
        var residualVariance = differences.Select(x => Math.Pow(x - drift, 2)).Sum() / Math.Max(1, differences.Length - 1);
        var validationStart = Math.Max(20, logs.Length - Math.Min(30, logs.Length / 4));
        var percentageErrors = new List<double>();

        for (var index = validationStart; index < logs.Length; index++)
        {
            var trainingDifferences = logs.Take(index).Zip(logs.Skip(1).Take(index - 1), (previous, current) => current - previous);
            var trainingDrift = trainingDifferences.Average();
            var predicted = Math.Exp(logs[index - 1] + trainingDrift);
            percentageErrors.Add(Math.Abs(predicted - Math.Exp(logs[index])) / Math.Exp(logs[index]) * 100d);
        }

        var lastLogPrice = logs[^1];
        var lastDate = series[^1].Date;
        var points = Horizons.Select(days =>
        {
            var tradingSessions = Math.Max(1, (int)Math.Round(days * 5d / 7d));
            var centre = lastLogPrice + drift * tradingSessions;
            var standardError = Math.Sqrt(residualVariance * tradingSessions);
            return new StockPriceForecastDto(
                days,
                lastDate.AddDays(days),
                ToDecimal(Math.Exp(centre)),
                ToDecimal(Math.Exp(centre - 1.96d * standardError)),
                ToDecimal(Math.Exp(centre + 1.96d * standardError)));
        }).ToArray();

        return new(true, "ARIMA(0,1,0) com deriva", ToDecimal(percentageErrors.Average()), null, points);
    }

    /// <summary>Converts a finite positive model value to a monetary decimal.</summary>
    /// <param name="value">The model value.</param>
    /// <returns>The safely bounded decimal value.</returns>
    private static decimal ToDecimal(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)Math.Min(value, (double)decimal.MaxValue), 6);
    }
}
