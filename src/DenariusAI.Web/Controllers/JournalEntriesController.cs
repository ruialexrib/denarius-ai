using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages journal entry creation, editing, posting, and list operations.
/// </summary>
[Authorize]
public sealed class JournalEntriesController(IJournalEntryService service, IAccountService accountService, ICategoryService categoryService, IFinancialGroupService groupService, IBudgetService budgetService, IJournalEntrySuggestionService suggestionService, ILogger<JournalEntriesController> logger) : Controller
{
    /// <summary>
    /// Displays a paginated list of journal entries with optional filtering and sorting.
    /// </summary>
    /// <param name="from">Start date filter (inclusive).</param>
    /// <param name="to">End date filter (inclusive).</param>
    /// <param name="status">Status filter for journal entries.</param>
    /// <param name="budget">Budget filter - can be a GUID, "none", or empty for all.</param>
    /// <param name="search">Search term to filter by description or reference.</param>
    /// <param name="sort">Sort order: "dateDesc", "dateAsc", "description", or "amountDesc".</param>
    /// <param name="page">Current page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with filtered and paginated journal entries.</returns>
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

    /// <summary>
    /// Displays detailed information about a specific journal entry.
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with journal entry details or NotFound if entry doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var entry = await service.GetAsync(id, cancellationToken);
        return entry is null ? NotFound() : View(new JournalEntryDetailsViewModel(entry));
    }

    /// <summary>
    /// Displays the form to create a new journal entry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with empty form for creating a new journal entry.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new JournalEntryFormViewModel();
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    /// <summary>
    /// Processes the creation of a new journal entry.
    /// </summary>
    /// <param name="model">The form data for the new journal entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to Details on success, or returns to form with validation errors.</returns>
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

    /// <summary>
    /// Provides AI-powered suggestions for journal entry classification based on user messages.
    /// </summary>
    /// <param name="model">The suggestion request containing message and conversation history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON with suggestion data, or error response if service unavailable or request fails.</returns>
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

    /// <summary>
    /// Displays the form to edit an existing journal entry.
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry to edit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with populated form, or NotFound/redirect if entry doesn't exist or cannot be edited.</returns>
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

    /// <summary>
    /// Processes the update of an existing journal entry.
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry to update.</param>
    /// <param name="model">The form data with updated journal entry information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to Details on success, or returns to form with validation errors.</returns>
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

    /// <summary>
    /// Cancels a journal entry, changing its status to Cancelled.
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirects to Details with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try { await service.CancelAsync(id, UserId(), cancellationToken); TempData["SuccessMessage"] = "Movimento anulado."; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Populates dropdown options for accounts, categories, and budgets in the form.
    /// </summary>
    /// <param name="model">The form view model to populate with options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task PopulateOptionsAsync(JournalEntryFormViewModel model, CancellationToken cancellationToken)
    {
        model.AiSuggestionAvailable = suggestionService.IsAvailable;
        var accounts = await accountService.ListAsync(true, cancellationToken);
        model.Accounts = accounts.OrderBy(item => item.Name).Select(item => new SelectListItem($"{item.Name} · {item.Currency}", item.Id.ToString())).Prepend(new SelectListItem("Selecionar conta", string.Empty)).ToList();
        model.TransactionAccounts = accounts.Where(item => item.AccountType is AccountType.BankAccount or AccountType.Savings or AccountType.Cash)
            .OrderBy(item => item.Name).Select(item => new SelectListItem($"{item.Name} · {item.Currency}", item.Id.ToString())).Prepend(new SelectListItem("Selecionar conta", string.Empty)).ToList();
        model.ExpenseAccountId = accounts.FirstOrDefault(item => item.AccountType == AccountType.Expense)?.Id;
        model.IncomeAccountId = accounts.FirstOrDefault(item => item.AccountType == AccountType.Income)?.Id;
        var categories = await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name); var groupKinds = groups.ToDictionary(item => item.Id, item => item.Kind);
        model.Categories = categories.OrderBy(item => groupNames.GetValueOrDefault(item.FinancialGroupId)).ThenBy(item => item.SortOrder)
            .Select(item => new SelectListItem($"{(groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Income ? "↓" : groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Expense ? "↑" : "◆")} {groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}", item.Id.ToString())).Prepend(new SelectListItem("Sem categoria", string.Empty)).ToList();
        model.ExpenseCategories = categories.Where(item => groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Expense).OrderBy(item => groupNames.GetValueOrDefault(item.FinancialGroupId)).ThenBy(item => item.SortOrder)
            .Select(item => new SelectListItem($"{groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}", item.Id.ToString())).Prepend(new SelectListItem("Selecionar categoria", string.Empty)).ToList();
        model.IncomeCategories = categories.Where(item => groupKinds.GetValueOrDefault(item.FinancialGroupId) == FinancialGroupKind.Income).OrderBy(item => groupNames.GetValueOrDefault(item.FinancialGroupId)).ThenBy(item => item.SortOrder)
            .Select(item => new SelectListItem($"{groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}", item.Id.ToString())).Prepend(new SelectListItem("Selecionar categoria", string.Empty)).ToList();
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        if (!model.BudgetId.HasValue)
            model.BudgetId = budgets.FirstOrDefault(item => item.Year == model.Date.Year && item.Month == model.Date.Month)?.Id;
        model.Budgets = budgets.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == model.BudgetId))
            .Append(new SelectListItem("Sem orçamento", string.Empty, !model.BudgetId.HasValue)).ToList();
    }

    /// <summary>
    /// Creates a list of select items for journal entry status filtering.
    /// </summary>
    /// <param name="selected">The currently selected status.</param>
    /// <returns>List of select items for status options.</returns>
    private static IReadOnlyList<SelectListItem> StatusItems(JournalEntryStatus? selected) =>
    [
        new("Todos os estados", string.Empty, selected is null),
        new("Ativo", ((int)JournalEntryStatus.Active).ToString(), selected == JournalEntryStatus.Active),
        new("Anulado", ((int)JournalEntryStatus.Cancelled).ToString(), selected == JournalEntryStatus.Cancelled)
    ];

    /// <summary>
    /// Creates a list of select items for sorting options.
    /// </summary>
    /// <param name="selected">The currently selected sort option.</param>
    /// <returns>List of select items for sort options.</returns>
    private static IReadOnlyList<SelectListItem> SortItems(string selected) =>
    [
        new("Data mais recente", "dateDesc", selected == "dateDesc"),
        new("Data mais antiga", "dateAsc", selected == "dateAsc"),
        new("Descrição", "description", selected == "description"),
        new("Maior valor", "amountDesc", selected == "amountDesc")
    ];

    /// <summary>
    /// Converts a journal entry status enum to a display name.
    /// </summary>
    /// <param name="status">The journal entry status.</param>
    /// <returns>The localized status name.</returns>
    public static string StatusName(JournalEntryStatus status) => status == JournalEntryStatus.Active ? "Ativo" : "Anulado";

    /// <summary>
    /// Converts a reconciliation status enum to a display name.
    /// </summary>
    /// <param name="status">The reconciliation status.</param>
    /// <returns>The localized reconciliation status name.</returns>
    public static string ReconciliationName(ReconciliationStatus status) => status == ReconciliationStatus.Reconciled ? "Reconciliado" : "Não reconciliado";

    /// <summary>
    /// Retrieves the current user's identifier from claims.
    /// </summary>
    /// <returns>The user's unique identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user is not identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");

    /// <summary>
    /// Converts a form view model to a create journal entry request DTO.
    /// </summary>
    /// <param name="model">The form view model.</param>
    /// <returns>A create journal entry request DTO.</returns>
    private static CreateJournalEntryRequest ToRequest(JournalEntryFormViewModel model) => new(model.Date, model.Description, model.Reference, model.Notes, model.Lines.Select(line => new JournalEntryLineInput(line.AccountId, line.Debit, line.Credit, line.Description, line.CategoryId)).ToList(), model.BudgetId);
}
