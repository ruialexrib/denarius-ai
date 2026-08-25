using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a line item within a budget, associating a category with a specific allocated amount.
/// </summary>
public sealed class BudgetLine : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the budget this line belongs to.
    /// </summary>
    public Guid BudgetId { get; set; }
    
    /// <summary>
    /// Gets or sets the budget associated with this budget line.
    /// </summary>
    public Budget Budget { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the identifier of the category for this budget line.
    /// </summary>
    public Guid CategoryId { get; set; }
    
    /// <summary>
    /// Gets or sets the category associated with this budget line.
    /// </summary>
    public Category Category { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the monetary amount allocated for this budget line.
    /// </summary>
    public decimal Amount { get; set; }
}
