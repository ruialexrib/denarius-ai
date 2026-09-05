using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies savings certificate clipboard extraction at the language-model boundary.</summary>
public sealed class SavingsCertificateClipboardSuggestionServiceTests
{
    /// <summary>Verifies configured prompts and numeric JSON values are converted into a validated proposal.</summary>
    [Fact]
    public async Task SuggestAsyncUsesConfiguredPromptAndParsesNumericFields()
    {
        var llm = new StubLlm("""
            ```json
            {
              "investmentDate": "2026-08-14",
              "requestNumber": 123456,
              "series": "F",
              "product": "Certificados de Aforro Série F",
              "units": 42,
              "investmentValue": 4200.50,
              "confidence": "high",
              "message": "Dados identificados"
            }
            ```
            """);
        var service = new SavingsCertificateClipboardSuggestionService(llm, new StubSettings());

        var result = await service.SuggestAsync(" Pedido 123456 · Série F · 42 unidades · 4.200,50 € ", CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 8, 14), result.InvestmentDate);
        Assert.Equal(4200.50m, result.InvestmentValue);
        Assert.Equal(4200.50m, result.CurrentValue);
        Assert.Equal(new DateOnly(2026, 11, 14), result.NextCapitalization);
        Assert.Contains("F", result.SeriesNumber, StringComparison.Ordinal);
        Assert.Contains("Pedido 123456", result.SeriesNumber, StringComparison.Ordinal);
        Assert.Contains("42 unidades", result.Description, StringComparison.Ordinal);
        Assert.Equal("high", result.Confidence);
        Assert.Equal("Prompt configurado de certificados", llm.Messages![0].Content);
        Assert.Contains("conteúdo não fidedigno", llm.Messages[1].Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(800, llm.MaxTokens);
    }

    /// <summary>Verifies localized string amounts are accepted and non-high confidence is normalized to low.</summary>
    [Fact]
    public async Task SuggestAsyncParsesLocalizedAmountAndNormalizesConfidence()
    {
        var llm = new StubLlm("""{"investmentDate":"2026-01-31","investmentValue":"1.234,56 €","series":"F","confidence":"medium"}""");
        var service = new SavingsCertificateClipboardSuggestionService(llm, new StubSettings());

        var result = await service.SuggestAsync("certificado válido");

        Assert.Equal(1234.56m, result.InvestmentValue);
        Assert.Equal("low", result.Confidence);
        Assert.Equal(new DateOnly(2026, 4, 30), result.NextCapitalization);
    }

    /// <summary>Verifies unavailable providers and invalid clipboard sizes fail before calling the model.</summary>
    [Fact]
    public async Task SuggestAsyncRejectsUnavailableProviderAndInvalidInput()
    {
        var unavailable = new StubLlm("{}", isConfigured: false);
        var unavailableService = new SavingsCertificateClipboardSuggestionService(unavailable, new StubSettings());
        await Assert.ThrowsAsync<InvalidOperationException>(() => unavailableService.SuggestAsync("texto"));

        var available = new StubLlm("{}");
        var service = new SavingsCertificateClipboardSuggestionService(available, new StubSettings());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestAsync("   "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SuggestAsync(new string('x', 20_001)));
        Assert.Null(available.Messages);
    }

    /// <summary>Verifies malformed and empty model output is reported as a deterministic extraction error.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("não é json")]
    public async Task SuggestAsyncRejectsMalformedModelOutput(string response)
    {
        var service = new SavingsCertificateClipboardSuggestionService(new StubLlm(response), new StubSettings());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync("texto válido"));

        Assert.Contains(response.Length == 0 ? "resposta vazia" : "formato inválido", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies structurally valid output without certificate data is not accepted as a proposal.</summary>
    [Fact]
    public async Task SuggestAsyncRejectsJsonWithoutBusinessFields()
    {
        var service = new SavingsCertificateClipboardSuggestionService(new StubLlm("""{"confidence":"high","message":"sem dados"}"""), new StubSettings());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync("texto sem dados de subscrição"));

        Assert.Contains("Não foram identificados dados", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Provides deterministic language-model responses and captures the request contract.</summary>
    private sealed class StubLlm(string response, bool isConfigured = true) : ILLMService
    {
        /// <inheritdoc />
        public string Provider => "Test";

        /// <inheritdoc />
        public string Model => "test-model";

        /// <inheritdoc />
        public bool IsConfigured { get; } = isConfigured;

        /// <summary>Gets the last completion messages supplied by the service.</summary>
        public IReadOnlyList<LlmMessageDto>? Messages { get; private set; }

        /// <summary>Gets the last workflow-specific output token limit.</summary>
        public int? MaxTokens { get; private set; }

        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        {
            Messages = messages.ToList();
            return Task.FromResult(new LlmCompletionDto(response, Model, null, null));
        }

        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, int maxTokens, CancellationToken cancellationToken = default)
        {
            Messages = messages.ToList();
            MaxTokens = maxTokens;
            return Task.FromResult(new LlmCompletionDto(response, Model, null, null));
        }
    }

    /// <summary>Provides deterministic application settings for the extraction workflow.</summary>
    private sealed class StubSettings : IApplicationSettingsService
    {
        /// <inheritdoc />
        public Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSettingsDto(
            "test", "https://example.test/", 2048, .2, "assistant", 12, 200, 10, "journal", 10, "extract", "classify",
            SavingsCertificateClipboardPrompt: "Prompt configurado de certificados"));

        /// <inheritdoc />
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
