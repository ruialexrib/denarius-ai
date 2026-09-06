using DenariusAI.Application.DTOs;
using DenariusAI.Web.Models;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the canonical ordering shared by budget views and PDF reports.
/// </summary>
public sealed class BudgetExecutionOrderingTests
{
    /// <summary>
    /// Verifies that report ordering sorts first by financial group and then by category.
    /// </summary>
    [Fact]
    public void ApplyReportOrder_OrdersByGroupThenCategory()
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

        Assert.Collection(ordered,
            item => Assert.Equal("Despesas correntes", item.FinancialGroupName),
            item => Assert.Equal("Despesas correntes", item.FinancialGroupName),
            item => Assert.Equal("Rendimentos correntes", item.FinancialGroupName),
            item => Assert.Equal("Rendimentos extraordinários", item.FinancialGroupName));
        Assert.Equal("Alimentação", ordered[0].CategoryName);
        Assert.Equal("Transportes", ordered[1].CategoryName);
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
