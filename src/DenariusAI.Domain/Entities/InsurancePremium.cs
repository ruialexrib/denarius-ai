using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents one premium due under an insurance policy.
/// </summary>
public sealed class InsurancePremium : AuditableEntity
{
    private readonly List<InsurancePremiumAttachment> _attachments = [];

    /// <summary>Initializes an empty premium for Entity Framework Core.</summary>
    private InsurancePremium() { }

    /// <summary>Creates an insurance premium.</summary>
    /// <param name="policyId">Owning policy identifier.</param>
    /// <param name="amount">Premium amount.</param>
    /// <param name="periodStart">Covered period start.</param>
    /// <param name="periodEnd">Covered period end.</param>
    /// <param name="dueDate">Payment due date.</param>
    /// <param name="reference">Optional provider reference.</param>
    public InsurancePremium(Guid policyId, decimal amount, DateOnly periodStart, DateOnly periodEnd, DateOnly dueDate, string? reference = null)
    {
        if (policyId == Guid.Empty) throw new ArgumentException("Policy is required.", nameof(policyId));
        PolicyId = policyId;
        Update(amount, periodStart, periodEnd, dueDate, reference);
    }

    /// <summary>Gets the owning policy identifier.</summary>
    public Guid PolicyId { get; private set; }
    /// <summary>Gets the owning policy.</summary>
    public InsurancePolicy Policy { get; private set; } = null!;
    /// <summary>Gets the premium amount.</summary>
    public decimal Amount { get; private set; }
    /// <summary>Gets the covered period start.</summary>
    public DateOnly PeriodStart { get; private set; }
    /// <summary>Gets the covered period end.</summary>
    public DateOnly PeriodEnd { get; private set; }
    /// <summary>Gets the due date.</summary>
    public DateOnly DueDate { get; private set; }
    /// <summary>Gets the optional provider reference.</summary>
    public string? Reference { get; private set; }
    /// <summary>Gets the linked accounting movement identifier, when confirmed by the user.</summary>
    public Guid? JournalEntryId { get; private set; }
    /// <summary>Gets the linked accounting movement.</summary>
    public JournalEntry? JournalEntry { get; private set; }
    /// <summary>Gets the premium attachments.</summary>
    public IReadOnlyCollection<InsurancePremiumAttachment> Attachments => _attachments.AsReadOnly();
    /// <summary>Gets whether the premium is paid according to an active linked journal entry.</summary>
    public bool IsPaid => JournalEntry is { Status: JournalEntryStatus.Active };
    /// <summary>Gets the effective payment date from the linked active journal entry.</summary>
    public DateOnly? PaymentDate => IsPaid ? JournalEntry!.Date : null;

    /// <summary>Updates premium scheduling details.</summary>
    /// <param name="amount">Premium amount.</param>
    /// <param name="periodStart">Covered period start.</param>
    /// <param name="periodEnd">Covered period end.</param>
    /// <param name="dueDate">Payment due date.</param>
    /// <param name="reference">Optional provider reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    /// <exception cref="ArgumentException">Thrown when the covered period is invalid.</exception>
    public void Update(decimal amount, DateOnly periodStart, DateOnly periodEnd, DateOnly dueDate, string? reference)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Premium amount must be positive.");
        if (periodEnd < periodStart) throw new ArgumentException("Premium period end cannot precede its start.", nameof(periodEnd));
        Amount = amount; PeriodStart = periodStart; PeriodEnd = periodEnd; DueDate = dueDate; Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    /// <summary>Associates the premium with an existing accounting movement after user confirmation.</summary>
    /// <param name="journalEntryId">Accounting movement identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the identifier is empty.</exception>
    public void AssociateMovement(Guid journalEntryId)
    {
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry is required.", nameof(journalEntryId));
        JournalEntryId = journalEntryId;
    }

    /// <summary>Removes the association with an accounting movement without changing that movement.</summary>
    public void RemoveMovementAssociation() => JournalEntryId = null;
}
