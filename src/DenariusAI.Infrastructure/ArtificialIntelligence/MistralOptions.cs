namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>
/// Configuration options for the Mistral AI service.
/// </summary>
public sealed class MistralOptions
{
    /// <summary>
    /// The configuration section name for Mistral settings.
    /// </summary>
    public const string SectionName = "Mistral";

    /// <summary>
    /// Gets or sets the base URL for the Mistral AI API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1/";

    /// <summary>
    /// Gets or sets the API key for authenticating with the Mistral AI service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model to use for AI operations.
    /// </summary>
    public string Model { get; set; } = "mistral-small-2603";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the temperature parameter that controls the randomness of the model's output.
    /// Lower values make the output more focused and deterministic.
    /// </summary>
    public double Temperature { get; set; } = 0.2;
}
