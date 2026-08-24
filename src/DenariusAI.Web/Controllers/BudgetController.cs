using System.Globalization;
using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class BudgetController(IBudgetService service, ILogger<BudgetController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, Guid? groupId, string? search, string sort = "group", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = month ?? DateTime.Today.Month;
        IReadOnlyList<BudgetExecutionItemDto> execution;
        try { execution = await service.GetExecutionAsync(selectedYear, selectedMonth, cancellationToken); }
        catch (ArgumentOutOfRangeException) { return BadRequest(); }
        var groups = execution.GroupBy(item => new { item.FinancialGroupId, item.FinancialGroupName }).OrderBy(group => group.Key.FinancialGroupName).ToList();
        if (groupId.HasValue) execution = execution.Where(item => item.FinancialGroupId == groupId).ToList();
        if (!string.IsNullOrWhiteSpace(search)) execution = execution.Where(item => item.CategoryName.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        execution = sort switch
        {
            "category" => execution.OrderBy(item => item.CategoryName).ToList(),
            "budgetDesc" => execution.OrderByDescending(item => item.Budgeted).ThenBy(item => item.CategoryName).ToList(),
            "actualDesc" => execution.OrderByDescending(item => item.Actual).ThenBy(item => item.CategoryName).ToList(),
            "varianceDesc" => execution.OrderByDescending(item => item.Variance).ThenBy(item => item.CategoryName).ToList(),
            _ => execution.OrderBy(item => item.FinancialGroupName).ThenBy(item => item.CategoryName).ToList()
        };
        var totalBudgeted = execution.Sum(item => item.Budgeted);
        var totalActual = execution.Sum(item => item.Actual);
        var pagination = PaginationViewModel.Create(execution.Count, page, pageSize);
        var lines = execution.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(item => new BudgetLineFormViewModel { CategoryId = item.CategoryId, CategoryName = item.CategoryName, FinancialGroupName = item.FinancialGroupName, Amount = item.Budgeted, Actual = item.Actual }).ToList();
        return View(new BudgetIndexViewModel(selectedYear, selectedMonth, groupId, search, sort, lines, YearItems(selectedYear), MonthItems(selectedMonth),
            groups.Select(group => new SelectListItem(group.Key.FinancialGroupName, group.Key.FinancialGroupId.ToString(), group.Key.FinancialGroupId == groupId)).Prepend(new SelectListItem("Todos os grupos", string.Empty, groupId is null)).ToList(), SortItems(sort), totalBudgeted, totalActual, pagination));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BudgetSaveViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Corrija os valores do orçamento."; return RedirectToIndex(model); }
        try
        {
            await service.SaveAsync(model.Year, model.Month, model.Lines.Select(line => new SaveBudgetLineDto(line.CategoryId, line.Amount)).ToList(), UserId(), cancellationToken);
            logger.LogInformation("Budget for {Year}-{Month} updated by user {UserId}.", model.Year, model.Month, UserId());
            TempData["SuccessMessage"] = "Orçamento guardado com sucesso.";
        }
        catch (ArgumentException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToIndex(model);
    }

    private IActionResult RedirectToIndex(BudgetSaveViewModel model) => RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month, groupId = model.GroupId, search = model.Search, sort = model.Sort, page = model.Page, pageSize = model.PageSize });
    private static IReadOnlyList<SelectListItem> YearItems(int selected) => Enumerable.Range(DateTime.Today.Year - 5, 11).Reverse().Select(year => new SelectListItem(year.ToString(), year.ToString(), year == selected)).ToList();
    private static IReadOnlyList<SelectListItem> MonthItems(int selected) => Enumerable.Range(1, 12).Select(month => new SelectListItem(CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(month), month.ToString(), month == selected)).ToList();
    private static IReadOnlyList<SelectListItem> SortItems(string selected) => [new("Grupo e categoria", "group", selected == "group"), new("Categoria", "category", selected == "category"), new("Maior orçamento", "budgetDesc", selected == "budgetDesc"), new("Maior realizado", "actualDesc", selected == "actualDesc"), new("Maior desvio", "varianceDesc", selected == "varianceDesc")];
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
