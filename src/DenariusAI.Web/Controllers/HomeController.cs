using System.Diagnostics;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using DenariusAI.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class HomeController(IDashboardService dashboardService, IBudgetService budgetService) : Controller
{
    public async Task<IActionResult> Index(int? year, int? month, CancellationToken cancellationToken)
    {
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        var latest = budgets.FirstOrDefault();
        var selectedYear = year ?? latest?.Year ?? DateTime.Today.Year;
        var selectedMonth = month ?? latest?.Month ?? DateTime.Today.Month;
        if (selectedYear is < 2000 or > 9999 || selectedMonth is < 1 or > 12) return BadRequest();
        var dashboard = await dashboardService.GetAsync(selectedYear, selectedMonth, cancellationToken);
        var years = budgets.Select(item => item.Year).Append(DateTime.Today.Year).Distinct().OrderByDescending(item => item)
            .Select(item => new SelectListItem(item.ToString(), item.ToString(), item == selectedYear)).ToList();
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-PT");
        var months = Enumerable.Range(1, 12).Select(item => new SelectListItem(culture.DateTimeFormat.GetMonthName(item), item.ToString(), item == selectedMonth)).ToList();
        return View(new DashboardViewModel(dashboard, years, months));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
