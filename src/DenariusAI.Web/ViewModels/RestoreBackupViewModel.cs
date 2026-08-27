using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DenariusAI.Web.ViewModels;

public sealed class RestoreBackupViewModel
{
    [Required(ErrorMessage = "Selecione o ficheiro JSON do backup.")]
    [Display(Name = "Ficheiro de backup")]
    public IFormFile? BackupFile { get; set; }

    [Required(ErrorMessage = "Introduza a palavra-passe atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Palavra-passe atual")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escreva RESTAURAR TUDO para confirmar.")]
    [RegularExpression("^RESTAURAR TUDO$", ErrorMessage = "Escreva exatamente RESTAURAR TUDO.")]
    [Display(Name = "Confirmação")]
    public string Confirmation { get; set; } = string.Empty;
}
