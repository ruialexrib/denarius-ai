using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

public sealed class Account : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType AccountType { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
