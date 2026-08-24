using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class JournalEntryLine : AuditableEntity
{
    private JournalEntryLine() { }

    internal JournalEntryLine(Guid accountId, decimal debit, decimal credit, string? description, Guid? categoryId)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("An account is required.", nameof(accountId));
        if (debit < 0 || credit < 0) throw new ArgumentOutOfRangeException(nameof(debit), "Debit and credit cannot be negative.");
        if ((debit == 0) == (credit == 0)) throw new ArgumentException("A line must contain either a debit or a credit, but not both.");
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Description = description?.Trim();
        CategoryId = categoryId;
    }

    public Guid JournalEntryId { get; private set; }
    public JournalEntry JournalEntry { get; private set; } = null!;
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Description { get; private set; }
}
