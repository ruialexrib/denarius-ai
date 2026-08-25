namespace DenariusAI.Domain.Enums;

/// <summary>
/// Represents the status of a journal entry in the accounting system.
/// </summary>
public enum JournalEntryStatus
{
    /// <summary>
    /// Indicates that the journal entry is active and valid.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Indicates that the journal entry has been cancelled and is no longer valid.
    /// </summary>
    Cancelled = 2
}
