using System.Net;
using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

/// <summary>
/// Provides financial assistant services powered by a Large Language Model (LLM).
/// This service processes user questions about financial data and returns AI-generated responses
/// based on accounts, transactions, budgets, and analytics context.
/// </summary>
public sealed class FinancialAssistantService(
    ILLMService llmService,
    IAccountService accountService,
    IJournalEntryService journalEntryService,
    IBudgetService budgetService,
    IReconciliationService reconciliationService,
    IDashboardService dashboardService,
    IAnalyticsService analyticsService,
    IApplicationSettingsService settingsService,
    ISavingsCertificateReadRepository? savingsRepository = null) : IAssistantService
{
    /// <summary>
    /// Gets a value indicating whether the assistant is available and properly configured.
    /// </summary>
    public bool IsAvailable => llmService.IsConfigured;
    
    /// <summary>
    /// Gets the name of the LLM model being used by the assistant.
    /// </summary>
    public string Model => llmService.Model;

    /// <summary>
    /// Processes a user question and returns an AI-generated response based on financial context.
    /// </summary>
    /// <param name="request">The assistant request containing the user's question and conversation history.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An assistant response containing the AI-generated answer and metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when the question is empty or exceeds 1000 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the AI assistant is not configured.</exception>
    public async Task<AssistantResponseDto> AskAsync(AssistantRequestDto request, CancellationToken cancellationToken = default)
    {
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("A pergunta é obrigatória.", nameof(request));
        if (question.Length > 1000) throw new ArgumentException("A pergunta não pode exceder 1000 caracteres.", nameof(request));
        if (!IsAvailable) throw new InvalidOperationException("O assistente de IA não está configurado.");

        var settings = await settingsService.GetAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var contextStart = today.AddMonths(-(settings.AssistantContextMonths - 1));
        var dataFrom = new DateOnly(contextStart.Year, contextStart.Month, 1);
        var greeting = IsGreeting(question);
        var history = greeting ? [] : request.History.TakeLast(Math.Min(settings.AssistantHistoryMessages, 4))
            .Where(item => (item.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(item.Content))
            .Select(item => new LlmMessageDto(item.Role, item.Content)).ToList();
        var query = AiContextBudget.Normalize(question + " " + string.Join(" ", history.Where(item => item.Role == "user").Select(item => AiContextBudget.Shorten(item.Content, 1000))));
        var facts = new Dictionary<string, object>();
        var samples = new Dictionary<string, IReadOnlyList<object>>();
        if (!greeting)
        {
            var dashboard = await dashboardService.GetAsync(today.Year, today.Month, cancellationToken);
            facts["currentMonth"] = new { dashboard.Year, dashboard.Month, dashboard.Income, dashboard.Expenses,
                dashboard.MonthlyResult, dashboard.LiquidBalance, dashboard.TotalAssets, dashboard.SavingsAndInvestments,
                dashboard.Budgeted, dashboard.BudgetActual, dashboard.BudgetAvailable, dashboard.BudgetExecution, dashboard.UnreconciledMovements };
            if (ContainsAny(query, "certificado", "aforro"))
                facts["savingsCertificateTotals"] = new { dashboard.SavingsCertificatesValue, dashboard.SavingsCertificatesYield,
                    dashboard.MaturedSavingsCertificates, dashboard.MaturedSavingsCertificatesValue,
                    dashboard.SavingsCertificatesFutureNetInterest, dashboard.SavingsCertificatesFutureValue };
            if (ContainsAny(query, "conta", "saldo", "patrimonio"))
                samples["accounts"] = (await accountService.ListAsync(cancellationToken: cancellationToken))
                    .Select(item => (object)new { item.Name, type = item.AccountType.ToString(), item.Balance, item.Currency }).ToList();
            if (ContainsAny(query, "movimento", "compra", "pagamento", "transac", "gaste", "despes"))
                samples["recentTransactions"] = (await journalEntryService.ListAsync(cancellationToken))
                    .Where(item => item.Status == JournalEntryStatus.Active && item.Date >= dataFrom && item.Date <= today)
                    .OrderByDescending(item => AiContextBudget.Relevance(item.Description, query)).ThenByDescending(item => item.Date)
                    .Select(item => (object)new { item.Date, description = AiContextBudget.Shorten(item.Description, 160), item.TotalDebit, item.TotalCredit, item.MovementType }).ToList();
            if (ContainsAny(query, "orcament", "categoria"))
                samples["currentBudget"] = (await budgetService.GetExecutionAsync(today.Year, today.Month, cancellationToken))
                    .Where(item => item.Budgeted != 0 || item.Actual != 0).Cast<object>().ToList();
            if (ContainsAny(query, "ano", "tendencia", "evolu", "taxa", "poupan", "maiores"))
            {
                var analytics = await analyticsService.GetAsync(new(new DateOnly(today.Year, 1, 1), today), cancellationToken);
                facts["currentYearAnalytics"] = new { from = new DateOnly(today.Year, 1, 1), to = today,
                    analytics.Income, analytics.Expenses, analytics.Savings, analytics.SavingsRate, analytics.NetWorth };
                samples["currentYearCategories"] = analytics.Categories.OrderByDescending(item => item.Amount)
                    .Select(item => (object)new { item.Name, item.Amount }).ToList();
                samples["currentYearTrend"] = analytics.Trend.Cast<object>().ToList();
            }
            if (ContainsAny(query, "reconcili"))
                samples["unreconciledTransactions"] = (await reconciliationService.ListAsync(from: dataFrom, to: today,
                    status: ReconciliationStatus.Unreconciled, cancellationToken: cancellationToken))
                    .Select(item => (object)new { item.Date, description = AiContextBudget.Shorten(item.Description, 160), item.Debit, item.Credit }).ToList();
            if (savingsRepository is not null && ContainsAny(query, "certificado", "aforro"))
                samples["savingsCertificates"] = (await savingsRepository.ListAsync(cancellationToken)).Cast<object>().ToList();
        }

        var prompt = settings.AssistantSystemPrompt + "\n\n" + settings.AiContextGuidancePrompt;
        var limit = Math.Min(settings.AssistantMaxTransactions, 12);
        var previousBytes = int.MaxValue;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var maxBytes = attempt == 0 ? settings.AiMaxInputBytes : settings.AiMaxInputBytes / 2;
            List<LlmMessageDto>? messages;
            do
            {
                var context = greeting ? null : "FINANCIAL_CONTEXT_JSON:\n" + AiContextBudget.Serialize(new
                {
                    generatedAt = today, currency = "EUR", transactionPeriod = new { from = dataFrom, to = today },
                    facts,
                    samples = samples.ToDictionary(item => item.Key, item => new { available = item.Value.Count,
                        included = Math.Min(item.Value.Count, limit), partial = item.Value.Count > limit, rows = item.Value.Take(limit) })
                });
                messages = AiContextBudget.Build(prompt, context, attempt == 0 ? history : [], question, maxBytes);
                if (messages is not null || limit == 0) break;
                limit /= 2;
            } while (true);
            if (messages is null)
                return new("O contexto excede o limite do pedido. Reduza os prompts nas Definições ou ajuste o limite de contexto para o fornecedor utilizado.", Model, dataFrom, today, 0);
            var requestBytes = AiContextBudget.Measure(messages);
            if (attempt > 0 && requestBytes >= previousBytes)
                return new("O fornecedor recusou o pedido por exceder o limite de contexto. Reduza os prompts nas Definições e tente novamente.", Model, dataFrom, today, 0);
            previousBytes = requestBytes;
            try
            {
                var completion = await llmService.CompleteAsync(messages, Math.Min(settings.AiMaxTokens, 1024), cancellationToken);
                return new(completion.Content, completion.Model, dataFrom, today,
                    samples.TryGetValue("recentTransactions", out var transactions) ? Math.Min(transactions.Count, limit) : 0);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                if (attempt == 1)
                    return new("O fornecedor recusou o pedido por exceder o limite de contexto. Reduza os prompts ou o limite de contexto nas Definições e tente novamente.", Model, dataFrom, today, 0);
                limit /= 2;
            }
        }
        throw new InvalidOperationException("Não foi possível preparar o contexto de IA.");
    }

    /// <summary>Recognizes standalone greetings without treating a financial question as a greeting.</summary>
    /// <param name="question">The current question.</param>
    /// <returns>True for an exact greeting or acknowledgement.</returns>
    private static bool IsGreeting(string question) => AiContextBudget.Normalize(question).Trim(' ', '.', '!', '?')
        is "ola" or "bom dia" or "boa tarde" or "boa noite" or "obrigado" or "obrigada" or "oi" or "hello" or "hi";

    /// <summary>Checks whether a question requests one of the supported context areas.</summary>
    /// <param name="query">The normalized query.</param>
    /// <param name="terms">The relevant word stems.</param>
    /// <returns>True when at least one term occurs.</returns>
    private static bool ContainsAny(string query, params string[] terms) => terms.Any(term => query.Contains(term, StringComparison.Ordinal));
}
