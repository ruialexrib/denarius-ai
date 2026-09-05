using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Routes application LLM requests to registered provider adapters.</summary>
/// <param name="providers">The adapters registered by infrastructure configuration.</param>
/// <param name="settingsService">The effective application settings.</param>
/// <param name="dbContext">Persisted settings for synchronous availability properties.</param>
public sealed class ConfigurableLLMService(
    IEnumerable<ILLMProvider> providers,
    IApplicationSettingsService settingsService,
    DenariusDbContext dbContext) : ILLMService
{
    private readonly IReadOnlyDictionary<string, ILLMProvider> _providers =
        providers.ToDictionary(provider => provider.Id, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the selected provider's display name.</summary>
    public string Provider => GetStatus().Provider;

    /// <summary>Gets the selected provider's effective model.</summary>
    public string Model => GetStatus().Model;

    /// <summary>Gets whether the selected adapter has the required configuration.</summary>
    public bool IsConfigured => GetStatus().IsConfigured;

    /// <summary>Reads current settings so saved provider changes apply immediately.</summary>
    /// <returns>The selected adapter's status, or unavailable for an unknown provider.</returns>
    private LlmProviderStatus GetStatus()
    {
        var values = dbContext.ApplicationSettings.AsNoTracking()
            .ToDictionary(setting => setting.Key, setting => setting.Value);
        var id = values.GetValueOrDefault("AI.Provider", "Mistral").Trim();
        return _providers.TryGetValue(id, out var provider)
            ? provider.GetStatus(values)
            : new LlmProviderStatus(id, string.Empty, false);
    }

    /// <summary>Completes a chat with the configured common output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected provider's completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return await Resolve(settings.AiProvider).CompleteAsync(messages, settings.AiMaxTokens, cancellationToken);
    }

    /// <summary>Completes a chat with an explicit workflow output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The maximum output token count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected provider's completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return await Resolve(settings.AiProvider).CompleteAsync(messages, maxTokens, cancellationToken);
    }

    /// <summary>Resolves an adapter without silently selecting a different provider.</summary>
    /// <param name="id">The configured provider identifier.</param>
    /// <returns>The registered adapter.</returns>
    /// <exception cref="InvalidOperationException">The selected provider is not registered.</exception>
    private ILLMProvider Resolve(string id) => _providers.TryGetValue(id.Trim(), out var provider)
        ? provider
        : throw new InvalidOperationException("O fornecedor de IA selecionado não é suportado. Verifique as Definições.");
}
