using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

public sealed class Reconciliation : AuditableEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Unreconciled;
    public DateTimeOffset? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }

    public void MarkReconciled(string userId, DateTimeOffset reconciledAt)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user is required.", nameof(userId));
        Status = ReconciliationStatus.Reconciled;
        ReconciledAt = reconciledAt;
        ReconciledBy = userId;
    }

    public void MarkUnreconciled()
    {
        Status = ReconciliationStatus.Unreconciled;
        ReconciledAt = null;
        ReconciledBy = null;
    }
}
