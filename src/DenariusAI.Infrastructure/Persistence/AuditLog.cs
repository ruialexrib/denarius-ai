namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Contains definitions for AuditLog.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? RecordLabel { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ChangedColumns { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
