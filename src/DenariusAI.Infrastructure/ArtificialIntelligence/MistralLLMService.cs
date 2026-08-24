using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

public sealed class MistralLLMService(HttpClient httpClient, IOptions<MistralOptions> options, IApplicationSettingsService settingsService, ILogger<MistralLLMService> logger) : ILLMService
{
    private readonly MistralOptions _options = options.Value;
    public string Provider => "Mistral AI";
    public string Model => _options.Model;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

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

    private sealed record MistralRequest(string Model, IReadOnlyCollection<MistralMessage> Messages, [property: JsonPropertyName("temperature")] double Temperature, [property: JsonPropertyName("max_tokens")] int MaxTokens);
    private sealed record MistralMessage(string Role, string Content);
    private sealed record MistralResponse(IReadOnlyCollection<MistralChoice> Choices, string? Model, MistralUsage? Usage);
    private sealed record MistralChoice(MistralMessage Message);
    private sealed record MistralUsage([property: JsonPropertyName("prompt_tokens")] int PromptTokens, [property: JsonPropertyName("completion_tokens")] int CompletionTokens);
}
