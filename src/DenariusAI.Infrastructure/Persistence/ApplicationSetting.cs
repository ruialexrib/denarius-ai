using DenariusAI.Domain.Common;

namespace DenariusAI.Infrastructure.Persistence;

public sealed class ApplicationSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
