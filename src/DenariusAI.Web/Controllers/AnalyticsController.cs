using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using DenariusAI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Provides expense and income analytics views for configurable date ranges.
/// </summary>
[Authorize]
public sealed class AnalyticsController(IAnalyticsService analyticsService, IFinancialGroupService groupService, ICategoryService categoryService, IAccountService accountService, IDashboardService dashboardService, DenariusDbContext dbContext, ILLMService llmService) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, Guid? groupId, Guid? categoryId, Guid? accountId, CancellationToken cancellationToken)
    {
        var selectedTo = to ?? DateOnly.FromDateTime(DateTime.Today);
        var selectedFrom = from ?? new DateOnly(selectedTo.Year, 1, 1);
        if (selectedFrom > selectedTo) return BadRequest();
        var filter = new AnalyticsFilterDto(selectedFrom, selectedTo, groupId, categoryId, accountId);
        var analytics = await analyticsService.GetAsync(filter, cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var categories = await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken);
        var accounts = await accountService.ListAsync(true, cancellationToken);
        var annual = await dashboardService.GetAsync(DateTime.Today.Year, DateTime.Today.Month, cancellationToken);
        return View(new AnalyticsViewModel(filter, analytics,
            groups.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == groupId)).Prepend(new("Todos os grupos", "")).ToList(),
            categories.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == categoryId)).Prepend(new("Todas as categorias", "")).ToList(),
            accounts.Select(item => new SelectListItem(item.Name, item.Id.ToString(), item.Id == accountId)).Prepend(new("Todas as contas", "")).ToList(),
            annual.Evolution, annual.BudgetEvolution));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateIntelligentReport(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (from == default || to == default || from > to) return BadRequest();
        if (!llmService.IsConfigured) { TempData["ErrorMessage"] = "Configure a integração Mistral antes de gerar o relatório inteligente."; return RedirectToAction(nameof(Index), new { from, to }); }
        var data = new
        {
            period = new { from, to },
            groups = await dbContext.FinancialGroups.AsNoTracking().Select(x => new { x.Name, x.Kind, x.IsActive }).ToListAsync(cancellationToken),
            categories = await dbContext.Categories.AsNoTracking().Select(x => new { x.Name, Group = x.FinancialGroup.Name, x.IsActive }).ToListAsync(cancellationToken),
            accounts = await dbContext.Accounts.AsNoTracking().Select(x => new { x.Name, x.AccountType, x.InitialBalance, x.Currency, x.IsActive }).ToListAsync(cancellationToken),
            movements = await dbContext.JournalEntries.AsNoTracking().Where(x => x.Date >= from && x.Date <= to).Select(x => new { x.Date, x.Description, x.Reference, x.Status, x.BudgetId, Lines = x.Lines.Select(l => new { Account = l.Account.Name, Category = l.Category != null ? l.Category.Name : null, l.Debit, l.Credit }) }).ToListAsync(cancellationToken),
            budgets = await dbContext.Budgets.AsNoTracking().Where(x => x.Year >= from.Year && x.Year <= to.Year).Select(x => new { x.Year, x.Month, Lines = x.Lines.Select(l => new { Category = l.Category.Name, l.Amount }) }).ToListAsync(cancellationToken),
            reconciliations = await dbContext.Reconciliations.AsNoTracking().Select(x => new { x.Status, x.ReconciledAt }).ToListAsync(cancellationToken),
            savingsCertificates = await dbContext.SavingsCertificates.AsNoTracking().Select(x => new { x.InvestmentDate, x.SeriesNumber, x.Description, x.InvestmentValue, x.Rate, x.CurrentValue, x.NextCapitalization }).ToListAsync(cancellationToken)
        };
        var prompt = "És um analista financeiro pessoal. Produz um relatório completo mas conciso em Markdown, em português de Portugal, usando todas as tabelas fornecidas. Inclui obrigatoriamente: resumo executivo, rendimentos e despesas, orçamento, património, Certificados de Aforro, reconciliação, riscos/anomalias, oportunidades, ações recomendadas e conclusão. Não inventes valores e indica quando faltam dados. Termina sempre todas as tabelas e secções. Devolve apenas o Markdown do relatório, sem o envolver numa cerca de código ```markdown.";
        var completion = await llmService.CompleteAsync([new("system", prompt), new("user", JsonSerializer.Serialize(data))], 8192, cancellationToken);
        if (string.Equals(completion.FinishReason, "length", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "O modelo atingiu o limite antes de concluir o relatório. Reduza o período analisado e tente novamente."; return RedirectToAction(nameof(Index), new { from, to }); }
        return View("IntelligentReport", new IntelligentReportViewModel(from, to, DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"), completion.Model, MarkdownPreview.Normalize(completion.Content)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExportMarkdown(string markdown, DateOnly from, DateOnly to) =>
        File(Encoding.UTF8.GetBytes(markdown ?? string.Empty), "text/markdown; charset=utf-8", $"relatorio-financeiro-{from:yyyyMMdd}-{to:yyyyMMdd}.md");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ExportPdf(string markdown, DateOnly from, DateOnly to) =>
        File(FinancialReportPdf.Generate(markdown, from, to), "application/pdf", $"relatorio-financeiro-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
}
