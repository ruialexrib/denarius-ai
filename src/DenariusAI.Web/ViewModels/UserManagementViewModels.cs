using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for UserManagementViewModels.
/// </summary>
public sealed record UserListItemViewModel(string Id, string DisplayName, string Email, string Role, bool IsCurrentUser);
public sealed record UserIndexViewModel(IReadOnlyList<UserListItemViewModel> Items);

public sealed class UserFormViewModel
{
    public string? Id { get; set; }
    [Required, StringLength(100), Display(Name = "Nome")]
    public string DisplayName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256), Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    [Required, Display(Name = "Permissão")]
    public string Role { get; set; } = "User";
    [StringLength(100, MinimumLength = 12), DataType(DataType.Password), Display(Name = "Palavra-passe")]
    public string? Password { get; set; }
}
