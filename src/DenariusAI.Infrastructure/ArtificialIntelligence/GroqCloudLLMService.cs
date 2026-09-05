using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Implements the GroqCloud chat-completion transport behind the provider-neutral LLM boundary.</summary>
/// <param name="httpClient">The HTTP client configured for GroqCloud.</param>
/// <param name="options">Deployment-only credentials and installation defaults.</param>
/// <param name="settingsService">Effective administrator-editable settings.</param>
/// <param name="logger">The logger for non-sensitive request metadata.</param>
public sealed class GroqCloudLLMService(HttpClient httpClient, IOptions<GroqCloudOptions> options,
    IApplicationSettingsService settingsService, ILogger<GroqCloudLLMService> logger) : ILLMProvider
{
    private readonly GroqCloudOptions _options = options.Value;

    /// <summary>Gets the stable provider selection identifier.</summary>
    public string Id => "GroqCloud";

    /// <summary>Resolves effective model and readiness without making a remote request.</summary>
    /// <param name="settings">Persisted non-secret application settings.</param>
    /// <returns>The provider name, selected model and configuration status.</returns>
    public LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings)
    {
        var model = settings.GetValueOrDefault("GroqCloud.Model", _options.Model);
        var baseUrl = settings.GetValueOrDefault("GroqCloud.BaseUrl", _options.BaseUrl);
        var effort = settings.GetValueOrDefault("GroqCloud.ReasoningEffort", _options.ReasoningEffort);
        return new(Id, model, !string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(model)
            && IsValidBaseUrl(baseUrl) && IsValidReasoningEffort(effort));
    }

    /// <summary>Checks that a configured API root uses HTTPS without embedded credentials or query data.</summary>
    /// <param name="value">The candidate API root.</param>
    /// <returns>True for a valid HTTPS API root.</returns>
    internal static bool IsValidBaseUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    /// <summary>Checks the supported GPT-OSS reasoning settings.</summary>
    /// <param name="value">The candidate effort.</param>
    /// <returns>True for low, medium or high effort.</returns>
    internal static bool IsValidReasoningEffort(string value) => value is "low" or "medium" or "high";

    /// <summary>Sends a non-streaming chat request and returns only the final textual answer and usage.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The maximum completion tokens, including model reasoning.</param>
    /// <param name="cancellationToken">Token used to cancel settings retrieval and HTTP operations.</param>
    /// <returns>The model answer and available usage metadata.</returns>
    /// <exception cref="ArgumentException">The messages or output limit are invalid.</exception>
    /// <exception cref="InvalidOperationException">Configuration is incomplete or the response contains no usable answer.</exception>
    /// <exception cref="HttpRequestException">GroqCloud returns an unsuccessful status, preserved for caller policies.</exception>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("A chave API do GroqCloud não está configurada.");
        if (messages.Count == 0 || messages.Any(message => string.IsNullOrWhiteSpace(message.Content)))
            throw new ArgumentException("É necessária pelo menos uma mensagem com conteúdo.", nameof(messages));
        if (maxTokens is < 64 or > 8192) throw new ArgumentOutOfRangeException(nameof(maxTokens), "O limite deve estar entre 64 e 8192 tokens.");
        var settings = await settingsService.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.GroqCloudModel) || !IsValidBaseUrl(settings.GroqCloudBaseUrl)
            || !IsValidReasoningEffort(settings.GroqCloudReasoningEffort))
            throw new InvalidOperationException("A configuração do GroqCloud é inválida. Verifique as Definições.");

        var model = settings.GroqCloudModel.Trim();
        var effort = model.StartsWith("openai/gpt-oss-", StringComparison.Ordinal) ? settings.GroqCloudReasoningEffort : null;
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.GroqCloudBaseUrl.TrimEnd('/') + "/"), "chat/completions"))
        {
            Content = JsonContent.Create(new ChatRequest(model, messages.Select(message => new ChatMessage(message.Role, message.Content)).ToArray(),
                settings.AiTemperature, maxTokens, false, effort))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        logger.LogInformation("Calling GroqCloud model {Model} with {MessageCount} messages.", model, messages.Count);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("GroqCloud request failed with HTTP status {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"O GroqCloud devolveu o estado HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }
        ChatResponse? payload;
        try { payload = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken); }
        catch (JsonException) { throw new InvalidOperationException("O GroqCloud devolveu uma resposta num formato inválido. Tente novamente."); }
        var choice = payload?.Choices?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(choice?.Message?.Content))
            throw new InvalidOperationException(choice?.FinishReason == "length"
                ? "O GroqCloud atingiu o limite de geração sem devolver uma resposta. Reveja o limite de tokens e o esforço de raciocínio nas Definições."
                : "O GroqCloud não devolveu conteúdo textual. Tente novamente.");
        return new(choice.Message.Content, payload?.Model ?? model, payload?.Usage?.PromptTokens, payload?.Usage?.CompletionTokens, choice.FinishReason);
    }

    /// <summary>Represents the supported Groq chat request fields.</summary>
    private sealed record ChatRequest(string Model, IReadOnlyCollection<ChatMessage> Messages, double Temperature,
        [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens, bool Stream,
        [property: JsonPropertyName("reasoning_effort"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReasoningEffort);

    /// <summary>Represents a text message without provider-specific extra fields.</summary>
    private sealed record ChatMessage(string Role, string? Content);

    /// <summary>Represents the consumed fields of a Groq response.</summary>
    private sealed record ChatResponse(IReadOnlyList<ChatChoice>? Choices, string? Model, ChatUsage? Usage);

    /// <summary>Represents the final answer and stop reason; private reasoning fields are ignored.</summary>
    private sealed record ChatChoice(ChatMessage? Message, [property: JsonPropertyName("finish_reason")] string? FinishReason);

    /// <summary>Represents provider-reported token usage.</summary>
    private sealed record ChatUsage([property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int? CompletionTokens);
}
