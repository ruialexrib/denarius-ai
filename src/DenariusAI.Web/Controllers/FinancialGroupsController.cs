using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Namespace containing controllers for the DenariusAI web application.
/// </summary>
namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages financial group entities that organize transaction categories.
/// </summary>
/// <remarks>
/// This controller provides functionality to list, create, edit, view statements, and manage the active status of financial groups.
/// All actions require user authorization.
/// </remarks>
/// <param name="service">The financial group service for business logic operations.</param>
[Authorize]
public sealed class FinancialGroupsController(IFinancialGroupService service) : Controller
{
    /// <summary>
    /// Displays a paginated list of financial groups with optional search and filtering.
    /// </summary>
    /// <param name="search">Optional search term to filter groups by name.</param>
    /// <param name="showInactive">When true, includes inactive groups in the results.</param>
    /// <param name="page">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A view displaying the list of financial groups.</returns>
    public async Task<IActionResult> Index(string? search, bool showInactive = false, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var items = await service.ListAsync(activeOnly: !showInactive, cancellationToken);
        if (!string.IsNullOrWhiteSpace(search)) items = items.Where(item => item.Name.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        var pagination = PaginationViewModel.Create(items.Count, page, pageSize);
        items = items.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new FinancialGroupIndexViewModel(items, search, showInactive, pagination));
    }

    /// <summary>
    /// Displays the financial statement for a specific financial group.
    /// </summary>
    /// <param name="id">The unique identifier of the financial group.</param>
    /// <param name="from">Optional start date to filter transactions.</param>
    /// <param name="to">Optional end date to filter transactions.</param>
    /// <param name="search">Optional search term to filter statement lines.</param>
    /// <param name="page">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A view displaying the financial group statement, or NotFound if the group doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Statement(Guid id, DateOnly? from, DateOnly? to, string? search, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var group = await service.GetAsync(id, cancellationToken);
        if (group is null) return NotFound();
        var lines = await service.GetStatementAsync(id, cancellationToken);
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
            "Grupo", group.Id, group.Name, group.Kind, currentBalance, items, from, to, search, pagination));
    }

    /// <summary>
    /// Displays the form for creating a new financial group.
    /// </summary>
    /// <returns>A view with the creation form initialized with default expense type.</returns>
    [HttpGet]
    public IActionResult Create() => View("Form", new FinancialGroupFormViewModel { Kind = FinancialGroupKind.Expense });

    /// <summary>
    /// Processes the creation of a new financial group.
    /// </summary>
    /// <param name="model">The view model containing the financial group data.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index on success, or returns the form with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        try { await service.CreateAsync(ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Grupo criado com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View("Form", model); }
    }

    /// <summary>
    /// Displays the form for editing an existing financial group.
    /// </summary>
    /// <param name="id">The unique identifier of the financial group to edit.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A view with the edit form populated with existing data, or NotFound if the group doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken); if (item is null) return NotFound();
        return View("Form", new FinancialGroupFormViewModel { Id = item.Id, Name = item.Name, Description = item.Description, Kind = item.Kind, SortOrder = item.SortOrder });
    }

    /// <summary>
    /// Processes the update of an existing financial group.
    /// </summary>
    /// <param name="id">The unique identifier of the financial group to update.</param>
    /// <param name="model">The view model containing the updated financial group data.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index on success, returns NotFound if group doesn't exist, or returns the form with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FinancialGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        try { await service.UpdateAsync(id, ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Grupo atualizado com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View("Form", model); }
    }

    /// <summary>
    /// Sets the active status of a financial group.
    /// </summary>
    /// <param name="id">The unique identifier of the financial group.</param>
    /// <param name="isActive">True to activate the group, false to deactivate it.</param>
    /// <param name="search">Search term to preserve in the redirect.</param>
    /// <param name="showInactive">Filter setting to preserve in the redirect.</param>
    /// <param name="page">Page number to preserve in the redirect.</param>
    /// <param name="pageSize">Page size to preserve in the redirect.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index with preserved filter parameters, or NotFound if the group doesn't exist.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Grupo ativado." : "Grupo desativado."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { search, showInactive, page, pageSize });
    }

    /// <summary>
    /// Retrieves the current authenticated user's identifier.
    /// </summary>
    /// <returns>The user identifier from claims.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not authenticated.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    
    /// <summary>
    /// Converts a view model to a data transfer object for service operations.
    /// </summary>
    /// <param name="model">The view model to convert.</param>
    /// <returns>A DTO containing the financial group data.</returns>
    private static SaveFinancialGroupDto ToDto(FinancialGroupFormViewModel model) => new(model.Name, model.Description, model.Kind, model.SortOrder);
}
