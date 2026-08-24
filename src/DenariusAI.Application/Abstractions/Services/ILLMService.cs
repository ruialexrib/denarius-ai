using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface ILLMService
{
    string Provider { get; }
    string Model { get; }
    bool IsConfigured { get; }
    Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default);
}
