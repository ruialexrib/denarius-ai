using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Defines the contract for AI assistant services that provide conversational capabilities.
/// </summary>
public interface IAssistantService
{
    /// <summary>
    /// Gets a value indicating whether the assistant service is available and ready to process requests.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the name or identifier of the AI model being used by this assistant service.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Processes an assistant request asynchronously and returns a response.
    /// </summary>
    /// <param name="request">The assistant request containing the user's message and context.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation, containing the assistant's response.</returns>
    Task<AssistantResponseDto> AskAsync(AssistantRequestDto request, CancellationToken cancellationToken = default);
}
