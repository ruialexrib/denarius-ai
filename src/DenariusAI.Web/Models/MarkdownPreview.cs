using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DenariusAI.Web.Models;

/// <summary>
/// Normalizes and renders the constrained Markdown subset used by Denarius AI previews and release notes.
/// </summary>
public static class MarkdownPreview
{
    /// <summary>
    /// Removes surrounding whitespace and an optional outer Markdown code fence from supplied content.
    /// </summary>
    /// <param name="markdown">The Markdown content to normalize.</param>
    /// <returns>The normalized Markdown, or an empty string when no content is supplied.</returns>
    public static string Normalize(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        var lines = markdown.Replace("\r", string.Empty).Split('\n').ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);
        if (lines.Count >= 2 && Regex.IsMatch(lines[0].Trim(), @"^```(?:markdown|md)?\s*$", RegexOptions.IgnoreCase) && lines[^1].Trim() == "```")
        {
            lines.RemoveAt(lines.Count - 1);
            lines.RemoveAt(0);
        }
        return string.Join('\n', lines).Trim();
    }

    /// <summary>
    /// Renders the supported Markdown subset as HTML while encoding source text before inline formatting is applied.
    /// </summary>
    /// <param name="markdown">The Markdown content to render.</param>
    /// <returns>HTML suitable for the application preview surface.</returns>
    public static string Render(string? markdown)
    {
        markdown = Normalize(markdown);
        if (markdown.Length == 0) return "<p>O relatório não contém conteúdo.</p>";
        var html = new StringBuilder();
        string? list = null;
        var lines = markdown.Replace("\r", string.Empty).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (index + 1 < lines.Length && IsTableRow(line) && IsTableSeparator(lines[index + 1]))
            {
                if (list is not null) { html.Append("</").Append(list).Append('>'); list = null; }
                var headers = TableCells(line);
                html.Append("<div class=\"markdown-table-wrap\"><table><thead><tr>");
                foreach (var cell in headers) html.Append("<th>").Append(Inline(cell)).Append("</th>");
                html.Append("</tr></thead><tbody>");
                index += 2;
                while (index < lines.Length && IsTableRow(lines[index]))
                {
                    html.Append("<tr>");
                    var cells = TableCells(lines[index]);
                    for (var column = 0; column < headers.Length; column++) html.Append("<td>").Append(Inline(column < cells.Length ? cells[column] : string.Empty)).Append("</td>");
                    html.Append("</tr>");
                    index++;
                }
                html.Append("</tbody></table></div>");
                index--;
                continue;
            }
            var unordered = line.StartsWith("- ") || line.StartsWith("* ");
            var ordered = Regex.IsMatch(line, @"^\d+\.\s+");
            var wantedList = unordered ? "ul" : ordered ? "ol" : null;
            if (list is not null && list != wantedList) { html.Append("</").Append(list).Append('>'); list = null; }
            if (line.Length == 0) continue;
            if (wantedList is not null)
            {
                if (list is null) { list = wantedList; html.Append('<').Append(list).Append('>'); }
                var content = unordered ? line[2..] : Regex.Replace(line, @"^\d+\.\s+", string.Empty);
                html.Append("<li>").Append(Inline(content)).Append("</li>");
                continue;
            }
            var heading = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (heading.Success)
            {
                var level = heading.Groups[1].Value.Length;
                html.Append("<h").Append(level).Append('>').Append(Inline(heading.Groups[2].Value)).Append("</h").Append(level).Append('>');
            }
            else if (line is "---" or "***") html.Append("<hr>");
            else if (line.StartsWith("> ")) html.Append("<blockquote>").Append(Inline(line[2..])).Append("</blockquote>");
            else html.Append("<p>").Append(Inline(line)).Append("</p>");
        }
        if (list is not null) html.Append("</").Append(list).Append('>');
        return html.ToString();
    }

    /// <summary>
    /// Ensures release-note content has Markdown structure, converting plain non-empty lines to a bullet list when required.
    /// </summary>
    /// <param name="markdown">The release-note content to normalize.</param>
    /// <returns>Structured Markdown suitable for the release-note view.</returns>
    public static string NormalizeReleaseNotes(string? markdown)
    {
        markdown = Normalize(markdown);
        if (markdown.Length == 0) return "## Novidades\n\n- Atualizações e melhorias.";

        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hasMarkdownStructure = lines.Any(line =>
            Regex.IsMatch(line, @"^#{1,6}\s+")
            || Regex.IsMatch(line, @"^(?:[-*]|\d+\.)\s+")
            || line.StartsWith("> ", StringComparison.Ordinal)
            || IsTableRow(line));
        if (hasMarkdownStructure) return markdown;

        return "## Novidades\n\n" + string.Join('\n', lines.Select(line => $"- {line.TrimEnd()}"));
    }

    /// <summary>
    /// Determines whether a line contains the pipe-delimited shape used for a Markdown table row.
    /// </summary>
    /// <param name="value">The line to inspect.</param>
    /// <returns><see langword="true"/> when the line has a table-row shape; otherwise <see langword="false"/>.</returns>
    private static bool IsTableRow(string value) => value.Trim().Trim('|').Contains('|');

    /// <summary>
    /// Determines whether a line is a valid Markdown table separator.
    /// </summary>
    /// <param name="value">The separator line to inspect.</param>
    /// <returns><see langword="true"/> when all cells are separator markers; otherwise <see langword="false"/>.</returns>
    private static bool IsTableSeparator(string value)
    {
        var cells = TableCells(value);
        return cells.Length > 0 && cells.All(cell => Regex.IsMatch(cell, @"^:?-{3,}:?$"));
    }

    /// <summary>
    /// Splits a pipe-delimited Markdown table row into trimmed cells.
    /// </summary>
    /// <param name="value">The table row to split.</param>
    /// <returns>The trimmed table cells.</returns>
    private static string[] TableCells(string value) => value.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();

    /// <summary>
    /// Encodes inline text and applies the supported strong, emphasis and code formatting markers.
    /// </summary>
    /// <param name="value">The inline Markdown text to encode and format.</param>
    /// <returns>The encoded inline HTML.</returns>
    private static string Inline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        encoded = Regex.Replace(encoded, @"(?<!\*)\*([^*]+)\*(?!\*)", "<em>$1</em>");
        return Regex.Replace(encoded, @"`([^`]+)`", "<code>$1</code>");
    }
}
