namespace DenariusAI.Domain.Common;

/// <summary>
/// Base class for entities that require audit tracking.
/// Provides common properties for tracking entity creation and modification.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// Automatically initialized with a new GUID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// Automatically initialized with the current UTC date and time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
