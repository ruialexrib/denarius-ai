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
/// Represents the AccountsController type.
/// </summary>
[Authorize]
public sealed class AccountsController(IAccountService service, ICategoryService categoryService, IFinancialGroupService groupService) : Controller
{
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

        return View(new AccountIndexViewModel(items, AccountTypeItems(true, accountType), CategoryItems(categories, groupNames, true, categoryId), accountType, categoryId, search, showInactive, pagination));
    }

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

        lines = lines.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.LineId).ToList();
        var pagination = PaginationViewModel.Create(lines.Count, page, pageSize);
        var items = lines.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new AccountStatementViewModel(account, items, from, to, search, pagination));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new AccountFormViewModel();
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

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

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new AccountFormViewModel { Id = item.Id, Name = item.Name, Description = item.Description, AccountType = item.AccountType, InitialBalance = item.InitialBalance, Currency = item.Currency, CategoryId = item.CategoryId };
        await PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, AccountType? accountType, Guid? categoryId, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Conta ativada." : "Conta desativada."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { accountType, categoryId, search, showInactive, page, pageSize });
    }

    private async Task PopulateOptionsAsync(AccountFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await categoryService.ListAsync(activeOnly: false, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(false, cancellationToken);
        model.AccountTypes = AccountTypeItems(false, model.AccountType);
        model.Categories = CategoryItems(categories, groups.ToDictionary(item => item.Id, item => item.Name), true, model.CategoryId);
    }

    private static IReadOnlyList<SelectListItem> AccountTypeItems(bool includeAll, AccountType? selected)
    {
        var items = Enum.GetValues<AccountType>().Select(type => new SelectListItem(AccountTypeName(type), ((int)type).ToString(), type == selected)).ToList();
        if (includeAll) items.Insert(0, new SelectListItem("Todos os tipos", string.Empty, selected is null));
        return items;
    }

    private static IReadOnlyList<SelectListItem> CategoryItems(IReadOnlyList<CategoryDto> categories, IReadOnlyDictionary<Guid, string> groupNames, bool includeNone, Guid? selected)
    {
        var items = categories.OrderBy(item => groupNames.GetValueOrDefault(item.FinancialGroupId)).ThenBy(item => item.SortOrder)
            .Select(item => new SelectListItem($"{groupNames.GetValueOrDefault(item.FinancialGroupId, "—")} — {item.Name}{(item.IsActive ? "" : " (inativa)")}", item.Id.ToString(), item.Id == selected, !item.IsActive)).ToList();
        if (includeNone) items.Insert(0, new SelectListItem("Sem categoria", string.Empty, selected is null));
        return items;
    }

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

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SaveAccountDto ToDto(AccountFormViewModel model) => new(model.Name, model.Description, model.AccountType, model.InitialBalance, model.Currency, model.CategoryId);
}
