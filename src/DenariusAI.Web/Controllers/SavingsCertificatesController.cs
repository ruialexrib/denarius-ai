using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the SavingsCertificatesController type.
/// </summary>
[Authorize]
public sealed class SavingsCertificatesController(DenariusDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? search, string sort = "date-asc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (from > to) return BadRequest();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = dbContext.SavingsCertificates.AsNoTracking();
        if (from.HasValue) query = query.Where(item => item.InvestmentDate >= from.Value);
        if (to.HasValue) query = query.Where(item => item.InvestmentDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(item => item.SeriesNumber.Contains(term) || item.Description.Contains(term)); }
        query = sort switch { "date-desc" => query.OrderByDescending(item => item.InvestmentDate), "value-desc" => query.OrderByDescending(item => item.CurrentValue), "yield-desc" => query.OrderByDescending(item => item.CurrentValue - item.InvestmentValue), "series" => query.OrderBy(item => item.SeriesNumber), _ => query.OrderBy(item => item.InvestmentDate) };
        var certificates = await query.ToListAsync(cancellationToken);
        var allRows = certificates.Select(item => ToRow(item, today)).ToList();
        var pagination = PaginationViewModel.Create(allRows.Count, page, pageSize);
        var rows = allRows.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new SavingsCertificateIndexViewModel(rows, allRows.Sum(item => item.InvestmentValue),
            allRows.Sum(item => item.CurrentValue), allRows.Sum(item => item.Yield),
            allRows.Sum(item => item.FutureNetInterest), allRows.Sum(item => item.FutureValue), from, to, search, sort,
            [new("Data — mais antiga", "date-asc", sort == "date-asc"), new("Data — mais recente", "date-desc", sort == "date-desc"), new("Maior valor atual", "value-desc", sort == "value-desc"), new("Maior rendimento", "yield-desc", sort == "yield-desc"), new("Série/Número", "series", sort == "series")], pagination));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new SavingsCertificateFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        if (await dbContext.SavingsCertificates.AnyAsync(item => item.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken))
        { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); return View("Form", model); }
        var item = CreateEntity(model); item.CreatedBy = UserId(); dbContext.Add(item); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Certificado de Aforro adicionado."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        return View("Form", ToForm(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        if (await dbContext.SavingsCertificates.AnyAsync(other => other.Id != id && other.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken))
        { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); return View("Form", model); }
        item.Update(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);
        item.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado atualizado."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado removido."; return RedirectToAction(nameof(Index));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SavingsCertificate CreateEntity(SavingsCertificateFormViewModel model) => new(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);
    private static SavingsCertificateFormViewModel ToForm(SavingsCertificate item) => new() { Id = item.Id, InvestmentDate = item.InvestmentDate, SeriesNumber = item.SeriesNumber, Description = item.Description, InvestmentValue = item.InvestmentValue, Rate = item.Rate, CurrentValue = item.CurrentValue, NextCapitalization = item.NextCapitalization };
    private static SavingsCertificateRowViewModel ToRow(SavingsCertificate item, DateOnly today)
    {
        var age = today.DayNumber - item.InvestmentDate.DayNumber;
        var difference = item.NextCapitalization.DayNumber - today.DayNumber;
        var yield = item.CurrentValue - item.InvestmentValue;
        var futureNetInterest = item.CurrentValue * (item.Rate / 100m * .72m / 4m);
        return new(item.Id, item.InvestmentDate, age, item.SeriesNumber, item.Description, item.InvestmentValue,
            item.Rate, item.CurrentValue, yield, item.NextCapitalization, difference, futureNetInterest, item.CurrentValue + futureNetInterest);
    }
}
