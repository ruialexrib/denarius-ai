using System.Security.Claims;
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
[Authorize]
public sealed class SavingsCertificatesController(DenariusDbContext dbContext) : Controller
{
    /// <summary>
    /// Displays a paginated, filterable, and sortable list of savings certificates.
    /// </summary>
    /// <param name="from">Optional start date filter for investment date.</param>
    /// <param name="to">Optional end date filter for investment date.</param>
    /// <param name="search">Optional search term for series number or description.</param>
    /// <param name="sort">Sort order for the list (default: "date-asc").</param>
    /// <param name="page">Current page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The index view with filtered and sorted savings certificates.</returns>
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

    /// <summary>
    /// Displays the form to create a new savings certificate.
    /// </summary>
    /// <returns>The create form view.</returns>
    [HttpGet]
    public IActionResult Create() => View("Form", new SavingsCertificateFormViewModel());

    /// <summary>
    /// Processes the creation of a new savings certificate.
    /// </summary>
    /// <param name="model">The form data for the new certificate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to index on success, or returns form with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingsCertificateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        if (await dbContext.SavingsCertificates.AnyAsync(item => item.SeriesNumber == model.SeriesNumber.Trim(), cancellationToken))
        { ModelState.AddModelError(nameof(model.SeriesNumber), "Já existe um certificado com esta série/número."); return View("Form", model); }
        var item = CreateEntity(model); item.CreatedBy = UserId(); dbContext.Add(item); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Certificado de Aforro adicionado."; return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the form to edit an existing savings certificate.
    /// </summary>
    /// <param name="id">The unique identifier of the certificate to edit.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The edit form view or NotFound if certificate doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        return View("Form", ToForm(item));
    }

    /// <summary>
    /// Processes the update of an existing savings certificate.
    /// </summary>
    /// <param name="id">The unique identifier of the certificate to update.</param>
    /// <param name="model">The form data with updated values.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to index on success, or returns form with validation errors.</returns>
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

    /// <summary>
    /// Deletes a savings certificate from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the certificate to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to index on success, or NotFound if certificate doesn't exist.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SavingsCertificates.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Certificado removido."; return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Gets the current user's identifier from claims.
    /// </summary>
    /// <returns>The user ID string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user is not identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
    
    /// <summary>
    /// Creates a new SavingsCertificate entity from form data.
    /// </summary>
    /// <param name="model">The form view model containing certificate data.</param>
    /// <returns>A new SavingsCertificate entity.</returns>
    private static SavingsCertificate CreateEntity(SavingsCertificateFormViewModel model) => new(model.InvestmentDate, model.SeriesNumber, model.Description, model.InvestmentValue, model.Rate, model.CurrentValue, model.NextCapitalization);
    
    /// <summary>
    /// Converts a SavingsCertificate entity to a form view model.
    /// </summary>
    /// <param name="item">The savings certificate entity.</param>
    /// <returns>A form view model populated with entity data.</returns>
    private static SavingsCertificateFormViewModel ToForm(SavingsCertificate item) => new() { Id = item.Id, InvestmentDate = item.InvestmentDate, SeriesNumber = item.SeriesNumber, Description = item.Description, InvestmentValue = item.InvestmentValue, Rate = item.Rate, CurrentValue = item.CurrentValue, NextCapitalization = item.NextCapitalization };
    
    /// <summary>
    /// Converts a SavingsCertificate entity to a row view model with calculated values.
    /// </summary>
    /// <param name="item">The savings certificate entity.</param>
    /// <param name="today">The current date for calculations.</param>
    /// <returns>A row view model with certificate data and calculated metrics.</returns>
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
