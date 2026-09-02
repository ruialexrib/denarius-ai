using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>Manages insurance policies, premiums, movement associations, and premium documents.</summary>
/// <param name="dbContext">Application database context.</param>
/// <param name="clipboardSuggestionService">AI service used to interpret insurance clipboard text.</param>
/// <param name="logger">Controller logger.</param>
[Authorize]
public sealed class InsuranceController(DenariusDbContext dbContext, IInsuranceClipboardSuggestionService clipboardSuggestionService, ILogger<InsuranceController> logger) : Controller
{
    /// <summary>Displays the insurance portfolio with optional filters and pagination.</summary>
    /// <param name="search">Free-text search across policy name, insurer, policy number, and insured subject.</param>
    /// <param name="type">Optional insurance type filter.</param>
    /// <param name="status">Optional policy status filter.</param>
    /// <param name="page">Requested page number.</param>
    /// <param name="pageSize">Requested number of policies per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The insurance portfolio view.</returns>
    public async Task<IActionResult> Index(string? search, InsurancePolicyType? type, InsurancePolicyStatus? status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var policies = await dbContext.InsurancePolicies.AsNoTracking().Include(x => x.Premiums).ThenInclude(x => x.JournalEntry).OrderBy(x => x.RenewalDate).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var active = policies.Where(x => x.Status == InsurancePolicyStatus.Active).ToList();
        var premiums = active.SelectMany(x => x.Premiums).ToList();

        IEnumerable<InsurancePolicy> filtered = policies;
        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filtered = filtered.Where(x => x.Name.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase)
                || x.Insurer.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase)
                || x.PolicyNumber.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase)
                || (x.InsuredSubject?.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }
        if (type.HasValue) filtered = filtered.Where(x => x.Type == type.Value);
        if (status.HasValue) filtered = filtered.Where(x => x.Status == status.Value);

        var filteredPolicies = filtered.ToList();
        var pagination = PaginationViewModel.Create(filteredPolicies.Count, page, pageSize);
        var pagePolicies = filteredPolicies.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var model = new InsurancePortfolioViewModel
        {
            Policies = pagePolicies,
            Search = normalizedSearch,
            Type = type,
            Status = status,
            Pagination = pagination,
            ActivePolicies = active.Count,
            AnnualCost = premiums.Where(x => x.PeriodStart.Year == today.Year).Sum(x => x.Amount),
            OutstandingPremiums = premiums.Count(x => x.DueDate <= today && !x.IsPaid),
            UpcomingRenewals = active.Count(x => x.RenewalDate is { } renewal && renewal >= today && renewal <= today.AddDays(30))
        };
        return View(model);
    }

    /// <summary>Displays a policy and its premium history.</summary>
    /// <param name="id">Policy identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The policy detail view or not found.</returns>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var policy = await dbContext.InsurancePolicies.AsNoTracking().Include(x => x.Attachments).Include(x => x.Premiums).ThenInclude(x => x.JournalEntry).Include(x => x.Premiums).ThenInclude(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (policy is null) return NotFound();
        var movements = await dbContext.JournalEntries.AsNoTracking().Where(x => x.Status == JournalEntryStatus.Active)
            .Include(x => x.Lines).OrderByDescending(x => x.Date).ThenBy(x => x.Description).ToListAsync(cancellationToken);
        return View(new InsurancePolicyDetailsViewModel
        {
            Policy = policy,
            AvailableMovements = movements.Select(x => new SelectListItem(
                $"{x.Date:dd/MM/yyyy} · {x.Description} · {x.TotalDebit:N2} €{(string.IsNullOrWhiteSpace(x.Reference) ? string.Empty : $" · {x.Reference}")}",
                x.Id.ToString())).Prepend(new SelectListItem("Selecionar movimento", string.Empty)).ToList()
        });
    }

    /// <summary>Displays the create policy form.</summary><returns>The policy form.</returns>
    [HttpGet] public IActionResult Create() => View("Form", new InsurancePolicyFormViewModel { AiSuggestionAvailable = clipboardSuggestionService.IsAvailable });

    /// <summary>Creates a policy.</summary><param name="model">Policy form.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to the portfolio when successful.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InsurancePolicyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { model.AiSuggestionAvailable = clipboardSuggestionService.IsAvailable; return View("Form", model); }
        var policy = CreatePolicy(model); policy.CreatedBy = UserId(); dbContext.InsurancePolicies.Add(policy); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Apólice adicionada."; return RedirectToAction(nameof(Details), new { id = policy.Id });
    }

    /// <summary>Analyzes clipboard text and returns an editable insurance policy suggestion.</summary>
    /// <param name="model">Clipboard text request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON containing validated fields identified by the model.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestFromClipboard([FromBody] InsuranceClipboardRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { error = "Copie texto com até 20 000 caracteres." });
        try
        {
            var suggestion = await clipboardSuggestionService.SuggestAsync(model.Text, cancellationToken);
            logger.LogInformation("Insurance clipboard suggestion processed. Confidence: {Confidence}.", suggestion.Confidence);
            return Json(suggestion);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Insurance clipboard suggestion could not be completed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = exception.Message });
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Insurance clipboard suggestion request failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Não foi possível obter a sugestão. Tente novamente." });
        }
    }

    /// <summary>Displays the edit policy form.</summary><param name="id">Policy identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The form or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var policy = await dbContext.InsurancePolicies.FindAsync([id], cancellationToken); if (policy is null) return NotFound();
        return View("Form", new InsurancePolicyFormViewModel { Name = policy.Name, Insurer = policy.Insurer, PolicyNumber = policy.PolicyNumber, Type = policy.Type, PaymentFrequency = policy.PaymentFrequency, StartDate = policy.StartDate, EndDate = policy.EndDate, RenewalDate = policy.RenewalDate, InsuredSubject = policy.InsuredSubject, Notes = policy.Notes });
    }

    /// <summary>Updates a policy.</summary><param name="id">Policy identifier.</param><param name="model">Policy form.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to details when successful.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InsurancePolicyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model); var policy = await dbContext.InsurancePolicies.FindAsync([id], cancellationToken); if (policy is null) return NotFound();
        policy.Update(model.Name, model.Insurer, model.PolicyNumber, model.Type, model.PaymentFrequency, model.StartDate, model.EndDate, model.RenewalDate, model.InsuredSubject, model.Notes); policy.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Apólice atualizada."; return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Archives a policy without deleting its history.</summary><param name="id">Policy identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to the portfolio.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) { var policy = await dbContext.InsurancePolicies.FindAsync([id], cancellationToken); if (policy is null) return NotFound(); policy.Archive(); policy.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); return RedirectToAction(nameof(Index)); }

    /// <summary>Cancels a policy without deleting its history.</summary><param name="id">Policy identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to the portfolio.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) { var policy = await dbContext.InsurancePolicies.FindAsync([id], cancellationToken); if (policy is null) return NotFound(); policy.Cancel(); policy.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); return RedirectToAction(nameof(Index)); }

    /// <summary>Reactivates an archived or cancelled policy.</summary><param name="id">Policy identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) { var policy = await dbContext.InsurancePolicies.FindAsync([id], cancellationToken); if (policy is null) return NotFound(); policy.Activate(); policy.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Apólice reativada."; return RedirectToAction(nameof(Details), new { id }); }

    /// <summary>Creates a premium for a policy.</summary><param name="policyId">Policy identifier.</param><param name="model">Premium form.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPremium(Guid policyId, InsurancePremiumFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Revê os dados do prémio."; return RedirectToAction(nameof(Details), new { id = policyId }); }
        var policyStatus = await dbContext.InsurancePolicies.Where(x => x.Id == policyId).Select(x => (InsurancePolicyStatus?)x.Status).SingleOrDefaultAsync(cancellationToken);
        if (policyStatus is null) return NotFound();
        if (policyStatus != InsurancePolicyStatus.Active) { TempData["ErrorMessage"] = "Apenas apólices ativas podem receber novos prémios."; return RedirectToAction(nameof(Details), new { id = policyId }); }
        var premium = new InsurancePremium(policyId, model.Amount, model.PeriodStart, model.PeriodEnd, model.DueDate, model.Reference) { CreatedBy = UserId() }; dbContext.InsurancePremiums.Add(premium); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Prémio adicionado."; return RedirectToAction(nameof(Details), new { id = policyId });
    }

    /// <summary>Associates an existing active financial movement with a premium.</summary><param name="premiumId">Premium identifier.</param><param name="journalEntryId">Movement identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssociateMovement(Guid premiumId, Guid journalEntryId, CancellationToken cancellationToken)
    {
        var premium = await dbContext.InsurancePremiums.FindAsync([premiumId], cancellationToken); if (premium is null) return NotFound();
        if (!await dbContext.JournalEntries.AnyAsync(x => x.Id == journalEntryId && x.Status == JournalEntryStatus.Active, cancellationToken)) { TempData["ErrorMessage"] = "O movimento selecionado não está ativo."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId }); }
        premium.AssociateMovement(journalEntryId); premium.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Pagamento associado ao movimento."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId });
    }

    /// <summary>Removes a premium movement association without changing accounting data.</summary><param name="premiumId">Premium identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMovement(Guid premiumId, CancellationToken cancellationToken) { var premium = await dbContext.InsurancePremiums.FindAsync([premiumId], cancellationToken); if (premium is null) return NotFound(); premium.RemoveMovementAssociation(); premium.UpdatedBy = UserId(); await dbContext.SaveChangesAsync(cancellationToken); return RedirectToAction(nameof(Details), new { id = premium.PolicyId }); }

    /// <summary>Uploads a general PDF document directly to a policy.</summary><param name="policyId">Policy identifier.</param><param name="file">PDF file.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadPolicyAttachment(Guid policyId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!await dbContext.InsurancePolicies.AnyAsync(x => x.Id == policyId, cancellationToken)) return NotFound();
        var content = await ReadPdfAsync(file, cancellationToken);
        if (content is null) { TempData["ErrorMessage"] = "Seleciona um ficheiro PDF válido com até 5 MB."; return RedirectToAction(nameof(Details), new { id = policyId }); }
        var attachment = new InsurancePolicyAttachment(policyId, Path.GetFileName(file.FileName), "application/pdf", Convert.ToBase64String(content)) { CreatedBy = UserId() };
        dbContext.InsurancePolicyAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Documento geral adicionado à apólice.";
        return RedirectToAction(nameof(Details), new { id = policyId });
    }

    /// <summary>Downloads a general insurance policy document.</summary><param name="id">Attachment identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The PDF file or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> PolicyAttachment(Guid id, CancellationToken cancellationToken) { var item = await dbContext.InsurancePolicyAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken); return item is null ? NotFound() : File(Convert.FromBase64String(item.DocumentBase64), item.ContentType, item.FileName); }

    /// <summary>Uploads a PDF supporting document for a premium.</summary><param name="premiumId">Premium identifier.</param><param name="file">PDF file.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadAttachment(Guid premiumId, IFormFile file, CancellationToken cancellationToken)
    {
        var premium = await dbContext.InsurancePremiums.FindAsync([premiumId], cancellationToken); if (premium is null) return NotFound();
        var content = await ReadPdfAsync(file, cancellationToken);
        if (content is null) { TempData["ErrorMessage"] = "Seleciona um ficheiro PDF válido com até 5 MB."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId }); }
        var attachment = new InsurancePremiumAttachment(premiumId, Path.GetFileName(file.FileName), "application/pdf", Convert.ToBase64String(content)) { CreatedBy = UserId() }; dbContext.InsurancePremiumAttachments.Add(attachment); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Documento adicionado ao prémio."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId });
    }

    /// <summary>Downloads a premium supporting document.</summary><param name="id">Attachment identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The PDF file or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Attachment(Guid id, CancellationToken cancellationToken) { var item = await dbContext.InsurancePremiumAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken); return item is null ? NotFound() : File(Convert.FromBase64String(item.DocumentBase64), item.ContentType, item.FileName); }

    /// <summary>Reads and validates a PDF upload using its declared type, size, and file signature.</summary><param name="file">Uploaded file.</param><param name="cancellationToken">Cancellation token.</param><returns>Validated PDF bytes, or null when invalid.</returns>
    private static async Task<byte[]?> ReadPdfAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 5_000_000 || !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return null;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var content = stream.ToArray();
        return content.Length >= 5 && content[0] == '%' && content[1] == 'P' && content[2] == 'D' && content[3] == 'F' && content[4] == '-' ? content : null;
    }

    /// <summary>Creates a domain policy from form data.</summary><param name="model">Policy form.</param><returns>A new policy.</returns>
    private static InsurancePolicy CreatePolicy(InsurancePolicyFormViewModel model) => new(model.Name, model.Insurer, model.PolicyNumber, model.Type, model.PaymentFrequency, model.StartDate, model.EndDate, model.RenewalDate, model.InsuredSubject, model.Notes);
    /// <summary>Gets the current user identifier.</summary><returns>User identifier.</returns><exception cref="InvalidOperationException">Thrown when no user is identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
