using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for CategoryViewModels.
/// </summary>
public sealed class CategoryFormViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Selecione um grupo.")]
    [Display(Name = "Grupo")]
    public Guid FinancialGroupId { get; set; }
    [Required(ErrorMessage = "Introduza o nome da categoria.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "A ordem não pode ser negativa.")]
    [Display(Name = "Ordem")]
    public int SortOrder { get; set; }
    public IReadOnlyList<SelectListItem> Groups { get; set; } = [];
}

public sealed record CategoryListItemViewModel(CategoryDto Category, string GroupName, DenariusAI.Domain.Enums.FinancialGroupKind Kind);
public sealed record CategoryIndexViewModel(
    IReadOnlyList<CategoryListItemViewModel> Items,
    IReadOnlyList<SelectListItem> Groups,
    Guid? GroupId,
    string? Search,
    bool ShowInactive,
    PaginationViewModel Pagination);
