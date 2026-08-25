using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed class SavingsCertificateFormViewModel
{
    public Guid Id { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Data")]
    public DateOnly InvestmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required, StringLength(80), Display(Name = "Série/Número")]
    public string SeriesNumber { get; set; } = string.Empty;
    [Required, StringLength(200), Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999999"), Display(Name = "Valor do investimento")]
    public decimal InvestmentValue { get; set; }
    [Range(typeof(decimal), "0", "100"), Display(Name = "Taxa (%)")]
    public decimal Rate { get; set; }
    [Range(typeof(decimal), "0", "999999999999"), Display(Name = "Valor atual")]
    public decimal CurrentValue { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Próxima capitalização")]
    public DateOnly NextCapitalization { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public sealed record SavingsCertificateRowViewModel(Guid Id, DateOnly InvestmentDate, int AgeDays,
    string SeriesNumber, string Description, decimal InvestmentValue, decimal Rate, decimal CurrentValue,
    decimal Yield, DateOnly NextCapitalization, int DaysUntilCapitalization, decimal FutureNetInterest, decimal FutureValue);

public sealed record SavingsCertificateIndexViewModel(IReadOnlyList<SavingsCertificateRowViewModel> Items,
    decimal TotalInvestment, decimal TotalCurrentValue, decimal TotalYield, decimal TotalFutureNetInterest, decimal TotalFutureValue,
    DateOnly? From, DateOnly? To, string? Search, string Sort, IReadOnlyList<SelectListItem> SortOptions, PaginationViewModel Pagination);
