using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies persisted settings and compatibility defaults.</summary>
public sealed class ApplicationSettingsServiceTests
{
    /// <summary>Verifies legacy prompts upgrade without changing custom instructions.</summary>
    [Fact]
    public async Task LegacyBehaviorPromptsAreUpgradedToTheVisibleDefaults()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        context.ApplicationSettings.AddRange(
            new() { Key = "Prompts.ReconciliationExtraction", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyReconciliationExtractionPrompt },
            new() { Key = "Prompts.ReconciliationClassification", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyReconciliationClassificationPrompt },
            new() { Key = "Prompts.DashboardWelcome", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyDashboardWelcomePrompt },
            new() { Key = "Prompts.JournalSuggestion", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyJournalSuggestionPrompt },
            new() { Key = "Prompts.InsuranceClipboard", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyInsuranceClipboardPrompt });
        await context.SaveChangesAsync();

        var loaded = await new ApplicationSettingsService(context, Options.Create(new MistralOptions())).GetAsync();

        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.ReconciliationExtractionPrompt, loaded.ReconciliationExtractionPrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.ReconciliationClassificationPrompt, loaded.ReconciliationClassificationPrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.DashboardWelcomePrompt, loaded.DashboardWelcomePrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.JournalSuggestionPrompt, loaded.JournalSuggestionSystemPrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.InsuranceClipboardPrompt, loaded.InsuranceClipboardPrompt);
    }

    /// <summary>Verifies settings persist and are immediately effective.</summary>
    [Fact]
    public async Task UpdatedSettingsArePersistedAndReadImmediately()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        var service = new ApplicationSettingsService(context, Options.Create(new MistralOptions()));
        var updated = new ApplicationSettingsDto("custom-model", "https://example.test/v1/", 500, .4, "Prompt assistant alterado", 6, 80, 4, "Prompt movimentos alterado", 3, "Prompt extração alterado", "Prompt classificação alterado", AiProvider: "Ollama", OllamaModel: "qwen3:8b", OllamaBaseUrl: "http://ollama:11434");

        await service.UpdateAsync(updated, "admin");
        var loaded = await service.GetAsync();

        Assert.Equal(updated, loaded);
        Assert.Equal(25, await context.ApplicationSettings.CountAsync());
        Assert.Equal(updated.ReconciliationExtractionPrompt, loaded.ReconciliationExtractionPrompt);
        Assert.Equal(updated.ReconciliationClassificationPrompt, loaded.ReconciliationClassificationPrompt);
        Assert.Equal(updated.FinancialAnalysisPrompt, loaded.FinancialAnalysisPrompt);
        Assert.Equal(updated.ConnectionTestPrompt, loaded.ConnectionTestPrompt);
        Assert.Equal(updated.CorrespondenceMetadataPrompt, loaded.CorrespondenceMetadataPrompt);
        Assert.Equal(updated.InsuranceClipboardPrompt, loaded.InsuranceClipboardPrompt);
        Assert.Equal(updated.SavingsCertificateClipboardPrompt, loaded.SavingsCertificateClipboardPrompt);
        Assert.Equal("Ollama", loaded.AiProvider);
        Assert.Equal("qwen3:8b", loaded.OllamaModel);
        Assert.Equal("http://ollama:11434", loaded.OllamaBaseUrl);
        Assert.Equal("AlphaVantage", loaded.MarketDataProvider);
        Assert.Equal("https://www.alphavantage.co/query", loaded.MarketDataBaseUrl);
    }
}
