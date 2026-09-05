using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Calls a local or remote Ollama server using its non-streaming chat API.</summary>
public sealed class OllamaLLMService(HttpClient httpClient, IApplicationSettingsService settingsService, ILogger<OllamaLLMService> logger) : ILLMProvider
{
    /// <summary>Gets the stable provider registration identifier.</summary>
    public string Id => "Ollama";

    /// <summary>Resolves the effective model and validates provider configuration.</summary>
    /// <param name="settings">Persisted application settings.</param>
    /// <returns>The provider display name, model and readiness.</returns>
    public LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings)
    {
        var model = settings.GetValueOrDefault("Ollama.Model", "llama3.2");
        var baseUrl = settings.GetValueOrDefault("Ollama.BaseUrl", "http://localhost:11434");
        var configured = IsConfigured && !string.IsNullOrWhiteSpace(model)
            && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return new("Ollama", model, configured);
    }

    /// <summary>Gets whether Ollama can be used without an API credential.</summary>
    public bool IsConfigured => true;

    /// <summary>Sends chat messages to the configured Ollama <c>/api/chat</c> endpoint.</summary>
    /// <param name="messages">Messages forming the chat conversation.</param>
    /// <param name="maxTokens">Maximum number of tokens Ollama may generate.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The textual completion and token usage returned by Ollama.</returns>
    /// <exception cref="ArgumentException">Thrown when no usable messages are supplied.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the token limit is outside the supported range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Ollama is not configured or returns no textual completion.</exception>
    /// <exception cref="HttpRequestException">Thrown when Ollama returns a non-success HTTP status.</exception>
    public async Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0 || messages.Any(message => string.IsNullOrWhiteSpace(message.Content)))
            throw new ArgumentException("É necessária pelo menos uma mensagem com conteúdo.", nameof(messages));
        if (maxTokens is < 64 or > 8192)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "O limite deve estar entre 64 e 8192 tokens.");

        var settings = await settingsService.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.OllamaModel) || string.IsNullOrWhiteSpace(settings.OllamaBaseUrl))
            throw new InvalidOperationException("O Ollama não está configurado.");

        var endpoint = new Uri(new Uri(settings.OllamaBaseUrl.TrimEnd('/') + "/"), "api/chat");
        using var response = await httpClient.PostAsJsonAsync(endpoint,
            new OllamaRequest(settings.OllamaModel, messages.Select(x => new OllamaMessage(x.Role, x.Content)).ToArray(), false,
                new OllamaOptions(settings.AiTemperature, maxTokens)), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"O Ollama devolveu o estado HTTP {(int)response.StatusCode}.", null, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("O Ollama devolveu uma resposta vazia.");
        if (string.IsNullOrWhiteSpace(result.Message?.Content))
            throw new InvalidOperationException("O Ollama não devolveu conteúdo textual.");

        logger.LogInformation("Ollama completion generated with model {Model}.", result.Model ?? settings.OllamaModel);
        return new(result.Message.Content, result.Model ?? settings.OllamaModel, result.PromptEvalCount, result.EvalCount, result.DoneReason);
    }

    /// <summary>Represents the request body sent to Ollama.</summary>
    private sealed record OllamaRequest(string Model, IReadOnlyCollection<OllamaMessage> Messages, bool Stream, OllamaOptions Options);

    /// <summary>Represents one Ollama chat message.</summary>
    private sealed record OllamaMessage(string Role, string Content);

    /// <summary>Represents Ollama generation options used by Denarius AI.</summary>
    private sealed record OllamaOptions(double Temperature, [property: JsonPropertyName("num_predict")] int NumPredict);

    /// <summary>Represents the subset of an Ollama chat response consumed by Denarius AI.</summary>
    private sealed record OllamaResponse(string? Model, OllamaMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount,
        [property: JsonPropertyName("done_reason")] string? DoneReason);
}
