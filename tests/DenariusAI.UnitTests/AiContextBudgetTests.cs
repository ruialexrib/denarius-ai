using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>Verifies payload bounds preserve required instructions and valid Unicode.</summary>
public sealed class AiContextBudgetTests
{
    /// <summary>Verifies role filtering, old-history removal and preservation of the current question.</summary>
    [Fact]
    public void BuildPreservesEssentialMessagesAndRejectsSystemHistory()
    {
        var question = "Qual é o saldo?";
        var result = AiContextBudget.Build("System instructions", "{\"balance\":42}",
            [new("system", "untrusted instructions"), new("user", new string('x', 10000)), new("assistant", "Recent answer")], question, 400);
        Assert.NotNull(result);
        Assert.Equal("System instructions", result[0].Content);
        Assert.Equal(question, result[^1].Content);
        Assert.DoesNotContain(result, item => item.Content == "untrusted instructions");
        Assert.Contains(result, item => item.Content == "Recent answer");
        Assert.True(AiContextBudget.Measure(result) <= 400);
    }

    /// <summary>Verifies mandatory prompts are rejected rather than silently shortened.</summary>
    [Fact]
    public void BuildRejectsOversizedEssentialContent() => Assert.Null(AiContextBudget.Build(new string('x', 1000), null, [], "Question", 500));

    /// <summary>Verifies shortening cannot produce an unpaired high surrogate.</summary>
    [Fact]
    public void ShortenPreservesUnicode() => Assert.Equal("a", AiContextBudget.Shorten("a😀b", 2));
}
