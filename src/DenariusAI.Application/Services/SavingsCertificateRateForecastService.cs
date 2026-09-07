using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Produces a one-month ARIMA(0,1,0) forecast with drift for Savings Certificate reference rates.</summary>
public sealed class SavingsCertificateRateForecastService : ISavingsCertificateRateForecastService
{
    private const int MinimumObservations = 6;

    /// <inheritdoc />
    public SavingsCertificateRateForecastResultDto Forecast(IReadOnlyCollection<SavingsCertificateRateObservationDto> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var series = observations
            .Where(item => item.GrossRate >= 0m)
            .GroupBy(item => item.Date)
            .Select(group => group.OrderByDescending(item => item.Series, StringComparer.Ordinal).First())
            .OrderBy(item => item.Date)
            .ToArray();

        if (series.Length < MinimumObservations)
        {
            return new(false, "ARIMA(0,1,0) com deriva", null, $"São necessárias pelo menos {MinimumObservations} taxas mensais válidas para calcular a previsão.", null);
        }

        var values = series.Select(item => (double)item.GrossRate).ToArray();
        var differences = values.Zip(values.Skip(1), (previous, current) => current - previous).ToArray();
        var drift = differences.Average();
        var residualVariance = differences.Select(value => Math.Pow(value - drift, 2)).Sum() / Math.Max(1, differences.Length - 1);
        var standardError = Math.Sqrt(residualVariance);
        var centre = values[^1] + drift;
        var lower = Math.Max(0d, centre - 1.96d * standardError);
        var upper = Math.Max(lower, centre + 1.96d * standardError);
        var forecastDate = series[^1].Date.AddMonths(1);
        var validationErrors = CalculateValidationErrors(values);

        return new(
            true,
            "ARIMA(0,1,0) com deriva",
            validationErrors.Count == 0 ? null : ToDecimal(validationErrors.Average()),
            null,
            new SavingsCertificateRateForecastDto(forecastDate, ToDecimal(Math.Max(0d, centre)), ToDecimal(lower), ToDecimal(upper)));
    }

    /// <summary>Calculates rolling one-step absolute forecast errors in percentage points.</summary>
    /// <param name="values">Ordered monthly gross rates.</param>
    /// <returns>The validation errors for all eligible rolling origins.</returns>
    private static IReadOnlyList<double> CalculateValidationErrors(IReadOnlyList<double> values)
    {
        var errors = new List<double>();
        for (var index = 3; index < values.Count; index++)
        {
            var trainingDifferences = Enumerable.Range(1, index - 1)
                .Select(position => values[position] - values[position - 1]);
            var trainingDrift = trainingDifferences.Average();
            var predicted = values[index - 1] + trainingDrift;
            errors.Add(Math.Abs(predicted - values[index]));
        }

        return errors;
    }

    /// <summary>Converts a finite model value to a bounded decimal rounded to six places.</summary>
    /// <param name="value">The model value.</param>
    /// <returns>The bounded decimal value.</returns>
    private static decimal ToDecimal(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0m;
        }

        return Math.Round((decimal)Math.Clamp(value, 0d, (double)decimal.MaxValue), 6);
    }
}
