using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>Provides Savings Certificate reference-rate history to the dedicated UI.</summary>
/// <param name="Months">Selected recent period in calendar months.</param>
/// <param name="Observations">Official observations included in the selected period.</param>
/// <param name="UpdatedAt">Timestamp of the latest successful provider refresh.</param>
/// <param name="SourceName">Provider display name.</param>
/// <param name="SourceUrl">Canonical provider page.</param>
/// <param name="Forecast">Deterministic next-month ARIMA forecast based on the available stored history.</param>
public sealed record SavingsCertificateRateHistoryViewModel(
    int Months,
    IReadOnlyList<SavingsCertificateRateObservationDto> Observations,
    DateTimeOffset? UpdatedAt,
    string SourceName,
    string SourceUrl,
    SavingsCertificateRateForecastResultDto Forecast);
