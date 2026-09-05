using System.Net;
using System.Text;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies provider routing, configuration compatibility and structured error boundaries.</summary>
public sealed class ProviderAgnosticLlmTests
{
    /// <summary>Verifies both real adapters use common generation settings and preserve usage.</summary>
    /// <param name="provider">The adapter selected in persisted settings.</param>
    [Theory]
    [InlineData("Mistral")]
    [InlineData("ollama")]
    public async Task RoutesToSelectedAdapter(string provider)
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        await settings.UpdateAsync((await settings.GetAsync()) with { AiProvider = provider, AiMaxTokens = 512, AiTemperature = .4 }, "test");
        var mistralHandler = new Handler("""{"choices":[{"message":{"content":"ok"},"finish_reason":"stop"}],"usage":{"prompt_tokens":8,"completion_tokens":2}}""");
        var ollamaHandler = new Handler("""{"message":{"content":"ok"},"done_reason":"stop","prompt_eval_count":8,"eval_count":2}""");
        ILLMProvider[] adapters = [
            new MistralLLMService(new HttpClient(mistralHandler), Options.Create(new MistralOptions { ApiKey = "test" }), settings, NullLogger<MistralLLMService>.Instance),
            new OllamaLLMService(new HttpClient(ollamaHandler), settings, NullLogger<OllamaLLMService>.Instance)];
        var router = new ConfigurableLLMService(adapters, settings, db);

        var result = await router.CompleteAsync([new("user", "Test")]);

        Assert.True(router.IsConfigured);
        Assert.Equal("ok", result.Content);
        Assert.Equal(8, result.PromptTokens);
        Assert.Equal("stop", result.FinishReason);
        var selected = provider == "Mistral" ? mistralHandler : ollamaHandler;
        var other = provider == "Mistral" ? ollamaHandler : mistralHandler;
        Assert.Null(other.Body);
        Assert.Contains("0.4", selected.Body);
        Assert.Contains("512", selected.Body);
        Assert.Equal(provider == "Mistral" ? "mistral-small-2603" : "llama3.2", router.Model);
        await router.CompleteAsync([new("user", "Test")], 8192);
        Assert.Contains("8192", selected.Body);
    }

    /// <summary>Verifies legacy values remain effective until neutral settings are saved.</summary>
    [Fact]
    public async Task PreservesLegacyGenerationSettingsAndPrefersNewKeys()
    {
        await using var db = CreateContext();
        db.ApplicationSettings.AddRange(new() { Key = "Mistral.MaxTokens", Value = "777" }, new() { Key = "Mistral.Temperature", Value = "0.6" });
        await db.SaveChangesAsync();
        var service = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        var legacy = await service.GetAsync();
        Assert.Equal(777, legacy.AiMaxTokens);
        Assert.Equal(.6, legacy.AiTemperature);
        await service.UpdateAsync(legacy with { AiMaxTokens = 999, AiTemperature = .3 }, "test");
        var current = await service.GetAsync();
        Assert.Equal(999, current.AiMaxTokens);
        Assert.Equal(.3, current.AiTemperature);
        Assert.Equal("777", (await db.ApplicationSettings.SingleAsync(x => x.Key == "Mistral.MaxTokens")).Value);
    }

    /// <summary>Verifies unknown providers fail closed and a third adapter needs no router changes.</summary>
    [Fact]
    public async Task SupportsRegisteredProvidersWithoutFallback()
    {
        await using var db = CreateContext();
        var selection = new ApplicationSetting { Key = "AI.Provider", Value = "Unknown" };
        db.ApplicationSettings.Add(selection);
        await db.SaveChangesAsync();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        var router = new ConfigurableLLMService([new ExtraProvider()], settings, db);
        Assert.False(router.IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => router.CompleteAsync([new("user", "Test")]));
        selection.Value = "Extra";
        await db.SaveChangesAsync();
        Assert.True(router.IsConfigured);
        Assert.Equal("extra-model", router.Model);
        Assert.Equal("extra", (await router.CompleteAsync([new("user", "Test")])).Content);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => router.CompleteAsync([new("user", "Test")], cancellation.Token));
    }

    /// <summary>Verifies malformed structured suggestions use a provider-neutral error.</summary>
    [Fact]
    public async Task StructuredErrorsDoNotNameMistral()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        db.ApplicationSettings.Add(new() { Key = "AI.Provider", Value = "Extra" });
        await db.SaveChangesAsync();
        var router = new ConfigurableLLMService([new ExtraProvider()], settings, db);
        var service = new InsuranceClipboardSuggestionService(router, settings);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync("policy"));
        Assert.Contains("modelo de IA", exception.Message);
        Assert.DoesNotContain("Mistral", exception.Message);
    }

    /// <summary>Creates an isolated settings database.</summary>
    /// <returns>The disposable test context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Represents a future provider registered only in infrastructure.</summary>
    private sealed class ExtraProvider : ILLMProvider
    {
        /// <inheritdoc />
        public string Id => "Extra";
        /// <inheritdoc />
        public LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings) => new(Id, "extra-model", true);
        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new LlmCompletionDto("extra", "extra-model", null, null));
        }
    }

    /// <summary>Records outbound JSON and returns a deterministic provider response.</summary>
    /// <param name="response">The response JSON.</param>
    private sealed class Handler(string response) : HttpMessageHandler
    {
        /// <summary>Gets the latest request body, or null when not called.</summary>
        public string? Body { get; private set; }
        /// <summary>Captures a request without network access.</summary>
        /// <param name="request">The outgoing request.</param>
        /// <param name="cancellationToken">Token used to cancel body reading.</param>
        /// <returns>The configured JSON response.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
