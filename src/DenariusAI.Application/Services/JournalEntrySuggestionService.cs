using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>
/// Service responsible for generating journal entry suggestions using AI/LLM integration.
/// </summary>
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
    IApplicationSettingsService settingsService) : IJournalEntrySuggestionService
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
        if (!IsAvailable) throw new InvalidOperationException("A integração Mistral não está configurada.");

        var settings = await settingsService.GetAsync(cancellationToken);
        var accounts = await accountService.ListAsync(true, cancellationToken);
        var categories = await categoryService.ListAsync(activeOnly: true, cancellationToken: cancellationToken);
        var groups = await groupService.ListAsync(true, cancellationToken);
        var budgets = await budgetService.ListPeriodsAsync(cancellationToken);
        var recentSummaries = (await journalEntryService.ListAsync(cancellationToken))
            .Where(item => item.Status == DenariusAI.Domain.Enums.JournalEntryStatus.Active)
            .OrderByDescending(item => item.Date).Take(50).ToList();
        var recentDetails = new List<JournalEntryDetailsDto>(recentSummaries.Count);
        foreach (var summary in recentSummaries)
        {
            var details = await journalEntryService.GetAsync(summary.Id, cancellationToken);
            if (details is not null) recentDetails.Add(details);
        }
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name);
        var groupKinds = groups.ToDictionary(item => item.Id, item => item.Kind.ToString());
        var catalog = JsonSerializer.Serialize(new
        {
            today = DateOnly.FromDateTime(DateTime.Today), currency = "EUR",
            groups = groups.Select(item => new { item.Id, item.Name, type = item.Kind.ToString() }),
            accounts = accounts.Select(item => new { item.Id, item.Name, type = item.AccountType.ToString(), item.CategoryId, item.Currency }),
            categories = categories.Select(item => new { item.Id, item.Name, item.FinancialGroupId, group = groupNames.GetValueOrDefault(item.FinancialGroupId), type = groupKinds.GetValueOrDefault(item.FinancialGroupId) }),
            budgets = budgets.Select(item => new { item.Id, item.Name }),
            recentJournalEntries = recentDetails.Select(entry => new
            {
                entry.Id, entry.Date, entry.Description, entry.Reference, entry.BudgetId, entry.BudgetName,
                lines = entry.Lines.Select(line => new { line.AccountId, line.AccountName, line.CategoryId, line.CategoryName, line.Debit, line.Credit, line.Description })
            })
        });
        var messages = new List<LlmMessageDto>
        {
            new("system", settings.JournalSuggestionSystemPrompt),
            new("system", $"Contexto autorizado para classificação. Usa os catálogos como fonte única de IDs e os movimentos recentes apenas como exemplos de padrões anteriores:\n{catalog}"),
            new("system", "Compara descrição, referência, valor, sentido financeiro, conta usada, categoria, grupo e orçamento com os exemplos recentes. Não copies um exemplo se os dados atuais forem diferentes. Em status complete inclui classificationExplanation com uma justificação curta em português europeu: padrões semelhantes encontrados, critérios usados e motivos para escolher contas, categoria e orçamento. Não reveles raciocínio interno detalhado. O JSON deve incluir classificationExplanation ao nível principal.")
        };
        messages.AddRange(request.History.TakeLast(settings.JournalSuggestionHistoryMessages).Where(item => (item.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(item.Content)).Select(item => new LlmMessageDto(item.Role, item.Content)));
        messages.Add(new("user", userMessage));

        var completion = await llmService.CompleteAsync(messages, cancellationToken);
        var parsed = Parse(completion.Content);
        if (!string.Equals(parsed.Status, "complete", StringComparison.OrdinalIgnoreCase) || parsed.Suggestion is null)
            return new(false, string.IsNullOrWhiteSpace(parsed.Message) ? "Que informação falta para completar o movimento?" : parsed.Message, parsed.ClassificationExplanation, null);

        var validation = Validate(parsed.Suggestion, accounts, categories, budgets);
        if (validation is not null) return new(false, validation, parsed.ClassificationExplanation, null);
        var suggestion = parsed.Suggestion;
        var budgetId = suggestion.BudgetId ?? budgets.FirstOrDefault()?.Id;
        return new(true, string.IsNullOrWhiteSpace(parsed.Message) ? "Sugestão pronta para revisão." : parsed.Message,
            string.IsNullOrWhiteSpace(parsed.ClassificationExplanation) ? "A classificação foi baseada nos catálogos disponíveis e em movimentos recentes semelhantes; confirme a proposta antes de guardar." : parsed.ClassificationExplanation.Trim(),
            new(suggestion.Date!.Value, suggestion.Description!.Trim(), suggestion.Reference, suggestion.Notes, budgetId,
                suggestion.Lines!.Select(line => new SuggestedJournalEntryLineDto(line.AccountId!.Value, line.CategoryId, line.Debit, line.Credit, line.Description)).ToList()));
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
