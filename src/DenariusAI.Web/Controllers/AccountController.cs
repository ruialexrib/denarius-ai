using DenariusAI.Infrastructure.Identity;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the AccountController type.
/// </summary>
public sealed class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null) { ModelState.AddModelError(string.Empty, "Email ou palavra-passe inválidos."); return View(model); }
        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Conta temporariamente bloqueada." : "Email ou palavra-passe inválidos."); return View(model); }
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [AllowAnonymous] public IActionResult AccessDenied() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptCookieConsent(string? returnUrl = null)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        user.CookieConsentAcceptedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) TempData["ErrorMessage"] = "Não foi possível guardar o consentimento de cookies.";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        return View(new ProfileViewModel { DisplayName = user.DisplayName, Email = user.Email ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!ModelState.IsValid) { model = new ProfileViewModel { DisplayName = model.DisplayName, Email = user.Email ?? string.Empty }; return View(model); }
        user.DisplayName = model.DisplayName.Trim(); var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, "Não foi possível atualizar o perfil."); return View(model); }
        await signInManager.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Preferências atualizadas."; return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Preencha corretamente os campos da palavra-passe."; return RedirectToAction(nameof(Profile), new { fragment = "security" }); }
        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded) { TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => PasswordError(error.Code))); return RedirectToAction(nameof(Profile), new { fragment = "security" }); }
        await signInManager.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Palavra-passe alterada com sucesso."; return RedirectToAction(nameof(Profile), new { fragment = "security" });
    }

    private static string PasswordError(string code) => code switch { "PasswordMismatch" => "A palavra-passe atual está incorreta.", "PasswordTooShort" => "A nova palavra-passe deve ter pelo menos 12 caracteres.", _ => "A nova palavra-passe não cumpre os requisitos de segurança." };
}
