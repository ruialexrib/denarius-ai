using DenariusAI.Application.DTOs;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Defines a registered provider adapter behind the application LLM boundary.</summary>
public interface ILLMProvider
{
    /// <summary>Gets the stable identifier stored in AI.Provider.</summary>
    string Id { get; }

    /// <summary>Resolves effective model and readiness without contacting the provider.</summary>
    /// <param name="settings">Persisted settings; credentials remain inside the adapter.</param>
    /// <returns>The effective provider status.</returns>
    LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings);

    /// <summary>Generates a completion using this provider's configured transport.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The maximum output token count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The completion and available usage metadata.</returns>
    Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default);
}

/// <summary>Describes the configured provider without exposing credentials.</summary>
/// <param name="Provider">The provider display name.</param>
/// <param name="Model">The effective model identifier.</param>
/// <param name="IsConfigured">Whether required settings and credentials are present.</param>
public sealed record LlmProviderStatus(string Provider, string Model, bool IsConfigured);
