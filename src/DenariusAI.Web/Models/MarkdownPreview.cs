using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DenariusAI.Web.Models;

public static class MarkdownPreview
{
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

    public static string Render(string? markdown)
    {
        markdown = Normalize(markdown);
        if (markdown.Length == 0) return "<p>O relatório não contém conteúdo.</p>";
        var html = new StringBuilder();
        string? list = null;
        foreach (var rawLine in markdown.Replace("\r", string.Empty).Split('\n'))
        {
            var line = rawLine.Trim();
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

    private static string Inline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        encoded = Regex.Replace(encoded, @"(?<!\*)\*([^*]+)\*(?!\*)", "<em>$1</em>");
        return Regex.Replace(encoded, @"`([^`]+)`", "<code>$1</code>");
    }
}
