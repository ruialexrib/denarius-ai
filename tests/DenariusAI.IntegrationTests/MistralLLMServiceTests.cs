using System.Net;
using System.Text;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for MistralLLMServiceTests.
/// </summary>
public sealed class MistralLLMServiceTests
{
    [Fact]
    public async Task CompleteAsyncUsesConfiguredModelAndParsesUsage()
    {
        var handler = new RecordingHandler("""{"choices":[{"message":{"role":"assistant","content":"Ligação confirmada"}}],"model":"mistral-small-latest","usage":{"prompt_tokens":8,"completion_tokens":3}}""");
        var service = CreateService(handler);

        var result = await service.CompleteAsync([new LlmMessageDto("user", "Teste")]);

        Assert.Equal("Ligação confirmada", result.Content);
        Assert.Equal("mistral-small-latest", result.Model);
        Assert.Equal(8, result.PromptTokens);
        Assert.Equal("Bearer", handler.Request?.Headers.Authorization?.Scheme);
        Assert.Contains("mistral-small-latest", handler.Body);
    }

    [Fact]
    public async Task CompleteAsyncRejectsMissingApiKey()
    {
        var service = CreateService(new RecordingHandler("{}"), apiKey: string.Empty);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync([new("user", "Teste")]));
    }

    private static MistralLLMService CreateService(HttpMessageHandler handler, string apiKey = "test-key") => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.mistral.ai/v1/") },
        Options.Create(new MistralOptions { ApiKey = apiKey, Model = "mistral-small-latest" }), new TestSettings(),
        NullLogger<MistralLLMService>.Instance);

    private sealed class TestSettings : DenariusAI.Application.Abstractions.Services.IApplicationSettingsService
    {
        public Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSettingsDto("mistral-small-latest", "https://api.mistral.ai/v1/", 1024, .2, "assistant", 12, 200, 10, "suggestion", 10, "Prompt de extração", "Prompt de classificação"));
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
