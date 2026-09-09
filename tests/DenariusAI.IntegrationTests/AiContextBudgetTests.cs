using DenariusAI.Application.Services;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies deterministic relevance matching used to select bounded AI context.</summary>
public sealed class AiContextBudgetTests
{
    /// <summary>Verifies Portuguese orthographic variants with one edit remain relevant.</summary>
    [Fact]
    public void RelevanceMatchesElectricidadeToEletricidade()
    {
        var relevance = AiContextBudget.Relevance("Eletricidade", "Paguei a electricidade ontem");

        Assert.Equal(1, relevance);
    }

    /// <summary>Verifies a small typographical error remains relevant to the intended catalog value.</summary>
    [Fact]
    public void RelevanceMatchesSmallTypographicalDifference()
    {
        var relevance = AiContextBudget.Relevance("Eletricidade", "Paguei a eletricdade ontem");

        Assert.Equal(1, relevance);
    }

    /// <summary>Verifies unrelated financial categories do not become relevant through approximate matching.</summary>
    [Fact]
    public void RelevanceRejectsUnrelatedCategory()
    {
        var relevance = AiContextBudget.Relevance("Combustível", "Paguei a electricidade ontem");

        Assert.Equal(0, relevance);
    }

    /// <summary>Verifies exact matching is preserved for short words joined by a hyphen.</summary>
    [Fact]
    public void RelevancePreservesExactHyphenatedName()
    {
        var relevance = AiContextBudget.Relevance("MB-Way", "Paguei com MB-Way");

        Assert.Equal(1, relevance);
    }
}
