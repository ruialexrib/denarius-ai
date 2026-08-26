using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Handles authentication, session/profile, and cookie-consent workflows, including anonymous sign-in entry points.
/// </summary>
/// <param name="signInManager">The sign-in manager for handling user authentication.</param>
/// <param name="userManager">The user manager for managing user accounts.</param>
/// <param name="dbContext">The database context for accessing application data.</param>
public sealed class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, DenariusDbContext dbContext) : Controller
{
    /// <summary>
    /// Displays the login page or redirects authenticated users to the home page.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login.</param>
    /// <returns>The login view or a redirect to the home page.</returns>
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginViewModel { ReturnUrl = returnUrl });

    /// <summary>
    /// Processes the login form submission and authenticates the user.
    /// </summary>
    /// <param name="model">The login view model containing user credentials.</param>
    /// <returns>A redirect to the return URL or home page on success, or the login view with errors on failure.</returns>
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null) { ModelState.AddModelError(string.Empty, "Email ou palavra-passe inválidos."); return View(model); }
        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Conta temporariamente bloqueada." : "Email ou palavra-passe inválidos."); return View(model); }
        var previousLogin = await dbContext.LoginHistory.AsNoTracking().Where(item => item.UserId == user.Id).OrderByDescending(item => item.LoggedInAt).FirstOrDefaultAsync();
        dbContext.LoginHistory.Add(new LoginHistory { UserId = user.Id, IpAddress = ClientIp() });
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = previousLogin is null
            ? "Bem-vindo. Este é o seu primeiro acesso registado."
            : $"Bem-vindo. O seu acesso anterior foi em {FormatLogin(previousLogin.LoggedInAt)}, a partir do IP {previousLogin.IpAddress}.";
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Logs out the current user and redirects to the login page.
    /// </summary>
    /// <returns>A redirect to the login page.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    
    /// <summary>
    /// Displays the access denied page for unauthorized access attempts.
    /// </summary>
    /// <returns>The access denied view.</returns>
    [AllowAnonymous] public IActionResult AccessDenied() => View();

    /// <summary>
    /// Records the user's cookie consent acceptance.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after accepting consent.</param>
    /// <returns>A redirect to the return URL or home page.</returns>
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

    /// <summary>
    /// Displays the user's profile page with login history.
    /// </summary>
    /// <returns>The profile view.</returns>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        return View(await ProfileModelAsync(user));
    }

    /// <summary>
    /// Processes profile updates submitted by the user.
    /// </summary>
    /// <param name="model">The profile view model containing updated user information.</param>
    /// <returns>A redirect to the profile page on success, or the profile view with errors on failure.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!ModelState.IsValid) { model.Email = user.Email ?? string.Empty; model.LoginHistory = (await ProfileModelAsync(user)).LoginHistory; return View(model); }
        user.DisplayName = model.DisplayName.Trim(); var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, "Não foi possível atualizar o perfil."); return View(model); }
        await signInManager.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Preferências atualizadas."; return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Processes a password change request for the current user.
    /// </summary>
    /// <param name="model">The change password view model containing current and new passwords.</param>
    /// <returns>A redirect to the profile page with success or error message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Preencha corretamente os campos da palavra-passe."; return RedirectToAction(nameof(Profile), new { fragment = "security" }); }
        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded) { TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => PasswordError(error.Code))); return RedirectToAction(nameof(Profile), new { fragment = "security" }); }
        await signInManager.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Palavra-passe alterada com sucesso."; return RedirectToAction(nameof(Profile), new { fragment = "security" });
    }

    /// <summary>
    /// Maps password error codes to user-friendly error messages.
    /// </summary>
    /// <param name="code">The error code from Identity.</param>
    /// <returns>A localized error message.</returns>
    private static string PasswordError(string code) => code switch { "PasswordMismatch" => "A palavra-passe atual está incorreta.", "PasswordTooShort" => "A nova palavra-passe deve ter pelo menos 12 caracteres.", _ => "A nova palavra-passe não cumpre os requisitos de segurança." };
    
    /// <summary>
    /// Creates a profile view model for the specified user.
    /// </summary>
    /// <param name="user">The application user.</param>
    /// <returns>A profile view model with user data and login history.</returns>
    private async Task<ProfileViewModel> ProfileModelAsync(ApplicationUser user) => new()
    {
        DisplayName = user.DisplayName,
        Email = user.Email ?? string.Empty,
        LoginHistory = await dbContext.LoginHistory.AsNoTracking().Where(item => item.UserId == user.Id).OrderByDescending(item => item.LoggedInAt).Take(10).Select(item => new LoginHistoryItemViewModel(item.LoggedInAt, item.IpAddress)).ToListAsync()
    };
    
    /// <summary>
    /// Retrieves the client's IP address from the HTTP context.
    /// </summary>
    /// <returns>The client's IP address as a string.</returns>
    private string ClientIp()
    {
        var address = HttpContext.Connection.RemoteIpAddress;
        if (address is null) return "Desconhecido";
        if (IPAddress.IsLoopback(address)) return "127.0.0.1";
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
    }
    
    /// <summary>
    /// Formats a login timestamp to the Lisbon timezone with a localized format.
    /// </summary>
    /// <param name="value">The UTC timestamp to format.</param>
    /// <returns>A formatted date-time string.</returns>
    private static string FormatLogin(DateTimeOffset value)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        return TimeZoneInfo.ConvertTime(value, zone).ToString("dd/MM/yyyy 'às' HH:mm");
    }
}
