using DenariusAI.Web.Models;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains tests for the MarkdownPreview type.
/// </summary>
public sealed class MarkdownPreviewTests
{
    [Theory]
    [InlineData("```markdown\n# Relatório\n\nConteúdo\n```", "# Relatório\n\nConteúdo")]
    [InlineData("```md\n# Relatório\n```", "# Relatório")]
    [InlineData("# Relatório\n\n`código`", "# Relatório\n\n`código`")]
    public void Normalize_RemovesOnlyDocumentMarkdownFence(string input, string expected)
    {
        Assert.Equal(expected, MarkdownPreview.Normalize(input));
    }

    [Fact]
    public void Render_DoesNotExposeMarkdownFenceLanguage()
    {
        var html = MarkdownPreview.Render("```markdown\n# Relatório\n```");

        Assert.Equal("<h1>Relat&#243;rio</h1>", html);
    }
}
