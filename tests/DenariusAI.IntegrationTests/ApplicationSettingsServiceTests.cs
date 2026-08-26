using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.ArtificialIntelligence;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests;

public sealed class ApplicationSettingsServiceTests
{
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
        Assert.Equal(13, await context.ApplicationSettings.CountAsync());
        Assert.Equal(updated.ReconciliationExtractionPrompt, loaded.ReconciliationExtractionPrompt);
        Assert.Equal(updated.ReconciliationClassificationPrompt, loaded.ReconciliationClassificationPrompt);
    }
}
