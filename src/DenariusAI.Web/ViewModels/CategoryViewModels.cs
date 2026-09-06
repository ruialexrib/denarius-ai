using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

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

/// <summary>
/// Represents a category row together with its financial-group presentation and movement usage state.
/// </summary>
/// <param name="Category">Category data.</param>
/// <param name="GroupName">Display name of the financial group.</param>
/// <param name="Kind">Financial group kind used for the category icon.</param>
/// <param name="HasMovementUsage">Whether the category has ever been referenced by a journal movement line.</param>
public sealed record CategoryListItemViewModel(CategoryDto Category, string GroupName, DenariusAI.Domain.Enums.FinancialGroupKind Kind, bool HasMovementUsage);

public sealed record CategoryIndexViewModel(
    IReadOnlyList<CategoryListItemViewModel> Items,
    IReadOnlyList<SelectListItem> Groups,
    Guid? GroupId,
    string? Search,
    bool ShowInactive,
    PaginationViewModel Pagination);
