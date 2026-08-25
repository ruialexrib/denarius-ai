using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Service interface for suggesting journal entries using AI capabilities.
/// </summary>
public interface IJournalEntrySuggestionService
{
    /// <summary>
    /// Gets a value indicating whether the journal entry suggestion service is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Suggests journal entries based on the provided request.
    /// </summary>
    /// <param name="request">The journal entry suggestion request containing the input data.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the suggestion results.</returns>
    Task<JournalEntrySuggestionResultDto> SuggestAsync(JournalEntrySuggestionRequestDto request, CancellationToken cancellationToken = default);
}
