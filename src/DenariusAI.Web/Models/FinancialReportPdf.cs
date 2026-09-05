using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DenariusAI.Web.Models;

/// <summary>
/// Generates PDF documents for AI-assisted financial reports rendered from Markdown content.
/// </summary>
public static class FinancialReportPdf
{
    private const string Ink = "#17243A";
    private const string Muted = "#607086";
    private const string Accent = "#159A70";

    /// <summary>
    /// Generates a financial report PDF for the supplied period and Markdown content.
    /// </summary>
    /// <param name="markdown">The report content expressed as Markdown.</param>
    /// <param name="from">The first date in the analysed period.</param>
    /// <param name="to">The last date in the analysed period.</param>
    /// <returns>The generated PDF document bytes.</returns>
    public static byte[] Generate(string? markdown, DateOnly from, DateOnly to)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var content = MarkdownPreview.Normalize(markdown);
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(38);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink).LineHeight(1.35f));
            page.Header().PaddingBottom(18).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("DENARIUSAI").FontSize(8).Bold().FontColor(Accent).LetterSpacing(.08f);
                    column.Item().PaddingTop(3).Text("Relatório financeiro inteligente").FontSize(16).Bold().FontColor(Ink);
                });
                row.ConstantItem(155).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text("PERÍODO ANALISADO").FontSize(7).Bold().FontColor(Muted);
                    column.Item().AlignRight().PaddingTop(3).Text($"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}").FontSize(8).SemiBold();
                });
            });
            page.Content().BorderTop(1).BorderColor("#DCE5E9").PaddingTop(22).Column(column => RenderMarkdown(column, content));
            page.Footer().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Text("Gerado por IA - confirme os dados antes de tomar decisões financeiras.").FontSize(7).FontColor(Muted);
                row.ConstantItem(80).DefaultTextStyle(style => style.FontSize(7).FontColor(Muted)).AlignRight().Text(text => { text.Span("Página "); text.CurrentPageNumber(); text.Span(" de "); text.TotalPages(); });
            });
        })).GeneratePdf();
    }

    /// <summary>
    /// Renders the supported Markdown structures into a QuestPDF column.
    /// </summary>
    /// <param name="column">The target PDF column.</param>
    /// <param name="markdown">The normalized Markdown content.</param>
    private static void RenderMarkdown(ColumnDescriptor column, string markdown)
    {
        var lines = markdown.Replace("\r", string.Empty).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0) { column.Item().Height(5); continue; }
            if (index + 1 < lines.Length && IsTableRow(line) && IsTableSeparator(lines[index + 1]))
            {
                var headers = Cells(line); var rows = new List<string[]>(); index += 2;
                while (index < lines.Length && IsTableRow(lines[index])) { rows.Add(Cells(lines[index])); index++; }
                index--;
                column.Item().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns => { foreach (var _ in headers) columns.RelativeColumn(); });
                    table.Header(header => { foreach (var cell in headers) header.Cell().Background("#EDF5F2").BorderBottom(1).BorderColor("#CFE0DA").Padding(7).Text(Clean(cell)).FontSize(7).Bold().FontColor(Muted); });
                    foreach (var row in rows)
                        for (var cellIndex = 0; cellIndex < headers.Length; cellIndex++)
                            table.Cell().BorderBottom(1).BorderColor("#E8ECEF").Padding(7).Text(Clean(cellIndex < row.Length ? row[cellIndex] : string.Empty)).FontSize(7.5f);
                });
                continue;
            }
            var heading = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (heading.Success)
            {
                var level = heading.Groups[1].Value.Length;
                column.Item().PaddingTop(level == 1 ? 5 : 12).PaddingBottom(5).Text(Clean(heading.Groups[2].Value)).FontSize(level == 1 ? 18 : level == 2 ? 13 : 10.5f).Bold().FontColor(Ink);
            }
            else if (line is "---" or "***") column.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#DCE5E9");
            else if (line.StartsWith("- ") || line.StartsWith("* ") || Regex.IsMatch(line, @"^\d+\.\s+"))
            {
                var text = Regex.Replace(line, @"^(?:[-*]|\d+\.)\s+", string.Empty);
                column.Item().PaddingBottom(3).Row(row => { row.ConstantItem(13).Text("•").FontColor(Accent).Bold(); row.RelativeItem().Text(Clean(text)); });
            }
            else if (line.StartsWith("> ")) column.Item().PaddingVertical(5).BorderLeft(3).BorderColor(Accent).Background("#F1F8F5").Padding(10).Text(Clean(line[2..])).FontColor(Muted);
            else column.Item().PaddingBottom(5).Text(Clean(line));
        }
    }

    /// <summary>Removes supported Markdown formatting markers from a text value.</summary>
    /// <param name="value">The Markdown text to clean.</param>
    /// <returns>The cleaned plain text.</returns>
    private static string Clean(string value) => Regex.Replace(value, @"(?:\*\*|__|\*|_|`)", string.Empty).Trim();

    /// <summary>Determines whether a line has the pipe-delimited shape of a Markdown table row.</summary>
    /// <param name="value">The line to inspect.</param>
    /// <returns><see langword="true"/> for a table-shaped row; otherwise <see langword="false"/>.</returns>
    private static bool IsTableRow(string value) => value.Trim().Trim('|').Contains('|');

    /// <summary>Determines whether a line is a Markdown table separator.</summary>
    /// <param name="value">The line to inspect.</param>
    /// <returns><see langword="true"/> when all cells are valid separator markers; otherwise <see langword="false"/>.</returns>
    private static bool IsTableSeparator(string value) => Cells(value).All(cell => Regex.IsMatch(cell, @"^:?-{3,}:?$"));

    /// <summary>Splits a pipe-delimited table row into trimmed cells.</summary>
    /// <param name="value">The table row to split.</param>
    /// <returns>The trimmed table cells.</returns>
    private static string[] Cells(string value) => value.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
}
