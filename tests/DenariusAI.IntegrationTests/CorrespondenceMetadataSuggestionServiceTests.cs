using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DenariusAI.IntegrationTests;

public sealed class CorrespondenceMetadataSuggestionServiceTests
{
    [Fact]
    public async Task UsesConfiguredPromptAndParsesStructuredMetadata()
    {
        var llm = new StubLlm("""{"metadata":[{"key":"Entidade","value":"Autoridade Tributária","confidence":"high"},{"key":"Prazo","value":"30 dias","confidence":"low"}]}""");
        var service = new CorrespondenceMetadataSuggestionService(llm, new StubSettings());

        var result = await service.SuggestAsync(CreatePdf("Entidade: Autoridade Tributaria. Prazo: 30 dias."));

        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal("Entidade", result.Metadata[0].Key);
        Assert.Equal("high", result.Metadata[0].Confidence);
        Assert.Equal("Prompt configurado de metadados", llm.Messages![0].Content);
        Assert.Contains("Autoridade", llm.Messages[1].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsMalformedModelOutput()
    {
        var service = new CorrespondenceMetadataSuggestionService(new StubLlm("não é json"), new StubSettings());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuggestAsync(CreatePdf("Documento com referencia ABC123")));
        Assert.Contains("formato inválido", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreatePdf(string text)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        page.AddText(text, 12, new PdfPoint(40, 760), font);
        return Convert.ToBase64String(builder.Build());
    }

    private sealed class StubLlm(string response) : ILLMService
    {
        public string Provider => "Test"; public string Model => "test"; public bool IsConfigured => true;
        public IReadOnlyList<LlmMessageDto>? Messages { get; private set; }
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        { Messages = messages.ToList(); return Task.FromResult(new LlmCompletionDto(response, Model, null, null)); }
    }

    private sealed class StubSettings : IApplicationSettingsService
    {
        public Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ApplicationSettingsDto(
            "test", "https://example.test/", 2048, .2, "assistant", 12, 200, 10, "journal", 10, "extract", "classify",
            CorrespondenceMetadataPrompt: "Prompt configurado de metadados"));
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
