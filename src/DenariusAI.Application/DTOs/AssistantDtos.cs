namespace DenariusAI.Application.DTOs;

public sealed record AssistantChatMessageDto(string Role, string Content);
public sealed record AssistantRequestDto(string Question, IReadOnlyCollection<AssistantChatMessageDto> History);
public sealed record AssistantResponseDto(string Answer, string Model, DateOnly DataFrom, DateOnly DataTo, int TransactionCount);
