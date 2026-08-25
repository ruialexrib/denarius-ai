using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a financial group that categorizes and organizes financial data.
/// </summary>
public sealed class FinancialGroup : AuditableEntity
{
    /// <summary>
    /// Gets or sets the name of the financial group.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the optional description of the financial group.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the kind/type of the financial group.
    /// </summary>
    public FinancialGroupKind Kind { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this financial group is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the sort order for displaying this financial group.
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of categories associated with this financial group.
    /// </summary>
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
