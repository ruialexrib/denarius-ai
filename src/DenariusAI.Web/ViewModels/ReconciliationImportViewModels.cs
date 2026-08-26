using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

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
    public string? EligibilityMessage { get; set; }
}

public sealed class ReconciliationImportReviewViewModel
{
    public Guid BankAccountId { get; set; }
    public string BankAccountName { get; set; } = string.Empty;
    public Guid BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public int BudgetYear { get; set; }
    public int BudgetMonth { get; set; }
    public List<ReconciliationImportRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
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
