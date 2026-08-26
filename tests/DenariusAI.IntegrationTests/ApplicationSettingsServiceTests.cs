using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

public sealed class ApplicationSettingsServiceTests
{
    [Fact]
    public async Task LegacyBehaviorPromptsAreUpgradedToTheVisibleDefaults()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        context.ApplicationSettings.AddRange(
            new() { Key = "Prompts.ReconciliationExtraction", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyReconciliationExtractionPrompt },
            new() { Key = "Prompts.ReconciliationClassification", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyReconciliationClassificationPrompt },
            new() { Key = "Prompts.DashboardWelcome", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyDashboardWelcomePrompt },
            new() { Key = "Prompts.JournalSuggestion", Value = DenariusAI.Application.Configuration.ApplicationSettingsDefaults.LegacyJournalSuggestionPrompt });
        await context.SaveChangesAsync();

        var loaded = await new ApplicationSettingsService(context, Options.Create(new MistralOptions())).GetAsync();

        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.ReconciliationExtractionPrompt, loaded.ReconciliationExtractionPrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.ReconciliationClassificationPrompt, loaded.ReconciliationClassificationPrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.DashboardWelcomePrompt, loaded.DashboardWelcomePrompt);
        Assert.Equal(DenariusAI.Application.Configuration.ApplicationSettingsDefaults.JournalSuggestionPrompt, loaded.JournalSuggestionSystemPrompt);
    }

    [Fact]
    public async Task UpdatedSettingsArePersistedAndReadImmediately()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        var service = new ApplicationSettingsService(context, Options.Create(new MistralOptions()));
        var updated = new ApplicationSettingsDto("custom-model", "https://example.test/v1/", 500, .4, "Prompt assistant alterado", 6, 80, 4, "Prompt movimentos alterado", 3, "Prompt extração alterado", "Prompt classificação alterado");

        await service.UpdateAsync(updated, "admin");
        var loaded = await service.GetAsync();

        Assert.Equal(updated, loaded);
        Assert.Equal(15, await context.ApplicationSettings.CountAsync());
        Assert.Equal(updated.ReconciliationExtractionPrompt, loaded.ReconciliationExtractionPrompt);
        Assert.Equal(updated.ReconciliationClassificationPrompt, loaded.ReconciliationClassificationPrompt);
        Assert.Equal(updated.FinancialAnalysisPrompt, loaded.FinancialAnalysisPrompt);
        Assert.Equal(updated.ConnectionTestPrompt, loaded.ConnectionTestPrompt);
    }
}
