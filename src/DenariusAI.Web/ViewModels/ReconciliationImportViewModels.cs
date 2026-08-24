using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed class ReconciliationImportRowViewModel
{
    public int RowNumber { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CounterAccountId { get; set; }
    public string? SuggestionReason { get; set; }
    public bool Selected { get; set; } = true;
}

public sealed class ReconciliationImportReviewViewModel
{
    public Guid BankAccountId { get; set; }
    public string BankAccountName { get; set; } = string.Empty;
    public List<ReconciliationImportRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> CounterAccounts { get; set; } = [];
}
