using DenariusAI.Web.Models;
using DenariusAI.Application.DTOs;

namespace DenariusAI.IntegrationTests;

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

    [Fact]
    public void Render_FormatsMarkdownTables()
    {
        var html = MarkdownPreview.Render("| Categoria | Total |\n|---|---:|\n| Salário | **1 500,00 €** |");

        Assert.Contains("<table>", html);
        Assert.Contains("<th>Categoria</th>", html);
        Assert.Contains("<td><strong>1 500,00 €</strong></td>", html);
        Assert.DoesNotContain("|---|", html);
    }

    [Fact]
    public void FinancialReportPdf_GeneratesAValidPdf()
    {
        var pdf = FinancialReportPdf.Generate("# Relatório\n\n| Categoria | Total |\n|---|---:|\n| Salário | 1 500,00 € |", new DateOnly(2026, 1, 1), new DateOnly(2026, 8, 26));

        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void BudgetReportPdf_GeneratesAValidPdf()
    {
        var pdf = BudgetReportPdf.Generate(2026, 8,
        [
            new(Guid.NewGuid(), "Habitação", 780m, 780m, Guid.NewGuid(), "Despesas correntes"),
            new(Guid.NewGuid(), "Alimentação", 320m, 242m, Guid.NewGuid(), "Despesas correntes"),
            new(Guid.NewGuid(), "Sem atividade", 0m, 0m, Guid.NewGuid(), "Despesas correntes")
        ]);

        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
