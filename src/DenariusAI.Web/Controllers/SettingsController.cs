using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DenariusAI.Infrastructure.Identity;

namespace DenariusAI.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
public sealed class SettingsController(IApplicationSettingsService settingsService, ILLMService llmService, IFinancialDataResetService resetService, IDemonstrationDataService demonstrationDataService, UserManager<ApplicationUser> userManager, ILogger<SettingsController> logger) : Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(ApplicationSettingsViewModel.From(await settingsService.GetAsync(cancellationToken), llmService.IsConfigured));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ApplicationSettingsViewModel model, CancellationToken cancellationToken)
    {
        model.AiIsConfigured = llmService.IsConfigured;
        if (!ModelState.IsValid) return View(model);
        try { await settingsService.UpdateAsync(model.ToDto(), UserId(), cancellationToken); TempData["SuccessMessage"] = "Definições da aplicação atualizadas e aplicadas."; return RedirectToAction(nameof(Index)); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View(model); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestAiConnection(CancellationToken cancellationToken)
    {
        if (!llmService.IsConfigured) TempData["ErrorMessage"] = "Configure MISTRAL_API_KEY antes de testar a ligação.";
        else try { var response = await llmService.CompleteAsync([new("user", "Responde apenas com: Ligação confirmada")], cancellationToken); TempData["SuccessMessage"] = $"Ligação confirmada com {response.Model}."; }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException) { logger.LogWarning(exception, "AI connection test failed."); TempData["ErrorMessage"] = "Não foi possível confirmar a ligação à Mistral."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDemonstrationData(CancellationToken cancellationToken)
    {
        var result = await demonstrationDataService.LoadAsync(cancellationToken); TempData[result.Loaded ? "SuccessMessage" : "ErrorMessage"] = result.Loaded ? "Dados de demonstração carregados." : "Os dados de demonstração exigem uma base financeira vazia."; return RedirectToAction(nameof(Index));
    }
    [HttpGet] public IActionResult ResetFinancialData() => View(new ResetFinancialDataViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetFinancialData(ResetFinancialDataViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!await userManager.CheckPasswordAsync(user, model.Password)) ModelState.AddModelError(nameof(model.Password), "A palavra-passe está incorreta.");
        if (!ModelState.IsValid) return View(model); var result = await resetService.ResetAsync(cancellationToken); logger.LogWarning("Financial data reset by {UserId}: {Entries} entries.", UserId(), result.JournalEntries); TempData["SuccessMessage"] = "Dados financeiros reiniciados."; return RedirectToAction(nameof(Index));
    }
    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
