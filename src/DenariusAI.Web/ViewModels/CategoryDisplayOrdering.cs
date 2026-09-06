using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Provides the canonical display ordering for financial categories.
/// </summary>
public static class CategoryDisplayOrdering
{
    /// <summary>
    /// Orders categories by configured financial-group order, category order, and category name.
    /// </summary>
    /// <param name="categories">The categories to order.</param>
    /// <param name="groups">The financial groups that define the parent display order.</param>
    /// <returns>The categories in canonical display order.</returns>
    public static IReadOnlyList<CategoryDto> Order(IReadOnlyList<CategoryDto> categories, IReadOnlyList<FinancialGroupDto> groups)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(groups);

        var groupOrders = groups.ToDictionary(item => item.Id, item => item.SortOrder);
        return categories
            .OrderBy(item => groupOrders.GetValueOrDefault(item.FinancialGroupId, int.MaxValue))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToList();
    }
}
