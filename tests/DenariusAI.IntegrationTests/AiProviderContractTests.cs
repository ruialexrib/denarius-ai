using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies provider-neutral routing, cancellation, and safe structured-response failure behavior offline.</summary>
public sealed class AiProviderContractTests
{
    /// <summary>Verifies that the configured provider receives the request without fallback to another adapter.</summary>
    [Fact]
    public async Task ConfiguredProviderReceivesCompletionRequest()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = "AI.Provider", Value = "Ollama" });
        await dbContext.SaveChangesAsync();
        var mistral = new StubProvider("Mistral");
        var ollama = new StubProvider("Ollama");
        var service = new ConfigurableLLMService([mistral, ollama], new StubSettings("Ollama"), dbContext);

        var completion = await service.CompleteAsync([new LlmMessageDto("user", "teste")], 321);

        Assert.Equal("ok-Ollama", completion.Content);
        Assert.Equal(0, mistral.Calls);
        Assert.Equal(1, ollama.Calls);
        Assert.Equal(321, ollama.LastMaxTokens);
    }

    /// <summary>Verifies that an unknown configured provider fails instead of silently selecting a different adapter.</summary>
    [Fact]
    public async Task UnknownProviderFailsWithoutFallback()
    {
        await using var dbContext = CreateDbContext();
        var provider = new StubProvider("Mistral");
        var service = new ConfigurableLLMService([provider], new StubSettings("Unknown"), dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync([new LlmMessageDto("user", "teste")]));

        Assert.Contains("fornecedor", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, provider.Calls);
    }

    /// <summary>Verifies that cancellation reaches the selected provider boundary.</summary>
    [Fact]
    public async Task CancellationPropagatesToProvider()
    {
        await using var dbContext = CreateDbContext();
        var provider = new StubProvider("Mistral");
        var service = new ConfigurableLLMService([provider], new StubSettings("Mistral"), dbContext);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CompleteAsync([new LlmMessageDto("user", "teste")], cancellation.Token));
    }

    /// <summary>Verifies that malformed structured model output is rejected safely and uses the configured prompt.</summary>
    [Fact]
    public async Task MalformedStructuredResponseFailsSafelyWithConfiguredPrompt()
    {
        var llm = new StubLlm("{not-json");
        var service = new InsuranceClipboardSuggestionService(llm, new StubSettings("Mistral", "Prompt efetivo de seguros"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync("Apólice 123"));

        Assert.Contains("formato inválido", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Prompt efetivo de seguros", llm.Messages![0].Content);
    }

    /// <summary>Verifies that a structured response with no editable policy fields is rejected instead of inventing data.</summary>
    [Fact]
    public async Task MissingRequiredBusinessFieldsAreRejected()
    {
        var service = new InsuranceClipboardSuggestionService(new StubLlm("{\"confidence\":\"high\",\"message\":\"Sem dados\"}"), new StubSettings("Mistral"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync("texto sem apólice"));

        Assert.Contains("não foram identificados", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Creates an isolated in-memory persistence store for provider selection tests.</summary>
    /// <returns>An isolated Denarius database context.</returns>
    private static DenariusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new DenariusDbContext(options);
    }

    /// <summary>Provides deterministic application settings to provider and prompt tests.</summary>
    private sealed class StubSettings(string provider, string insurancePrompt = "Prompt de seguros") : IApplicationSettingsService
    {
        /// <inheritdoc />
        public Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ApplicationSettingsDto(
                "mistral-test", "https://example.invalid/", 1024, 0.2, "assistant", 12, 200, 10,
                "journal", 10, "extract", "classify", InsuranceClipboardPrompt: insurancePrompt, AiProvider: provider));
        }

        /// <inheritdoc />
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Captures provider-neutral routing without performing network I/O.</summary>
    private sealed class StubProvider(string id) : ILLMProvider
    {
        /// <inheritdoc />
        public string Id { get; } = id;

        /// <summary>Gets the number of completion calls received.</summary>
        public int Calls { get; private set; }

        /// <summary>Gets the most recent output-token limit.</summary>
        public int LastMaxTokens { get; private set; }

        /// <inheritdoc />
        public LlmProviderStatus GetStatus(IReadOnlyDictionary<string, string> settings) => new(Id, "test", true);

        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastMaxTokens = maxTokens;
            return Task.FromResult(new LlmCompletionDto("ok-" + Id, "test", null, null));
        }
    }

    /// <summary>Returns deterministic structured output without external provider access.</summary>
    private sealed class StubLlm(string response) : ILLMService
    {
        /// <inheritdoc />
        public string Provider => "Test";

        /// <inheritdoc />
        public string Model => "test";

        /// <inheritdoc />
        public bool IsConfigured => true;

        /// <summary>Gets the most recent messages supplied by the workflow.</summary>
        public IReadOnlyList<LlmMessageDto>? Messages { get; private set; }

        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Messages = messages.ToList();
            return Task.FromResult(new LlmCompletionDto(response, Model, null, null));
        }
    }
}
