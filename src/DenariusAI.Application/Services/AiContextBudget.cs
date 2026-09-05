using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Bounds chat payloads while preserving system instructions and the current question.</summary>
public static class AiContextBudget
{
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

    /// <summary>Ranks catalog values against meaningful words in the current conversation.</summary>
    /// <param name="text">The candidate name or description.</param>
    /// <param name="query">The conversation used for context selection.</param>
    /// <returns>The count of matching words with at least four characters.</returns>
    public static int Relevance(string? text, string query)
    {
        var normalized = Normalize(text ?? string.Empty);
        return Normalize(query).Split([' ', '.', ',', ';', ':', '?', '!', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 4).Distinct().Count(word => normalized.Contains(word, StringComparison.Ordinal));
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
}
