using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>Represents the credentials and deployment context shown on the login page.</summary>
public sealed class LoginViewModel
{
    /// <summary>Gets or sets the email address used to authenticate.</summary>
    [Required(ErrorMessage = "Introduza o endereço de email.")]
    [EmailAddress(ErrorMessage = "Introduza um endereço de email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the password used to authenticate.</summary>
    [Required(ErrorMessage = "Introduza a palavra-passe.")]
    [DataType(DataType.Password)]
    [Display(Name = "Palavra-passe")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the authentication cookie should persist across browser sessions.</summary>
    [Display(Name = "Manter sessão iniciada")]
    public bool RememberMe { get; set; }

    /// <summary>Gets or sets the local URL requested before authentication.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Gets or sets whether Google authentication is available.</summary>
    public bool GoogleEnabled { get; set; }

    /// <summary>Gets or sets whether the installation is running in public demonstration mode.</summary>
    public bool DemoModeEnabled { get; set; }

    /// <summary>Gets or sets the public demonstration account email address.</summary>
    public string? DemoEmail { get; set; }

    /// <summary>Gets or sets the public demonstration account password.</summary>
    public string? DemoPassword { get; set; }
}
