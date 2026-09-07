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
/// <param name="rateService">The service that imports and reads official Savings Certificate reference rates.</param>
/// <param name="rateForecastService">The deterministic service that forecasts the next monthly reference rate.</param>
/// <param name="logger">The application logger.</param>
[Authorize]
public sealed class SavingsCertificatesController(
    DenariusDbContext dbContext,
    ISavingsCertificateClipboardSuggestionService clipboardSuggestionService,
    ISavingsCertificateRateService rateService,
    ISavingsCertificateRateForecastService rateForecastService,
    ILogger<SavingsCertificatesController> logger) : Controller
{
    /// <summary>Displays a paginated, filterable, and sortable list of savings certificates.</summary>
    /// <param name="from">Optional earliest investment date.</param>
    /// <param name="to">Optional latest investment date.</param>
    /// <param name="search">Optional series-number or description filter.</param>
    /// <param name="sort">Requested sort order.</param>
    /// <param name="page">Requested page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="cancellationToken">Token used to cancel database access.</param>
    /// <returns>The Savings Certificates portfolio view.</returns>
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

    /// <summary>Displays the stored official Savings Certificate reference-rate history.</summary>
    /// <param name="months">Recent period to display: 3, 6, or 12 calendar months.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The reference-rate history view.</returns>
    [HttpGet]
    public async Task<IActionResult> RateHistory(int months = 12, CancellationToken cancellationToken = default)
    {
        months = months is 3 or 6 or 12 ? months : 12;
        var history = await rateService.GetHistoryAsync(months, cancellationToken);
        var forecastHistory = months == 12 ? history : await rateService.GetHistoryAsync(12, cancellationToken);
        var forecast = rateForecastService.Forecast(forecastHistory.Observations);
        return View(new SavingsCertificateRateHistoryViewModel(months, history.Observations, history.UpdatedAt, history.SourceName, history.SourceUrl, forecast));
    }

    /// <summary>Refreshes the official Savings Certificate reference-rate history from IGCP.</summary>
    /// <param name="months">Period to return to after the refresh.</param>
    /// <param name="cancellationToken">Token used to cancel provider and persistence access.</param>
    /// <returns>A redirect to the reference-rate history view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshRateHistory(int months = 12, CancellationToken cancellationToken = default)
    {
        months = months is 3 or 6 or 12 ? months : 12;
        try
        {
            var result = await rateService.RefreshAsync(UserId(), cancellationToken);
            TempData["SuccessMessage"] = $"Histórico atualizado a partir do IGCP: {result.ImportedCount} taxas recolhidas.";
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Savings Certificate reference-rate refresh returned no valid observations.");
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Savings Certificate reference-rate refresh failed.");
            TempData["ErrorMessage"] = "Não foi possível contactar o IGCP. O histórico anteriormente guardado foi mantido.";
        }

        return RedirectToAction(nameof(RateHistory), new { months });
    }

    /// <summary>Displays the form to create a new savings certificate.</summary>
    /// <returns>The creation form.</returns>
    [HttpGet]
    public IActionResult Create() => View("Form", new SavingsCertificateFormViewModel { AiSuggestionAvailable = clipboardSuggestionService.IsAvailable });

    /// <summary>Processes the creation of a new savings certificate.</summary>
    /// <param name="model">Submitted certificate data.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The form on validation failure or the certificate list after success.</returns>
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
    /// <param name="id">Certificate identifier.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The edit form or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.Include(value => value.Reminder).SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        return View("Form", ToForm(item));
    }

    /// <summary>Processes the update of an existing savings certificate.</summary>
    /// <param name="id">Certificate identifier.</param>
    /// <param name="model">Submitted certificate data.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The form on validation failure, not found, or the certificate list after success.</returns>
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
    /// <param name="id">Certificate identifier.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The certificate list or not found.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado removido."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Gets the authenticated user identifier.</summary>
    /// <returns>The authenticated user identifier.</returns>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");

    /// <summary>Creates a domain entity from the submitted certificate form.</summary>
    /// <param name="model">Submitted certificate data.</param>
    /// <returns>The new Savings Certificate entity.</returns>
    private static SavingsCertificate CreateEntity(SavingsCertificateFormViewModel model) => new(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);

    /// <summary>Maps a persisted Savings Certificate to the edit form.</summary>
    /// <param name="item">Persisted certificate.</param>
    /// <returns>The populated form model.</returns>
    private static SavingsCertificateFormViewModel ToForm(SavingsCertificate item) => new() { Id = item.Id, InvestmentDate = item.InvestmentDate, SeriesNumber = item.SeriesNumber, Description = item.Description, InvestmentValue = item.InvestmentValue, Rate = item.Rate, CurrentValue = item.CurrentValue, NextCapitalization = item.NextCapitalization, NoticeDays = item.Reminder.NoticeDays };

    /// <summary>Builds the reminder text associated with a Savings Certificate.</summary>
    /// <param name="item">Certificate whose capitalization will be reminded.</param>
    /// <returns>The reminder description.</returns>
    private static string ReminderText(SavingsCertificate item) => $"Capitalização do Certificado de Aforro {item.SeriesNumber}: {item.Description}";

    /// <summary>Builds a deterministic portfolio row for a Savings Certificate.</summary>
    /// <param name="item">Persisted certificate.</param>
    /// <param name="today">Current date used for age and capitalization calculations.</param>
    /// <returns>The calculated portfolio row.</returns>
    private static SavingsCertificateRowViewModel ToRow(SavingsCertificate item, DateOnly today)
    {
        var age = today.DayNumber - item.InvestmentDate.DayNumber;
        var difference = item.NextCapitalization.DayNumber - today.DayNumber;
        var yield = item.CurrentValue - item.InvestmentValue;
        var futureNetInterest = item.CurrentValue * (item.Rate / 100m * .72m / 4m);
        return new(item.Id, item.InvestmentDate, age, item.SeriesNumber, item.Description, item.InvestmentValue, item.Rate, item.CurrentValue, yield, item.NextCapitalization, difference, futureNetInterest, item.CurrentValue + futureNetInterest);
    }
}
