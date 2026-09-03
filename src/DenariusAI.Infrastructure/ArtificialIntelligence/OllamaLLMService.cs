using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Calls a local or remote Ollama server using its chat API.</summary>
public sealed class OllamaLLMService(HttpClient httpClient, IApplicationSettingsService settingsService, ILogger<OllamaLLMService> logger)
{
    /// <summary>Gets whether Ollama can be used without an API credential.</summary>
    public bool IsConfigured => true;

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
                new OllamaOptions(settings.MistralTemperature, maxTokens)), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"O Ollama devolveu o estado HTTP {(int)response.StatusCode}.", null, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("O Ollama devolveu uma resposta vazia.");
        if (string.IsNullOrWhiteSpace(result.Message?.Content))
            throw new InvalidOperationException("O Ollama não devolveu conteúdo textual.");

        logger.LogInformation("Ollama completion generated with model {Model}.", result.Model ?? settings.OllamaModel);
        return new(result.Message.Content, result.Model ?? settings.OllamaModel, result.PromptEvalCount, result.EvalCount, result.DoneReason);
    }

    private sealed record OllamaRequest(string Model, IReadOnlyCollection<OllamaMessage> Messages, bool Stream, OllamaOptions Options);
    private sealed record OllamaMessage(string Role, string Content);
    private sealed record OllamaOptions(double Temperature, [property: JsonPropertyName("num_predict")] int NumPredict);
    private sealed record OllamaResponse(string? Model, OllamaMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount,
        [property: JsonPropertyName("done_reason")] string? DoneReason);
}
