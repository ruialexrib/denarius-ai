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
/// Serves the application home page and shared top-level navigation endpoints.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HomeController"/> class.
/// </remarks>
/// <param name="dashboardService">The dashboard service for retrieving dashboard data.</param>
/// <param name="budgetService">The budget service for managing budget periods.</param>
/// <param name="dbContext">The database context for accessing application data.</param>
/// <param name="userManager">The user manager for handling user operations.</param>
/// <param name="llmService">The LLM service for generating AI-powered content.</param>
/// <param name="settingsService">The settings service for retrieving application settings.</param>
/// <param name="cache">The memory cache for storing temporary data.</param>
/// <param name="logger">The logger for recording application events.</param>
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
    /// <summary>
    /// Displays the main dashboard with financial overview for the specified period.
    /// </summary>
    /// <param name="year">The year to display. Defaults to the current year if not specified.</param>
    /// <param name="month">The month to display. Defaults to the current month if not specified.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A view with the dashboard data.</returns>
    public async Task<IActionResult> Index(int? year, int? month, CancellationToken cancellationToken)
    {
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = month ?? DateTime.Today.Month;
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

    /// <summary>
    /// Acknowledges that the user has been informed about demonstration data in the system.
    /// </summary>
    /// <returns>A redirect to the index page.</returns>
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

    /// <summary>
    /// Displays the error page.
    /// </summary>
    /// <returns>A view with error details.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });

    /// <summary>
    /// Generates a personalized welcome message for the dashboard, optionally using AI.
    /// </summary>
    /// <param name="user">The current user.</param>
    /// <param name="dashboard">The dashboard data for the current period.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A tuple containing the welcome message and a flag indicating if it was generated by AI.</returns>
    private async Task<(string Message, bool GeneratedByAi)> GetWelcomeMessageAsync(ApplicationUser? user, DenariusAI.Application.DTOs.DashboardDto dashboard, CancellationToken cancellationToken)
    {
        var remainingBudgetedExpenses = Math.Max(dashboard.Budgeted - dashboard.BudgetActual, 0m);
        var projectedClosingBalance = dashboard.LiquidBalance - remainingBudgetedExpenses;
        var budgetIsCovered = projectedClosingBalance >= 0m;
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-PT");
        var coverageMessage = budgetIsCovered
            ? "O saldo atual permite cobrir as despesas previstas."
            : $"Existe um défice potencial de {Math.Abs(projectedClosingBalance).ToString("N2", culture)} €; reveja as despesas previstas e ajuste o orçamento.";
        var fallback = dashboard.Budgeted > 0m
            ? $"Situação atual: o saldo disponível é {dashboard.LiquidBalance.ToString("N2", culture)} €, com {dashboard.Income.ToString("N2", culture)} € de rendimentos e {dashboard.Expenses.ToString("N2", culture)} € de despesas no período.\n\nPrevisão: depois de considerar {remainingBudgetedExpenses.ToString("N2", culture)} € de despesas ainda por executar, o saldo projetado no fim do orçamento é {projectedClosingBalance.ToString("N2", culture)} €. {coverageMessage}\n\nNa aplicação: acompanhe a execução do orçamento, reconcilie os movimentos pendentes e consulte a análise financeira para identificar desvios.\n\nDica financeira: reveja regularmente as despesas recorrentes e preserve uma margem para imprevistos."
            : $"Situação atual: o saldo disponível é {dashboard.LiquidBalance.ToString("N2", culture)} €, com {dashboard.Income.ToString("N2", culture)} € de rendimentos e {dashboard.Expenses.ToString("N2", culture)} € de despesas no período.\n\nPrevisão: ainda não existe um orçamento com despesas previstas, pelo que não é possível estimar de forma útil o saldo final.\n\nNa aplicação: defina o orçamento, classifique os movimentos e utilize a análise financeira para acompanhar a evolução.\n\nDica financeira: planeie primeiro as despesas essenciais e mantenha uma reserva para acontecimentos inesperados.";
        if (user is null || !llmService.IsConfigured) return (fallback, false);
        var key = $"dashboard-welcome:{user.Id}:{dashboard.Year}:{dashboard.Month}:{dashboard.LiquidBalance}:{dashboard.Budgeted}:{dashboard.BudgetActual}";
        if (cache.TryGetValue<string>(key, out var cached) && !string.IsNullOrWhiteSpace(cached)) return (cached, true);
        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var context = JsonSerializer.Serialize(new { user = user.DisplayName, period = $"{dashboard.Month:D2}/{dashboard.Year}", dashboard.LiquidBalance, dashboard.TotalAssets, dashboard.Income, dashboard.Expenses, result = dashboard.MonthlyResult, budgetedExpenses = dashboard.Budgeted, executedBudgetExpenses = dashboard.BudgetActual, remainingBudgetedExpenses, projectedClosingBalance, budgetIsCovered, projectedShortfall = Math.Max(-projectedClosingBalance, 0m), dashboard.UnreconciledMovements, dashboard.SavingsCertificatesValue });
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
