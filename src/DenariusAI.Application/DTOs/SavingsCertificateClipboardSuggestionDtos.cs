namespace DenariusAI.Application.DTOs;

/// <summary>Contains editable Savings Certificate fields extracted from clipboard text.</summary>
public sealed record SavingsCertificateClipboardSuggestionDto(
    DateOnly? InvestmentDate,
    string? SeriesNumber,
    string? Description,
    decimal? InvestmentValue,
    decimal? CurrentValue,
    DateOnly? NextCapitalization,
    string Confidence,
    string Message);
