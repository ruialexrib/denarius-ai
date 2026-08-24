using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

public sealed class JournalEntry : AuditableEntity
{
    private readonly List<JournalEntryLine> _lines = [];
    private JournalEntry() { }

    public JournalEntry(DateOnly date, string description, string? reference = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        Date = date;
        Description = description.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
    }

    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public JournalEntryStatus Status { get; private set; } = JournalEntryStatus.Active;
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();
    public Reconciliation? Reconciliation { get; private set; }
    public Guid? BudgetId { get; private set; }
    public Budget? Budget { get; private set; }
    public decimal TotalDebit => _lines.Sum(line => line.Debit);
    public decimal TotalCredit => _lines.Sum(line => line.Credit);
    public decimal Difference => TotalDebit - TotalCredit;

    public JournalEntryLine AddLine(Guid accountId, decimal debit, decimal credit, string? description = null, Guid? categoryId = null)
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        var line = new JournalEntryLine(accountId, debit, credit, description, categoryId);
        _lines.Add(line);
        return line;
    }

    public void UpdateDetails(DateOnly date, string description, string? reference, string? notes)
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        Date = date;
        Description = description.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
    }

    public void ClearLines()
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        _lines.Clear();
    }

    public void AssignBudget(Guid? budgetId) => BudgetId = budgetId;

    public void EnsureBalanced()
    {
        if (_lines.Count < 2) throw new InvalidOperationException("A journal entry must contain at least two lines.");
        if (TotalDebit != TotalCredit) throw new InvalidOperationException("Total debit must equal total credit.");
    }

    public void Cancel(string userId, DateTimeOffset cancelledAt)
    {
        EnsureBalanced();
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("The journal entry is already cancelled.");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user is required.", nameof(userId));
        Status = JournalEntryStatus.Cancelled;
        CancelledBy = userId;
        CancelledAt = cancelledAt;
    }
}
