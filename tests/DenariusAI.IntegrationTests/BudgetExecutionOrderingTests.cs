using DenariusAI.Application.DTOs;
using DenariusAI.Web.Models;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the canonical ordering shared by budget views and PDF reports.
/// </summary>
public sealed class BudgetExecutionOrderingTests
{
    /// <summary>
    /// Verifies that report ordering preserves the group and category sort sequence supplied by the repository.
    /// </summary>
    [Fact]
    public void ApplyReportOrder_PreservesConfiguredRepositorySequence()
    {
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var items = new[]
        {
            new BudgetExecutionItemDto(Guid.NewGuid(), "Transportes", 100m, 80m, groupB, "Despesas correntes"),
            new BudgetExecutionItemDto(Guid.NewGuid(), "Alimentação", 200m, 150m, groupB, "Despesas correntes"),
            new BudgetExecutionItemDto(Guid.NewGuid(), "Prémio", 500m, 500m, groupA, "Rendimentos extraordinários"),
            new BudgetExecutionItemDto(Guid.NewGuid(), "Salário", 2_000m, 2_000m, groupA, "Rendimentos correntes")
        };

        var ordered = BudgetExecutionOrdering.ApplyReportOrder(items);

        Assert.Equal(items, ordered);
        Assert.Equal("Transportes", ordered[0].CategoryName);
        Assert.Equal("Alimentação", ordered[1].CategoryName);
    }

    /// <summary>
    /// Verifies that a null item sequence is rejected explicitly.
    /// </summary>
    [Fact]
    public void ApplyReportOrder_NullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BudgetExecutionOrdering.ApplyReportOrder(null!));
    }
}
