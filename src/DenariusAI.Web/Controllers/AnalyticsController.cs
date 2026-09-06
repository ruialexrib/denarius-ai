using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using DenariusAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using System.Text;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Provides expense and income analytics views for configurable date ranges.
/// </summary>
/// <param name="analyticsService">Service for retrieving analytics data.</param>
/// <param name="groupService">Service for managing financial groups.</param>
/// <param name="categoryService">Service for managing categories.</param>
/// <param name="accountService">Service for managing accounts.</param>
/// <param name="dashboardService">Service for retrieving dashboard data.</param>
/// <param name="dbContext">Database context for direct data access.</param>
/// <param name="llmService">Service for LLM-based intelligent report generation.</param>
/// <param name="settingsService">Service for retrieving application settings.</param>
[Authorize]
public sealed class AnalyticsController(IAnalyticsService analyticsService, IFinancialGroupService groupService, ICategoryService categoryService, IAccountService accountService, IDashboardService dashboardService, IFinancialReportDataService reportDataService, ILLMService llmService, IApplicationSettingsService settingsService) : Controller
{
    /// <summary>
    /// Displays the analytics dashboard with filtering options.
    /// </summary>
    /// <param name="from">Start date for the analytics period.</param>
    /// <param name="to">End date for the analytics period.</param>
    /// <param name="groupId">Optional financial group ID filter.</param>
    /// <param name="categoryId">Optional category ID filter.</param>
    /// <param name="accountId">Optional account ID filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The analytics view with filtered data.</returns>
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, Guid? groupId, Guid? categoryId, Guid? accountId, CancellationToken cancellationToken)
    {
        var selectedTo = to ?? DateOnly.FromDateTime(DateTime.Today);
        var selectedFrom = from ?? new DateOnly(selectedTo.Year, 1, 1);
        if (selectedFrom > selectedTo) return BadRequest();
        var filter = new AnalyticsFilterDto(selectedFrom, selectedTo, groupId, categoryId, accountId);
        var analytics = await analyticsService.GetAsync(filter, cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var categories = CategoryDisplayOrdering.Order(await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken), groups);
        var accounts = await accountService.ListAsync(true, cancellationToken);
        var annual = await dashboardService.GetAsync(DateTime.Today.Year, DateTime.Today.Month, cancellationToken);
        return View(new AnalyticsViewModel(filter, analytics,
            groups.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == groupId)).Prepend(new("Todos os grupos", "")).ToList(),
            categories.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == categoryId)).Prepend(new("Todas as categorias", "")).ToList(),
            accounts.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == accountId)).Prepend(new("Todas as contas", "")).ToList(),
            annual.Evolution, annual.BudgetEvolution));
    }

    /// <summary>
    /// Generates an intelligent financial report using LLM analysis for the specified date range.
    /// </summary>
    /// <param name="from">Start date of the report period.</param>
    /// <param name="to">End date of the report period.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The intelligent report view or redirects with error message if generation fails.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateIntelligentReport(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (from == default || to == default || from > to) return BadRequest();
        if (!llmService.IsConfigured) { TempData["ErrorMessage"] = "Configure a integração de IA antes de gerar o relatório inteligente."; return RedirectToAction(nameof(Index), new { from, to }); }
        var data = await reportDataService.GetAsync(from, to, cancellationToken);
        var prompt = (await settingsService.GetAsync(cancellationToken)).FinancialAnalysisPrompt;
        var accuracyRules = """
            Os dados JSON seguintes são factos financeiros autoritativos já calculados pela aplicação. Não recalcules, não somes movimentos e não alteres nenhum valor. Usa Income, Expenses, Savings, SavingsRate e NetWorth exatamente como fornecidos. Em Months, Expenses representa todas as despesas do mês; BudgetExecuted representa apenas movimentos associados ao orçamento desse mês e não são valores equivalentes. Se uma lista estiver vazia, indica que não existem registos. Não inventes contas, categorias, reconciliações ou Certificados de Aforro.
            """;
        var completion = await llmService.CompleteAsync([new("system", $"{prompt}\n\n{accuracyRules}"), new("user", JsonSerializer.Serialize(data))], 8192, cancellationToken);
        if (string.Equals(completion.FinishReason, "length", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "O modelo atingiu o limite antes de concluir o relatório. Reduza o período analisado e tente novamente."; return RedirectToAction(nameof(Index), new { from, to }); }
        return View("IntelligentReport", new IntelligentReportViewModel(from, to, DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"), completion.Model, MarkdownPreview.Normalize(completion.Content)));
    }

    /// <summary>
    /// Exports the generated intelligent report as a Markdown file.
    /// </summary>
    /// <param name="markdown">The markdown content of the report.</param>
    /// <param name="from">Start date of the report period.</param>
    /// <param name="to">End date of the report period.</param>
    /// <returns>A file download result with the Markdown content.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExportMarkdown(string markdown, DateOnly from, DateOnly to) =>
        File(Encoding.UTF8.GetBytes(markdown ?? string.Empty), "text/markdown; charset=utf-8", $"relatorio-financeiro-{from:yyyyMMdd}-{to:yyyyMMdd}.md");

    /// <summary>
    /// Exports the generated intelligent report as a PDF file.
    /// </summary>
    /// <param name="markdown">The markdown content of the report.</param>
    /// <param name="from">Start date of the report period.</param>
    /// <param name="to">End date of the report period.</param>
    /// <returns>A file download result with the PDF content.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExportPdf(string markdown, DateOnly from, DateOnly to) =>
        File(FinancialReportPdf.Generate(markdown, from, to), "application/pdf", $"relatorio-financeiro-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
}
