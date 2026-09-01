using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>Manages insurance policies, premiums, movement associations, and premium documents.</summary>
/// <param name="dbContext">Application database context.</param>
[Authorize]
public sealed class InsuranceController(DenariusDbContext dbContext) : Controller
{
    /// <summary>Displays the insurance portfolio.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The insurance portfolio view.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var policies = await dbContext.InsurancePolicies.AsNoTracking().Include(x => x.Premiums).ThenInclude(x => x.JournalEntry).OrderBy(x => x.RenewalDate).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var active = policies.Where(x => x.Status == InsurancePolicyStatus.Active).ToList();
        var premiums = active.SelectMany(x => x.Premiums).ToList();
        var model = new InsurancePortfolioViewModel
        {
            Policies = policies,
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
        var policy = await dbContext.InsurancePolicies.AsNoTracking().Include(x => x.Premiums).ThenInclude(x => x.JournalEntry).Include(x => x.Premiums).ThenInclude(x => x.Attachments).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return policy is null ? NotFound() : View(policy);
    }

    /// <summary>Displays the create policy form.</summary><returns>The policy form.</returns>
    [HttpGet] public IActionResult Create() => View("Form", new InsurancePolicyFormViewModel());

    /// <summary>Creates a policy.</summary><param name="model">Policy form.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to the portfolio when successful.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InsurancePolicyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var policy = CreatePolicy(model); policy.CreatedBy = UserId(); dbContext.InsurancePolicies.Add(policy); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Apólice adicionada."; return RedirectToAction(nameof(Details), new { id = policy.Id });
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

    /// <summary>Creates a premium for a policy.</summary><param name="policyId">Policy identifier.</param><param name="model">Premium form.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPremium(Guid policyId, InsurancePremiumFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Revê os dados do prémio."; return RedirectToAction(nameof(Details), new { id = policyId }); }
        if (!await dbContext.InsurancePolicies.AnyAsync(x => x.Id == policyId, cancellationToken)) return NotFound();
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

    /// <summary>Uploads a PDF supporting document for a premium.</summary><param name="premiumId">Premium identifier.</param><param name="file">PDF file.</param><param name="cancellationToken">Cancellation token.</param><returns>Redirects to policy details.</returns>
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadAttachment(Guid premiumId, IFormFile file, CancellationToken cancellationToken)
    {
        var premium = await dbContext.InsurancePremiums.FindAsync([premiumId], cancellationToken); if (premium is null) return NotFound();
        if (file is null || file.Length == 0 || !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "Seleciona um ficheiro PDF válido."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId }); }
        if (file.Length > 5_000_000) { TempData["ErrorMessage"] = "O ficheiro não pode exceder 5 MB."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId }); }
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, cancellationToken); var attachment = new InsurancePremiumAttachment(premiumId, Path.GetFileName(file.FileName), file.ContentType, Convert.ToBase64String(stream.ToArray())) { CreatedBy = UserId() }; dbContext.InsurancePremiumAttachments.Add(attachment); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "Documento adicionado ao prémio."; return RedirectToAction(nameof(Details), new { id = premium.PolicyId });
    }

    /// <summary>Downloads a premium supporting document.</summary><param name="id">Attachment identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The PDF file or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Attachment(Guid id, CancellationToken cancellationToken) { var item = await dbContext.InsurancePremiumAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken); return item is null ? NotFound() : File(Convert.FromBase64String(item.DocumentBase64), item.ContentType, item.FileName); }

    /// <summary>Creates a domain policy from form data.</summary><param name="model">Policy form.</param><returns>A new policy.</returns>
    private static InsurancePolicy CreatePolicy(InsurancePolicyFormViewModel model) => new(model.Name, model.Insurer, model.PolicyNumber, model.Type, model.PaymentFrequency, model.StartDate, model.EndDate, model.RenewalDate, model.InsuredSubject, model.Notes);
    /// <summary>Gets the current user identifier.</summary><returns>User identifier.</returns><exception cref="InvalidOperationException">Thrown when no user is identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
