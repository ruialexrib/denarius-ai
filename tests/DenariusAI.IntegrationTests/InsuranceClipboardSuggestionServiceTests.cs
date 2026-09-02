using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.ArtificialIntelligence;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies structured insurance policy extraction from clipboard text.</summary>
public sealed class InsuranceClipboardSuggestionServiceTests
{
    /// <summary>Verifies the effective configured prompt and supported fields are used.</summary>
    [Fact]
    public async Task UsesConfiguredPromptAndParsesSupportedFields()
    {
        var llm = new StubLlm("""{"name":"Seguro automóvel","insurer":"Seguradora","policyNumber":"AP-42","type":"Motor","paymentFrequency":"Annual","startDate":"2026-01-02","endDate":null,"renewalDate":"2027-01-02","insuredSubject":"00-AA-00","notes":null,"confidence":"high","message":"Dados identificados."}""");
        var service = new InsuranceClipboardSuggestionService(llm, new StubSettings());

        var result = await service.SuggestAsync("Apólice AP-42 da Seguradora");

        Assert.Equal("Prompt configurado para seguros", llm.Messages![0].Content);
        Assert.Equal(InsurancePolicyType.Motor, result.Type);
        Assert.Equal(InsurancePaymentFrequency.Annual, result.PaymentFrequency);
        Assert.Equal(new DateOnly(2026, 1, 2), result.StartDate);
        Assert.Equal("high", result.Confidence);
    }

    /// <summary>Verifies invented enum values and invalid dates are omitted safely.</summary>
    [Fact]
    public async Task RejectsUnsupportedIndividualValuesWithoutForcingFields()
    {
        var service = new InsuranceClipboardSuggestionService(new StubLlm("""{"name":"Seguro","type":"Viagem","startDate":"amanhã","confidence":"low","message":"Rever."}"""), new StubSettings());

        var result = await service.SuggestAsync("Seguro de viagem");

        Assert.Null(result.Type);
        Assert.Null(result.StartDate);
        Assert.Equal("Seguro", result.Name);
    }

    /// <summary>Provides deterministic language-model output for extraction tests.</summary>
    private sealed class StubLlm(string response) : ILLMService
    {
        /// <inheritdoc />
        public string Provider => "Test";
        /// <inheritdoc />
        public string Model => "test";
        /// <inheritdoc />
        public bool IsConfigured => true;
        /// <summary>Gets the latest messages received by the stub.</summary>
        public IReadOnlyList<LlmMessageDto>? Messages { get; private set; }
        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        { Messages = messages.ToList(); return Task.FromResult(new LlmCompletionDto(response, Model, null, null)); }
    }

    /// <summary>Provides the runtime prompt required by the extraction service.</summary>
    private sealed class StubSettings : IApplicationSettingsService
    {
        /// <inheritdoc />
        public Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSettingsDto(
            "test", "https://example.test/", 2048, .2, "assistant", 12, 200, 10, "journal", 10, "extract", "classify",
            InsuranceClipboardPrompt: "Prompt configurado para seguros"));
        /// <inheritdoc />
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
