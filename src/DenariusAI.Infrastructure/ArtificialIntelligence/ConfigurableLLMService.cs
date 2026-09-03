using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Routes LLM calls to the provider selected in application settings.</summary>
public sealed class ConfigurableLLMService(
    MistralLLMService mistralService,
    OllamaLLMService ollamaService,
    IApplicationSettingsService settingsService) : ILLMService
{
    /// <summary>Gets the providers supported by this router.</summary>
    public string Provider => "Mistral / Ollama";

    /// <summary>Gets a provider-neutral model description because the selected model is stored in application settings.</summary>
    public string Model => "Configured in application settings";

    /// <summary>Gets whether an LLM provider is potentially available. Provider-specific readiness is checked before each call.</summary>
    public bool IsConfigured => ollamaService.IsConfigured || mistralService.IsConfigured;

    /// <summary>Completes a chat using the token limit configured in application settings.</summary>
    /// <param name="messages">Messages to send to the selected provider.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The generated completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return await CompleteAsync(messages, settings.MistralMaxTokens, cancellationToken);
    }

    /// <summary>Completes a chat using the provider selected in application settings.</summary>
    /// <param name="messages">Messages to send to the selected provider.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The generated completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (string.Equals(settings.AiProvider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (!ollamaService.IsConfigured) throw new InvalidOperationException("O Ollama não está configurado.");
            return await ollamaService.CompleteAsync(messages, maxTokens, cancellationToken);
        }

        if (!mistralService.IsConfigured) throw new InvalidOperationException("A API key da Mistral não está configurada.");
        return await mistralService.CompleteAsync(messages, maxTokens, cancellationToken);
    }
}
