using System.Net;
using System.Text;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.Configuration;
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

/// <summary>Verifies NVIDIA NIM transport, configuration and registration behind the application LLM boundary.</summary>
public sealed class NvidiaNimLLMServiceTests
{
    private const string ValidResponse = """{"choices":[{"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}],"model":"meta/llama-3.1-8b-instruct","usage":{"prompt_tokens":11,"completion_tokens":4}}""";

    /// <summary>Verifies infrastructure DI exposes NVIDIA NIM and routes it without another cloud credential.</summary>
    [Fact]
    public async Task DependencyInjectionResolvesAndRoutesNvidiaNim()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DenariusAIDatabase"] = "Server=unused;Database=unused;Integrated Security=True",
            ["NvidiaNim:ApiKey"] = "nvidia-test-key",
            ["NvidiaNim:Model"] = NvidiaNimDefaults.Model
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.RemoveAll<DenariusDbContext>();
        var dbOptions = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        services.AddScoped(_ => new DenariusDbContext(dbOptions));
        var handler = new RecordingHandler(ValidResponse);
        services.AddHttpClient<NvidiaNimLLMService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var container = services.BuildServiceProvider();
        using var scope = container.CreateScope();
        var registered = scope.ServiceProvider.GetServices<ILLMProvider>().Select(item => item.Id).Order().ToArray();
        Assert.Equal(["GroqCloud", "Mistral", "NvidiaNim", "Ollama"], registered);
        var settings = scope.ServiceProvider.GetRequiredService<IApplicationSettingsService>();
        await settings.UpdateAsync((await settings.GetAsync()) with { AiProvider = "nvidianim", AiMaxTokens = 512 }, "test");
        var router = scope.ServiceProvider.GetRequiredService<ILLMService>();
        Assert.True(router.IsConfigured);
        Assert.Equal("NvidiaNim", router.Provider);
        Assert.Equal(NvidiaNimDefaults.Model, router.Model);
        Assert.Equal("ok", (await router.CompleteAsync([new("user", "test")])).Content);
        Assert.Equal("https://integrate.api.nvidia.com/v1/chat/completions", handler.Uri?.ToString());
        Assert.Equal("Bearer nvidia-test-key", handler.Authorization);
        Assert.Contains("\"max_tokens\":512", handler.Body);
    }

    /// <summary>Verifies effective model, endpoint and provider-neutral generation settings control the request.</summary>
    [Fact]
    public async Task UsesEffectiveSettingsAndReturnsUsage()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        await settings.UpdateAsync((await settings.GetAsync()) with
        {
            NvidiaNimModel = "custom/model",
            NvidiaNimBaseUrl = "https://example.test/v1/",
            AiTemperature = .3
        }, "test");
        var handler = new RecordingHandler(ValidResponse);
        var result = await CreateService(handler, settings).CompleteAsync([new("system", "instructions"), new("user", "test")], 2048);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("custom/model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(2048, body.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.Equal(.3, body.RootElement.GetProperty("temperature").GetDouble());
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.Equal("https://example.test/v1/chat/completions", handler.Uri?.ToString());
        Assert.DoesNotContain("nvidia-test-key", handler.Body);
        Assert.Equal("ok", result.Content);
        Assert.Equal(11, result.PromptTokens);
        Assert.Equal(4, result.CompletionTokens);
        Assert.Equal("stop", result.FinishReason);
    }

    /// <summary>Verifies missing credentials make NVIDIA NIM unavailable without sending a request.</summary>
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

    /// <summary>Verifies deployment defaults are used without persisting the API key and unsafe endpoints are rejected.</summary>
    [Fact]
    public async Task PreservesInstallationDefaultsAndRejectsInvalidEndpoints()
    {
        await using var db = CreateContext();
        var options = Options.Create(new NvidiaNimOptions { Model = "custom/model", BaseUrl = "https://example.test/v1/", ApiKey = "deployment-secret" });
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()), null, options);
        var initial = await settings.GetAsync();
        Assert.Equal("custom/model", initial.NvidiaNimModel);
        Assert.Equal("https://example.test/v1/", initial.NvidiaNimBaseUrl);
        foreach (var endpoint in new[] { "http://example.test/", "https://user:secret@example.test/", "https://example.test/?key=secret", "invalid" })
            await Assert.ThrowsAsync<ArgumentException>(() => settings.UpdateAsync(initial with { NvidiaNimBaseUrl = endpoint }, "test"));
        await settings.UpdateAsync(initial with { AiProvider = "NvidiaNim" }, "test");
        Assert.DoesNotContain(await db.ApplicationSettings.ToListAsync(), item => item.Key.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies upstream failures and malformed successful responses fail without exposing response details.</summary>
    [Fact]
    public async Task FailsSafelyForUpstreamAndMalformedResponses()
    {
        await using var db = CreateContext();
        var settings = new ApplicationSettingsService(db, Options.Create(new MistralOptions()));
        var failure = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(new RecordingHandler("sensitive response detail", HttpStatusCode.TooManyRequests), settings).CompleteAsync([new("user", "test")], 1024));
        Assert.Equal(HttpStatusCode.TooManyRequests, failure.StatusCode);
        Assert.DoesNotContain("sensitive", failure.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(new RecordingHandler("not json"), settings).CompleteAsync([new("user", "test")], 1024));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(new RecordingHandler("{\"choices\":[]}"), settings).CompleteAsync([new("user", "test")], 1024));
    }

    /// <summary>Creates an isolated settings store.</summary>
    /// <returns>The disposable database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Builds a provider with a deterministic HTTP transport.</summary>
    /// <param name="handler">The fake HTTP transport.</param>
    /// <param name="settings">The effective settings service.</param>
    /// <param name="apiKey">The test credential.</param>
    /// <returns>The provider under test.</returns>
    private static NvidiaNimLLMService CreateService(HttpMessageHandler handler, IApplicationSettingsService settings, string apiKey = "nvidia-test-key") =>
        new(new HttpClient(handler), Options.Create(new NvidiaNimOptions { ApiKey = apiKey }), settings, NullLogger<NvidiaNimLLMService>.Instance);

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
