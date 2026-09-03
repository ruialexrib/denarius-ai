using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Extracts an editable Savings Certificate suggestion from clipboard text.</summary>
public interface ISavingsCertificateClipboardSuggestionService
{
    bool IsAvailable { get; }
    Task<SavingsCertificateClipboardSuggestionDto> SuggestAsync(string text, CancellationToken cancellationToken = default);
}
