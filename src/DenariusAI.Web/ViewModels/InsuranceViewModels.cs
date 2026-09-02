using System.ComponentModel.DataAnnotations;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

/// <summary>Form model for creating or editing an insurance policy.</summary>
public sealed class InsurancePolicyFormViewModel
{
    /// <summary>Gets or sets whether clipboard-assisted completion is available.</summary>
    public bool AiSuggestionAvailable { get; set; }
    /// <summary>Gets or sets the policy name.</summary>
    [Required, Display(Name = "Designação")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the insurer.</summary>
    [Required, Display(Name = "Seguradora")]
    public string Insurer { get; set; } = string.Empty;

    /// <summary>Gets or sets the policy number.</summary>
    [Required, Display(Name = "N.º da apólice")]
    public string PolicyNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the insurance type.</summary>
    [Display(Name = "Tipo")]
    public InsurancePolicyType Type { get; set; } = InsurancePolicyType.Other;

    /// <summary>Gets or sets payment frequency.</summary>
    [Display(Name = "Periodicidade")]
    public InsurancePaymentFrequency PaymentFrequency { get; set; } = InsurancePaymentFrequency.Annual;

    /// <summary>Gets or sets coverage start.</summary>
    [Display(Name = "Início"), DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets coverage end.</summary>
    [Display(Name = "Fim"), DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    /// <summary>Gets or sets next renewal.</summary>
    [Display(Name = "Renovação"), DataType(DataType.Date)]
    public DateOnly? RenewalDate { get; set; }

    /// <summary>Gets or sets insured subject.</summary>
    [Display(Name = "Pessoa ou bem seguro")]
    public string? InsuredSubject { get; set; }

    /// <summary>Gets or sets notes.</summary>
    [Display(Name = "Notas")]
    public string? Notes { get; set; }
}

/// <summary>Represents clipboard text submitted for insurance policy extraction.</summary>
public sealed class InsuranceClipboardRequestViewModel
{
    /// <summary>Gets or sets the clipboard text to analyze.</summary>
    [Required, StringLength(20000, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>Form model for an insurance premium.</summary>
public sealed class InsurancePremiumFormViewModel
{
    /// <summary>Gets or sets amount.</summary>
    [Range(typeof(decimal), "0.01", "999999999"), Display(Name = "Prémio")]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets covered period start.</summary>
    [Display(Name = "Período desde"), DataType(DataType.Date)]
    public DateOnly PeriodStart { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets covered period end.</summary>
    [Display(Name = "Período até"), DataType(DataType.Date)]
    public DateOnly PeriodEnd { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1).AddDays(-1));

    /// <summary>Gets or sets due date.</summary>
    [Display(Name = "Vencimento"), DataType(DataType.Date)]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets reference.</summary>
    [Display(Name = "Referência")]
    public string? Reference { get; set; }
}

/// <summary>Insurance portfolio overview model.</summary>
public sealed class InsurancePortfolioViewModel
{
    /// <summary>Gets or sets the filtered policies displayed on the current page.</summary>
    public IReadOnlyList<InsurancePolicy> Policies { get; set; } = [];

    /// <summary>Gets or sets the free-text search applied to the portfolio.</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the selected policy type filter.</summary>
    public InsurancePolicyType? Type { get; set; }

    /// <summary>Gets or sets the selected policy status filter.</summary>
    public InsurancePolicyStatus? Status { get; set; }

    /// <summary>Gets or sets pagination metadata for the filtered policy list.</summary>
    public PaginationViewModel Pagination { get; set; } = PaginationViewModel.Create(0, 1, 10);

    /// <summary>Gets or sets active policy count.</summary>
    public int ActivePolicies { get; set; }

    /// <summary>Gets or sets annual premium cost for the current year.</summary>
    public decimal AnnualCost { get; set; }

    /// <summary>Gets or sets premiums currently due and not linked to an active movement.</summary>
    public int OutstandingPremiums { get; set; }

    /// <summary>Gets or sets renewals occurring in the next 30 days.</summary>
    public int UpcomingRenewals { get; set; }
}

/// <summary>Insurance policy details together with eligible accounting movements.</summary>
public sealed class InsurancePolicyDetailsViewModel
{
    /// <summary>Gets or sets the policy being displayed.</summary>
    public InsurancePolicy Policy { get; set; } = null!;

    /// <summary>Gets or sets active accounting movements available for premium association.</summary>
    public IReadOnlyList<SelectListItem> AvailableMovements { get; set; } = [];
}
