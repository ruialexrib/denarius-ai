using DenariusAI.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Provides persisted budget execution snapshots used while creating journal entries.
/// </summary>
/// <param name="budgetRepository">Repository that calculates deterministic budget execution totals.</param>
[Authorize]
[Route("JournalEntries")]
public sealed class JournalEntryBudgetExecutionController(IBudgetRepository budgetRepository) : Controller
{
    /// <summary>
    /// Returns budgeted and executed amounts by category for the explicitly selected budget.
    /// </summary>
    /// <param name="budgetId">Selected budget identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON collection containing category identifiers and persisted budget totals.</returns>
    [HttpGet("CategoryBudgetExecution")]
    public async Task<IActionResult> CategoryBudgetExecution(Guid budgetId, CancellationToken cancellationToken)
    {
        var items = await budgetRepository.GetCategoryExecutionAsync(budgetId, cancellationToken);
        return Json(items.Select(item => new
        {
            categoryId = item.CategoryId,
            budgeted = item.Budgeted,
            executed = item.Actual
        }));
    }
}
