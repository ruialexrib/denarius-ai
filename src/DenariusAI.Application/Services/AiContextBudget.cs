using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Bounds chat payloads while preserving system instructions and the current question.</summary>
public static class AiContextBudget
{
    private const double ApproximateMatchThreshold = 0.85;
    private static readonly char[] ExactTokenSeparators = [' ', '.', ',', ';', ':', '?', '!', '\n', '\r'];
    private static readonly char[] ApproximateTokenSeparators = [' ', '.', ',', ';', ':', '?', '!', '\n', '\r', '\t', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}'];
    private static readonly JsonSerializerOptions CompactJson = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Serializes compact context without unnecessarily escaping Portuguese text.</summary>
    /// <param name="value">The structured context.</param>
    /// <returns>Valid compact JSON.</returns>
    public static string Serialize(object value) => JsonSerializer.Serialize(value, CompactJson);

    /// <summary>Measures a conservative JSON message payload, including escaped content and roles.</summary>
    /// <param name="messages">The outgoing messages.</param>
    /// <returns>The serialized UTF-8 byte count, which is not a tokenizer count.</returns>
    public static int Measure(IReadOnlyCollection<LlmMessageDto> messages) => JsonSerializer.SerializeToUtf8Bytes(messages).Length;

    /// <summary>Builds a bounded request, removing oldest history before rejecting oversized essential data.</summary>
    /// <param name="prompt">The administrator-configured system prompt.</param>
    /// <param name="context">The context message, or null for a nonfinancial exchange.</param>
    /// <param name="history">The eligible conversation history.</param>
    /// <param name="question">The current question, preserved in full.</param>
    /// <param name="maxBytes">Maximum serialized message bytes.</param>
    /// <returns>The messages, or null if essential content exceeds the limit.</returns>
    public static List<LlmMessageDto>? Build(string prompt, string? context, IEnumerable<LlmMessageDto> history, string question, int maxBytes)
    {
        var messages = new List<LlmMessageDto> { new("system", prompt) };
        if (context is not null) messages.Add(new("user", context));
        var historyStart = messages.Count;
        messages.AddRange(history.Where(item => item.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(item.Content))
            .TakeLast(4).Select(item => new LlmMessageDto(item.Role, Shorten(item.Content, 1000))));
        messages.Add(new("user", question));
        while (Measure(messages) > maxBytes && messages.Count > historyStart + 1) messages.RemoveAt(historyStart);
        return Measure(messages) <= maxBytes ? messages : null;
    }

    /// <summary>Normalizes accents and case for deterministic context selection.</summary>
    /// <param name="text">Text to normalize.</param>
    /// <returns>Lowercase text without combining accents.</returns>
    public static string Normalize(string text) => string.Concat(text.Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();

    /// <summary>Ranks catalog values against meaningful words in the current conversation, including conservative approximate token matches.</summary>
    /// <param name="text">The candidate name or description.</param>
    /// <param name="query">The conversation used for context selection.</param>
    /// <returns>The count of exact, contained, or highly similar words with at least four characters.</returns>
    public static int Relevance(string? text, string query)
    {
        var normalizedText = Normalize(text ?? string.Empty);
        var candidateTokens = Tokenize(normalizedText, ApproximateTokenSeparators).Where(word => word.Length >= 4).Distinct().ToArray();
        return Tokenize(query, ExactTokenSeparators).Where(word => word.Length >= 4).Distinct().Count(word =>
        {
            if (normalizedText.Contains(word, StringComparison.Ordinal)) return true;
            return Tokenize(word, ApproximateTokenSeparators).Where(token => token.Length >= 4)
                .Any(token => candidateTokens.Any(candidate => IsApproximateMatch(token, candidate)));
        });
    }

    /// <summary>Bounds a descriptive field without cutting a UTF-16 surrogate pair.</summary>
    /// <param name="text">The source text.</param>
    /// <param name="length">The maximum character count.</param>
    /// <returns>The original or shortened text.</returns>
    public static string Shorten(string text, int length)
    {
        if (text.Length <= length) return text;
        var end = char.IsHighSurrogate(text[length - 1]) ? length - 1 : length;
        return text[..end];
    }

    /// <summary>Splits normalized text into deterministic tokens for relevance comparison.</summary>
    /// <param name="text">The text to tokenize.</param>
    /// <param name="separators">The separators to use for the requested matching mode.</param>
    /// <returns>The normalized non-empty tokens.</returns>
    private static IEnumerable<string> Tokenize(string text, char[] separators) => Normalize(text).Split(separators, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Determines whether two tokens are sufficiently similar using normalized Levenshtein distance.</summary>
    /// <param name="left">The first normalized token.</param>
    /// <param name="right">The second normalized token.</param>
    /// <returns><see langword="true"/> when similarity is at least the configured threshold; otherwise <see langword="false"/>.</returns>
    private static bool IsApproximateMatch(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.Ordinal)) return true;
        var maxLength = Math.Max(left.Length, right.Length);
        if (maxLength == 0) return true;
        var maximumAllowedDistance = (int)Math.Floor(maxLength * (1 - ApproximateMatchThreshold));
        if (maximumAllowedDistance == 0 || Math.Abs(left.Length - right.Length) > maximumAllowedDistance) return false;
        return LevenshteinDistance(left, right, maximumAllowedDistance) <= maximumAllowedDistance;
    }

    /// <summary>Calculates Levenshtein edit distance and stops early when the configured match distance cannot be met.</summary>
    /// <param name="left">The first token.</param>
    /// <param name="right">The second token.</param>
    /// <param name="maximumDistance">The maximum distance that is useful to the caller.</param>
    /// <returns>The edit distance, or a value greater than <paramref name="maximumDistance"/> when the threshold is exceeded.</returns>
    private static int LevenshteinDistance(string left, string right, int maximumDistance)
    {
        if (left.Length == 0) return right.Length;
        if (right.Length == 0) return left.Length;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var column = 0; column <= right.Length; column++) previous[column] = column;

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            var rowMinimum = current[0];
            for (var column = 1; column <= right.Length; column++)
            {
                var substitutionCost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitutionCost);
                rowMinimum = Math.Min(rowMinimum, current[column]);
            }

            if (rowMinimum > maximumDistance) return maximumDistance + 1;
            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
