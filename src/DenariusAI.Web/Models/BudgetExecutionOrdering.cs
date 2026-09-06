using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.Models;

/// <summary>
/// Provides the canonical ordering used by budget execution reports and default budget views.
/// </summary>
public static class BudgetExecutionOrdering
{
    /// <summary>
    /// Preserves the configured financial-group and category sequence supplied by the budget repository.
    /// </summary>
    /// <param name="items">The budget execution items already ordered by group and category sort order.</param>
    /// <returns>The budget execution items in their configured report sequence.</returns>
    public static IReadOnlyList<BudgetExecutionItemDto> ApplyReportOrder(IEnumerable<BudgetExecutionItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.ToList();
    }
}
