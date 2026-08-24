using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Introduza o nome a apresentar.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    [Display(Name = "Nome")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

}

public sealed class ResetFinancialDataViewModel
{
    [Required(ErrorMessage = "Introduza a palavra-passe atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Palavra-passe atual")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escreva APAGAR TUDO para confirmar.")]
    [RegularExpression("^APAGAR TUDO$", ErrorMessage = "Escreva exatamente APAGAR TUDO.")]
    [Display(Name = "Confirmação")]
    public string Confirmation { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Introduza a palavra-passe atual."), DataType(DataType.Password), Display(Name = "Palavra-passe atual")]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "Introduza a nova palavra-passe."), StringLength(100, MinimumLength = 12), DataType(DataType.Password), Display(Name = "Nova palavra-passe")]
    public string NewPassword { get; set; } = string.Empty;
    [Required, Compare(nameof(NewPassword), ErrorMessage = "A confirmação não coincide."), DataType(DataType.Password), Display(Name = "Confirmar nova palavra-passe")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
