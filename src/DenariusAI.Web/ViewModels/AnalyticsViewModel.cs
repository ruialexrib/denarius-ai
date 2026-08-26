using DenariusAI.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for AnalyticsViewModel.
/// </summary>
public sealed record AnalyticsViewModel(AnalyticsFilterDto Filter, AnalyticsDto Analytics,
    IReadOnlyList<SelectListItem> Groups, IReadOnlyList<SelectListItem> Categories, IReadOnlyList<SelectListItem> Accounts,
    IReadOnlyList<DashboardMonthDto> AnnualIncomeExpenses, IReadOnlyList<DashboardBudgetMonthDto> AnnualBudgetExecution);
