using DenariusAI.Application.Configuration;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Contains GroqCloud installation defaults and the deployment-only API credential.</summary>
public sealed class GroqCloudOptions
{
    /// <summary>Identifies the configuration section.</summary>
    public const string SectionName = "GroqCloud";
    /// <summary>Gets or sets the credential supplied by environment configuration or user secrets.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the model used when no database override exists.</summary>
    public string Model { get; set; } = GroqCloudDefaults.Model;
    /// <summary>Gets or sets the API root used when no database override exists.</summary>
    public string BaseUrl { get; set; } = GroqCloudDefaults.BaseUrl;
    /// <summary>Gets or sets the GPT-OSS reasoning effort used when no database override exists.</summary>
    public string ReasoningEffort { get; set; } = GroqCloudDefaults.ReasoningEffort;
}
