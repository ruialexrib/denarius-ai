using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class Category : AuditableEntity
{
    public Guid FinancialGroupId { get; set; }
    public FinancialGroup FinancialGroup { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
}
