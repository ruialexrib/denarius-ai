using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a journal entry in the accounting system.
/// A journal entry is a record of a financial transaction with multiple lines that must balance.
/// </summary>
public sealed class JournalEntry : AuditableEntity
{
    private readonly List<JournalEntryLine> _lines = [];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="JournalEntry"/> class.
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private JournalEntry() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="JournalEntry"/> class.
    /// </summary>
    /// <param name="date">The date of the journal entry.</param>
    /// <param name="description">The description of the journal entry.</param>
    /// <param name="reference">Optional reference number or code.</param>
    /// <param name="notes">Optional additional notes.</param>
    /// <exception cref="ArgumentException">Thrown when description is null or whitespace.</exception>
    public JournalEntry(DateOnly date, string description, string? reference = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        Date = date;
        Description = description.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
    }

    /// <summary>
    /// Gets the date of the journal entry.
    /// </summary>
    public DateOnly Date { get; private set; }
    
    /// <summary>
    /// Gets the description of the journal entry.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the reference number or code for the journal entry.
    /// </summary>
    public string? Reference { get; private set; }
    
    /// <summary>
    /// Gets additional notes for the journal entry.
    /// </summary>
    public string? Notes { get; private set; }
    
    /// <summary>
    /// Gets the status of the journal entry.
    /// </summary>
    public JournalEntryStatus Status { get; private set; } = JournalEntryStatus.Active;
    
    /// <summary>
    /// Gets the date and time when the journal entry was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }
    
    /// <summary>
    /// Gets the user ID who cancelled the journal entry.
    /// </summary>
    public string? CancelledBy { get; private set; }
    
    /// <summary>
    /// Gets the collection of journal entry lines.
    /// </summary>
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();
    
    /// <summary>
    /// Gets the reconciliation associated with this journal entry.
    /// </summary>
    public Reconciliation? Reconciliation { get; private set; }
    
    /// <summary>
    /// Gets the budget ID associated with this journal entry.
    /// </summary>
    public Guid? BudgetId { get; private set; }
    
    /// <summary>
    /// Gets the budget associated with this journal entry.
    /// </summary>
    public Budget? Budget { get; private set; }
    
    /// <summary>
    /// Gets the total debit amount across all lines.
    /// </summary>
    public decimal TotalDebit => _lines.Sum(line => line.Debit);
    
    /// <summary>
    /// Gets the total credit amount across all lines.
    /// </summary>
    public decimal TotalCredit => _lines.Sum(line => line.Credit);
    
    /// <summary>
    /// Gets the difference between total debit and total credit.
    /// Should be zero for a balanced entry.
    /// </summary>
    public decimal Difference => TotalDebit - TotalCredit;

    /// <summary>
    /// Adds a new line to the journal entry.
    /// </summary>
    /// <param name="accountId">The account ID for the line.</param>
    /// <param name="debit">The debit amount.</param>
    /// <param name="credit">The credit amount.</param>
    /// <param name="description">Optional description for the line.</param>
    /// <param name="categoryId">Optional category ID for the line.</param>
    /// <returns>The newly created journal entry line.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entry is cancelled.</exception>
    public JournalEntryLine AddLine(Guid accountId, decimal debit, decimal credit, string? description = null, Guid? categoryId = null)
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        var line = new JournalEntryLine(accountId, debit, credit, description, categoryId);
        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Updates the details of the journal entry.
    /// </summary>
    /// <param name="date">The new date.</param>
    /// <param name="description">The new description.</param>
    /// <param name="reference">The new reference.</param>
    /// <param name="notes">The new notes.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entry is cancelled.</exception>
    /// <exception cref="ArgumentException">Thrown when description is null or whitespace.</exception>
    public void UpdateDetails(DateOnly date, string description, string? reference, string? notes)
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        Date = date;
        Description = description.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
    }

    /// <summary>
    /// Clears all lines from the journal entry.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the entry is cancelled.</exception>
    public void ClearLines()
    {
        if (Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("A cancelled entry cannot be changed.");
        _lines.Clear();
    }

    /// <summary>
    /// Assigns a budget to the journal entry.
    /// </summary>
    /// <param name="budgetId">The budget ID to assign, or null to remove the association.</param>
    public void AssignBudget(Guid? budgetId) => BudgetId = budgetId;

    /// <summary>
    /// Ensures the journal entry is balanced and has at least two lines.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the entry has fewer than two lines or is not balanced.</exception>
    public void EnsureBalanced()
    {
        if (_lines.Count < 2) throw new InvalidOperationException("A journal entry must contain at least two lines.");
        if (TotalDebit != TotalCredit) throw new InvalidOperationException("Total debit must equal total credit.");
    }

    /// <summary>
    /// Cancels the journal entry.
    /// </summary>
    /// <param name="userId">The ID of the user cancelling the entry.</param>
    /// <param name="cancelledAt">The date and time of cancellation.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entry is already cancelled or is not balanced.</exception>
    /// <exception cref="ArgumentException">Thrown when userId is null or whitespace.</exception>
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
