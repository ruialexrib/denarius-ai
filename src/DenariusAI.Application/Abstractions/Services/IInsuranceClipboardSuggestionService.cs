using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Extracts an editable insurance policy suggestion from user-provided clipboard text.</summary>
public interface IInsuranceClipboardSuggestionService
{
    /// <summary>Gets a value indicating whether AI extraction is configured.</summary>
    bool IsAvailable { get; }

    /// <summary>Analyzes clipboard text without persisting any insurance data.</summary>
    /// <param name="text">Clipboard text supplied by the authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validated policy field suggestion.</returns>
    Task<InsuranceClipboardSuggestionDto> SuggestAsync(string text, CancellationToken cancellationToken = default);
}
