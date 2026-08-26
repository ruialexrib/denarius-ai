using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for FinancialGroupViewModels.
/// </summary>
public sealed class FinancialGroupFormViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Introduza o nome do grupo.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }
    [Required, Display(Name = "Tipo")]
    public FinancialGroupKind Kind { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "A ordem não pode ser negativa.")]
    [Display(Name = "Ordem")]
    public int SortOrder { get; set; }
}

public sealed record FinancialGroupIndexViewModel(
    IReadOnlyList<FinancialGroupDto> Items,
    string? Search,
    bool ShowInactive,
    PaginationViewModel Pagination);
