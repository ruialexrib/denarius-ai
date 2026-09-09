using DenariusAI.Application.Configuration;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Contains NVIDIA NIM installation defaults and the deployment-only API credential.</summary>
public sealed class NvidiaNimOptions
{
    /// <summary>Identifies the configuration section.</summary>
    public const string SectionName = "NvidiaNim";

    /// <summary>Gets or sets the credential supplied by environment configuration or user secrets.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the model used when no database override exists.</summary>
    public string Model { get; set; } = NvidiaNimDefaults.Model;

    /// <summary>Gets or sets the API root used when no database override exists.</summary>
    public string BaseUrl { get; set; } = NvidiaNimDefaults.BaseUrl;
}
