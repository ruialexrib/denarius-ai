using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a financial category that organizes accounts and transactions within a financial group.
/// </summary>
public sealed class Category : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the financial group to which this category belongs.
    /// </summary>
    public Guid FinancialGroupId { get; set; }
    
    /// <summary>
    /// Gets or sets the financial group associated with this category.
    /// </summary>
    public FinancialGroup FinancialGroup { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional description of the category.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the sort order for displaying this category.
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of accounts associated with this category.
    /// </summary>
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    
    /// <summary>
    /// Gets or sets the collection of journal entry lines associated with this category.
    /// </summary>
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
    
    /// <summary>
    /// Gets or sets the collection of budget lines associated with this category.
    /// </summary>
    public ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();
}
