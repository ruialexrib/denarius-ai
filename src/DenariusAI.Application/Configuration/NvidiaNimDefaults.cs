namespace DenariusAI.Application.Configuration;

/// <summary>Defines non-secret installation defaults for the optional NVIDIA NIM provider.</summary>
public static class NvidiaNimDefaults
{
    /// <summary>Identifies the default NVIDIA NIM text generation model.</summary>
    public const string Model = "meta/llama-3.1-8b-instruct";

    /// <summary>Identifies the NVIDIA hosted OpenAI-compatible API root.</summary>
    public const string BaseUrl = "https://integrate.api.nvidia.com/v1/";
}
