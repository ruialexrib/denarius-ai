using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Routes LLM calls to the provider selected in application settings.</summary>
public sealed class ConfigurableLLMService(
    MistralLLMService mistralService,
    OllamaLLMService ollamaService,
    IApplicationSettingsService settingsService) : ILLMService
{
    public string Provider => "Configured";
    public string Model => "Configured";
    public bool IsConfigured => true;

    public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        => CompleteAsync(messages, 1024, cancellationToken);

    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return string.Equals(settings.AiProvider, "Ollama", StringComparison.OrdinalIgnoreCase)
            ? await ollamaService.CompleteAsync(messages, maxTokens, cancellationToken)
            : await mistralService.CompleteAsync(messages, maxTokens, cancellationToken);
    }
}
