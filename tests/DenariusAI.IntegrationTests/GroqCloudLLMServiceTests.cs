using System.Net;
using System.Text;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.Configuration;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies GroqCloud transport, configuration and registration behind the application LLM boundary.</summary>
public sealed class GroqCloudLLMServiceTests
{
    private const string ValidResponse = """{"choices":[{"message":{"role":"assistant","content":"ok","reasoning":"private reasoning"},"finish_reason":"stop"}],"model":"openai/gpt-oss-20b","usage":{"prompt_tokens":12,"completion_tokens":7}}""";

    /// <summary>Verifies actual infrastructure DI exposes all three providers and routes Groq without a Mistral credential.</summary>
    [Fact]
    public async Task DependencyInjectionResolvesAndRoutesAllThreeProviders()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DenariusAIDatabase"] = "Server=unused;Database=unused;Integrated Security=True",
            ["GroqCloud:ApiKey"] = "groq-test-key",
            ["GroqCloud:Model"] = "openai/gpt-oss-20b"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.RemoveAll<DenariusDbContext>();
        var dbOptions = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        services.AddScoped(_ => new DenariusDbContext(dbOptions));
        var handler = new RecordingHandler(ValidResponse);
        services.AddHttpClient<GroqCloudLLMService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        var registered = scope.ServiceProvider.GetServices<ILLMProvider>().ToList();
        Assert.Equal(["GroqCloud", "Mistral", "Ollama"], registered.Select(item => item.Id).Order().ToArray());
        var settings = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();
        await settings.UpdateAsync((await settings.GetAsync()) with { AiProvider = "groqcloud", AiMaxTokens = 512 }, "test");
        var router = scope.ServiceProvider.GetRequiredService<ILLMService>();
        Assert.True(router.IsConfigured);
        Assert.Equal("GroqCloud", router.Provider);
        Assert.Equal(GroqCloudDefaults.Model, router.Model);
        Assert.Equal("ok", (await router.CompleteAsync([new("user", "test")])).Content);
        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", handler.Uri?.ToString());
        Assert.Equal("Bearer groq-test-key", handler.Authorization);
        Assert.Contains("\"max_completion_tokens\":512", handler.Body);
    }

    /// <summary>Verifies saved model, endpoint, temperature, reasoning and output limit control the outgoing request.</summary>
    [Fact]
    public async Task UsesEffectiveSettingsAndReturnsOnlyAnswerAndUsage()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        await settings.UpdateAsync((await settings.GetAsync()) with { AiProvider = "GroqCloud", GroqCloudModel = "openai/gpt-oss-120b",
            GroqCloudBaseUrl = "https://example.test/openai/v1/", GroqCloudReasoningEffort = "high", AiTemperature = .4 }, "test");
        var handler = new RecordingHandler(ValidResponse);
        var service = CreateService(handler, settings);
        var result = await service.CompleteAsync([new("system", "instructions"), new("user", "test")], 8192);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("openai/gpt-oss-120b", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("high", body.RootElement.GetProperty("reasoning_effort").GetString());
        Assert.Equal(8192, body.RootElement.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal(.4, body.RootElement.GetProperty("temperature").GetDouble());
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.False(body.RootElement.TryGetProperty("max_tokens", out _));
        Assert.DoesNotContain("groq-test-key", handler.Body);
        Assert.Equal("https://example.test/openai/v1/chat/completions", handler.Uri?.ToString());
        Assert.Equal("ok", result.Content);
        Assert.Equal(12, result.PromptTokens);
        Assert.Equal(7, result.CompletionTokens);
        Assert.Equal("stop", result.FinishReason);
    }

    /// <summary>Verifies unsupported model-specific reasoning options are omitted for other models.</summary>
    [Fact]
    public async Task OmitsGptOssReasoningForOtherModels()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        await settings.UpdateAsync((await settings.GetAsync()) with { GroqCloudModel = "other-model" }, "test");
        var handler = new RecordingHandler(ValidResponse);
        await CreateService(handler, settings).CompleteAsync([new("user", "test")], 1024);
        Assert.DoesNotContain("reasoning_effort", handler.Body);
    }

    /// <summary>Verifies missing Groq credentials never fall back to a different provider or send a request.</summary>
    [Fact]
    public async Task MissingCredentialIsUnavailable()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions { ApiKey = "mistral-only" }));
        var handler = new RecordingHandler(ValidResponse);
        var service = CreateService(handler, settings, string.Empty);
        Assert.False(service.GetStatus(new Dictionary<string, string>()).IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync([new("user", "test")], 1024));
        Assert.Equal(0, handler.Calls);
    }

    /// <summary>Verifies status codes remain available to bounded-context and other caller policies.</summary>
    /// <param name="status">The simulated provider failure.</param>
    [Theory]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task PreservesHttpFailureStatusWithoutExposingBody(HttpStatusCode status)
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        var handler = new RecordingHandler("sensitive response detail", status);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(handler, settings).CompleteAsync([new("user", "test")], 1024));
        Assert.Equal(status, error.StatusCode);
        Assert.DoesNotContain("sensitive", error.Message);
        Assert.Equal(1, handler.Calls);
    }

    /// <summary>Verifies invalid or empty successful responses fail safely.</summary>
    /// <param name="response">The simulated response body.</param>
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":null},\"finish_reason\":\"length\"}]}")]
    public async Task RejectsMalformedOrEmptyResponses(string response)
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(new RecordingHandler(response), settings).CompleteAsync([new("user", "test")], 1024));
    }

    /// <summary>Verifies cancellation is propagated before any provider call.</summary>
    [Fact]
    public async Task HonorsCancellation()
    {
        await using var db = CreateContext();
        var handler = new RecordingHandler(ValidResponse);
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(handler, settings).CompleteAsync([new("user", "test")], 1024, cancellation.Token));
        Assert.Equal(0, handler.Calls);
    }

    /// <summary>Verifies environment defaults remain effective until settings are explicitly saved.</summary>
    [Fact]
    public async Task PreservesGroqInstallationDefaultsAndRejectsInvalidEndpoints()
    {
        await using var db = CreateContext();
        var options = Options.Create(new GroqCloudOptions { Model = "custom-model", BaseUrl = "https://example.test/v1/", ReasoningEffort = "medium" });
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()), options);
        var initial = await settings.GetAsync();
        Assert.Equal("Mistral", initial.AiProvider);
        Assert.Equal("custom-model", initial.GroqCloudModel);
        Assert.Equal("medium", initial.GroqCloudReasoningEffort);
        foreach (var endpoint in new[] { "http://example.test/", "https://user:secret@example.test/", "https://example.test/?key=secret", "invalid" })
            await Assert.ThrowsAsync<ArgumentException>(() => settings.UpdateAsync(initial with { GroqCloudBaseUrl = endpoint }, "test"));
        await settings.UpdateAsync(initial with { AiProvider = "GroqCloud" }, "test");
        Assert.DoesNotContain(await db.ApplicationSettings.ToListAsync(), item => item.Key.Contains("ApiKey"));
        Assert.Equal("custom-model", (await settings.GetAsync()).GroqCloudModel);
    }

    /// <summary>Creates an isolated settings store.</summary>
    /// <returns>The disposable database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Builds a provider with a deterministic HTTP transport.</summary>
    /// <param name="handler">The fake HTTP transport.</param>
    /// <param name="settings">The effective settings service.</param>
    /// <param name="apiKey">The test credential.</param>
    /// <returns>The provider under test.</returns>
    private static GroqCloudLLMService CreateService(HttpMessageHandler handler, IApplicationSettingsService settings, string apiKey = "groq-test-key") =>
        new(new HttpClient(handler), Options.Create(new GroqCloudOptions { ApiKey = apiKey }), settings, NullLogger<GroqCloudLLMService>.Instance);

    /// <summary>Captures non-production requests and supplies deterministic provider responses.</summary>
    /// <param name="response">The response body.</param>
    /// <param name="status">The response status.</param>
    private sealed class RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        /// <summary>Gets the last request body.</summary>
        public string? Body { get; private set; }
        /// <summary>Gets the last endpoint.</summary>
        public Uri? Uri { get; private set; }
        /// <summary>Gets the test authorization header.</summary>
        public string? Authorization { get; private set; }
        /// <summary>Gets the number of requests.</summary>
        public int Calls { get; private set; }
        /// <summary>Records a request and returns the configured response.</summary>
        /// <param name="request">The outgoing request.</param>
        /// <param name="cancellationToken">Token used to cancel body reading.</param>
        /// <returns>The deterministic response.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            return new(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
