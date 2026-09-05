using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>Coordinates correspondence documents and user-confirmed metadata.</summary>
[Authorize]
public sealed class CorrespondenceController(
    DenariusDbContext dbContext,
    ICorrespondenceMetadataSuggestionService metadataSuggestionService) : Controller
{
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Correspondence.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(item => item.Subject.Contains(term) || (item.Sender != null && item.Sender.Contains(term))); }
        var items = await query.OrderByDescending(item => item.ReceivedDate).Select(item => new CorrespondenceRowViewModel(item.Id, item.Subject, item.Sender, item.ReceivedDate, item.DocumentFileName, item.Metadata.Count)).ToListAsync(cancellationToken);
        return View(new CorrespondenceIndexViewModel(items, search?.Trim()));
    }

    [HttpGet] public IActionResult Create() => View("Form", new CorrespondenceFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CorrespondenceFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        (string FileName, string Base64)? document = null;
        if (model.Document is not null) { document = await ReadDocumentAsync(model.Document, cancellationToken); if (document is null) return View("Form", model); }
        var item = new Correspondence(model.Subject, model.Sender, model.ReceivedDate, model.Notes, document?.FileName, document?.Base64) { CreatedBy = UserId() };
        dbContext.Add(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Correspondência registada."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        return View("Form", new CorrespondenceFormViewModel { Id = item.Id, Subject = item.Subject, Sender = item.Sender, ReceivedDate = item.ReceivedDate, Notes = item.Notes, ExistingDocumentFileName = item.DocumentFileName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CorrespondenceFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        var item = await dbContext.Correspondence.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        (string FileName, string Base64)? document = null;
        if (model.Document is not null) { document = await ReadDocumentAsync(model.Document, cancellationToken); if (document is null) return View("Form", model); }
        item.Update(model.Subject, model.Sender, model.ReceivedDate, model.Notes, document?.FileName, document?.Base64); item.UpdatedBy = UserId();
        await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Correspondência atualizada."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Document(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.AsNoTracking().Where(value => value.Id == id).Select(value => new { value.DocumentBase64 }).SingleOrDefaultAsync(cancellationToken);
        if (item?.DocumentBase64 is null) return NotFound();
        try { return File(Convert.FromBase64String(item.DocumentBase64), "application/pdf", enableRangeProcessing: true); }
        catch (FormatException) { return Problem("O documento guardado está danificado.", statusCode: StatusCodes.Status500InternalServerError); }
    }

    [HttpGet]
    public async Task<IActionResult> Metadata(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.AsNoTracking().Include(value => value.Metadata)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (item is null) return NotFound();
        return View(ToMetadataPage(item));
    }

    /// <summary>Proposes PDF metadata without saving financial or document records.</summary>
    /// <param name="id">The correspondence identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The metadata proposal or validation feedback.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeMetadata(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.AsNoTracking().Include(value => value.Metadata)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (item is null) return NotFound();
        var model = ToMetadataPage(item);
        try
        {
            var suggestion = await metadataSuggestionService.SuggestAsync(item.DocumentBase64 ?? string.Empty, cancellationToken);
            model.Items = suggestion.Metadata.Select(value => new CorrespondenceMetadataRowViewModel
            {
                Key = value.Key, Value = value.Value, Confidence = value.Confidence
            }).ToList();
            model.IsProposal = true; model.ExtractedCharacters = suggestion.ExtractedCharacters; model.ExtractedPages = suggestion.ExtractedPages;
            return View("Metadata", model);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message); return View("Metadata", model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Não foi possível contactar o fornecedor de IA. Tente novamente."); return View("Metadata", model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMetadata(CorrespondenceMetadataPageViewModel model, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.Include(value => value.Metadata)
            .SingleOrDefaultAsync(value => value.Id == model.CorrespondenceId, cancellationToken);
        if (item is null) return NotFound();
        model.Subject = item.Subject; model.HasDocument = item.DocumentBase64 is not null;
        for (var index = 0; index < model.Items.Count; index++)
        {
            var row = model.Items[index];
            if (!row.Remove && (!string.IsNullOrWhiteSpace(row.Key) || !string.IsNullOrWhiteSpace(row.Value))) continue;
            ModelState.Remove($"Items[{index}].Key"); ModelState.Remove($"Items[{index}].Value");
        }
        var rows = model.Items.Where(value => !value.Remove && (!string.IsNullOrWhiteSpace(value.Key) || !string.IsNullOrWhiteSpace(value.Value))).ToList();
        if (rows.Count > 30) ModelState.AddModelError(nameof(model.Items), "Pode guardar no máximo 30 metadados por correspondência.");
        foreach (var duplicate in rows.GroupBy(value => value.Key?.Trim(), StringComparer.CurrentCultureIgnoreCase).Where(group => group.Key is not null && group.Count() > 1))
            ModelState.AddModelError(nameof(model.Items), $"A chave «{duplicate.Key}» está repetida.");
        if (!ModelState.IsValid) return View("Metadata", model);

        dbContext.CorrespondenceMetadata.RemoveRange(item.Metadata);
        foreach (var row in rows)
            dbContext.CorrespondenceMetadata.Add(new CorrespondenceMetadata(item.Id, row.Key, row.Value, row.Confidence is "high" or "low" ? row.Confidence : null) { CreatedBy = UserId() });
        item.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Metadados da correspondência guardados."; return RedirectToAction(nameof(Metadata), new { id = item.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Correspondence.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Correspondência removida."; return RedirectToAction(nameof(Index));
    }

    private async Task<(string FileName, string Base64)?> ReadDocumentAsync(IFormFile file, CancellationToken token) { try { return await PdfUploadReader.ReadAsync(file, token); } catch (InvalidOperationException exception) { ModelState.AddModelError(nameof(CorrespondenceFormViewModel.Document), exception.Message); return null; } }
    private static CorrespondenceMetadataPageViewModel ToMetadataPage(Correspondence item) => new()
    {
        CorrespondenceId = item.Id, Subject = item.Subject, HasDocument = item.DocumentBase64 is not null,
        Items = item.Metadata.OrderBy(value => value.Key).Select(value => new CorrespondenceMetadataRowViewModel
        {
            Id = value.Id, Key = value.Key, Value = value.Value, Confidence = value.Confidence
        }).ToList()
    };
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
