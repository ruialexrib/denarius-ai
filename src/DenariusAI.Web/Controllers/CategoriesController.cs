using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the CategoriesController type.
/// </summary>
[Authorize]
public sealed class CategoriesController(ICategoryService service, IFinancialGroupService groupService) : Controller
{
    public async Task<IActionResult> Index(Guid? groupId, string? search, bool showInactive = false, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var groups = await groupService.ListAsync(false, cancellationToken); var categories = await service.ListAsync(groupId, !showInactive, cancellationToken);
        if (!string.IsNullOrWhiteSpace(search)) categories = categories.Where(item => item.Name.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        var names = groups.ToDictionary(item => item.Id, item => item.Name); var kinds = groups.ToDictionary(item => item.Id, item => item.Kind);
        var pagination = PaginationViewModel.Create(categories.Count, page, pageSize);
        var items = categories.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(item => new CategoryListItemViewModel(item, names.GetValueOrDefault(item.FinancialGroupId, "—"), kinds.GetValueOrDefault(item.FinancialGroupId))).ToList();
        return View(new CategoryIndexViewModel(items, ToSelectList(groups, true, groupId), groupId, search, showInactive, pagination));
    }

    [HttpGet]
    public async Task<IActionResult> Statement(Guid id, DateOnly? from, DateOnly? to, string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var category = await service.GetAsync(id, cancellationToken);
        if (category is null) return NotFound();
        var lines = await service.GetStatementAsync(id, cancellationToken);
        var group = await groupService.GetAsync(category.FinancialGroupId, cancellationToken);
        var currentBalance = lines.LastOrDefault()?.Balance ?? 0m;
        if (from.HasValue) lines = lines.Where(item => item.Date >= from.Value).ToList();
        if (to.HasValue) lines = lines.Where(item => item.Date <= to.Value).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            lines = lines.Where(item => item.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || item.AccountName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || item.CategoryName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || (item.Reference?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();
        }
        lines = lines.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.LineId).ToList();
        var pagination = PaginationViewModel.Create(lines.Count, page, pageSize);
        var items = lines.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View("~/Views/Shared/ClassificationStatement.cshtml", new ClassificationStatementViewModel(
            "Categoria", category.Id, category.Name, group?.Kind ?? DenariusAI.Domain.Enums.FinancialGroupKind.Asset, currentBalance, items, from, to, search, pagination));
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? groupId, CancellationToken cancellationToken)
    {
        var model = new CategoryFormViewModel { FinancialGroupId = groupId ?? Guid.Empty }; await PopulateGroupsAsync(model, cancellationToken); return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.FinancialGroupId == Guid.Empty) ModelState.AddModelError(nameof(model.FinancialGroupId), "Selecione um grupo.");
        if (!ModelState.IsValid) { await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
        try { await service.CreateAsync(ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Categoria criada com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken); if (item is null) return NotFound();
        var model = new CategoryFormViewModel { Id = item.Id, FinancialGroupId = item.FinancialGroupId, Name = item.Name, Description = item.Description, SortOrder = item.SortOrder };
        await PopulateGroupsAsync(model, cancellationToken); return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (model.FinancialGroupId == Guid.Empty) ModelState.AddModelError(nameof(model.FinancialGroupId), "Selecione um grupo.");
        if (!ModelState.IsValid) { await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
        try { await service.UpdateAsync(id, ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Categoria atualizada com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, Guid? groupId, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Categoria ativada." : "Categoria desativada."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { groupId, search, showInactive, page, pageSize });
    }

    private async Task PopulateGroupsAsync(CategoryFormViewModel model, CancellationToken cancellationToken) => model.Groups = ToSelectList(await groupService.ListAsync(true, cancellationToken), false, model.FinancialGroupId);
    private static IReadOnlyList<SelectListItem> ToSelectList(IReadOnlyList<FinancialGroupDto> groups, bool includeAll, Guid? selectedGroupId)
    {
        var selectedValue = selectedGroupId?.ToString();
        var items = groups.OrderBy(item => item.SortOrder)
            .Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id.ToString() == selectedValue)).ToList();
        if (includeAll) items.Insert(0, new SelectListItem("Todos os grupos", string.Empty, selectedGroupId is null)); return items;
    }
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SaveCategoryDto ToDto(CategoryFormViewModel model) => new(model.FinancialGroupId, model.Name, model.Description, model.SortOrder);
}
