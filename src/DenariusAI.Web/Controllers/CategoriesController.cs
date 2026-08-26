using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages category definitions used to classify financial transactions.
/// Provides CRUD operations, statement viewing, and activation/deactivation functionality for categories.
/// </summary>
[Authorize]
public sealed class CategoriesController(ICategoryService service, IFinancialGroupService groupService) : Controller
{
    /// <summary>
    /// Displays a paginated list of categories with optional filtering by group, search term, and active status.
    /// </summary>
    /// <param name="groupId">Optional financial group identifier to filter categories.</param>
    /// <param name="search">Optional search term to filter categories by name.</param>
    /// <param name="showInactive">Indicates whether to include inactive categories in the results.</param>
    /// <param name="page">Current page number for pagination (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A view displaying the filtered and paginated list of categories.</returns>
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

    /// <summary>
    /// Displays a financial statement for a specific category, showing all transactions classified under it.
    /// Supports filtering by date range and search term, with pagination.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="from">Optional start date for filtering transactions.</param>
    /// <param name="to">Optional end date for filtering transactions.</param>
    /// <param name="search">Optional search term to filter transactions by description, account, category, or reference.</param>
    /// <param name="page">Current page number for pagination (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A view displaying the category statement with filtered and paginated transactions.</returns>
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

    /// <summary>
    /// Displays the form to create a new category.
    /// </summary>
    /// <param name="groupId">Optional financial group identifier to pre-select in the form.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A view with the category creation form.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(Guid? groupId, CancellationToken cancellationToken)
    {
        var model = new CategoryFormViewModel { FinancialGroupId = groupId ?? Guid.Empty }; await PopulateGroupsAsync(model, cancellationToken); return View("Form", model);
    }

    /// <summary>
    /// Processes the submitted form to create a new category.
    /// </summary>
    /// <param name="model">The category form view model containing the data to create.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirects to the Index action on success, or returns the form view with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.FinancialGroupId == Guid.Empty) ModelState.AddModelError(nameof(model.FinancialGroupId), "Selecione um grupo.");
        if (!ModelState.IsValid) { await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
        try { await service.CreateAsync(ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Categoria criada com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); await PopulateGroupsAsync(model, cancellationToken); return View("Form", model); }
    }

    /// <summary>
    /// Displays the form to edit an existing category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to edit.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A view with the category edit form, or NotFound if the category doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken); if (item is null) return NotFound();
        var model = new CategoryFormViewModel { Id = item.Id, FinancialGroupId = item.FinancialGroupId, Name = item.Name, Description = item.Description, SortOrder = item.SortOrder };
        await PopulateGroupsAsync(model, cancellationToken); return View("Form", model);
    }

    /// <summary>
    /// Processes the submitted form to update an existing category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to update.</param>
    /// <param name="model">The category form view model containing the updated data.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirects to the Index action on success, returns the form view with validation errors, or NotFound if the category doesn't exist.</returns>
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

    /// <summary>
    /// Activates or deactivates a category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to activate or deactivate.</param>
    /// <param name="isActive">True to activate the category, false to deactivate it.</param>
    /// <param name="groupId">Optional financial group identifier to maintain filter state.</param>
    /// <param name="search">Optional search term to maintain filter state.</param>
    /// <param name="showInactive">Indicates whether to show inactive categories to maintain filter state.</param>
    /// <param name="page">Current page number to maintain pagination state.</param>
    /// <param name="pageSize">Number of items per page to maintain pagination state.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirects to the Index action with maintained filter and pagination state, or NotFound if the category doesn't exist.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, Guid? groupId, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Categoria ativada." : "Categoria desativada."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { groupId, search, showInactive, page, pageSize });
    }

    /// <summary>
    /// Populates the financial groups dropdown list in the category form view model.
    /// </summary>
    /// <param name="model">The category form view model to populate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    private async Task PopulateGroupsAsync(CategoryFormViewModel model, CancellationToken cancellationToken) => model.Groups = ToSelectList(await groupService.ListAsync(true, cancellationToken), false, model.FinancialGroupId);
    
    /// <summary>
    /// Converts a list of financial groups to a select list for dropdown rendering.
    /// </summary>
    /// <param name="groups">The list of financial groups to convert.</param>
    /// <param name="includeAll">Indicates whether to include an "All groups" option.</param>
    /// <param name="selectedGroupId">Optional identifier of the group to pre-select.</param>
    /// <returns>A read-only list of select list items.</returns>
    private static IReadOnlyList<SelectListItem> ToSelectList(IReadOnlyList<FinancialGroupDto> groups, bool includeAll, Guid? selectedGroupId)
    {
        var selectedValue = selectedGroupId?.ToString();
        var items = groups.OrderBy(item => item.SortOrder)
            .Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id.ToString() == selectedValue)).ToList();
        if (includeAll) items.Insert(0, new SelectListItem("Todos os grupos", string.Empty, selectedGroupId is null)); return items;
    }
    
    /// <summary>
    /// Retrieves the current user's identifier from the claims principal.
    /// </summary>
    /// <returns>The user identifier as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    
    /// <summary>
    /// Converts a category form view model to a save category DTO.
    /// </summary>
    /// <param name="model">The category form view model to convert.</param>
    /// <returns>A save category DTO containing the category data.</returns>
    private static SaveCategoryDto ToDto(CategoryFormViewModel model) => new(model.FinancialGroupId, model.Name, model.Description, model.SortOrder);
}
