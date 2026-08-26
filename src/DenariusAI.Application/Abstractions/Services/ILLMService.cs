using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Defines the contract for Language Model services that interact with various LLM providers.
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Gets the name of the LLM provider (e.g., "OpenAI", "Anthropic", "Google").
    /// </summary>
    string Provider { get; }
    
    /// <summary>
    /// Gets the specific model identifier being used (e.g., "gpt-4", "claude-3-opus").
    /// </summary>
    string Model { get; }
    
    /// <summary>
    /// Gets a value indicating whether the service is properly configured with necessary credentials and settings.
    /// </summary>
    bool IsConfigured { get; }
    
    /// <summary>
    /// Generates a completion response from the LLM based on the provided conversation messages.
    /// </summary>
    /// <param name="messages">The collection of messages representing the conversation history.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation, containing the LLM completion response.</returns>
    Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default);

    Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default) => CompleteAsync(messages, cancellationToken);
}
