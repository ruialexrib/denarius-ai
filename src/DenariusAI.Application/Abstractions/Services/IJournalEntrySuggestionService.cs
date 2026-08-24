using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface IJournalEntrySuggestionService
{
    bool IsAvailable { get; }
    Task<JournalEntrySuggestionResultDto> SuggestAsync(JournalEntrySuggestionRequestDto request, CancellationToken cancellationToken = default);
}
