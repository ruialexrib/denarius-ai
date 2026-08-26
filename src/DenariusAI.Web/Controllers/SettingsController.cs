using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DenariusAI.Infrastructure.Identity;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Manages runtime application settings available to administrators.
/// </summary>
[Authorize(Roles = ApplicationRoles.Administrator)]
public sealed class SettingsController(IApplicationSettingsService settingsService, ILLMService llmService, IFinancialDataResetService resetService, IDemonstrationDataService demonstrationDataService, UserManager<ApplicationUser> userManager, ILogger<SettingsController> logger) : Controller
{
    /// <summary>
    /// Displays the application settings page.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>View with current application settings.</returns>
    [HttpGet] public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(ApplicationSettingsViewModel.From(await settingsService.GetAsync(cancellationToken), llmService.IsConfigured));
    
    /// <summary>
    /// Updates the application settings with the provided values.
    /// </summary>
    /// <param name="model">View model containing the updated settings.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index on success, or returns view with validation errors.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ApplicationSettingsViewModel model, CancellationToken cancellationToken)
    {
        model.AiIsConfigured = llmService.IsConfigured;
        if (!ModelState.IsValid) return View(model);
        try { await settingsService.UpdateAsync(model.ToDto(), UserId(), cancellationToken); TempData["SuccessMessage"] = "Definições da aplicação atualizadas e aplicadas."; return RedirectToAction(nameof(Index)); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View(model); }
    }
    
    /// <summary>
    /// Tests the connection to the AI service (Mistral).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestAiConnection(CancellationToken cancellationToken)
    {
        if (!llmService.IsConfigured) TempData["ErrorMessage"] = "Configure MISTRAL_API_KEY antes de testar a ligação.";
        else try { var settings = await settingsService.GetAsync(cancellationToken); var response = await llmService.CompleteAsync([new("user", settings.ConnectionTestPrompt)], cancellationToken); TempData["SuccessMessage"] = $"Ligação confirmada com {response.Model}."; }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException) { logger.LogWarning(exception, "AI connection test failed."); TempData["ErrorMessage"] = "Não foi possível confirmar a ligação à Mistral."; }
        return RedirectToAction(nameof(Index));
    }
    
    /// <summary>
    /// Displays the form to load demonstration data.
    /// </summary>
    /// <returns>View with empty form model.</returns>
    [HttpGet]
    public IActionResult LoadDemonstrationData() => View(new LoadDemonstrationDataViewModel());

    /// <summary>
    /// Loads demonstration data into the system after password verification.
    /// </summary>
    /// <param name="model">View model containing the user's password for confirmation.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDemonstrationData(LoadDemonstrationDataViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!await userManager.CheckPasswordAsync(user, model.Password)) ModelState.AddModelError(nameof(model.Password), "A palavra-passe está incorreta.");
        if (!ModelState.IsValid) return View(model);
        var result = await demonstrationDataService.LoadAsync(cancellationToken);
        logger.LogWarning("Demonstration data load requested by {UserId}. Loaded: {Loaded}.", UserId(), result.Loaded);
        TempData[result.Loaded ? "SuccessMessage" : "ErrorMessage"] = result.Loaded ? "Dados de demonstração carregados." : "Os dados de demonstração exigem uma base financeira vazia.";
        return RedirectToAction(nameof(Index));
    }
    
    /// <summary>
    /// Displays the form to reset financial data.
    /// </summary>
    /// <returns>View with empty form model.</returns>
    [HttpGet] public IActionResult ResetFinancialData() => View(new ResetFinancialDataViewModel());
    
    /// <summary>
    /// Resets all financial data after password verification.
    /// </summary>
    /// <param name="model">View model containing the user's password for confirmation.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>Redirects to Index with success message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetFinancialData(ResetFinancialDataViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!await userManager.CheckPasswordAsync(user, model.Password)) ModelState.AddModelError(nameof(model.Password), "A palavra-passe está incorreta.");
        if (!ModelState.IsValid) return View(model); var result = await resetService.ResetAsync(cancellationToken); logger.LogWarning("Financial data reset by {UserId}: {Entries} entries.", UserId(), result.JournalEntries); TempData["SuccessMessage"] = "Dados financeiros reiniciados."; return RedirectToAction(nameof(Index));
    }
    
    /// <summary>
    /// Retrieves the current user's identifier from claims.
    /// </summary>
    /// <returns>The user's unique identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when user cannot be identified.</exception>
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
