using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Routes LLM calls to the provider selected in application settings.</summary>
public sealed class ConfigurableLLMService(
    MistralLLMService mistralService,
    OllamaLLMService ollamaService,
    IApplicationSettingsService settingsService) : ILLMService
{
    /// <summary>Gets the configured provider. Runtime calls resolve the persisted setting before dispatch.</summary>
    public string Provider => "Mistral / Ollama";

    /// <summary>Gets a provider-neutral model description because the selected model is stored in the database.</summary>
    public string Model => "Configured in application settings";

    /// <summary>
    /// Gets whether at least one provider can be used. Ollama requires no credential; Mistral requires its API key.
    /// Endpoint and model validation is performed when settings are saved.
    /// </summary>
    public bool IsConfigured => ollamaService.IsConfigured || mistralService.IsConfigured;

    public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        => CompleteAsync(messages, 1024, cancellationToken);

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
