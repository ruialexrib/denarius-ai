namespace DenariusAI.Web.Models;

/// <summary>Defines the session contract and visibility rules for the cloud AI privacy notice.</summary>
public static class CloudAiPrivacyNoticePolicy
{
    /// <summary>Identifies the session value that keeps the notice visible until acknowledged.</summary>
    public const string SessionKey = "CloudAiPrivacyNoticePending";

    /// <summary>Determines whether the cloud AI privacy warning should be shown for the current session.</summary>
    /// <param name="aiProvider">The active AI provider.</param>
    /// <param name="sessionValue">The current acknowledgement state stored in the login session.</param>
    /// <returns><see langword="true"/> when a non-local provider is active and acknowledgement is pending.</returns>
    public static bool ShouldShow(string aiProvider, string? sessionValue) =>
        !string.Equals(aiProvider, "Ollama", StringComparison.OrdinalIgnoreCase)
        && string.Equals(sessionValue, bool.TrueString, StringComparison.Ordinal);
}
