namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents a message in a Large Language Model conversation.
/// </summary>
/// <param name="Role">The role of the message sender (e.g., "user", "assistant", "system").</param>
/// <param name="Content">The content of the message.</param>
public sealed record LlmMessageDto(string Role, string Content);

/// <summary>
/// Represents the completion response from a Large Language Model.
/// </summary>
/// <param name="Content">The generated content from the LLM.</param>
/// <param name="Model">The name or identifier of the LLM model used.</param>
/// <param name="PromptTokens">The number of tokens used in the prompt, if available.</param>
/// <param name="CompletionTokens">The number of tokens generated in the completion, if available.</param>
public sealed record LlmCompletionDto(string Content, string Model, int? PromptTokens, int? CompletionTokens);
