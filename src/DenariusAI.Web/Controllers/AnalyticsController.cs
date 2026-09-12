using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Provides the financial-analysis catalogue and the specialised global financial view.
/// </summary>
/// <param name="globalFinancialViewService">Service that calculates the deterministic global financial view.</param>
/// <param name="incomeExpenseFlowAnalysisService">Service that calculates income, expense and account-flow analysis.</param>
/// <param name="llmService">Provider-neutral language model service.</param>
/// <param name="settingsService">Application settings service used to obtain the effective financial-analysis prompt.</param>
/// <param name="logger">Logger used for safe AI failure diagnostics.</param>
[Authorize]
public sealed class AnalyticsController(
    IGlobalFinancialViewService globalFinancialViewService,
    IIncomeExpenseFlowAnalysisService incomeExpenseFlowAnalysisService,
    ILLMService llmService,
    IApplicationSettingsService settingsService,
    ILogger<AnalyticsController> logger) : Controller
{
    /// <summary>
    /// Displays the catalogue of thematic financial-analysis areas.
    /// </summary>
    /// <returns>The analytics catalogue view.</returns>
    public IActionResult Index() => View();

    /// <summary>
    /// Displays the deterministic global financial view for the selected interval.
    /// </summary>
    /// <param name="from">Optional selected-period start.</param>
    /// <param name="to">Optional selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The populated global financial view.</returns>
    [HttpGet]
    public async Task<IActionResult> Global(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var period = ResolvePeriod(from, to);
        var analysis = await globalFinancialViewService.GetAsync(period.From, period.To, cancellationToken);
        return View(new GlobalFinancialViewViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>
    /// Generates an optional AI interpretation from the already calculated global financial facts.
    /// </summary>
    /// <param name="from">Selected-period start.</param>
    /// <param name="to">Selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The global financial view with an AI interpretation or safe error feedback.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GlobalAi(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var analysis = await globalFinancialViewService.GetAsync(from, to, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Global", new GlobalFinancialViewViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var context = JsonSerializer.Serialize(BuildAiContext(analysis));
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.FinancialAnalysisPrompt),
                    new LlmMessageDto("user", context)
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);

            return View("Global", new GlobalFinancialViewViewModel(
                analysis,
                true,
                completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Global financial AI interpretation failed.");
            return View("Global", new GlobalFinancialViewViewModel(
                analysis,
                true,
                AiError: "Não foi possível gerar a interpretação por IA. Os indicadores calculados continuam disponíveis."));
        }
    }

    /// <summary>
    /// Displays the deterministic Income, Expenses and Flows analysis.
    /// </summary>
    /// <param name="from">Optional selected-period start.</param>
    /// <param name="to">Optional selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The populated flow analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Flows(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var period = ResolvePeriod(from, to);
        var analysis = await incomeExpenseFlowAnalysisService.GetAsync(period.From, period.To, cancellationToken);
        return View(new IncomeExpenseFlowViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>
    /// Generates an optional AI interpretation of already calculated income, expense and flow facts.
    /// </summary>
    /// <param name="from">Selected-period start.</param>
    /// <param name="to">Selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The flow analysis view with an optional AI interpretation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlowsAi(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var analysis = await incomeExpenseFlowAnalysisService.GetAsync(from, to, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Flows", new IncomeExpenseFlowViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var context = JsonSerializer.Serialize(BuildFlowAiContext(analysis));
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.IncomeExpenseFlowAnalysisPrompt),
                    new LlmMessageDto("user", context)
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);

            return View("Flows", new IncomeExpenseFlowViewModel(
                analysis,
                true,
                completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Income, expense and flow AI interpretation failed.");
            return View("Flows", new IncomeExpenseFlowViewModel(
                analysis,
                true,
                AiError: "Não foi possível gerar a interpretação por IA. A análise calculada continua disponível."));
        }
    }

    /// <summary>
    /// Resolves the requested interval and applies the current-month default.
    /// </summary>
    /// <param name="from">Optional selected-period start.</param>
    /// <param name="to">Optional selected-period end.</param>
    /// <returns>The validated interval.</returns>
    private static (DateOnly From, DateOnly To) ResolvePeriod(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var resolvedTo = to ?? today;
        var resolvedFrom = from ?? new DateOnly(resolvedTo.Year, resolvedTo.Month, 1);
        if (resolvedFrom > resolvedTo)
            throw new ArgumentException("O intervalo de datas é inválido.");
        return (resolvedFrom, resolvedTo);
    }

    /// <summary>
    /// Builds the bounded, pre-calculated context supplied to the language model.
    /// </summary>
    /// <param name="analysis">Authoritative global financial view.</param>
    /// <returns>An anonymous context object containing only calculated financial facts.</returns>
    private static object BuildAiContext(GlobalFinancialViewDto analysis) => new
    {
        purpose = "Interpretar a Visão Financeira Global sem recalcular valores.",
        period = new { from = analysis.From, to = analysis.To },
        comparisonPeriod = new { from = analysis.ComparisonFrom, to = analysis.ComparisonTo },
        currency = analysis.Currency,
        metrics = new
        {
            netWorth = MetricContext(analysis.NetWorth),
            liquidBalance = MetricContext(analysis.LiquidBalance),
            savings = MetricContext(analysis.Savings),
            savingsRate = MetricContext(analysis.SavingsRate),
            income = MetricContext(analysis.Income),
            expenses = MetricContext(analysis.Expenses)
        },
        findings = analysis.Findings.Select(item => new { item.Tone, item.Title, item.Detail, item.TargetArea }),
        trend = analysis.Trend.Select(item => new
        {
            item.Year,
            item.Month,
            item.Income,
            item.Expenses,
            item.Savings,
            item.SavingsRate,
            item.NetWorth
        }),
        dataQuality = new
        {
            analysis.HasActivity,
            analysis.HasComparisonActivity,
            analysis.UnreconciledMovements
        }
    };

    /// <summary>
    /// Converts one calculated metric into a provider-neutral AI context shape.
    /// </summary>
    /// <param name="metric">Calculated metric.</param>
    /// <returns>The metric facts supplied to the model.</returns>
    private static object MetricContext(GlobalFinancialMetricDto metric) => new
    {
        metric.Key,
        metric.Label,
        current = metric.CurrentValue,
        comparison = metric.ComparisonValue,
        absoluteChange = metric.AbsoluteChange,
        percentageChange = metric.PercentageChange,
        direction = metric.Direction.ToString(),
        metric.IsPercentage
    };

    /// <summary>
    /// Builds bounded, calculated context for the income, expense and flow AI interpretation.
    /// </summary>
    /// <param name="analysis">Calculated flow analysis.</param>
    /// <returns>Provider-neutral facts with no raw unrestricted financial history.</returns>
    private static object BuildFlowAiContext(IncomeExpenseFlowAnalysisDto analysis) => new
    {
        purpose = "Interpretar Rendimentos, Despesas e Fluxos sem recalcular valores.",
        period = new { from = analysis.Data.From, to = analysis.Data.To },
        comparisonPeriod = new { from = analysis.Data.ComparisonFrom, to = analysis.Data.ComparisonTo },
        metrics = new
        {
            income = MetricContext(analysis.IncomeMetric),
            expenses = MetricContext(analysis.ExpenseMetric),
            balance = MetricContext(analysis.BalanceMetric)
        },
        incomeGroups = analysis.Data.IncomeGroups.Take(8),
        incomeCategories = analysis.Data.IncomeCategories.Take(10),
        expenseGroups = analysis.Data.ExpenseGroups.Take(8),
        expenseCategories = analysis.Data.ExpenseCategories.Take(10),
        accountFlows = analysis.Data.AccountFlows.Take(10),
        trend = analysis.Data.Trend,
        largestMovements = analysis.Data.LargestMovements,
        findings = analysis.Findings
    };
}
