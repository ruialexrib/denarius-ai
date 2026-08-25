using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a single line item in a journal entry, recording either a debit or credit to an account.
/// </summary>
public sealed class JournalEntryLine : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JournalEntryLine"/> class.
    /// Private constructor for EF Core.
    /// </summary>
    private JournalEntryLine() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="JournalEntryLine"/> class with the specified values.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="debit">The debit amount. Must be zero if credit is non-zero.</param>
    /// <param name="credit">The credit amount. Must be zero if debit is non-zero.</param>
    /// <param name="description">Optional description for this line item.</param>
    /// <param name="categoryId">Optional category identifier for classification.</param>
    /// <exception cref="ArgumentException">Thrown when accountId is empty or when both debit and credit are zero or both are non-zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when debit or credit is negative.</exception>
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

    /// <summary>
    /// Gets the unique identifier of the journal entry this line belongs to.
    /// </summary>
    public Guid JournalEntryId { get; private set; }
    
    /// <summary>
    /// Gets the journal entry this line belongs to.
    /// </summary>
    public JournalEntry JournalEntry { get; private set; } = null!;
    
    /// <summary>
    /// Gets the unique identifier of the account affected by this line.
    /// </summary>
    public Guid AccountId { get; private set; }
    
    /// <summary>
    /// Gets the account affected by this line.
    /// </summary>
    public Account Account { get; private set; } = null!;
    
    /// <summary>
    /// Gets the optional category identifier for this line item.
    /// </summary>
    public Guid? CategoryId { get; private set; }
    
    /// <summary>
    /// Gets the optional category for this line item.
    /// </summary>
    public Category? Category { get; private set; }
    
    /// <summary>
    /// Gets the debit amount. Zero if this line represents a credit.
    /// </summary>
    public decimal Debit { get; private set; }
    
    /// <summary>
    /// Gets the credit amount. Zero if this line represents a debit.
    /// </summary>
    public decimal Credit { get; private set; }
    
    /// <summary>
    /// Gets the optional description for this line item.
    /// </summary>
    public string? Description { get; private set; }
}
