using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

[Authorize]
/// <summary>
/// Contains definitions for JournalEntriesController.
/// </summary>
public sealed class JournalEntriesController(IJournalEntryService service, IAccountService accountService, ICategoryService categoryService, IFinancialGroupService groupService, IBudgetService budgetService, IJournalEntrySuggestionService suggestionService, ILogger<JournalEntriesController> logger) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, JournalEntryStatus? status, string? budget, string? search, string sort = "dateDesc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var entries = await service.ListAsync(cancellationToken);
        if (from.HasValue) entries = entries.Where(item => item.Date >= from.Value).ToList();
        if (to.HasValue) entries = entries.Where(item => item.Date <= to.Value).ToList();
        if (status.HasValue) entries = entries.Where(item => item.Status == status.Value).ToList();
        if (string.Equals(budget, "none", StringComparison.OrdinalIgnoreCase)) entries = entries.Where(item => item.BudgetId is null).ToList();
        else if (Guid.TryParse(budget, out var budgetId)) entries = entries.Where(item => item.BudgetId == budgetId).ToList();
        if (!string.IsNullOrWhiteSpace(search)) entries = entries.Where(item => item.Description.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase) || (item.Reference?.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();
        entries = sort switch
        {
            "dateAsc" => entries.OrderBy(item => item.Date).ThenBy(item => item.Description).ToList(),
            "description" => entries.OrderBy(item => item.Description).ThenByDescending(item => item.Date).ToList(),
            "amountDesc" => entries.OrderByDescending(item => item.TotalDebit).ThenByDescending(item => item.Date).ToList(),
            _ => entries.OrderByDescending(item => item.Date).ThenBy(item => item.Description).ToList()
        };
        var pagination = PaginationViewModel.Create(entries.Count, page, pageSize);
        entries = entries.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var budgetPeriods = await budgetService.ListPeriodsAsync(cancellationToken);
        var budgetOptions = budgetPeriods.Select(item => new SelectListItem(item.Name, item.Id.ToString(), string.Equals(budget, item.Id.ToString(), StringComparison.OrdinalIgnoreCase)))
            .Prepend(new SelectListItem("Sem orçamento", "none", string.Equals(budget, "none", StringComparison.OrdinalIgnoreCase)))
            .Prepend(new SelectListItem("Todos os orçamentos", string.Empty, string.IsNullOrWhiteSpace(budget))).ToList();
        return View(new JournalEntryIndexViewModel(entries, from, to, status, budget, search, sort, StatusItems(status), budgetOptions, SortItems(sort), pagination));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var entry = await service.GetAsync(id, cancellationToken);
        return entry is null ? NotFound() : View(new JournalEntryDetailsViewModel(entry));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new JournalEntryFormViewModel();
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JournalEntryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await PopulateOptionsAsync(model, cancellationToken); return View("Form", model); }
        try
        {
            var result = await service.CreateAsync(ToRequest(model), UserId(), cancellationToken);
            TempData["SuccessMessage"] = "Movimento criado com sucesso.";
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Suggest([FromBody] JournalEntrySuggestionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Introduza uma mensagem com até 1000 caracteres." });
        try
        {
            var result = await suggestionService.SuggestAsync(new(model.Message, model.History.Select(item => new JournalEntrySuggestionMessageDto(item.Role, item.Content)).ToList()), cancellationToken);
            logger.LogInformation("Journal entry suggestion processed. Complete: {IsComplete}.", result.IsComplete);
            return Json(new { isComplete = result.IsComplete, message = result.Message, classificationExplanation = result.ClassificationExplanation, suggestion = result.Suggestion });
        }
        catch (InvalidOperationException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "A integração Mistral não está configurada." }); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Journal entry suggestion request failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Não foi possível obter a sugestão. Tente novamente." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var entry = await service.GetAsync(id, cancellationToken);
        if (entry is null) return NotFound();
        if (entry.Status == JournalEntryStatus.Cancelled || entry.ReconciliationStatus == ReconciliationStatus.Reconciled)
        {
            TempData["ErrorMessage"] = "Apenas movimentos ativos e não reconciliados podem ser editados.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var model = new JournalEntryFormViewModel
        {
            Id = entry.Id,
            Date = entry.Date,
            Description = entry.Description,
            Reference = entry.Reference,
            Notes = entry.Notes,
            BudgetId = entry.BudgetId,
            Lines = entry.Lines.Select(line => new JournalEntryLineFormViewModel { AccountId = line.AccountId, CategoryId = line.CategoryId, Debit = line.Debit, Credit = line.Credit, Description = line.Description }).ToList()
        };
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, JournalEntryFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) { await PopulateOptionsAsync(model, cancellationToken); return View("Form", model); }
        try
        {
            await service.UpdateAsync(id, ToRequest(model), UserId(), cancellationToken);
            TempData["SuccessMessage"] = "Movimento atualizado com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try { await service.CancelAsync(id, UserId(), cancellationToken); TempData["SuccessMessage"] = "Movimento anulado."; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateOptionsAsync(JournalEntryFormViewModel model, CancellationToken cancellationToken)
    {
        model.AiSuggestionAvailable = suggestionService.IsAvailable;
        var accounts = await accountService.ListAsync(true, cancellationToken);
        model.Accounts = accounts.OrderBy(item => item.Name).Select(item => new SelectListItem($"{item.Name} · {item.Currency}", item.Id.ToString())).Prepend(new SelectListItem("Selecionar conta", string.Empty)).ToList();
        var categories = await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name); var groupKinds = groups.ToDictionary(item => item.Id, item => item.Kind);
        model.Categories = categories.OrderBy(item => groupNames.GetValueOrDefault(item.FinancialGroupId)).ThenBy(item => item.SortOrder)
            .Select(item => new SelectListItem($"{(groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Income ? "↓" : groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Expense ? "↑" : "◆")} {groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}", item.Id.ToString())).Prepend(new SelectListItem("Sem categoria", string.Empty)).ToList();
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        if (!model.BudgetId.HasValue) model.BudgetId = budgets.FirstOrDefault()?.Id;
        model.Budgets = budgets.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == model.BudgetId))
            .Append(new SelectListItem("Sem orçamento", string.Empty, !model.BudgetId.HasValue)).ToList();
    }

    private static IReadOnlyList<SelectListItem> StatusItems(JournalEntryStatus? selected) =>
    [
        new("Todos os estados", string.Empty, selected is null),
        new("Ativo", ((int)JournalEntryStatus.Active).ToString(), selected == JournalEntryStatus.Active),
        new("Anulado", ((int)JournalEntryStatus.Cancelled).ToString(), selected == JournalEntryStatus.Cancelled)
    ];

    private static IReadOnlyList<SelectListItem> SortItems(string selected) =>
    [
        new("Data mais recente", "dateDesc", selected == "dateDesc"),
        new("Data mais antiga", "dateAsc", selected == "dateAsc"),
        new("Descrição", "description", selected == "description"),
        new("Maior valor", "amountDesc", selected == "amountDesc")
    ];

    public static string StatusName(JournalEntryStatus status) => status == JournalEntryStatus.Active ? "Ativo" : "Anulado";
    public static string ReconciliationName(ReconciliationStatus status) => status == ReconciliationStatus.Reconciled ? "Reconciliado" : "Não reconciliado";
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static CreateJournalEntryRequest ToRequest(JournalEntryFormViewModel model) => new(model.Date, model.Description, model.Reference, model.Notes, model.Lines.Select(line => new JournalEntryLineInput(line.AccountId, line.Debit, line.Credit, line.Description, line.CategoryId)).ToList(), model.BudgetId);
}
