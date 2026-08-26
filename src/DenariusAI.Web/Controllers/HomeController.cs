using System.Diagnostics;
using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;
using DenariusAI.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the HomeController type.
/// </summary>
[Authorize]
public sealed class HomeController(
    IDashboardService dashboardService,
    IBudgetService budgetService,
    DenariusDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILLMService llmService,
    IApplicationSettingsService settingsService,
    IMemoryCache cache,
    ILogger<HomeController> logger) : Controller
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
        var user = await userManager.GetUserAsync(User);
        var hasDemonstrationData = user?.DemonstrationDataAcknowledgedAt is null
            && await dbContext.JournalEntries.AsNoTracking()
                .AnyAsync(item => item.CreatedBy == "demo-seed", cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var reminders = user is null ? [] : await dbContext.Reminders.AsNoTracking()
            .Where(item => item.EventDate.AddDays(-item.NoticeDays) <= today
                && !item.Acknowledgements.Any(value => value.UserId == user.Id))
            .OrderBy(item => item.EventDate).Take(5)
            .Select(item => new DashboardReminderViewModel(item.Id, item.Text, item.EventDate, item.EventDate.DayNumber - today.DayNumber))
            .ToListAsync(cancellationToken);
        var (welcomeMessage, welcomeGeneratedByAi) = await GetWelcomeMessageAsync(user, dashboard, cancellationToken);
        return View(new DashboardViewModel(dashboard, years, months, hasDemonstrationData, reminders, welcomeMessage, welcomeGeneratedByAi));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeDemonstrationData()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        user.DemonstrationDataAcknowledgedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) TempData["ErrorMessage"] = "Não foi possível guardar a confirmação.";
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });

    private async Task<(string Message, bool GeneratedByAi)> GetWelcomeMessageAsync(ApplicationUser? user, DenariusAI.Application.DTOs.DashboardDto dashboard, CancellationToken cancellationToken)
    {
        const string fallback = "Bem-vindo ao DenariusAI. Consulte os indicadores, organize o orçamento, reconcilie os movimentos e use a análise financeira para acompanhar a evolução. Reserve regularmente uma parte dos rendimentos e reveja as despesas recorrentes antes de assumir novos compromissos.";
        if (user is null || !llmService.IsConfigured) return (fallback, false);
        var key = $"dashboard-welcome:{user.Id}:{dashboard.Year}:{dashboard.Month}";
        if (cache.TryGetValue<string>(key, out var cached) && !string.IsNullOrWhiteSpace(cached)) return (cached, true);
        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var context = JsonSerializer.Serialize(new { user = user.DisplayName, period = $"{dashboard.Month:D2}/{dashboard.Year}", dashboard.LiquidBalance, dashboard.TotalAssets, dashboard.Income, dashboard.Expenses, result = dashboard.MonthlyResult, dashboard.Budgeted, executed = dashboard.BudgetActual, dashboard.UnreconciledMovements, dashboard.SavingsCertificatesValue });
            var completion = await llmService.CompleteAsync([new("system", settings.DashboardWelcomePrompt), new("user", context)], cancellationToken);
            var message = completion.Content.Trim();
            if (string.IsNullOrWhiteSpace(message)) return (fallback, false);
            cache.Set(key, message, TimeSpan.FromMinutes(30));
            return (message, true);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Dashboard welcome message could not be generated.");
            return (fallback, false);
        }
    }
}
