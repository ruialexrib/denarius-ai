using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.Models;

/// <summary>
/// Provides the canonical ordering used by budget execution reports and default budget views.
/// </summary>
public static class BudgetExecutionOrdering
{
    /// <summary>
    /// Orders budget execution items using the canonical report sequence.
    /// </summary>
    /// <param name="items">The budget execution items to order.</param>
    /// <returns>The ordered budget execution items.</returns>
    public static IReadOnlyList<BudgetExecutionItemDto> ApplyReportOrder(IEnumerable<BudgetExecutionItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .OrderBy(item => item.FinancialGroupName)
            .ThenBy(item => item.CategoryName)
            .ToList();
    }
}
