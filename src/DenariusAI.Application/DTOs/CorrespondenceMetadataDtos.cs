namespace DenariusAI.Application.DTOs;

public sealed record CorrespondenceMetadataSuggestionDto(string Key, string Value, string Confidence);

public sealed record CorrespondenceMetadataSuggestionResultDto(
    IReadOnlyList<CorrespondenceMetadataSuggestionDto> Metadata,
    int ExtractedCharacters,
    int ExtractedPages);
