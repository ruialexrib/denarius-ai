using System.Text;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Routes application LLM requests to registered provider adapters.</summary>
/// <param name="providers">The adapters registered by infrastructure configuration.</param>
/// <param name="settingsService">The effective application settings.</param>
/// <param name="dbContext">Persisted settings for synchronous availability properties.</param>
/// <param name="logger">Optional logger used for provider-neutral model diagnostics.</param>
public sealed class ConfigurableLLMService(
    IEnumerable<ILLMProvider> providers,
    IApplicationSettingsService settingsService,
    DenariusDbContext dbContext,
    ILogger<ConfigurableLLMService>? logger = null) : ILLMService
{
    private readonly IReadOnlyDictionary<string, ILLMProvider> _providers =
        providers.ToDictionary(provider => provider.Id, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the selected provider's display name.</summary>
    public string Provider => GetStatus().Provider;

    /// <summary>Gets the selected provider's effective model.</summary>
    public string Model => GetStatus().Model;

    /// <summary>Gets whether the selected adapter has the required configuration.</summary>
    public bool IsConfigured => GetStatus().IsConfigured;

    /// <summary>Completes a chat with the configured common output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected provider's completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return await CompleteCoreAsync(messages, settings.AiMaxTokens, settings, cancellationToken);
    }

    /// <summary>Completes a chat with an explicit workflow output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The maximum output token count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected provider's completion.</returns>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return await CompleteCoreAsync(messages, maxTokens, settings, cancellationToken);
    }

    /// <summary>Executes one provider call and emits optional provider-neutral diagnostics.</summary>
    /// <param name="messages">The conversation sent to the configured model.</param>
    /// <param name="maxTokens">Maximum output tokens requested by the workflow.</param>
    /// <param name="settings">Effective application settings for this call.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected provider's completion.</returns>
    private async Task<LlmCompletionDto> CompleteCoreAsync(
        IReadOnlyCollection<LlmMessageDto> messages,
        int maxTokens,
        ApplicationSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var provider = Resolve(settings.AiProvider);
        LlmProviderStatus? diagnosticStatus = null;
        if (settings.AiVerboseModelLogging)
        {
            diagnosticStatus = provider.GetStatus(await ReadStoredSettingsAsync(cancellationToken));
            LogVerboseRequest(diagnosticStatus, messages, maxTokens);
        }

        try
        {
            var completion = await provider.CompleteAsync(messages, maxTokens, cancellationToken);
            if (settings.AiVerboseModelLogging)
                LogVerboseResponse(diagnosticStatus?.Provider ?? provider.Id, completion);
            return completion;
        }
        catch (Exception exception) when (settings.AiVerboseModelLogging && exception is HttpRequestException or TaskCanceledException)
        {
            logger?.LogWarning(
                exception,
                "AI verbose diagnostics: provider call failed. Provider: {Provider}; Model: {Model}; Messages: {MessageCount}; MaxTokens: {MaxTokens}.",
                diagnosticStatus?.Provider ?? provider.Id,
                diagnosticStatus?.Model ?? string.Empty,
                messages.Count,
                maxTokens);
            throw;
        }
    }

    /// <summary>Writes detailed request diagnostics without exposing financial or credential-bearing message content.</summary>
    /// <param name="status">Effective provider status.</param>
    /// <param name="messages">Messages passed to the provider adapter.</param>
    /// <param name="maxTokens">Maximum output tokens requested.</param>
    private void LogVerboseRequest(LlmProviderStatus status, IReadOnlyCollection<LlmMessageDto> messages, int maxTokens)
    {
        if (logger is null) return;
        var diagnostics = messages.Select((message, index) => new
        {
            index,
            role = message.Role,
            utf8Bytes = Encoding.UTF8.GetByteCount(message.Content),
            content = string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase)
                ? message.Content
                : "[REDACTED: USER OR FINANCIAL CONTENT]"
        });

        logger.LogInformation(
            "AI verbose request. Provider: {Provider}; Model: {Model}; MessageCount: {MessageCount}; RequestBytes: {RequestBytes}; MaxTokens: {MaxTokens}; Messages: {Messages}.",
            status.Provider,
            status.Model,
            messages.Count,
            JsonSerializer.SerializeToUtf8Bytes(messages).Length,
            maxTokens,
            JsonSerializer.Serialize(diagnostics));
    }

    /// <summary>Writes detailed response diagnostics without exposing generated financial or personal content.</summary>
    /// <param name="provider">Effective provider display name.</param>
    /// <param name="completion">Completion returned by the provider adapter.</param>
    private void LogVerboseResponse(string provider, LlmCompletionDto completion)
    {
        if (logger is null) return;
        logger.LogInformation(
            "AI verbose response. Provider: {Provider}; Model: {Model}; ResponseBytes: {ResponseBytes}; PromptTokens: {PromptTokens}; CompletionTokens: {CompletionTokens}; FinishReason: {FinishReason}; ResponseStructure: {ResponseStructure}.",
            provider,
            completion.Model,
            Encoding.UTF8.GetByteCount(completion.Content),
            completion.PromptTokens,
            completion.CompletionTokens,
            completion.FinishReason,
            DescribeResponseStructure(completion.Content));
    }

    /// <summary>Describes model output structure while omitting model-controlled names and values.</summary>
    /// <param name="content">Raw model completion text.</param>
    /// <returns>A compact structural description of the completion.</returns>
    private static string DescribeResponseStructure(string content)
    {
        var json = content.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine >= 0 && lastFence > firstLine)
                json = json[(firstLine + 1)..lastFence].Trim();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var propertyCount = document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().Count()
                : 0;
            var itemCount = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : 0;
            return JsonSerializer.Serialize(new
            {
                validJson = true,
                rootKind = document.RootElement.ValueKind.ToString(),
                propertyCount,
                itemCount,
                values = "[REDACTED]"
            });
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { validJson = false, characters = content.Length, values = "[REDACTED]" });
        }
    }

    /// <summary>Reads stored settings required to resolve provider-specific status.</summary>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The current persisted setting values.</returns>
    private async Task<IReadOnlyDictionary<string, string>> ReadStoredSettingsAsync(CancellationToken cancellationToken) =>
        await dbContext.ApplicationSettings.AsNoTracking().ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

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

    /// <summary>Resolves an adapter without silently selecting a different provider.</summary>
    /// <param name="id">The configured provider identifier.</param>
    /// <returns>The registered adapter.</returns>
    /// <exception cref="InvalidOperationException">The selected provider is not registered.</exception>
    private ILLMProvider Resolve(string id) => _providers.TryGetValue(id.Trim(), out var provider)
        ? provider
        : throw new InvalidOperationException("O fornecedor de IA selecionado não é suportado. Verifique as Definições.");
}
