using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

public sealed class JournalEntrySuggestionService(
    ILLMService llmService,
    IAccountService accountService,
    ICategoryService categoryService,
    IFinancialGroupService groupService,
    IBudgetService budgetService,
    IApplicationSettingsService settingsService) : IJournalEntrySuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public bool IsAvailable => llmService.IsConfigured;

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
        var groupNames = groups.ToDictionary(item => item.Id, item => item.Name);
        var catalog = JsonSerializer.Serialize(new
        {
            today = DateOnly.FromDateTime(DateTime.Today), currency = "EUR",
            accounts = accounts.Select(item => new { item.Id, item.Name, type = item.AccountType.ToString() }),
            categories = categories.Select(item => new { item.Id, item.Name, group = groupNames.GetValueOrDefault(item.FinancialGroupId) }),
            budgets = budgets.Select(item => new { item.Id, item.Name })
        });
        var messages = new List<LlmMessageDto>
        {
            new("system", settings.JournalSuggestionSystemPrompt),
            new("system", $"Catálogo permitido:\n{catalog}")
        };
        messages.AddRange(request.History.TakeLast(settings.JournalSuggestionHistoryMessages).Where(item => (item.Role is "user" or "assistant") && !string.IsNullOrWhiteSpace(item.Content)).Select(item => new LlmMessageDto(item.Role, item.Content)));
        messages.Add(new("user", userMessage));

        var completion = await llmService.CompleteAsync(messages, cancellationToken);
        var parsed = Parse(completion.Content);
        if (!string.Equals(parsed.Status, "complete", StringComparison.OrdinalIgnoreCase) || parsed.Suggestion is null)
            return new(false, string.IsNullOrWhiteSpace(parsed.Message) ? "Que informação falta para completar o movimento?" : parsed.Message, null);

        var validation = Validate(parsed.Suggestion, accounts, categories, budgets);
        if (validation is not null) return new(false, validation, null);
        var suggestion = parsed.Suggestion;
        var budgetId = suggestion.BudgetId ?? budgets.FirstOrDefault()?.Id;
        return new(true, string.IsNullOrWhiteSpace(parsed.Message) ? "Sugestão pronta para revisão." : parsed.Message,
            new(suggestion.Date!.Value, suggestion.Description!.Trim(), suggestion.Reference, suggestion.Notes, budgetId,
                suggestion.Lines!.Select(line => new SuggestedJournalEntryLineDto(line.AccountId!.Value, line.CategoryId, line.Debit, line.Credit, line.Description)).ToList()));
    }

    private static ParsedResponse Parse(string content)
    {
        var json = content.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal)) { var firstLine = json.IndexOf('\n'); var lastFence = json.LastIndexOf("```", StringComparison.Ordinal); if (firstLine >= 0 && lastFence > firstLine) json = json[(firstLine + 1)..lastFence].Trim(); }
        try { return JsonSerializer.Deserialize<ParsedResponse>(json, JsonOptions) ?? new(); }
        catch (JsonException) { return new ParsedResponse { Message = "Não consegui interpretar todos os dados. Pode reformular o movimento?" }; }
    }

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

    private sealed class ParsedResponse { public string? Status { get; set; } public string? Message { get; set; } public ParsedSuggestion? Suggestion { get; set; } }
    private sealed class ParsedSuggestion { public DateOnly? Date { get; set; } public string? Description { get; set; } public string? Reference { get; set; } public string? Notes { get; set; } public Guid? BudgetId { get; set; } public List<ParsedLine>? Lines { get; set; } }
    private sealed class ParsedLine { public Guid? AccountId { get; set; } public Guid? CategoryId { get; set; } public decimal Debit { get; set; } public decimal Credit { get; set; } public string? Description { get; set; } }
}
