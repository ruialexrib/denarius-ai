namespace DenariusAI.Application.DTOs;

public sealed record JournalEntrySuggestionMessageDto(string Role, string Content);
public sealed record JournalEntrySuggestionRequestDto(string Message, IReadOnlyCollection<JournalEntrySuggestionMessageDto> History);
public sealed record SuggestedJournalEntryLineDto(Guid AccountId, Guid? CategoryId, decimal Debit, decimal Credit, string? Description);
public sealed record SuggestedJournalEntryDto(DateOnly Date, string Description, string? Reference, string? Notes, Guid? BudgetId, IReadOnlyCollection<SuggestedJournalEntryLineDto> Lines);
public sealed record JournalEntrySuggestionResultDto(bool IsComplete, string Message, SuggestedJournalEntryDto? Suggestion);
