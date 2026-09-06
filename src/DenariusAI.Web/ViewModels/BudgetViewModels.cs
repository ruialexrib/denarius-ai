using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Web.ViewModels;

public sealed class BudgetLineFormViewModel
{
    public Guid? AuditId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinancialGroupName { get; set; } = string.Empty;
    public FinancialGroupKind Kind { get; set; } = FinancialGroupKind.Expense;
    [Range(0, double.MaxValue, ErrorMessage = "O valor orçamentado não pode ser negativo.")]
    public decimal Amount { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Actual - Amount;
    public decimal? ExecutionPercentage => Amount == 0m ? null : decimal.Round(Actual / Amount * 100m, 2);
}

/// <summary>
/// Captures one editable budget line submitted by budget actions.
/// </summary>
public sealed class BudgetSaveLineViewModel
{
    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name used for action feedback.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Gets or sets the budgeted amount.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "O valor orçamentado não pode ser negativo.")]
    public decimal Amount { get; set; }
}

/// <summary>
/// Captures the editable budget page state submitted by budget actions.
/// </summary>
public sealed class BudgetSaveViewModel
{
    /// <summary>
    /// Gets or sets the selected budget year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the selected budget month.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the selected financial group filter.
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the category search text.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets whether only categories with a positive budgeted amount are displayed.
    /// </summary>
    public bool BudgetedOnly { get; set; }

    /// <summary>
    /// Gets or sets the selected sort order.
    /// </summary>
    public string Sort { get; set; } = "group";

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of rows displayed per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the budget lines submitted from the current page.
    /// </summary>
    public List<BudgetSaveLineViewModel> Lines { get; set; } = [];
}

/// <summary>
/// Provides the budget execution table, filter state, summary totals, and pagination metadata.
/// </summary>
/// <param name="Year">Selected budget year.</param>
/// <param name="Month">Selected budget month.</param>
/// <param name="GroupId">Selected financial group filter.</param>
/// <param name="Search">Category search text.</param>
/// <param name="BudgetedOnly">Whether only categories with a positive budgeted amount are displayed.</param>
/// <param name="Sort">Selected sort order.</param>
/// <param name="Lines">Budget rows on the current page.</param>
/// <param name="Years">Available budget years.</param>
/// <param name="Months">Available months.</param>
/// <param name="Groups">Available financial group filters.</param>
/// <param name="SortOptions">Available sort options.</param>
/// <param name="TotalBudgeted">Budgeted total for the filtered result set.</param>
/// <param name="TotalActual">Actual total for the filtered result set.</param>
/// <param name="Pagination">Pagination metadata for the filtered result set.</param>
public sealed record BudgetIndexViewModel(
    int Year, int Month, Guid? GroupId, string? Search, bool BudgetedOnly, string Sort,
    IReadOnlyList<BudgetLineFormViewModel> Lines,
    IReadOnlyList<SelectListItem> Years, IReadOnlyList<SelectListItem> Months,
    IReadOnlyList<SelectListItem> Groups, IReadOnlyList<SelectListItem> SortOptions,
    decimal TotalBudgeted, decimal TotalActual, PaginationViewModel Pagination)
{
    public decimal TotalVariance => TotalActual - TotalBudgeted;
    public decimal? ExecutionPercentage => TotalBudgeted == 0m ? null : decimal.Round(TotalActual / TotalBudgeted * 100m, 2);
}

public sealed record BudgetCategoryHistoryItemViewModel(int Year, int Month, decimal Budgeted, decimal Actual)
{
    public decimal Variance => Actual - Budgeted;
    public decimal? ExecutionPercentage => Budgeted == 0m ? null : decimal.Round(Actual / Budgeted * 100m, 1);
    public string Period => $"{Month:D2}/{Year}";
}

public sealed record BudgetCategoryDetailsViewModel(
    Guid CategoryId, string CategoryName, string GroupName, FinancialGroupKind Kind,
    IReadOnlyList<BudgetCategoryHistoryItemViewModel> History)
{
    public decimal TotalBudgeted => History.Sum(item => item.Budgeted);
    public decimal TotalActual => History.Sum(item => item.Actual);
    public decimal TotalVariance => TotalActual - TotalBudgeted;
}
