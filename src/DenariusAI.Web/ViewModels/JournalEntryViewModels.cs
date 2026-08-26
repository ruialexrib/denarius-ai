using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed class JournalEntryLineFormViewModel
{
    [Display(Name = "Conta")]
    public Guid AccountId { get; set; }

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "O débito não pode ser negativo.")]
    public decimal Debit { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "O crédito não pode ser negativo.")]
    public decimal Credit { get; set; }

    [StringLength(250, ErrorMessage = "A descrição da linha não pode exceder 250 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }
}

public sealed class JournalEntryFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Introduza a data do movimento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Introduza a descrição do movimento.")]
    [StringLength(250, ErrorMessage = "A descrição não pode exceder 250 caracteres.")]
    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "A referência não pode exceder 100 caracteres.")]
    [Display(Name = "Referência")]
    public string? Reference { get; set; }

    [StringLength(2000, ErrorMessage = "As notas não podem exceder 2000 caracteres.")]
    [Display(Name = "Notas")]
    public string? Notes { get; set; }

    [Display(Name = "Orçamento")]
    public Guid? BudgetId { get; set; }

    public List<JournalEntryLineFormViewModel> Lines { get; set; } = [new(), new()];
    public IReadOnlyList<SelectListItem> Accounts { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> TransactionAccounts { get; set; } = [];
    public IReadOnlyList<SelectListItem> ExpenseCategories { get; set; } = [];
    public IReadOnlyList<SelectListItem> IncomeCategories { get; set; } = [];
    public Guid? ExpenseAccountId { get; set; }
    public Guid? IncomeAccountId { get; set; }
    public IReadOnlyList<SelectListItem> Budgets { get; set; } = [];
    public bool AiSuggestionAvailable { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count < 2) yield return new("O movimento deve possuir pelo menos duas linhas.", [nameof(Lines)]);
        if (Lines.Select(line => line.AccountId).Where(id => id != Guid.Empty).Distinct().Count() < 2)
            yield return new("Selecione pelo menos duas contas diferentes.", [nameof(Lines)]);
        foreach (var line in Lines)
        {
            if (line.AccountId == Guid.Empty) yield return new("Selecione uma conta em todas as linhas.", [nameof(Lines)]);
            if (line.Debit < 0m || line.Credit < 0m || (line.Debit == 0m) == (line.Credit == 0m))
                yield return new("Cada linha deve ter um valor positivo apenas no débito ou apenas no crédito.", [nameof(Lines)]);
        }
        if (Lines.Sum(line => line.Debit) != Lines.Sum(line => line.Credit))
            yield return new("O total do débito deve ser igual ao total do crédito.", [nameof(Lines)]);
    }
}

public sealed class JournalEntrySuggestionViewModel
{
    [Required, StringLength(1000)]
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<AssistantMessageViewModel> History { get; init; } = [];
}

public sealed record JournalEntryIndexViewModel(
    IReadOnlyList<JournalEntrySummaryDto> Items,
    DateOnly? From,
    DateOnly? To,
    JournalEntryStatus? Status,
    string? BudgetFilter,
    string? Search,
    string Sort,
    IReadOnlyList<SelectListItem> Statuses,
    IReadOnlyList<SelectListItem> BudgetOptions,
    IReadOnlyList<SelectListItem> SortOptions,
    PaginationViewModel Pagination);

public sealed record JournalEntryDetailsViewModel(JournalEntryDetailsDto Entry);
