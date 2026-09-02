using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.DTOs;

/// <summary>Contains the editable insurance policy fields extracted from clipboard text.</summary>
public sealed record InsuranceClipboardSuggestionDto(
    string? Name,
    string? Insurer,
    string? PolicyNumber,
    InsurancePolicyType? Type,
    InsurancePaymentFrequency? PaymentFrequency,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? RenewalDate,
    string? InsuredSubject,
    string? Notes,
    string Confidence,
    string Message);
