using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class WarrantiesController(DenariusDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Warranties.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Name.Contains(term) || (item.Supplier != null && item.Supplier.Contains(term)));
        }
        var items = await query.OrderBy(item => item.ExpiryDate).Select(item =>
            new WarrantyRowViewModel(item.Id, item.Name, item.Supplier, item.PurchaseDate, item.ExpiryDate, item.DocumentFileName)).ToListAsync(cancellationToken);
        return View(new WarrantyIndexViewModel(items, search?.Trim()));
    }

    [HttpGet] public IActionResult Create() => View("Form", new WarrantyFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarrantyFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateDates(model);
        if (!ModelState.IsValid) return View("Form", model);
        (string FileName, string Base64)? document = null;
        if (model.Document is not null) { document = await ReadDocumentAsync(model.Document, nameof(model.Document), cancellationToken); if (document is null) return View("Form", model); }
        var item = new Warranty(model.Name, model.Supplier, model.PurchaseDate, model.ExpiryDate, model.Notes, document?.FileName, document?.Base64) { CreatedBy = UserId() };
        var reminder = new Reminder(ReminderText(model.Name), model.ExpiryDate, model.NoticeDays) { CreatedBy = UserId() }; reminder.LinkToWarranty(item.Id);
        dbContext.AddRange(item, reminder); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Garantia registada."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Warranties.Include(value => value.Reminder).SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        return View("Form", new WarrantyFormViewModel { Id = item.Id, Name = item.Name, Supplier = item.Supplier, PurchaseDate = item.PurchaseDate, ExpiryDate = item.ExpiryDate, NoticeDays = item.Reminder.NoticeDays, Notes = item.Notes, ExistingDocumentFileName = item.DocumentFileName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WarrantyFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); ValidateDates(model); if (!ModelState.IsValid) return View("Form", model);
        var item = await dbContext.Warranties.Include(value => value.Reminder).SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        (string FileName, string Base64)? document = null;
        if (model.Document is not null) { document = await ReadDocumentAsync(model.Document, nameof(model.Document), cancellationToken); if (document is null) return View("Form", model); }
        item.Update(model.Name, model.Supplier, model.PurchaseDate, model.ExpiryDate, model.Notes, document?.FileName, document?.Base64); item.UpdatedBy = UserId();
        item.Reminder.Update(ReminderText(model.Name), model.ExpiryDate, model.NoticeDays); item.Reminder.UpdatedBy = UserId();
        await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Garantia atualizada."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Document(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Warranties.AsNoTracking().Where(value => value.Id == id).Select(value => new { value.DocumentBase64, value.DocumentFileName }).SingleOrDefaultAsync(cancellationToken);
        if (item?.DocumentBase64 is null) return NotFound();
        try { return File(Convert.FromBase64String(item.DocumentBase64), "application/pdf", enableRangeProcessing: true); }
        catch (FormatException) { return Problem("O documento guardado está danificado.", statusCode: StatusCodes.Status500InternalServerError); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Warranties.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Garantia removida."; return RedirectToAction(nameof(Index));
    }

    private void ValidateDates(WarrantyFormViewModel model) { if (model.ExpiryDate < model.PurchaseDate) ModelState.AddModelError(nameof(model.ExpiryDate), "A data de fim não pode ser anterior à data de compra."); }
    private static string ReminderText(string name) => $"Fim da garantia: {name.Trim()}";
    private async Task<(string FileName, string Base64)?> ReadDocumentAsync(IFormFile file, string field, CancellationToken token) { try { return await PdfUploadReader.ReadAsync(file, token); } catch (InvalidOperationException exception) { ModelState.AddModelError(field, exception.Message); return null; } }
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
