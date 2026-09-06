using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the canonical user-facing ordering of financial categories.
/// </summary>
public sealed class CategoryDisplayOrderingTests
{
    /// <summary>
    /// Verifies that configured group and category order take precedence over alphabetical names.
    /// </summary>
    [Fact]
    public void Order_PrioritizesConfiguredGroupAndCategoryOrder()
    {
        var firstGroup = new FinancialGroupDto(Guid.NewGuid(), "Zulu", null, FinancialGroupKind.Expense, true, 10);
        var secondGroup = new FinancialGroupDto(Guid.NewGuid(), "Alpha", null, FinancialGroupKind.Expense, true, 20);
        var firstCategory = new CategoryDto(Guid.NewGuid(), firstGroup.Id, "Zulu category", null, true, 10);
        var secondCategory = new CategoryDto(Guid.NewGuid(), firstGroup.Id, "Alpha category", null, true, 20);
        var thirdCategory = new CategoryDto(Guid.NewGuid(), secondGroup.Id, "First alphabetically", null, true, 1);

        var ordered = CategoryDisplayOrdering.Order(
            [thirdCategory, secondCategory, firstCategory],
            [secondGroup, firstGroup]);

        Assert.Equal([firstCategory.Id, secondCategory.Id, thirdCategory.Id], ordered.Select(item => item.Id));
    }

    /// <summary>
    /// Verifies that category name provides deterministic ordering when configured category order is equal.
    /// </summary>
    [Fact]
    public void Order_UsesCategoryNameAsTieBreaker()
    {
        var group = new FinancialGroupDto(Guid.NewGuid(), "Despesas", null, FinancialGroupKind.Expense, true, 10);
        var alpha = new CategoryDto(Guid.NewGuid(), group.Id, "Alpha", null, true, 10);
        var zulu = new CategoryDto(Guid.NewGuid(), group.Id, "Zulu", null, true, 10);

        var ordered = CategoryDisplayOrdering.Order([zulu, alpha], [group]);

        Assert.Equal([alpha.Id, zulu.Id], ordered.Select(item => item.Id));
    }
}
