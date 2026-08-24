namespace DenariusAI.Infrastructure.ArtificialIntelligence;

public sealed class MistralOptions
{
    public const string SectionName = "Mistral";
    public string BaseUrl { get; set; } = "https://api.mistral.ai/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "mistral-small-latest";
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.2;
}
