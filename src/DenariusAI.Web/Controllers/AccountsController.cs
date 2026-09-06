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
/// Manages financial account CRUD operations and account listing screens.
/// </summary>
[Authorize]
public sealed class AccountsController(IAccountService service, ICategoryService categoryService, IFinancialGroupService groupService) : Controller
{
    /// <summary>
    /// Displays a paginated list of accounts with optional filtering by account type, category, search term, and active status.
    /// </summary>
    /// <param name="accountType">Optional account type filter.</param>
    /// <param name="categoryId">Optional category identifier filter.</param>
    /// <param name="search">Optional search term for account names.</param>
    /// <param name="showInactive">Indicates whether to include inactive accounts.</param>
    /// <param name="page">Current page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with paginated and filtered account list.</returns>
    public async Task<IActionResult> Index(AccountType? accountType, Guid? categoryId, string? search, bool showInactive = false, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var accounts = await service.ListAsync(!showInactive, cancellationToken);
        if (accountType.HasValue) accounts = accounts.Where(item => item.AccountType == accountType).ToList();
        if (categoryId.HasValue) accounts = accounts.Where(item => item.CategoryId == categoryId).ToList();
        if (!string.IsNullOrWhiteSpace(search)) accounts = accounts.Where(item => item.Name.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();

        var categories = await categoryService.ListAsync(activeOnly: false, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(false, cancellationToken);
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name);
        var categoryNames = categories.ToDictionary(item => item.Id, item => $"{groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}");
        var pagination = PaginationViewModel.Create(accounts.Count, page, pageSize);
        var items = accounts.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(item => new AccountListItemViewModel(item, item.CategoryId.HasValue ? categoryNames.GetValueOrDefault(item.CategoryId.Value, "—") : "—")).ToList();

        return View(new AccountIndexViewModel(items, AccountTypeItems(true, accountType), CategoryItems(categories, groups, true, categoryId), accountType, categoryId, search, showInactive, pagination));
    }

    /// <summary>
    /// Displays detailed information about a specific account.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with account details or NotFound if account doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var account = await service.GetAsync(id, cancellationToken);
        if (account is null) return NotFound();
        var categories = await categoryService.ListAsync(activeOnly: false, cancellationToken: cancellationToken);
        var category = account.CategoryId.HasValue ? categories.SingleOrDefault(item => item.Id == account.CategoryId.Value) : null;
        var group = category is null ? null : await groupService.GetAsync(category.FinancialGroupId, cancellationToken);
        return View(new AccountDetailsViewModel(account, category?.Name ?? "Sem categoria", group?.Name));
    }

    /// <summary>
    /// Displays a paginated account statement with optional filtering by date range and search term.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="from">Optional start date for filtering transactions.</param>
    /// <param name="to">Optional end date for filtering transactions.</param>
    /// <param name="search">Optional search term for transaction descriptions, references, or categories.</param>
    /// <param name="page">Current page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with paginated and filtered statement or NotFound if account doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Statement(Guid id, DateOnly? from, DateOnly? to, string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var account = await service.GetAsync(id, cancellationToken);
        if (account is null) return NotFound();

        var lines = await service.GetStatementAsync(id, cancellationToken);
        if (from.HasValue) lines = lines.Where(item => item.Date >= from.Value).ToList();
        if (to.HasValue) lines = lines.Where(item => item.Date <= to.Value).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            lines = lines.Where(item => item.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || (item.Reference?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.LineDescription?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)
                || (item.CategoryName?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();
        }

        lines = lines.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.LineId).Take(50).ToList();
        var pagination = PaginationViewModel.Create(lines.Count, page, pageSize);
        var items = lines.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new AccountStatementViewModel(account, items, from, to, search, pagination));
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid id, DateOnly? from, DateOnly? to, string? search, CancellationToken cancellationToken = default)
    {
        var account = await service.GetAsync(id, cancellationToken);
        if (account is null) return NotFound();
        var lines = await service.GetStatementAsync(id, cancellationToken);
        if (from.HasValue) lines = lines.Where(item => item.Date >= from.Value).ToList();
        if (to.HasValue) lines = lines.Where(item => item.Date <= to.Value).ToList();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); lines = lines.Where(item => item.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase) || (item.Reference?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false) || (item.LineDescription?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false) || (item.CategoryName?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList(); }
        lines = lines.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.LineId).Take(50).ToList();
        return File(Models.AccountStatementPdf.Generate(account, lines, from, to), "application/pdf", $"extrato-{account.Name.Replace(' ', '-').ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    /// <summary>
    /// Displays the form to create a new account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with account creation form.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new AccountFormViewModel();
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    /// <summary>
    /// Processes the account creation form submission.
    /// </summary>
    /// <param name="model">Account form data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirect to Details on success or form view with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await PopulateOptionsAsync(model, cancellationToken); return View("Form", model); }
        try
        {
            var id = await service.CreateAsync(ToDto(model), UserId(), cancellationToken);
            TempData["SuccessMessage"] = "Conta criada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    /// <summary>
    /// Displays the form to edit an existing account.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>View with account edit form or NotFound if account doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new AccountFormViewModel { Id = item.Id, Name = item.Name, Description = item.Description, AccountType = item.AccountType, InitialBalance = item.InitialBalance, Currency = item.Currency, CategoryId = item.CategoryId };
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    /// <summary>
    /// Processes the account edit form submission.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="model">Account form data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirect to Details on success, BadRequest if ID mismatch, NotFound if account doesn't exist, or form view with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AccountFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) { await PopulateOptionsAsync(model, cancellationToken); return View("Form", model); }
        try
        {
            await service.UpdateAsync(id, ToDto(model), UserId(), cancellationToken);
            TempData["SuccessMessage"] = "Conta atualizada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    /// <summary>
    /// Activates or deactivates an account.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="isActive">Desired active status.</param>
    /// <param name="accountType">Optional account type filter for redirect.</param>
    /// <param name="categoryId">Optional category identifier filter for redirect.</param>
    /// <param name="search">Optional search term for redirect.</param>
    /// <param name="showInactive">Show inactive accounts flag for redirect.</param>
    /// <param name="page">Current page number for redirect.</param>
    /// <param name="pageSize">Number of items per page for redirect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirect to Index with filters or NotFound if account doesn't exist.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, AccountType? accountType, Guid? categoryId, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Conta ativada." : "Conta desativada."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { accountType, categoryId, search, showInactive, page, pageSize });
    }

    /// <summary>
    /// Populates the dropdown options for the account form.
    /// </summary>
    /// <param name="model">Account form view model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task PopulateOptionsAsync(AccountFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await categoryService.ListAsync(activeOnly: false, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(false, cancellationToken);
        model.AccountTypes = AccountTypeItems(false, model.AccountType);
        model.Categories = CategoryItems(categories, groups, true, model.CategoryId);
    }

    /// <summary>
    /// Creates a list of select items for account types.
    /// </summary>
    /// <param name="includeAll">Indicates whether to include an "All types" option.</param>
    /// <param name="selected">The currently selected account type.</param>
    /// <returns>List of select items for account types.</returns>
    private static IReadOnlyList<SelectListItem> AccountTypeItems(bool includeAll, AccountType? selected)
    {
        var items = Enum.GetValues<AccountType>().Select(type => new SelectListItem(AccountTypeName(type), ((int)type).ToString(), type == selected)).ToList();
        if (includeAll) items.Insert(0, new SelectListItem("Todos os tipos", string.Empty, selected is null));
        return items;
    }

    /// <summary>
    /// Creates a list of select items for categories grouped by financial group.
    /// </summary>
    /// <param name="categories">List of categories.</param>
    /// <param name="groups">Financial groups that define the parent display order and names.</param>
    /// <param name="includeNone">Indicates whether to include a "No category" option.</param>
    /// <param name="selected">The currently selected category identifier.</param>
    /// <returns>List of select items for categories.</returns>
    private static IReadOnlyList<SelectListItem> CategoryItems(IReadOnlyList<CategoryDto> categories, IReadOnlyList<FinancialGroupDto> groups, bool includeNone, Guid? selected)
    {
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name);
        var items = CategoryDisplayOrdering.Order(categories, groups)
            .Select(item => new SelectListItem($"{groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}{(item.IsActive ? "" : " (inativa)")}", item.Id.ToString(), item.Id == selected, !item.IsActive)).ToList();
        if (includeNone) items.Insert(0, new SelectListItem("Sem categoria", string.Empty, selected is null));
        return items;
    }

    /// <summary>
    /// Gets the localized display name for an account type.
    /// </summary>
    /// <param name="type">The account type.</param>
    /// <returns>Localized display name.</returns>
    public static string AccountTypeName(AccountType type) => type switch
    {
        AccountType.BankAccount => "Conta bancária",
        AccountType.Cash => "Dinheiro",
        AccountType.Savings => "Poupança",
        AccountType.TermDeposit => "Depósito a prazo",
        AccountType.Investment => "Investimento",
        AccountType.OtherAsset => "Outro ativo",
        AccountType.Income => "Rendimento",
        AccountType.Expense => "Despesa",
        _ => type.ToString()
    };

    /// <summary>
    /// Retrieves the current user's identifier from claims.
    /// </summary>
    /// <returns>User identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user is not identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");

    /// <summary>
    /// Converts the account form view model to a DTO for persistence.
    /// </summary>
    /// <param name="model">Account form view model.</param>
    /// <returns>Save account DTO.</returns>
    private static SaveAccountDto ToDto(AccountFormViewModel model) => new(model.Name, model.Description, model.AccountType, model.InitialBalance, model.Currency, model.CategoryId);
}
