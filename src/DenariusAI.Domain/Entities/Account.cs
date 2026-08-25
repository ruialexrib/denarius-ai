using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a financial account in the system.
/// </summary>
public sealed class Account : AuditableEntity
{
    /// <summary>
    /// Gets or sets the name of the account.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional description of the account.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the account (e.g., Asset, Liability, Equity, Revenue, Expense).
    /// </summary>
    public AccountType AccountType { get; set; }
    
    /// <summary>
    /// Gets or sets the initial balance of the account.
    /// </summary>
    public decimal InitialBalance { get; set; }
    
    /// <summary>
    /// Gets or sets the currency code for the account. Default is "EUR".
    /// </summary>
    public string Currency { get; set; } = "EUR";
    
    /// <summary>
    /// Gets or sets a value indicating whether the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the optional category identifier for the account.
    /// </summary>
    public Guid? CategoryId { get; set; }
    
    /// <summary>
    /// Gets or sets the category associated with the account.
    /// </summary>
    public Category? Category { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of journal entry lines associated with this account.
    /// </summary>
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
