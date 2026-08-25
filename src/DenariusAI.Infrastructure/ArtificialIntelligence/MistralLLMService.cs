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
public sealed class MistralLLMService(HttpClient httpClient, IOptions<MistralOptions> options, IApplicationSettingsService settingsService, ILogger<MistralLLMService> logger) : ILLMService
{
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
    {
        if (!IsConfigured) throw new InvalidOperationException("A API key da Mistral não está configurada.");
        if (messages.Count == 0 || messages.Any(message => string.IsNullOrWhiteSpace(message.Content)))
            throw new ArgumentException("É necessária pelo menos uma mensagem com conteúdo.", nameof(messages));
        var settings = await settingsService.GetAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.MistralBaseUrl), "chat/completions"))
        {
            Content = JsonContent.Create(new MistralRequest(settings.MistralModel, messages.Select(message => new MistralMessage(message.Role, message.Content)).ToArray(), settings.MistralTemperature, settings.MistralMaxTokens))
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
        var content = payload.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("A Mistral não devolveu conteúdo textual.");

        return new LlmCompletionDto(content, payload.Model ?? settings.MistralModel, payload.Usage?.PromptTokens, payload.Usage?.CompletionTokens);
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
    private sealed record MistralChoice(MistralMessage Message);

    /// <summary>
    /// Represents token usage statistics from the API response.
    /// </summary>
    private sealed record MistralUsage([property: JsonPropertyName("prompt_tokens")] int PromptTokens, [property: JsonPropertyName("completion_tokens")] int CompletionTokens);
}
