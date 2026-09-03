using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages Portuguese Savings Certificate positions and related views.
/// </summary>
/// <param name="dbContext">The database context for accessing savings certificates data.</param>
/// <param name="clipboardSuggestionService">The service that proposes certificate fields from copied text.</param>
/// <param name="logger">The application logger.</param>
[Authorize]
public sealed class SavingsCertificatesController(
    DenariusDbContext dbContext,
    ISavingsCertificateClipboardSuggestionService clipboardSuggestionService,
    ILogger<SavingsCertificatesController> logger) : Controller
{
    /// <summary>Displays a paginated, filterable, and sortable list of savings certificates.</summary>
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
        return View(new SavingsCertificateIndexViewModel(rows, allRows.Sum(item => item.InvestmentValue), allRows.Sum(item => item.CurrentValue), allRows.Sum(item => item.Yield), allRows.Sum(item => item.FutureNetInterest), allRows.Sum(item => item.FutureValue), from, to, search, sort, [new("Data — mais antiga", "date-asc", sort == "date-asc"), new("Data — mais recente", "date-desc", sort == "date-desc"), new("Maior valor atual", "value-desc", sort == "value-desc"), new("Maior rendimento", "yield-desc", sort == "yield-desc"), new("Série/Número", "series", sort == "series")], pagination));
    }

    /// <summary>Displays the form to create a new savings certificate.</summary>
    [HttpGet]
    public IActionResult Create() => View("Form", new SavingsCertificateFormViewModel { AiSuggestionAvailable = clipboardSuggestionService.IsAvailable });

    /// <summary>Processes the creation of a new savings certificate.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { model.AiSuggestionAvailable = clipboardSuggestionService.IsAvailable; return View("Form", model); }
        if (await dbContext.SavingsCertificates.AnyAsync(item => item.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken))
        { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); model.AiSuggestionAvailable = clipboardSuggestionService.IsAvailable; return View("Form", model); }
        var item = CreateEntity(model); item.CreatedBy = UserId();
        var reminder = new Reminder(ReminderText(item), model.NextCapitalization, model.NoticeDays) { CreatedBy = UserId() }; reminder.LinkToSavingsCertificate(item.Id);
        dbContext.AddRange(item, reminder); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Certificado de Aforro adicionado."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Extracts a proposed certificate from clipboard text without persisting it.</summary>
    /// <param name="model">Clipboard text to interpret.</param>
    /// <param name="cancellationToken">Cancellation token for the language-model request.</param>
    /// <returns>A JSON suggestion for the editable certificate fields.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestFromClipboard([FromBody] SavingsCertificateClipboardRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Copie texto com até 20 000 caracteres." });
        try
        {
            var suggestion = await clipboardSuggestionService.SuggestAsync(model.Text, cancellationToken);
            logger.LogInformation("Savings Certificate clipboard suggestion processed. Confidence: {Confidence}.", suggestion.Confidence);
            return Json(suggestion);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { logger.LogWarning(ex, "Savings Certificate clipboard suggestion failed."); return StatusCode(503, new { error = ex.Message }); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { logger.LogWarning(ex, "Savings Certificate clipboard request failed."); return StatusCode(502, new { error = "Não foi possível obter a sugestão. Tente novamente." }); }
    }

    /// <summary>Displays the form to edit an existing savings certificate.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.Include(value => value.Reminder).SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        return View("Form", ToForm(item));
    }

    /// <summary>Processes the update of an existing savings certificate.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        var item = await dbContext.SavingsCertificates.Include(value => value.Reminder).SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        if (await dbContext.SavingsCertificates.AnyAsync(other => other.Id != id && other.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken))
        { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); return View("Form", model); }
        item.Update(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);
        item.UpdatedBy = UserId(); item.Reminder.Update(ReminderText(item), model.NextCapitalization, model.NoticeDays); item.Reminder.UpdatedBy = UserId();
        await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado e lembrete atualizados."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes a savings certificate from the database.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado removido."; return RedirectToAction(nameof(Index));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    private static SavingsCertificate CreateEntity(SavingsCertificateFormViewModel model) => new(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);
    private static SavingsCertificateFormViewModel ToForm(SavingsCertificate item) => new() { Id = item.Id, InvestmentDate = item.InvestmentDate, SeriesNumber = item.SeriesNumber, Description = item.Description, InvestmentValue = item.InvestmentValue, Rate = item.Rate, CurrentValue = item.CurrentValue, NextCapitalization = item.NextCapitalization, NoticeDays = item.Reminder.NoticeDays };
    private static string ReminderText(SavingsCertificate item) => $"Capitalização do Certificado de Aforro {item.SeriesNumber}: {item.Description}";
    private static SavingsCertificateRowViewModel ToRow(SavingsCertificate item, DateOnly today)
    {
        var age = today.DayNumber - item.InvestmentDate.DayNumber;
        var difference = item.NextCapitalization.DayNumber - today.DayNumber;
        var yield = item.CurrentValue - item.InvestmentValue;
        var futureNetInterest = item.CurrentValue * (item.Rate / 100m * .72m / 4m);
        return new(item.Id, item.InvestmentDate, age, item.SeriesNumber, item.Description, item.InvestmentValue, item.Rate, item.CurrentValue, yield, item.NextCapitalization, difference, futureNetInterest, item.CurrentValue + futureNetInterest);
    }
}
