using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public static class ReconciliationImportPeriodPolicy
{
    public static int MonthDistance(DateOnly date, int budgetYear, int budgetMonth) =>
        Math.Abs((date.Year * 12 + date.Month) - (budgetYear * 12 + budgetMonth));
}

public sealed class ReconciliationImportRowViewModel
{
    public int RowNumber { get; set; }
    public DateOnly Date { get; set; }
    [Required, StringLength(240)] public string Description { get; set; } = string.Empty;
    [StringLength(120)] public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CounterAccountId { get; set; }
    public string? SuggestionReason { get; set; }
    public string SuggestionConfidence { get; set; } = "none";
    public bool Selected { get; set; } = true;
    public bool IsEligible { get; set; } = true;
    public bool IsPeriodWarning { get; set; }
    public string? EligibilityMessage { get; set; }
}

/// <summary>Holds review selections and server-supplied display data before import confirmation.</summary>
public sealed class ReconciliationImportReviewViewModel
{
    /// <summary>Gets or sets persisted totals keyed by category for the selected budget.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyDictionary<Guid, DenariusAI.Application.DTOs.BudgetExecutionItemDto> CategoryExecution { get; set; } = new Dictionary<Guid, DenariusAI.Application.DTOs.BudgetExecutionItemDto>();
    /// <summary>Gets or sets the review BankAccountId value.</summary>
    public Guid BankAccountId { get; set; }
    /// <summary>Gets or sets the review BankAccountName value.</summary>
    public string BankAccountName { get; set; } = string.Empty;
    /// <summary>Gets or sets the review BudgetId value.</summary>
    public Guid BudgetId { get; set; }
    /// <summary>Gets or sets the review BudgetName value.</summary>
    public string BudgetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the review BudgetYear value.</summary>
    public int BudgetYear { get; set; }
    /// <summary>Gets or sets the review BudgetMonth value.</summary>
    public int BudgetMonth { get; set; }
    /// <summary>Gets or sets the review Rows value.</summary>
    public List<ReconciliationImportRowViewModel> Rows { get; set; } = [];
    /// <summary>Gets or sets the review Categories value.</summary>
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    /// <summary>Gets or sets the review CounterAccounts value.</summary>
    public IReadOnlyList<SelectListItem> CounterAccounts { get; set; } = [];
}


public sealed class ReconciliationPasteViewModel
{
    public Guid BankAccountId { get; set; }
    public Guid BudgetId { get; set; }
    public string MovementsText { get; set; } = string.Empty;
    public string? AssistantMessage { get; set; }
    public IReadOnlyList<SelectListItem> BankAccounts { get; set; } = [];
    public IReadOnlyList<SelectListItem> Budgets { get; set; } = [];
}
