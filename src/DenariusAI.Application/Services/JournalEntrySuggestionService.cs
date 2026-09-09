using System.Net;
using System.Text;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace DenariusAI.Application.Services;

/// <summary>
/// Service responsible for generating journal entry suggestions using AI/LLM integration.
/// </summary>
/// <param name="llmService">Provider-neutral LLM service used to generate suggestions.</param>
/// <param name="accountService">Account catalog service.</param>
/// <param name="categoryService">Category catalog service.</param>
/// <param name="groupService">Financial group catalog service.</param>
/// <param name="budgetService">Budget period service.</param>
/// <param name="journalEntryService">Journal entry service used to retrieve relevant examples.</param>
/// <param name="settingsService">Application settings service.</param>
/// <param name="logger">Optional diagnostic logger for model request and response metadata.</param>
/// <remarks>
/// This service processes natural language requests and generates structured journal entry suggestions
/// by leveraging historical data, account catalogs, and LLM capabilities.
/// </remarks>
public sealed class JournalEntrySuggestionService(
    ILLMService llmService,
    IAccountService accountService,
    ICategoryService categoryService,
    IFinancialGroupService groupService,
    IBudgetService budgetService,
    IJournalEntryService journalEntryService,
    IApplicationSettingsService settingsService,
    ILogger<JournalEntrySuggestionService>? logger = null) : IJournalEntrySuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Gets a value indicating whether the LLM service is configured and available.
    /// </summary>
    public bool IsAvailable => llmService.IsConfigured;

    /// <summary>
    /// Generates a journal entry suggestion based on a natural language request.
    /// </summary>
    /// <param name="request">The suggestion request containing the user message and conversation history.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A result containing the suggested journal entry or error messages.</returns>
    /// <exception cref="ArgumentException">Thrown when the message is null, empty, or exceeds 1000 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the LLM service is not configured.</exception>
    public async Task<JournalEntrySuggestionResultDto> SuggestAsync(JournalEntrySuggestionRequestDto request, CancellationToken cancellationToken = default)
    {
        var userMessage = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Length > 1000) throw new ArgumentException("A mensagem deve ter entre 1 e 1000 caracteres.", nameof(request));
        if (!IsAvailable) throw new InvalidOperationException("A integração de IA não está configurada.");

        var settings = await settingsService.GetAsync(cancellationToken);
        var accounts = await accountService.ListAsync(true, cancellationToken);
        var categories = await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        var history = request.History.TakeLast(Math.Min(settings.JournalSuggestionHistoryMessages, 4))
            .Where(item => (item.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(item.Content))
            .Select(item => new LlmMessageDto(item.Role, item.Content)).ToList();
        var query = userMessage + " " + string.Join(" ", history.Where(item => item.Role == "user").Select(item => AiContextBudget.Shorten(item.Content, 1000)));
        var recentSummaries = (await journalEntryService.ListAsync(cancellationToken))
            .Where(item => item.Status == DenariusAI.Domain.Enums.JournalEntryStatus.Active)
            .Where(item => AiContextBudget.Relevance(item.Description, query) > 0)
            .OrderByDescending(item => AiContextBudget.Relevance(item.Description, query)).ThenByDescending(item => item.Date).Take(3).ToList();
        var recentDetails = new List<JournalEntryDetailsDto>(recentSummaries.Count);
        foreach (var summary in recentSummaries)
        {
            var details = await journalEntryService.GetAsync(summary.Id, cancellationToken);
            if (details is not null) recentDetails.Add(details);
        }
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name);
        var groupKinds = groups.ToDictionary(item => item.Id, item => item.Kind.ToString());
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rankedAccounts = accounts.OrderByDescending(item => AiContextBudget.Relevance(item.Name, query))
            .ThenBy(item => item.Name).ToList();
        var exampleCategoryIds = recentDetails.SelectMany(item => item.Lines).Where(item => item.CategoryId.HasValue)
            .Select(item => item.CategoryId!.Value).ToHashSet();
        var rankedCategories = categories.OrderByDescending(item => AiContextBudget.Relevance(item.Name, query))
            .ThenByDescending(item => exampleCategoryIds.Contains(item.Id)).ThenBy(item => item.Name).ToList();
        var rankedBudgets = budgets.OrderByDescending(item => AiContextBudget.Relevance(item.Name, query))
            .ThenByDescending(item => item.Year == today.Year && item.Month == today.Month)
            .ThenByDescending(item => item.Year).ThenByDescending(item => item.Month).ToList();
        var prompt = settings.JournalSuggestionSystemPrompt + "\n\n" + settings.AiContextGuidancePrompt;
        var categoryLimit = 24;
        var accountLimit = 20;
        var budgetLimit = 6;
        var exampleLimit = 3;
        IReadOnlyList<AccountDto> sentAccounts = [];
        IReadOnlyList<CategoryDto> sentCategories = [];
        IReadOnlyList<BudgetPeriodDto> sentBudgets = [];
        LlmCompletionDto? completion = null;
        var previousBytes = int.MaxValue;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            List<LlmMessageDto>? messages;
            var maxBytes = attempt == 0 ? settings.AiMaxInputBytes : settings.AiMaxInputBytes / 2;
            do
            {
                sentAccounts = rankedAccounts.Take(accountLimit).ToList();
                sentCategories = rankedCategories.Take(categoryLimit).ToList();
                sentBudgets = rankedBudgets.Take(budgetLimit).ToList();
                var accountIds = sentAccounts.Select(item => item.Id).ToHashSet();
                var categoryIds = sentCategories.Select(item => item.Id).ToHashSet();
                var examples = recentDetails.Where(entry => entry.Lines.All(line => accountIds.Contains(line.AccountId)
                    && (!line.CategoryId.HasValue || categoryIds.Contains(line.CategoryId.Value)))).Take(exampleLimit).ToList();
                var catalog = AiContextBudget.Serialize(new
                {
                    today,
                    currency = "EUR",
                    partial = new
                    {
                        accounts = accounts.Count > sentAccounts.Count,
                        categories = categories.Count > sentCategories.Count,
                        budgets = budgets.Count > sentBudgets.Count,
                        examples = true
                    },
                    accounts = sentAccounts.Select(item => new
                    {
                        item.Id,
                        name = AiContextBudget.Shorten(item.Name, 100),
                        type = item.AccountType.ToString(),
                        categoryId = item.CategoryId.HasValue && categoryIds.Contains(item.CategoryId.Value) ? item.CategoryId : null,
                        item.Currency
                    }),
                    categories = sentCategories.Select(item => new
                    {
                        item.Id,
                        name = AiContextBudget.Shorten(item.Name, 100),
                        group = AiContextBudget.Shorten(groupNames.GetValueOrDefault(item.FinancialGroupId) ?? "", 80),
                        type = groupKinds.GetValueOrDefault(item.FinancialGroupId)
                    }),
                    budgets = sentBudgets.Select(item => new { item.Id, item.Year, item.Month }),
                    recentJournalEntries = examples.Select(entry => new
                    {
                        entry.Date,
                        description = AiContextBudget.Shorten(entry.Description, 120),
                        lines = entry.Lines.Select(line => new { line.AccountId, line.CategoryId, line.Debit, line.Credit })
                    })
                });
                messages = AiContextBudget.Build(prompt, "CATALOG_JSON:\n" + catalog, attempt == 0 ? history : [], userMessage, maxBytes);
                if (messages is not null || accountLimit <= 2 && categoryLimit == 0 && budgetLimit == 0 && exampleLimit == 0) break;
                exampleLimit = 0;
                categoryLimit /= 2;
                budgetLimit /= 2;
                accountLimit = Math.Max(2, accountLimit / 2);
            } while (true);
            if (messages is null)
                return new(false, "O contexto excede o limite do pedido. Reduza os prompts nas Definições ou ajuste o limite de contexto para o fornecedor utilizado.", null, null);
            var requestBytes = AiContextBudget.Measure(messages);
            if (attempt > 0 && requestBytes >= previousBytes)
                return new(false, "O fornecedor recusou o pedido por exceder o limite de contexto. Reduza os prompts nas Definições e tente novamente.", null, null);
            previousBytes = requestBytes;

            LogModelRequest(messages, requestBytes, attempt + 1, sentAccounts.Count, sentCategories.Count, sentBudgets.Count, recentDetails.Count);
            try
            {
                completion = await llmService.CompleteAsync(messages, Math.Min(settings.AiMaxTokens, 1024), cancellationToken);
                LogModelResponse(completion, attempt + 1);
                break;
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                logger?.LogWarning(
                    "Journal entry AI request was rejected as too large. Provider: {Provider}; Model: {Model}; Attempt: {Attempt}; RequestBytes: {RequestBytes}.",
                    llmService.Provider,
                    llmService.Model,
                    attempt + 1,
                    requestBytes);
                if (attempt == 1)
                    return new(false, "O fornecedor recusou o pedido por exceder o limite de contexto. Reduza os prompts ou o limite de contexto nas Definições e tente novamente.", null, null);
                exampleLimit = 0;
                categoryLimit /= 2;
                budgetLimit /= 2;
            }
        }
        if (completion is null) return new(false, "Não foi possível preparar a sugestão. Tente novamente.", null, null);
        var parsed = Parse(completion.Content);
        if (!string.Equals(parsed.Status, "complete", StringComparison.OrdinalIgnoreCase) || parsed.Suggestion is null)
            return new(false, string.IsNullOrWhiteSpace(parsed.Message) ? "Que informação falta para completar o movimento?" : parsed.Message, parsed.ClassificationExplanation, null);

        var validation = Validate(parsed.Suggestion, sentAccounts, sentCategories, sentBudgets);
        if (validation is not null) return new(false, validation, parsed.ClassificationExplanation, null);
        var suggestion = parsed.Suggestion;
        var budgetId = suggestion.BudgetId ?? sentBudgets.FirstOrDefault(item => item.Year == suggestion.Date!.Value.Year && item.Month == suggestion.Date.Value.Month)?.Id;
        return new(true, string.IsNullOrWhiteSpace(parsed.Message) ? "Sugestão pronta para revisão." : parsed.Message,
            string.IsNullOrWhiteSpace(parsed.ClassificationExplanation) ? "A classificação foi baseada nos catálogos disponíveis e em movimentos recentes semelhantes; confirme a proposta antes de guardar." : parsed.ClassificationExplanation.Trim(),
            new(suggestion.Date!.Value, suggestion.Description!.Trim(), suggestion.Reference, suggestion.Notes, budgetId,
                suggestion.Lines!.Select(line => new SuggestedJournalEntryLineDto(line.AccountId!.Value, line.CategoryId, line.Debit, line.Credit, line.Description)).ToList()));
    }

    /// <summary>
    /// Writes a safe diagnostic description of the request sent to the configured model.
    /// </summary>
    /// <param name="messages">Messages passed to the provider-neutral LLM service.</param>
    /// <param name="requestBytes">Serialized request message size in UTF-8 bytes.</param>
    /// <param name="attempt">One-based provider request attempt.</param>
    /// <param name="accountCount">Number of account catalog entries included.</param>
    /// <param name="categoryCount">Number of category catalog entries included.</param>
    /// <param name="budgetCount">Number of budget periods included.</param>
    /// <param name="recentExampleCount">Number of relevant journal entry examples considered for the request.</param>
    private void LogModelRequest(
        IReadOnlyCollection<LlmMessageDto> messages,
        int requestBytes,
        int attempt,
        int accountCount,
        int categoryCount,
        int budgetCount,
        int recentExampleCount)
    {
        if (logger is null) return;

        var messageDiagnostics = messages.Select((message, index) => new
        {
            index,
            message.Role,
            utf8Bytes = Encoding.UTF8.GetByteCount(message.Content),
            content = message.Role == "system"
                ? "[SYSTEM PROMPT REDACTED]"
                : message.Content.StartsWith("CATALOG_JSON:", StringComparison.Ordinal)
                    ? "[FINANCIAL CATALOG REDACTED]"
                    : index == messages.Count - 1
                        ? "[CURRENT USER MESSAGE REDACTED]"
                        : "[CONVERSATION HISTORY REDACTED]"
        });

        logger.LogInformation(
            "Journal entry AI request. Provider: {Provider}; Model: {Model}; Attempt: {Attempt}; RequestBytes: {RequestBytes}; MaxTokens: {MaxTokens}; Accounts: {AccountCount}; Categories: {CategoryCount}; Budgets: {BudgetCount}; RelevantExamples: {RecentExampleCount}; Messages: {Messages}.",
            llmService.Provider,
            llmService.Model,
            attempt,
            requestBytes,
            1024,
            accountCount,
            categoryCount,
            budgetCount,
            recentExampleCount,
            JsonSerializer.Serialize(messageDiagnostics));
    }

    /// <summary>
    /// Writes a safe diagnostic description of the raw model response without logging financial content.
    /// </summary>
    /// <param name="completion">Raw completion returned by the provider-neutral LLM service.</param>
    /// <param name="attempt">One-based provider request attempt.</param>
    private void LogModelResponse(LlmCompletionDto completion, int attempt)
    {
        if (logger is null) return;

        logger.LogInformation(
            "Journal entry AI response. Provider: {Provider}; Model: {Model}; Attempt: {Attempt}; ResponseBytes: {ResponseBytes}; PromptTokens: {PromptTokens}; CompletionTokens: {CompletionTokens}; Structure: {Structure}.",
            llmService.Provider,
            completion.Model,
            attempt,
            Encoding.UTF8.GetByteCount(completion.Content),
            completion.PromptTokens,
            completion.CompletionTokens,
            DescribeResponseStructure(completion.Content));
    }

    /// <summary>
    /// Describes the JSON shape returned by the model while redacting values that may contain financial data.
    /// </summary>
    /// <param name="content">Raw model response content.</param>
    /// <returns>A compact JSON diagnostic containing only structural metadata.</returns>
    private static string DescribeResponseStructure(string content)
    {
        var json = content.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine >= 0 && lastFence > firstLine) json = json[(firstLine + 1)..lastFence].Trim();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return JsonSerializer.Serialize(new { validJson = true, rootKind = document.RootElement.ValueKind.ToString() });

            var root = document.RootElement;
            var status = root.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.String
                ? statusElement.GetString()
                : null;
            var suggestionProperties = Array.Empty<string>();
            var lineCount = 0;
            if (root.TryGetProperty("suggestion", out var suggestionElement) && suggestionElement.ValueKind == JsonValueKind.Object)
            {
                suggestionProperties = suggestionElement.EnumerateObject().Select(property => property.Name).ToArray();
                if (suggestionElement.TryGetProperty("lines", out var linesElement) && linesElement.ValueKind == JsonValueKind.Array)
                    lineCount = linesElement.GetArrayLength();
            }

            return JsonSerializer.Serialize(new
            {
                validJson = true,
                status,
                rootProperties = root.EnumerateObject().Select(property => property.Name).ToArray(),
                suggestionProperties,
                lineCount,
                values = "[REDACTED]"
            });
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { validJson = false, values = "[REDACTED]" });
        }
    }

    /// <summary>
    /// Parses the LLM response content into a structured format.
    /// </summary>
    /// <param name="content">The raw content from the LLM response.</param>
    /// <returns>A parsed response object containing status, message, explanation, and suggestion data.</returns>
    private static ParsedResponse Parse(string content)
    {
        var json = content.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal)) { var firstLine = json.IndexOf('\n'); var lastFence = json.LastIndexOf("```", StringComparison.Ordinal); if (firstLine >= 0 && lastFence > firstLine) json = json[(firstLine + 1)..lastFence].Trim(); }
        try { return JsonSerializer.Deserialize<ParsedResponse>(json, JsonOptions) ?? new(); }
        catch (JsonException) { return new ParsedResponse { Message = "Não consegui interpretar todos os dados. Pode reformular o movimento?" }; }
    }

    /// <summary>
    /// Validates the suggested journal entry against existing catalogs and business rules.
    /// </summary>
    /// <param name="suggestion">The parsed suggestion to validate.</param>
    /// <param name="accounts">The collection of available accounts.</param>
    /// <param name="categories">The collection of available categories.</param>
    /// <param name="budgets">The collection of available budget periods.</param>
    /// <returns>An error message if validation fails; otherwise, null.</returns>
    private static string? Validate(ParsedSuggestion suggestion, IReadOnlyCollection<AccountDto> accounts, IReadOnlyCollection<CategoryDto> categories, IReadOnlyCollection<BudgetPeriodDto> budgets)
    {
        if (!suggestion.Date.HasValue || string.IsNullOrWhiteSpace(suggestion.Description)) return "Qual é a data e a descrição do movimento?";
        if (suggestion.Lines is null || suggestion.Lines.Count < 2) return "Que contas devo utilizar como origem e destino?";
        var accountIds = accounts.Select(item => item.Id).ToHashSet(); var categoryIds = categories.Select(item => item.Id).ToHashSet(); var budgetIds = budgets.Select(item => item.Id).ToHashSet();
        if (suggestion.Lines.Any(line => !line.AccountId.HasValue || !accountIds.Contains(line.AccountId.Value))) return "Não consegui identificar uma das contas. Qual conta pretende utilizar?";
        if (suggestion.Lines.Select(line => line.AccountId!.Value).Distinct().Count() < 2) return "O movimento precisa de duas contas diferentes. Qual é a outra conta?";
        if (suggestion.Lines.Any(line => line.CategoryId.HasValue && !categoryIds.Contains(line.CategoryId.Value))) return "Não consegui identificar a categoria. Pode indicar qual pretende?";
        if (suggestion.BudgetId.HasValue && !budgetIds.Contains(suggestion.BudgetId.Value)) return "A que orçamento pretende associar este movimento?";
        if (suggestion.Lines.Any(line => line.Debit < 0 || line.Credit < 0 || (line.Debit == 0) == (line.Credit == 0))) return "Qual é o valor do movimento?";
        if (suggestion.Lines.Sum(line => line.Debit) != suggestion.Lines.Sum(line => line.Credit)) return "O valor indicado não permitiu equilibrar o movimento. Pode confirmá-lo?";
        return null;
    }

    /// <summary>
    /// Represents the parsed response from the LLM containing status and suggestion data.
    /// </summary>
    private sealed class ParsedResponse { public string? Status { get; set; } public string? Message { get; set; } public string? ClassificationExplanation { get; set; } public ParsedSuggestion? Suggestion { get; set; } }

    /// <summary>
    /// Represents a parsed journal entry suggestion with all required fields.
    /// </summary>
    private sealed class ParsedSuggestion { public DateOnly? Date { get; set; } public string? Description { get; set; } public string? Reference { get; set; } public string? Notes { get; set; } public Guid? BudgetId { get; set; } public List<ParsedLine>? Lines { get; set; } }

    /// <summary>
    /// Represents a parsed journal entry line with account, category, and amount information.
    /// </summary>
    private sealed class ParsedLine { public Guid? AccountId { get; set; } public Guid? CategoryId { get; set; } public decimal Debit { get; set; } public decimal Credit { get; set; } public string? Description { get; set; } }
}
