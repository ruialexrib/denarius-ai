using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the LoginViewModel type.
/// </summary>
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Introduza o endereço de email.")]
    [EmailAddress(ErrorMessage = "Introduza um endereço de email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Introduza a palavra-passe.")]
    [DataType(DataType.Password)]
    [Display(Name = "Palavra-passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Manter sessão iniciada")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
