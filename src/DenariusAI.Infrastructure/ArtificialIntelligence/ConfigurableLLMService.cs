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
    private const string JournalCatalogPrefix = "CATALOG_JSON:\n";
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

    /// <summary>Writes detailed request diagnostics while exposing only support data that is safe to audit.</summary>
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
            content = DescribeRequestMessageContent(message)
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

    /// <summary>Returns an auditable representation of one request message without exposing personal financial content.</summary>
    /// <param name="message">The message passed to the provider.</param>
    /// <returns>The original system prompt, a sanitized support catalog, or a redaction marker.</returns>
    private static string DescribeRequestMessageContent(LlmMessageDto message)
    {
        if (string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
            return message.Content;

        if (message.Content.StartsWith(JournalCatalogPrefix, StringComparison.Ordinal))
            return DescribeJournalCatalog(message.Content[JournalCatalogPrefix.Length..]);

        return "[REDACTED: USER OR FINANCIAL CONTENT]";
    }

    /// <summary>Sanitizes the journal suggestion catalog while preserving data needed to audit model classification.</summary>
    /// <param name="json">The serialized journal suggestion catalog.</param>
    /// <returns>A sanitized JSON representation of the support catalog.</returns>
    private static string DescribeJournalCatalog(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var accounts = root.TryGetProperty("accounts", out var accountArray) && accountArray.ValueKind == JsonValueKind.Array
                ? accountArray.EnumerateArray().Select(account => new
                {
                    id = ReadString(account, "Id", "id"),
                    name = "[REDACTED: ACCOUNT NAME]",
                    type = ReadString(account, "type"),
                    categoryId = ReadString(account, "categoryId"),
                    currency = ReadString(account, "Currency", "currency")
                }).ToList()
                : [];
            var categories = root.TryGetProperty("categories", out var categoryArray) && categoryArray.ValueKind == JsonValueKind.Array
                ? categoryArray.EnumerateArray().Select(category => new
                {
                    id = ReadString(category, "Id", "id"),
                    name = ReadString(category, "name"),
                    group = ReadString(category, "group"),
                    type = ReadString(category, "type")
                }).ToList()
                : [];
            var budgets = root.TryGetProperty("budgets", out var budgetArray) && budgetArray.ValueKind == JsonValueKind.Array
                ? budgetArray.EnumerateArray().Select(budget => new
                {
                    id = ReadString(budget, "Id", "id"),
                    year = ReadInt32(budget, "Year", "year"),
                    month = ReadInt32(budget, "Month", "month")
                }).ToList()
                : [];
            var recentExampleCount = root.TryGetProperty("recentJournalEntries", out var examples) && examples.ValueKind == JsonValueKind.Array
                ? examples.GetArrayLength()
                : 0;

            return JournalCatalogPrefix + JsonSerializer.Serialize(new
            {
                today = ReadString(root, "today"),
                currency = ReadString(root, "currency"),
                partial = root.TryGetProperty("partial", out var partial) ? partial.Clone() : default(JsonElement),
                accounts,
                categories,
                budgets,
                recentJournalEntries = new { count = recentExampleCount, content = "[REDACTED: PERSONAL FINANCIAL EXAMPLES]" }
            });
        }
        catch (JsonException)
        {
            return JournalCatalogPrefix + "[INVALID JSON: CONTENT REDACTED]";
        }
    }

    /// <summary>Reads a string-compatible property from a JSON object using one or more candidate names.</summary>
    /// <param name="element">The JSON object to inspect.</param>
    /// <param name="names">Candidate property names.</param>
    /// <returns>The textual value, or null when no candidate exists.</returns>
    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
                JsonValueKind.Null => null,
                _ => value.GetRawText()
            };
        }

        return null;
    }

    /// <summary>Reads an integer property from a JSON object using one or more candidate names.</summary>
    /// <param name="element">The JSON object to inspect.</param>
    /// <param name="names">Candidate property names.</param>
    /// <returns>The integer value, or null when unavailable.</returns>
    private static int? ReadInt32(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number))
                return number;
        }

        return null;
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
