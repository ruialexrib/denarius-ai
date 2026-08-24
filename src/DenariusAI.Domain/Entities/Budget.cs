using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class Budget : AuditableEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
