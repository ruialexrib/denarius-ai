using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

[Authorize]
/// <summary>
/// Contains definitions for FinancialGroupsController.
/// </summary>
public sealed class FinancialGroupsController(IFinancialGroupService service) : Controller
{
    public async Task<IActionResult> Index(string? search, bool showInactive = false, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var items = await service.ListAsync(activeOnly: !showInactive, cancellationToken);
        if (!string.IsNullOrWhiteSpace(search)) items = items.Where(item => item.Name.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
        var pagination = PaginationViewModel.Create(items.Count, page, pageSize);
        items = items.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new FinancialGroupIndexViewModel(items, search, showInactive, pagination));
    }

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

    [HttpGet]
    public IActionResult Create() => View("Form", new FinancialGroupFormViewModel { Kind = FinancialGroupKind.Expense });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        try { await service.CreateAsync(ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Grupo criado com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View("Form", model); }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken); if (item is null) return NotFound();
        return View("Form", new FinancialGroupFormViewModel { Id = item.Id, Name = item.Name, Description = item.Description, Kind = item.Kind, SortOrder = item.SortOrder });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FinancialGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        try { await service.UpdateAsync(id, ToDto(model), UserId(), cancellationToken); TempData["SuccessMessage"] = "Grupo atualizado com sucesso."; return RedirectToAction(nameof(Index)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View("Form", model); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, string? search, bool showInactive, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try { await service.SetActiveAsync(id, isActive, UserId(), cancellationToken); TempData["SuccessMessage"] = isActive ? "Grupo ativado." : "Grupo desativado."; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Index), new { search, showInactive, page, pageSize });
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SaveFinancialGroupDto ToDto(FinancialGroupFormViewModel model) => new(model.Name, model.Description, model.Kind, model.SortOrder);
}
