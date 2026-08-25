using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a budget for a specific month and year.
/// </summary>
public sealed class Budget : AuditableEntity
{
    /// <summary>
    /// Gets or sets the year of the budget.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month of the budget.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the collection of budget lines associated with this budget.
    /// </summary>
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();

    /// <summary>
    /// Gets or sets the collection of journal entries associated with this budget.
    /// </summary>
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
