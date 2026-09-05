using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>
/// Provides integration with Mistral AI's language model service.
/// </summary>
/// <param name="httpClient">The HTTP client for making API requests.</param>
/// <param name="options">Configuration options for Mistral AI.</param>
/// <param name="settingsService">Service for retrieving application settings.</param>
/// <param name="logger">Logger for tracking service operations.</param>
public sealed class MistralLLMService(HttpClient httpClient, IOptions<MistralOptions> options, IApplicationSettingsService settingsService, ILogger<MistralLLMService> logger) : ILLMService, ILLMProvider
{
    /// <summary>Gets the stable provider registration identifier.</summary>
    public string Id => "Mistral";

    /// <summary>Resolves the effective model and validates provider configuration.</summary>
    /// <param name="settings">Persisted application settings.</param>
    /// <returns>The provider display name, model and readiness.</returns>
    public LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings)
    {
        var model = settings.GetValueOrDefault("Mistral.Model", _options.Model);
        var baseUrl = settings.GetValueOrDefault("Mistral.BaseUrl", _options.BaseUrl);
        var configured = IsConfigured && !string.IsNullOrWhiteSpace(model)
            && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
        return new("Mistral", model, configured);
    }

    private readonly MistralOptions _options = options.Value;

    /// <summary>
    /// Gets the name of the LLM provider.
    /// </summary>
    public string Provider => "Mistral AI";

    /// <summary>
    /// Gets the model identifier being used.
    /// </summary>
    public string Model => _options.Model;

    /// <summary>
    /// Gets a value indicating whether the service is properly configured with an API key.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    /// <summary>
    /// Sends a completion request to Mistral AI with the provided messages.
    /// </summary>
    /// <param name="messages">The collection of messages to send to the model.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A completion response containing the generated content and usage metrics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the API key is not configured or the response is invalid.</exception>
    /// <exception cref="ArgumentException">Thrown when the messages collection is empty or contains invalid content.</exception>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        => await CompleteAsync(messages, null, cancellationToken);

    /// <summary>Sends a completion request with an explicit or configured output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The output limit, or null to use application settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The generated text and usage metadata.</returns>
    /// <exception cref="InvalidOperationException">Configuration or response content is missing.</exception>
    /// <exception cref="ArgumentException">Messages or the output limit are invalid.</exception>
    /// <exception cref="HttpRequestException">The provider returns a failure status.</exception>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
        => await CompleteAsync(messages, (int?)maxTokens, cancellationToken);

    /// <summary>Sends a completion request with an explicit or configured output limit.</summary>
    /// <param name="messages">The conversation to complete.</param>
    /// <param name="maxTokens">The output limit, or null to use application settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The generated text and usage metadata.</returns>
    /// <exception cref="InvalidOperationException">Configuration or response content is missing.</exception>
    /// <exception cref="ArgumentException">Messages or the output limit are invalid.</exception>
    /// <exception cref="HttpRequestException">The provider returns a failure status.</exception>
    private async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int? maxTokens, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("A API key da Mistral não está configurada.");
        if (messages.Count == 0 || messages.Any(message => string.IsNullOrWhiteSpace(message.Content)))
            throw new ArgumentException("É necessária pelo menos uma mensagem com conteúdo.", nameof(messages));
        var settings = await settingsService.GetAsync(cancellationToken);
        var effectiveMaxTokens = maxTokens ?? settings.AiMaxTokens;
        if (effectiveMaxTokens is < 64 or > 8192) throw new ArgumentOutOfRangeException(nameof(maxTokens), "O limite deve estar entre 64 e 8192 tokens.");

        var baseUri = new Uri(settings.MistralBaseUrl.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "chat/completions"))
        {
            Content = JsonContent.Create(new MistralRequest(settings.MistralModel, messages.Select(message => new MistralMessage(message.Role, message.Content)).ToArray(), settings.AiTemperature, effectiveMaxTokens))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        logger.LogInformation("Calling {Provider} model {Model} with {MessageCount} messages.", Provider, settings.MistralModel, messages.Count);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Mistral request failed with HTTP status {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"A Mistral devolveu o estado HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<MistralResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("A Mistral devolveu uma resposta vazia.");
        var content = payload.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("A Mistral não devolveu conteúdo textual.");

        return new LlmCompletionDto(content, payload.Model ?? settings.MistralModel, payload.Usage?.PromptTokens, payload.Usage?.CompletionTokens, payload.Choices?.FirstOrDefault()?.FinishReason);
    }

    /// <summary>
    /// Represents a request to the Mistral AI API.
    /// </summary>
    private sealed record MistralRequest(string Model, IReadOnlyCollection<MistralMessage> Messages, [property: JsonPropertyName("temperature")] double Temperature, [property: JsonPropertyName("max_tokens")] int MaxTokens);

    /// <summary>
    /// Represents a single message in the conversation.
    /// </summary>
    private sealed record MistralMessage(string Role, string Content);

    /// <summary>
    /// Represents the response from the Mistral AI API.
    /// </summary>
    private sealed record MistralResponse(IReadOnlyCollection<MistralChoice> Choices, string? Model, MistralUsage? Usage);

    /// <summary>
    /// Represents a choice in the API response containing the generated message.
    /// </summary>
    private sealed record MistralChoice(MistralMessage Message, [property: JsonPropertyName("finish_reason")] string? FinishReason);

    /// <summary>
    /// Represents token usage statistics from the API response.
    /// </summary>
    private sealed record MistralUsage([property: JsonPropertyName("prompt_tokens")] int PromptTokens, [property: JsonPropertyName("completion_tokens")] int CompletionTokens);
}
