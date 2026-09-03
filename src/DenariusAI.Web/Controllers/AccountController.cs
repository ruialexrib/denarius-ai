using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Handles authentication, session/profile, and cookie-consent workflows, including anonymous sign-in entry points.
/// </summary>
/// <param name="signInManager">The sign-in manager for handling user authentication.</param>
/// <param name="userManager">The user manager for managing user accounts.</param>
/// <param name="dbContext">The database context for accessing application data.</param>
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    DenariusDbContext dbContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<AccountController> logger) : Controller
{
    private const int MaximumProfileImageBytes = 512 * 1024;
    private static readonly HashSet<string> SupportedProfileImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };
    /// <summary>
    /// Displays the login page or redirects authenticated users to the home page.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login.</param>
    /// <returns>The login view or a redirect to the home page.</returns>
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(LoginModel(returnUrl));

    /// <summary>
    /// Processes the login form submission and authenticates the user.
    /// </summary>
    /// <param name="model">The login view model containing user credentials.</param>
    /// <returns>A redirect to the return URL or home page on success, or the login view with errors on failure.</returns>
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.GoogleEnabled = IsGoogleConfigured;
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null) { ModelState.AddModelError(string.Empty, "Email ou palavra-passe inválidos."); return View(model); }
        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Conta temporariamente bloqueada." : "Email ou palavra-passe inválidos."); return View(model); }
        return await CompleteLoginAsync(user, model.ReturnUrl);
    }

    /// <summary>Starts authentication with a configured external provider.</summary>
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!IsGoogleConfigured || !string.Equals(provider, "Google", StringComparison.Ordinal)) return NotFound();
        var callbackUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
        return Challenge(properties, provider);
    }

    /// <summary>Signs in an existing local account whose email matches the verified Google identity.</summary>
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!IsGoogleConfigured) return NotFound();
        if (!string.IsNullOrWhiteSpace(remoteError)) return ExternalLoginError(returnUrl, "O Google não conseguiu concluir a autenticação.");

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null || !string.Equals(info.LoginProvider, "Google", StringComparison.Ordinal))
            return ExternalLoginError(returnUrl, "Não foi possível validar a resposta do Google.");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return ExternalLoginError(returnUrl, "A conta Google não disponibilizou um endereço de email.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return ExternalLoginError(returnUrl, "Este email não está autorizado. Solicite ao administrador a criação da sua conta.");
        if (await userManager.IsLockedOutAsync(user))
            return ExternalLoginError(returnUrl, "A conta encontra-se temporariamente bloqueada.");

        await SynchronizeGoogleProfileImageAsync(user, info.Principal.FindFirstValue("urn:google:picture"));
        await signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: info.LoginProvider);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return await CompleteLoginAsync(user, returnUrl);
    }

    private IActionResult ExternalLoginError(string? returnUrl, string message)
    {
        ModelState.AddModelError(string.Empty, message);
        return View(nameof(Login), LoginModel(returnUrl));
    }

    /// <summary>Completes login tracking and starts the per-session cloud AI privacy acknowledgement.</summary>
    /// <param name="user">The successfully authenticated user.</param>
    /// <param name="returnUrl">The local URL requested before authentication.</param>
    /// <returns>A redirect to the requested local URL or the home page.</returns>
    private async Task<IActionResult> CompleteLoginAsync(ApplicationUser user, string? returnUrl)
    {
        var previousLogin = await dbContext.LoginHistory.AsNoTracking().Where(item => item.UserId == user.Id).OrderByDescending(item => item.LoggedInAt).FirstOrDefaultAsync();
        dbContext.LoginHistory.Add(new LoginHistory { UserId = user.Id, IpAddress = ClientIp() });
        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = previousLogin is null
            ? "Bem-vindo. Este é o seu primeiro acesso registado."
            : $"Bem-vindo. O seu acesso anterior foi em {FormatLogin(previousLogin.LoggedInAt)}, a partir do IP {previousLogin.IpAddress}.";
        HttpContext.Session.SetString(CloudAiPrivacyNoticePolicy.SessionKey, bool.TrueString);
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    private LoginViewModel LoginModel(string? returnUrl) => new() { ReturnUrl = returnUrl, GoogleEnabled = IsGoogleConfigured };

    private bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
        && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);

    private async Task SynchronizeGoogleProfileImageAsync(ApplicationUser user, string? imageUrl)
    {
        if (!IsTrustedGoogleImageUrl(imageUrl, out var uri)) return;
        try
        {
            using var response = await httpClientFactory.CreateClient("GoogleProfileImages")
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaximumProfileImageBytes) return;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !SupportedProfileImageTypes.Contains(contentType)) return;

            await using var stream = await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, HttpContext.RequestAborted)) > 0)
            {
                if (buffer.Length + read > MaximumProfileImageBytes) return;
                await buffer.WriteAsync(chunk.AsMemory(0, read), HttpContext.RequestAborted);
            }
            if (buffer.Length == 0) return;

            var base64 = Convert.ToBase64String(buffer.ToArray());
            if (user.ProfileImageBase64 == base64 && user.ProfileImageContentType == contentType) return;
            user.ProfileImageBase64 = base64;
            user.ProfileImageContentType = contentType;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) logger.LogWarning("The Google profile image could not be saved for user {UserId}.", user.Id);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
        {
            logger.LogWarning(exception, "The Google profile image could not be downloaded for user {UserId}.", user.Id);
        }
    }

    private static bool IsTrustedGoogleImageUrl(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var candidate)
            && candidate.Scheme == Uri.UriSchemeHttps
            && (candidate.Host.Equals("googleusercontent.com", StringComparison.OrdinalIgnoreCase)
                || candidate.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)))
        {
            uri = candidate;
            return true;
        }
        uri = null!;
        return false;
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
    /// Records that the authenticated user acknowledged the cloud AI privacy notice for the current login session.
    /// </summary>
    /// <param name="returnUrl">The local URL to return to after acknowledgement.</param>
    /// <returns>A redirect to the requested local URL or the home page.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AcknowledgeCloudAiPrivacyNotice(string? returnUrl = null)
    {
        HttpContext.Session.Remove(CloudAiPrivacyNoticePolicy.SessionKey);
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

    /// <summary>Enables or hides the persistent asset balance summary for the current user.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAssetBalancesWidget(bool enabled, string? returnUrl = null)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        user.ShowAssetBalancesWidget = enabled;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) TempData["ErrorMessage"] = "Não foi possível atualizar a visibilidade do resumo patrimonial.";
        else TempData["SuccessMessage"] = enabled
            ? "Resumo patrimonial reativado. Ficará visível em toda a aplicação."
            : "Resumo patrimonial ocultado. Pode reativá-lo a qualquer momento nas Preferências.";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Profile));
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
        ShowAssetBalancesWidget = user.ShowAssetBalancesWidget,
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
