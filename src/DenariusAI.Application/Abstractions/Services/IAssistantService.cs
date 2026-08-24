using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface IAssistantService
{
    bool IsAvailable { get; }
    string Model { get; }
    Task<AssistantResponseDto> AskAsync(AssistantRequestDto request, CancellationToken cancellationToken = default);
}
