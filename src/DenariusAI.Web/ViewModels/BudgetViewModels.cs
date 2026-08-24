using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed class BudgetLineFormViewModel
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinancialGroupName { get; set; } = string.Empty;
    [Range(0, double.MaxValue, ErrorMessage = "O valor orçamentado não pode ser negativo.")]
    public decimal Amount { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Actual - Amount;
    public decimal? ExecutionPercentage => Amount == 0m ? null : decimal.Round(Actual / Amount * 100m, 2);
}

public sealed class BudgetSaveViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? GroupId { get; set; }
    public string? Search { get; set; }
    public string Sort { get; set; } = "group";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public List<BudgetLineFormViewModel> Lines { get; set; } = [];
}

public sealed record BudgetIndexViewModel(
    int Year, int Month, Guid? GroupId, string? Search, string Sort,
    IReadOnlyList<BudgetLineFormViewModel> Lines,
    IReadOnlyList<SelectListItem> Years, IReadOnlyList<SelectListItem> Months,
    IReadOnlyList<SelectListItem> Groups, IReadOnlyList<SelectListItem> SortOptions,
    decimal TotalBudgeted, decimal TotalActual, PaginationViewModel Pagination)
{
    public decimal TotalVariance => TotalActual - TotalBudgeted;
    public decimal? ExecutionPercentage => TotalBudgeted == 0m ? null : decimal.Round(TotalActual / TotalBudgeted * 100m, 2);
}
