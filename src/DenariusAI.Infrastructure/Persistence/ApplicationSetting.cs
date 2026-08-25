using DenariusAI.Domain.Common;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Represents an application setting stored in the database.
/// </summary>
public sealed class ApplicationSetting : AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique key identifier for the setting.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the value associated with the setting key.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
