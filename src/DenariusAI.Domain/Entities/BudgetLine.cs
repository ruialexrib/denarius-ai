using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class BudgetLine : AuditableEntity
{
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal Amount { get; set; }
}
