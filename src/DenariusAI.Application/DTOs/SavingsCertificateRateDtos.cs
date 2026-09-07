namespace DenariusAI.Application.DTOs;

/// <summary>Represents one official Savings Certificate reference-rate observation.</summary>
/// <param name="Date">First day of the month for which the published rate applies.</param>
/// <param name="Series">Savings Certificate series.</param>
/// <param name="GrossRate">Published gross annual interest rate percentage.</param>
public sealed record SavingsCertificateRateObservationDto(DateOnly Date, string Series, decimal GrossRate);

/// <summary>Contains persisted Savings Certificate reference-rate history and source metadata.</summary>
/// <param name="Observations">Rate observations ordered chronologically.</param>
/// <param name="UpdatedAt">Timestamp of the latest successful refresh, when available.</param>
/// <param name="SourceName">Human-readable provider name.</param>
/// <param name="SourceUrl">Canonical provider page.</param>
public sealed record SavingsCertificateRateHistoryDto(
    IReadOnlyList<SavingsCertificateRateObservationDto> Observations,
    DateTimeOffset? UpdatedAt,
    string SourceName,
    string SourceUrl);

/// <summary>Describes the outcome of refreshing official Savings Certificate rates.</summary>
/// <param name="ImportedCount">Number of observations retrieved from the provider in this refresh.</param>
/// <param name="StoredCount">Total number of distinct observations stored after the refresh.</param>
/// <param name="UpdatedAt">Timestamp recorded for the successful refresh.</param>
public sealed record SavingsCertificateRateRefreshResultDto(int ImportedCount, int StoredCount, DateTimeOffset UpdatedAt);
