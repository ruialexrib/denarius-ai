using System.Globalization;
using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using DenariusAI.Web.Models;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Handles monthly budget planning, maintenance, and execution workflows.
/// </summary>
[Authorize]
public sealed class BudgetController(IBudgetService service, DenariusDbContext dbContext, ILogger<BudgetController> logger) : Controller
{
    /// <summary>
    /// Exports the complete budget for the selected period as a PDF report.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportPdf(int year, int month, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BudgetExecutionItemDto> execution;
        try { execution = await service.GetExecutionAsync(year, month, cancellationToken); }
        catch (ArgumentOutOfRangeException) { return BadRequest(); }
        var pdf = BudgetReportPdf.Generate(year, month, execution);
        return File(pdf, "application/pdf", $"orcamento-{year:D4}-{month:D2}.pdf");
    }

    /// <summary>
    /// Displays the budget execution index page with filtering, sorting, and pagination capabilities.
    /// </summary>
    /// <param name="year">The year to display the budget for. Defaults to current year.</param>
    /// <param name="month">The month to display the budget for. Defaults to current month.</param>
    /// <param name="groupId">Optional financial group filter.</param>
    /// <param name="search">Optional search term to filter categories by name.</param>
    /// <param name="sort">Sort order for the results. Defaults to the canonical report order.</param>
    /// <param name="page">Current page number for pagination. Default is 1.</param>
    /// <param name="pageSize">Number of items per page. Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget index view with execution data.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, Guid? groupId, string? search, string sort = "report", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
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
            "group" => execution.OrderBy(item => item.FinancialGroupName).ThenBy(item => item.CategoryName).ToList(),
            "category" => execution.OrderBy(item => item.CategoryName).ToList(),
            "budgetDesc" => execution.OrderByDescending(item => item.Budgeted).ThenBy(item => item.CategoryName).ToList(),
            "actualDesc" => execution.OrderByDescending(item => item.Actual).ThenBy(item => item.CategoryName).ToList(),
            "varianceDesc" => execution.OrderByDescending(item => item.Variance).ThenBy(item => item.CategoryName).ToList(),
            _ => BudgetExecutionOrdering.ApplyReportOrder(execution)
        };
        var totalBudgeted = execution.Sum(item => item.Budgeted);
        var totalActual = execution.Sum(item => item.Actual);
        var pagination = PaginationViewModel.Create(execution.Count, page, pageSize);
        var auditIds = await dbContext.BudgetLines.AsNoTracking()
            .Where(line => line.Budget.Year == selectedYear && line.Budget.Month == selectedMonth)
            .ToDictionaryAsync(line => line.CategoryId, line => line.Id, cancellationToken);
        var lines = execution.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(item => new BudgetLineFormViewModel { AuditId = auditIds.GetValueOrDefault(item.CategoryId), CategoryId = item.CategoryId, CategoryName = item.CategoryName, FinancialGroupName = item.FinancialGroupName, Kind = FinancialGroupKind.Expense, Amount = item.Budgeted, Actual = item.Actual }).ToList();
        return View(new BudgetIndexViewModel(selectedYear, selectedMonth, groupId, search, sort, lines, YearItems(selectedYear), MonthItems(selectedMonth),
            groups.Select(group => new SelectListItem(group.Key.FinancialGroupName, group.Key.FinancialGroupId.ToString(), group.Key.FinancialGroupId == groupId)).Prepend(new SelectListItem("Todos os grupos", string.Empty, groupId is null)).ToList(), SortItems(sort), totalBudgeted, totalActual, pagination));
    }

    /// <summary>
    /// Displays the historical budget and actual data for a specific category.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category details view with historical data, or NotFound if category doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Category(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.Id, item.Name, GroupName = item.FinancialGroup.Name, Kind = item.FinancialGroup.Kind })
            .SingleOrDefaultAsync(cancellationToken);
        if (category is null) return NotFound();

        var history = await dbContext.Budgets.AsNoTracking()
            .OrderBy(item => item.Year).ThenBy(item => item.Month)
            .Select(budget => new BudgetCategoryHistoryItemViewModel(
                budget.Year, budget.Month,
                budget.Lines.Where(line => line.CategoryId == id).Sum(line => (decimal?)line.Amount) ?? 0m,
                budget.JournalEntries.Where(entry => entry.Status == JournalEntryStatus.Active)
                    .SelectMany(entry => entry.Lines)
                    .Where(line => line.CategoryId == id || (line.CategoryId == null && line.Account.CategoryId == id))
                    .Sum(line => (decimal?)(category.Kind == FinancialGroupKind.Income ? line.Credit - line.Debit : line.Debit - line.Credit)) ?? 0m))
            .ToListAsync(cancellationToken);

        return View("Category", new BudgetCategoryDetailsViewModel(category.Id, category.Name, category.GroupName, category.Kind, history));
    }

    /// <summary>
    /// Saves budget line items for a specific year and month.
    /// </summary>
    /// <param name="model">The budget save view model containing the lines to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to the index page with success or error message.</returns>
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

    /// <summary>
    /// Copies a specific budget line amount forward from the current month to December of the same year.
    /// </summary>
    /// <param name="model">The budget save view model containing the current budget state.</param>
    /// <param name="categoryId">The identifier of the category to copy forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to the index page with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyLineForward(BudgetSaveViewModel model, Guid categoryId, CancellationToken cancellationToken)
    {
        var line = model.Lines.SingleOrDefault(item => item.CategoryId == categoryId);
        if (line is null || line.Amount < 0m) { TempData["ErrorMessage"] = "Não foi possível identificar a linha a copiar."; return RedirectToIndex(model); }
        try
        {
            for (var month = model.Month; month <= 12; month++)
                await service.SaveAsync(model.Year, month, [new SaveBudgetLineDto(categoryId, line.Amount)], UserId(), cancellationToken);
            TempData["SuccessMessage"] = $"O valor de {line.CategoryName} foi aplicado de {MonthName(model.Month)} até dezembro.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToIndex(model);
    }

    /// <summary>
    /// Copies the entire budget from the selected month to the next month.
    /// </summary>
    /// <param name="model">The budget save view model containing the source budget.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to the index page with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyToNextMonth(BudgetSaveViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            if (model.Lines.Count > 0)
                await service.SaveAsync(model.Year, model.Month, model.Lines.Select(line => new SaveBudgetLineDto(line.CategoryId, line.Amount)).ToList(), UserId(), cancellationToken);
            var source = await dbContext.BudgetLines.AsNoTracking()
                .Where(line => line.Budget.Year == model.Year && line.Budget.Month == model.Month)
                .Select(line => new SaveBudgetLineDto(line.CategoryId, line.Amount)).ToListAsync(cancellationToken);
            if (source.Count == 0) throw new InvalidOperationException("O orçamento selecionado não tem valores para copiar.");
            var next = new DateTime(model.Year, model.Month, 1).AddMonths(1);
            await service.SaveAsync(next.Year, next.Month, source, UserId(), cancellationToken);
            TempData["SuccessMessage"] = $"Orçamento copiado para {MonthName(next.Month)} de {next.Year}.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToIndex(model);
    }

    /// <summary>
    /// Redirects to the index action with the specified model parameters.
    /// </summary>
    /// <param name="model">The budget save view model containing the navigation state.</param>
    /// <returns>A redirect result to the index action.</returns>
    private IActionResult RedirectToIndex(BudgetSaveViewModel model) => RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month, groupId = model.GroupId, search = model.Search, sort = model.Sort, page = model.Page, pageSize = model.PageSize });

    /// <summary>
    /// Generates a list of year select items for the dropdown, centered around the current year.
    /// </summary>
    /// <param name="selected">The currently selected year.</param>
    /// <returns>A list of select items for years.</returns>
    private static IReadOnlyList<SelectListItem> YearItems(int selected) => Enumerable.Range(DateTime.Today.Year - 5, 11).Reverse().Select(year => new SelectListItem(year.ToString(), year.ToString(), year == selected)).ToList();

    /// <summary>
    /// Generates a list of month select items for the dropdown.
    /// </summary>
    /// <param name="selected">The currently selected month.</param>
    /// <returns>A list of select items for months.</returns>
    private static IReadOnlyList<SelectListItem> MonthItems(int selected) => Enumerable.Range(1, 12).Select(month => new SelectListItem(CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(month), month.ToString(), month == selected)).ToList();

    /// <summary>
    /// Gets the localized name of a month.
    /// </summary>
    /// <param name="month">The month number (1-12).</param>
    /// <returns>The localized month name.</returns>
    private static string MonthName(int month) => CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetMonthName(month);

    /// <summary>
    /// Generates a list of sort option select items for the dropdown.
    /// </summary>
    /// <param name="selected">The currently selected sort option.</param>
    /// <returns>A list of select items for sort options.</returns>
    private static IReadOnlyList<SelectListItem> SortItems(string selected) => [new("Ordem do relatório", "report", selected == "report"), new("Grupo e categoria", "group", selected == "group"), new("Categoria", "category", selected == "category"), new("Maior orçamento", "budgetDesc", selected == "budgetDesc"), new("Maior realizado", "actualDesc", selected == "actualDesc"), new("Maior desvio", "varianceDesc", selected == "varianceDesc")];

    /// <summary>
    /// Retrieves the current user's identifier from the claims.
    /// </summary>
    /// <returns>The user identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
