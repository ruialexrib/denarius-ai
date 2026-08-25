namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents a chat message in the assistant conversation.
/// </summary>
/// <param name="Role">The role of the message sender (e.g., "user", "assistant", "system").</param>
/// <param name="Content">The content of the message.</param>
public sealed record AssistantChatMessageDto(string Role, string Content);

/// <summary>
/// Represents a request to the assistant.
/// </summary>
/// <param name="Question">The user's question or prompt.</param>
/// <param name="History">The collection of previous chat messages in the conversation.</param>
public sealed record AssistantRequestDto(string Question, IReadOnlyCollection<AssistantChatMessageDto> History);

/// <summary>
/// Represents the response from the assistant.
/// </summary>
/// <param name="Answer">The assistant's answer to the user's question.</param>
/// <param name="Model">The AI model used to generate the response.</param>
/// <param name="DataFrom">The start date of the data range used for the response.</param>
/// <param name="DataTo">The end date of the data range used for the response.</param>
/// <param name="TransactionCount">The number of transactions analyzed in the response.</param>
public sealed record AssistantResponseDto(string Answer, string Model, DateOnly DataFrom, DateOnly DataTo, int TransactionCount);
