namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents a message in the journal entry suggestion conversation history.
/// </summary>
/// <param name="Role">The role of the message sender (e.g., "user", "assistant").</param>
/// <param name="Content">The content of the message.</param>
public sealed record JournalEntrySuggestionMessageDto(string Role, string Content);

/// <summary>
/// Represents a request for journal entry suggestions.
/// </summary>
/// <param name="Message">The user's current message or query.</param>
/// <param name="History">The conversation history containing previous messages.</param>
public sealed record JournalEntrySuggestionRequestDto(string Message, IReadOnlyCollection<JournalEntrySuggestionMessageDto> History);

/// <summary>
/// Represents a single line item in a suggested journal entry.
/// </summary>
/// <param name="AccountId">The unique identifier of the account for this line.</param>
/// <param name="CategoryId">The optional unique identifier of the category for this line.</param>
/// <param name="Debit">The debit amount for this line.</param>
/// <param name="Credit">The credit amount for this line.</param>
/// <param name="Description">An optional description for this line item.</param>
public sealed record SuggestedJournalEntryLineDto(Guid AccountId, Guid? CategoryId, decimal Debit, decimal Credit, string? Description);

/// <summary>
/// Represents a complete suggested journal entry with all its details.
/// </summary>
/// <param name="Date">The date of the journal entry.</param>
/// <param name="Description">The description of the journal entry.</param>
/// <param name="Reference">An optional reference number or identifier for the entry.</param>
/// <param name="Notes">Optional additional notes about the entry.</param>
/// <param name="BudgetId">The optional unique identifier of the associated budget.</param>
/// <param name="Lines">The collection of line items for this journal entry.</param>
public sealed record SuggestedJournalEntryDto(DateOnly Date, string Description, string? Reference, string? Notes, Guid? BudgetId, IReadOnlyCollection<SuggestedJournalEntryLineDto> Lines);

/// <summary>
/// Represents the result of a journal entry suggestion request.
/// </summary>
/// <param name="IsComplete">Indicates whether the suggestion is complete and ready to be used.</param>
/// <param name="Message">A message from the AI assistant to the user.</param>
/// <param name="ClassificationExplanation">An optional explanation of how the entry was classified.</param>
/// <param name="Suggestion">The suggested journal entry, if available.</param>
public sealed record JournalEntrySuggestionResultDto(bool IsComplete, string Message, string? ClassificationExplanation, SuggestedJournalEntryDto? Suggestion);
