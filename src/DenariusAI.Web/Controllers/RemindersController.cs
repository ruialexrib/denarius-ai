using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages reminder schedules and acknowledgement state for users.
/// </summary>
[Authorize]
public sealed class RemindersController(DenariusDbContext dbContext) : Controller
{
    /// <summary>
    /// Displays a list of reminders with optional filtering by search term, status, and date range.
    /// </summary>
    /// <param name="search">Optional search term to filter reminders by text.</param>
    /// <param name="status">Filter by status: "all", "active", "scheduled", or "acknowledged". Default is "all".</param>
    /// <param name="from">Optional start date for filtering reminders.</param>
    /// <param name="to">Optional end date for filtering reminders.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The index view with filtered reminders and summary statistics.</returns>
    public async Task<IActionResult> Index(string? search, string status = "all", DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var userId = UserId();
        var items = await dbContext.Reminders.AsNoTracking().OrderBy(item => item.EventDate).ToListAsync(cancellationToken);
        var acknowledged = await dbContext.ReminderAcknowledgements.AsNoTracking().Where(item => item.UserId == userId).Select(item => item.ReminderId).ToListAsync(cancellationToken);
        var allRows = items.Select(item => new ReminderRowViewModel(item.Id, item.Text, item.EventDate, item.NoticeDays,
            item.EventDate.AddDays(-item.NoticeDays) <= today, acknowledged.Contains(item.Id), item.EventDate.DayNumber - today.DayNumber)).ToList();
        var normalizedStatus = status is "active" or "scheduled" or "acknowledged" ? status : "all";
        var rows = allRows.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(item => item.Text.Contains(search.Trim(), StringComparison.CurrentCultureIgnoreCase));
        if (from.HasValue) rows = rows.Where(item => item.EventDate >= from.Value);
        if (to.HasValue) rows = rows.Where(item => item.EventDate <= to.Value);
        rows = normalizedStatus switch
        {
            "active" => rows.Where(item => item.IsAvailable && !item.IsAcknowledged),
            "scheduled" => rows.Where(item => !item.IsAvailable && !item.IsAcknowledged),
            "acknowledged" => rows.Where(item => item.IsAcknowledged),
            _ => rows
        };
        return View(new ReminderIndexViewModel(
            rows.ToList(),
            allRows.Count(item => item.IsAvailable && !item.IsAcknowledged),
            allRows.Count(item => !item.IsAvailable && !item.IsAcknowledged),
            allRows.Count(item => item.IsAcknowledged),
            allRows.Count,
            search?.Trim(), normalizedStatus, from, to));
    }

    /// <summary>
    /// Displays the form to create a new reminder.
    /// </summary>
    /// <returns>The reminder creation form view.</returns>
    [HttpGet] public IActionResult Create() => View("Form", new ReminderFormViewModel());
    
    /// <summary>
    /// Processes the creation of a new reminder.
    /// </summary>
    /// <param name="model">The reminder form data submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to the index on success, or returns the form with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReminderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var item = new Reminder(model.Text, model.EventDate, model.NoticeDays) { CreatedBy = UserId() };
        dbContext.Add(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Lembrete criado."; return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the form to edit an existing reminder.
    /// </summary>
    /// <param name="id">The unique identifier of the reminder to edit.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The reminder edit form view, or NotFound if the reminder doesn't exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Reminders.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        return View("Form", new ReminderFormViewModel { Id = item.Id, Text = item.Text, EventDate = item.EventDate, NoticeDays = item.NoticeDays });
    }

    /// <summary>
    /// Processes the update of an existing reminder and resets all user acknowledgements.
    /// </summary>
    /// <param name="id">The unique identifier of the reminder to update.</param>
    /// <param name="model">The reminder form data submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to the index on success, or returns the form with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ReminderFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model);
        var item = await dbContext.Reminders.FindAsync([id], cancellationToken); if (item is null) return NotFound();
        item.Update(model.Text, model.EventDate, model.NoticeDays); item.UpdatedBy = UserId();
        dbContext.ReminderAcknowledgements.RemoveRange(dbContext.ReminderAcknowledgements.Where(value => value.ReminderId == id));
        await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Lembrete atualizado e reativado para todos os utilizadores."; return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Records that the current user has acknowledged a specific reminder.
    /// </summary>
    /// <param name="id">The unique identifier of the reminder to acknowledge.</param>
    /// <param name="returnUrl">Optional URL to redirect to after acknowledgement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to the return URL or index page.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!await dbContext.Reminders.AnyAsync(item => item.Id == id, cancellationToken)) return NotFound(); var userId = UserId();
        if (!await dbContext.ReminderAcknowledgements.AnyAsync(item => item.ReminderId == id && item.UserId == userId, cancellationToken))
        { dbContext.Add(new ReminderAcknowledgement { ReminderId = id, UserId = userId, AcknowledgedAt = DateTimeOffset.UtcNow }); await dbContext.SaveChangesAsync(cancellationToken); }
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    /// <summary>
    /// Deletes a reminder and all associated acknowledgements.
    /// </summary>
    /// <param name="id">The unique identifier of the reminder to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Redirects to the index page after successful deletion.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    { var item = await dbContext.Reminders.FindAsync([id], cancellationToken); if (item is null) return NotFound(); dbContext.Remove(item); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Lembrete removido."; return RedirectToAction(nameof(Index)); }

    /// <summary>
    /// Retrieves the current authenticated user's identifier.
    /// </summary>
    /// <returns>The user's unique identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not authenticated.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
