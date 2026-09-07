using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Calculates deterministic forecasts from official Savings Certificate reference-rate history.</summary>
public interface ISavingsCertificateRateForecastService
{
    /// <summary>Forecasts the gross reference rate for the next calendar month.</summary>
    /// <param name="observations">Chronological or unordered official monthly rate observations.</param>
    /// <returns>The forecast result and model metadata.</returns>
    SavingsCertificateRateForecastResultDto Forecast(IReadOnlyCollection<SavingsCertificateRateObservationDto> observations);
}
