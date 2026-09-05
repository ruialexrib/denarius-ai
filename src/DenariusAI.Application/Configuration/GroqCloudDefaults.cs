namespace DenariusAI.Application.Configuration;

/// <summary>Defines non-secret installation defaults for the optional GroqCloud provider.</summary>
public static class GroqCloudDefaults
{
    /// <summary>Identifies the default text generation model.</summary>
    public const string Model = "openai/gpt-oss-20b";
    /// <summary>Identifies the Groq OpenAI-compatible API root.</summary>
    public const string BaseUrl = "https://api.groq.com/openai/v1/";
    /// <summary>Limits default reasoning effort for GPT-OSS models.</summary>
    public const string ReasoningEffort = "low";
}
