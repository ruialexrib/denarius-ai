using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a reconciliation record for a journal entry.
/// </summary>
public sealed class Reconciliation : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the associated journal entry.
    /// </summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>
    /// Gets or sets the associated journal entry.
    /// </summary>
    public JournalEntry JournalEntry { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reconciliation status.
    /// </summary>
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Unreconciled;

    /// <summary>
    /// Gets or sets the date and time when the reconciliation was completed.
    /// </summary>
    public DateTimeOffset? ReconciledAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the reconciliation.
    /// </summary>
    public string? ReconciledBy { get; set; }

    /// <summary>
    /// Marks the reconciliation as reconciled.
    /// </summary>
    /// <param name="userId">The identifier of the user performing the reconciliation.</param>
    /// <param name="reconciledAt">The date and time of reconciliation.</param>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
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
