using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed class AccountFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Introduza o nome da conta.")]
    [StringLength(120, ErrorMessage = "O nome não pode exceder 120 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    [Required, Display(Name = "Tipo de conta")]
    public AccountType AccountType { get; set; } = AccountType.BankAccount;

    [Display(Name = "Saldo inicial")]
    public decimal InitialBalance { get; set; }

    [Required(ErrorMessage = "Introduza a moeda.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "A moeda deve conter três letras.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "A moeda deve conter apenas letras.")]
    [Display(Name = "Moeda")]
    public string Currency { get; set; } = "EUR";

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }

    public IReadOnlyList<SelectListItem> AccountTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
}

public sealed record AccountListItemViewModel(AccountDto Account, string CategoryName);

public sealed record AccountIndexViewModel(
    IReadOnlyList<AccountListItemViewModel> Items,
    IReadOnlyList<SelectListItem> AccountTypes,
    IReadOnlyList<SelectListItem> Categories,
    AccountType? AccountType,
    Guid? CategoryId,
    string? Search,
    bool ShowInactive,
    PaginationViewModel Pagination);

public sealed record AccountDetailsViewModel(AccountDto Account, string CategoryName, string? GroupName);
