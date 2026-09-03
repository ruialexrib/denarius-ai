using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class SavingsCertificatesController(DenariusDbContext dbContext, ISavingsCertificateClipboardSuggestionService clipboardSuggestionService, ILogger<SavingsCertificatesController> logger) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? search, string sort = "date-asc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (from > to) return BadRequest();
        var today = DateOnly.FromDateTime(DateTime.Today); var query = dbContext.SavingsCertificates.AsNoTracking();
        if (from.HasValue) query = query.Where(x => x.InvestmentDate >= from.Value); if (to.HasValue) query = query.Where(x => x.InvestmentDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.SeriesNumber.Contains(term) || x.Description.Contains(term)); }
        query = sort switch { "date-desc" => query.OrderByDescending(x => x.InvestmentDate), "value-desc" => query.OrderByDescending(x => x.CurrentValue), "yield-desc" => query.OrderByDescending(x => x.CurrentValue - x.InvestmentValue), "series" => query.OrderBy(x => x.SeriesNumber), _ => query.OrderBy(x => x.InvestmentDate) };
        var certificates = await query.ToListAsync(cancellationToken); var allRows = certificates.Select(x => ToRow(x, today)).ToList(); var pagination = PaginationViewModel.Create(allRows.Count, page, pageSize); var rows = allRows.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        return View(new SavingsCertificateIndexViewModel(rows, allRows.Sum(x => x.InvestmentValue), allRows.Sum(x => x.CurrentValue), allRows.Sum(x => x.Yield), allRows.Sum(x => x.FutureNetInterest), allRows.Sum(x => x.FutureValue), from, to, search, sort, [new("Data — mais antiga", "date-asc", sort == "date-asc"), new("Data — mais recente", "date-desc", sort == "date-desc"), new("Maior valor atual", "value-desc", sort == "value-desc"), new("Maior rendimento", "yield-desc", sort == "yield-desc"), new("Série/Número", "series", sort == "series")], pagination));
    }

    [HttpGet] public IActionResult Create() => View("Form", new SavingsCertificateFormViewModel { AiSuggestionAvailable = clipboardSuggestionService.IsAvailable });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { model.AiSuggestionAvailable = clipboardSuggestionService.IsAvailable; return View("Form", model); }
        if (await dbContext.SavingsCertificates.AnyAsync(x => x.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken)) { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); model.AiSuggestionAvailable = clipboardSuggestionService.IsAvailable; return View("Form", model); }
        var item = CreateEntity(model); item.CreatedBy = UserId(); var reminder = new Reminder(ReminderText(item), model.NextCapitalization, model.NoticeDays) { CreatedBy = UserId() }; reminder.LinkToSavingsCertificate(item.Id); dbContext.AddRange(item, reminder); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado de Aforro adicionado."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestFromClipboard([FromBody] SavingsCertificateClipboardRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Copie texto com até 20 000 caracteres." });
        try { var suggestion = await clipboardSuggestionService.SuggestAsync(model.Text, cancellationToken); logger.LogInformation("Savings Certificate clipboard suggestion processed. Confidence: {Confidence}.", suggestion.Confidence); return Json(suggestion); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { logger.LogWarning(ex, "Savings Certificate clipboard suggestion failed."); return StatusCode(503, new { error = ex.Message }); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { logger.LogWarning(ex, "Savings Certificate clipboard request failed."); return StatusCode(502, new { error = "Não foi possível obter a sugestão. Tente novamente." }); }
    }

    [HttpGet] public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) { var item = await dbContext.SavingsCertificates.Include(x => x.Reminder).SingleOrDefaultAsync(x => x.Id == id, cancellationToken); return item is null ? NotFound() : View("Form", ToForm(item)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(Guid id, SavingsCertificateFormViewModel model, CancellationToken cancellationToken) { if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model); var item = await dbContext.SavingsCertificates.Include(x => x.Reminder).SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (item is null) return NotFound(); if (await dbContext.SavingsCertificates.AnyAsync(x => x.Id != id && x.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken)) { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); return View("Form", model); } item.Update(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization); item.UpdatedBy = UserId(); item.Reminder.Update(ReminderText(item), model.NextCapitalization, model.NoticeDays); item.Reminder.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado e lembrete atualizados."; return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) { var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound(); dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado removido."; return RedirectToAction(nameof(Index)); }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SavingsCertificate CreateEntity(SavingsCertificateFormViewModel m) => new(m.InvestmentDate, m.SeriesNumber, m.Description, m.InvestmentValue, m.Rate, m.CurrentValue, m.NextCapitalization);
    private static SavingsCertificateFormViewModel ToForm(SavingsCertificate x) => new() { Id = x.Id, InvestmentDate = x.InvestmentDate, SeriesNumber = x.SeriesNumber, Description = x.Description, InvestmentValue = x.InvestmentValue, Rate = x.Rate, CurrentValue = x.CurrentValue, NextCapitalization = x.NextCapitalization, NoticeDays = x.Reminder.NoticeDays };
    private static string ReminderText(SavingsCertificate x) => $"Capitalização do Certificado de Aforro {x.SeriesNumber}: {x.Description}";
    private static SavingsCertificateRowViewModel ToRow(SavingsCertificate x, DateOnly today) { var age = today.DayNumber - x.InvestmentDate.DayNumber; var difference = x.NextCapitalization.DayNumber - today.DayNumber; var yield = x.CurrentValue - x.InvestmentValue; var interest = x.CurrentValue * (x.Rate / 100m * .72m / 4m); return new(x.Id, x.InvestmentDate, age, x.SeriesNumber, x.Description, x.InvestmentValue, x.Rate, x.CurrentValue, yield, x.NextCapitalization, difference, interest, x.CurrentValue + interest); }
}
