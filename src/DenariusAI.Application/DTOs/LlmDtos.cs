namespace DenariusAI.Application.DTOs;

public sealed record LlmMessageDto(string Role, string Content);

public sealed record LlmCompletionDto(string Content, string Model, int? PromptTokens, int? CompletionTokens);
