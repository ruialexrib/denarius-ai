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
/// <param name="budgetExecutionAnalysisService">Service that calculates monthly budget execution analysis.</param>
/// <param name="savingsLiquidityAnalysisService">Service that calculates savings and liquidity analysis.</param>
/// <param name="investmentPortfolioAnalysisService">Service that calculates investment and financial-assets analysis.</param>
/// <param name="financialCommitmentsAnalysisService">Service that calculates known financial commitments and insurance analysis.</param>
/// <param name="llmService">Provider-neutral language model service.</param>
/// <param name="settingsService">Application settings service used to obtain the effective financial-analysis prompt.</param>
/// <param name="logger">Logger used for safe AI failure diagnostics.</param>
[Authorize]
public sealed class AnalyticsController(
    IGlobalFinancialViewService globalFinancialViewService,
    IIncomeExpenseFlowAnalysisService incomeExpenseFlowAnalysisService,
    IBudgetExecutionAnalysisService budgetExecutionAnalysisService,
    ISavingsLiquidityAnalysisService savingsLiquidityAnalysisService,
    IInvestmentPortfolioAnalysisService investmentPortfolioAnalysisService,
    IFinancialCommitmentsAnalysisService financialCommitmentsAnalysisService,
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
    /// Displays deterministic monthly budget execution analysis.
    /// </summary>
    /// <param name="year">Optional selected budget year.</param>
    /// <param name="month">Optional selected budget month.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The populated budget execution analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Budget(
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var selectedYear = year ?? today.Year;
        var selectedMonth = month ?? today.Month;
        var analysis = await budgetExecutionAnalysisService.GetAsync(selectedYear, selectedMonth, cancellationToken);
        return View(new BudgetExecutionAnalysisViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>
    /// Generates an optional AI interpretation from already calculated budget execution facts.
    /// </summary>
    /// <param name="year">Selected budget year.</param>
    /// <param name="month">Selected budget month.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The budget analysis view with an AI interpretation or safe error feedback.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BudgetAi(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var analysis = await budgetExecutionAnalysisService.GetAsync(year, month, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Budget", new BudgetExecutionAnalysisViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var context = JsonSerializer.Serialize(BuildBudgetAiContext(analysis));
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.BudgetExecutionAnalysisPrompt),
                    new LlmMessageDto("user", context)
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);

            return View("Budget", new BudgetExecutionAnalysisViewModel(
                analysis,
                true,
                completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Budget execution AI interpretation failed.");
            return View("Budget", new BudgetExecutionAnalysisViewModel(
                analysis,
                true,
                AiError: "Não foi possível gerar a interpretação por IA. A análise calculada continua disponível."));
        }
    }

    /// <summary>Displays deterministic savings and liquidity analysis for the selected interval.</summary>
    /// <param name="from">Optional selected-period start.</param>
    /// <param name="to">Optional selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The savings and liquidity analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Savings(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var period = ResolvePeriod(from, to);
        var analysis = await savingsLiquidityAnalysisService.GetAsync(period.From, period.To, cancellationToken);
        return View(new SavingsLiquidityAnalysisViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>Generates an optional AI interpretation of pre-calculated savings and liquidity facts.</summary>
    /// <param name="from">Selected-period start.</param>
    /// <param name="to">Selected-period end.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The savings analysis view with optional AI interpretation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavingsAi(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var analysis = await savingsLiquidityAnalysisService.GetAsync(from, to, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Savings", new SavingsLiquidityAnalysisViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.SavingsLiquidityAnalysisPrompt),
                    new LlmMessageDto("user", JsonSerializer.Serialize(BuildSavingsAiContext(analysis)))
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);
            return View("Savings", new SavingsLiquidityAnalysisViewModel(analysis, true, completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Savings and liquidity AI interpretation failed.");
            return View("Savings", new SavingsLiquidityAnalysisViewModel(
                analysis,
                true,
                AiError: "Não foi possível gerar a interpretação por IA. A análise calculada continua disponível."));
        }
    }

    /// <summary>Displays deterministic investment and financial-assets analysis.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The investment analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Investments(
        CancellationToken cancellationToken = default)
    {
        var referenceDate = DateOnly.FromDateTime(DateTime.Today);
        var analysis = await investmentPortfolioAnalysisService.GetAsync(referenceDate, cancellationToken);
        return View(new InvestmentPortfolioAnalysisViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>Generates an optional AI interpretation of pre-calculated investment facts.</summary>
    /// <param name="asOf">Reference date.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The investment analysis view with optional AI interpretation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InvestmentsAi(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var analysis = await investmentPortfolioAnalysisService.GetAsync(asOf, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Investments", new InvestmentPortfolioAnalysisViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.InvestmentPortfolioAnalysisPrompt),
                    new LlmMessageDto("user", JsonSerializer.Serialize(BuildInvestmentAiContext(analysis)))
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);
            return View("Investments", new InvestmentPortfolioAnalysisViewModel(analysis, true, completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Investment portfolio AI interpretation failed.");
            return View("Investments", new InvestmentPortfolioAnalysisViewModel(
                analysis,
                true,
                AiError: "Não foi possível gerar a interpretação por IA. A análise calculada continua disponível."));
        }
    }

    /// <summary>Displays deterministic known financial commitments and insurance analysis.</summary>
    /// <param name="asOf">Optional reference date.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The commitments analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Commitments(
        DateOnly? asOf,
        CancellationToken cancellationToken = default)
    {
        var referenceDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var analysis = await financialCommitmentsAnalysisService.GetAsync(referenceDate, cancellationToken);
        return View(new FinancialCommitmentsAnalysisViewModel(analysis, llmService.IsConfigured));
    }

    /// <summary>Generates an optional AI interpretation of pre-calculated commitments facts.</summary>
    /// <param name="asOf">Reference date.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The commitments analysis view with optional AI interpretation.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommitmentsAi(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var analysis = await financialCommitmentsAnalysisService.GetAsync(asOf, cancellationToken);
        if (!llmService.IsConfigured)
        {
            return View("Commitments", new FinancialCommitmentsAnalysisViewModel(
                analysis,
                false,
                AiError: "A interpretação por IA não está disponível porque o fornecedor selecionado não está configurado."));
        }

        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var completion = await llmService.CompleteAsync(
                [
                    new LlmMessageDto("system", settings.FinancialCommitmentsAnalysisPrompt),
                    new LlmMessageDto("user", JsonSerializer.Serialize(BuildCommitmentsAiContext(analysis)))
                ],
                Math.Min(settings.AiMaxTokens, 1600),
                cancellationToken);
            return View("Commitments", new FinancialCommitmentsAnalysisViewModel(analysis, true, completion.Content.Trim()));
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Financial commitments AI interpretation failed.");
            return View("Commitments", new FinancialCommitmentsAnalysisViewModel(
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

    /// <summary>
    /// Builds bounded, pre-calculated context for budget execution interpretation.
    /// </summary>
    /// <param name="analysis">Calculated budget execution analysis.</param>
    /// <returns>Provider-neutral budget facts with no unrestricted movement history.</returns>
    private static object BuildBudgetAiContext(BudgetExecutionAnalysisDto analysis) => new
    {
        purpose = "Interpretar Orçamento e Execução Orçamental sem recalcular valores.",
        period = new { analysis.Year, analysis.Month },
        summary = new
        {
            analysis.HasBudget,
            analysis.TotalBudgeted,
            analysis.TotalActual,
            analysis.TotalVariance,
            analysis.ExecutionPercentage,
            analysis.OverBudgetAmount,
            analysis.OverBudgetCategoryCount,
            analysis.NearLimitCategoryCount,
            analysis.UnbudgetedCategoryCount
        },
        categories = analysis.Categories.Take(12).Select(item => new
        {
            item.CategoryName,
            item.FinancialGroupName,
            item.Budgeted,
            item.Actual,
            item.Variance,
            item.RelativeVariancePercentage,
            item.ExecutionPercentage,
            status = item.StatusLabel
        }),
        trend = analysis.Trend,
        findings = analysis.Findings.Select(item => new { item.Tone, item.Title, item.Detail })
    };

    /// <summary>Builds bounded AI context for savings and liquidity interpretation.</summary>
    /// <param name="analysis">Calculated savings and liquidity analysis.</param>
    /// <returns>Pre-calculated provider-neutral facts.</returns>
    private static object BuildSavingsAiContext(SavingsLiquidityAnalysisDto analysis) => new
    {
        purpose = "Interpretar Poupança e Liquidez sem recalcular valores.",
        period = new { analysis.From, analysis.To },
        comparison = new { analysis.ComparisonFrom, analysis.ComparisonTo },
        summary = new
        {
            analysis.Savings,
            analysis.SavingsRate,
            analysis.PreviousSavings,
            analysis.PreviousSavingsRate,
            analysis.EurImmediateLiquidity,
            analysis.EurSavingsAccounts,
            analysis.SavingsCertificatesValue,
            analysis.SavingsCertificatesYield,
            analysis.LargestLiquidAccountWeight,
            analysis.NonEurLiquidAccountCount
        },
        liquidAccounts = analysis.LiquidAccounts.Take(12),
        trend = analysis.Trend,
        findings = analysis.Findings.Select(item => new { item.Tone, item.Title, item.Detail })
    };

    /// <summary>Builds bounded AI context for investment portfolio interpretation.</summary>
    /// <param name="analysis">Calculated financial-assets analysis.</param>
    /// <returns>Pre-calculated provider-neutral facts.</returns>
    private static object BuildInvestmentAiContext(InvestmentPortfolioAnalysisDto analysis) => new
    {
        purpose = "Interpretar Investimentos e Património Financeiro sem recalcular valores ou converter moedas.",
        analysis.AsOf,
        summary = new
        {
            analysis.EurStockCost,
            analysis.EurStockMarketValue,
            analysis.EurStockGain,
            analysis.EurStockReturnPercentage,
            analysis.SavingsCertificatesInvestment,
            analysis.SavingsCertificatesValue,
            analysis.SavingsCertificatesYield,
            analysis.EurTrackedFinancialAssets,
            analysis.OwnedStockPositions,
            analysis.SavingsCertificateCount,
            analysis.NonEurStockPositions
        },
        currencySummaries = analysis.CurrencySummaries,
        stocks = analysis.Stocks.Take(12),
        certificates = analysis.Certificates.Take(12),
        findings = analysis.Findings.Select(item => new { item.Tone, item.Title, item.Detail })
    };

    /// <summary>Builds bounded AI context for financial commitments interpretation.</summary>
    /// <param name="analysis">Calculated known commitments analysis.</param>
    /// <returns>Pre-calculated provider-neutral facts.</returns>
    private static object BuildCommitmentsAiContext(FinancialCommitmentsAnalysisDto analysis) => new
    {
        purpose = "Interpretar Compromissos Financeiros e Seguros sem inventar prémios futuros.",
        period = new { analysis.AsOf, analysis.HorizonEnd },
        summary = new
        {
            analysis.ActivePolicies,
            analysis.ScheduledPremiums,
            analysis.OutstandingAmount,
            analysis.OutstandingPremiums,
            analysis.RenewalsWithin90Days,
            analysis.PaidWithinHorizon
        },
        policies = analysis.Policies.Take(12),
        calendar = analysis.Calendar,
        types = analysis.Types,
        findings = analysis.Findings.Select(item => new { item.Tone, item.Title, item.Detail })
    };
}
