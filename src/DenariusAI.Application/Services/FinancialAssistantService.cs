using System.Text.Json;
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
        var accounts = await accountService.ListAsync(cancellationToken: cancellationToken);
        var allTransactions = await journalEntryService.ListAsync(cancellationToken);
        var transactions = allTransactions.Where(item => item.Status == JournalEntryStatus.Active && item.Date >= dataFrom && item.Date <= today).OrderByDescending(item => item.Date).Take(settings.AssistantMaxTransactions).ToList();
        var dashboard = await dashboardService.GetAsync(today.Year, today.Month, cancellationToken);
        var budget = await budgetService.GetExecutionAsync(today.Year, today.Month, cancellationToken);
        var analytics = await analyticsService.GetAsync(new(new DateOnly(today.Year, 1, 1), today), cancellationToken);
        var unreconciled = await reconciliationService.ListAsync(from: dataFrom, to: today, status: ReconciliationStatus.Unreconciled, cancellationToken: cancellationToken);
        var savingsCertificates = savingsRepository is null ? [] : await savingsRepository.ListAsync(cancellationToken);

        var context = JsonSerializer.Serialize(new
        {
            generatedAt = today,
            currency = "EUR",
            period = new { from = dataFrom, to = today },
            accounts,
            currentMonth = dashboard,
            currentBudget = budget,
            currentYearAnalytics = analytics,
            recentTransactions = transactions,
            unreconciledTransactions = unreconciled
            , savingsCertificates
        });

        var messages = new List<LlmMessageDto>
        {
            new("system", settings.AssistantSystemPrompt),
            new("user", $"FINANCIAL_CONTEXT_JSON:\n{context}")
        };
        messages.AddRange(request.History.TakeLast(settings.AssistantHistoryMessages)
            .Where(item => item.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(item.Content))
            .Select(item => new LlmMessageDto(item.Role, item.Content.Length > 2000 ? item.Content[..2000] : item.Content)));
        messages.Add(new("user", question));

        var completion = await llmService.CompleteAsync(messages, cancellationToken);
        return new(completion.Content, completion.Model, dataFrom, today, transactions.Count);
    }
}
