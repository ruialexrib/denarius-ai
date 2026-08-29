using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface ICorrespondenceMetadataSuggestionService
{
    Task<CorrespondenceMetadataSuggestionResultDto> SuggestAsync(
        string pdfBase64,
        CancellationToken cancellationToken = default);
}
