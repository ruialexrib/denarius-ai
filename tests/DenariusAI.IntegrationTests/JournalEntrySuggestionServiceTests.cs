using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Enums;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains tests for the JournalEntrySuggestionService type.
/// </summary>
public sealed class JournalEntrySuggestionServiceTests
{
    [Fact]
    public async Task CompleteSuggestionUsesOnlyCatalogIdsAndBalancedLines()
    {
        var data = new SuggestionData();
        data.Response = $$$"""{"status":"complete","message":"Sugestão pronta.","suggestion":{"date":"2026-08-24","description":"Eletricidade","reference":null,"notes":null,"budgetId":null,"lines":[{"accountId":"{{{SuggestionData.ExpenseAccountId}}}","categoryId":"{{{SuggestionData.CategoryId}}}","debit":42,"credit":0,"description":"Eletricidade"},{"accountId":"{{{SuggestionData.BankAccountId}}}","categoryId":null,"debit":0,"credit":42,"description":"Pagamento"}]}}""";
        var service = new JournalEntrySuggestionService(data, data, data, data, data, data, data);

        var result = await service.SuggestAsync(new("Paguei 42 euros de eletricidade", []));

        Assert.True(result.IsComplete);
        Assert.Equal(42m, result.Suggestion!.Lines.Sum(line => line.Debit));
        Assert.Equal(42m, result.Suggestion.Lines.Sum(line => line.Credit));
        Assert.Equal(SuggestionData.BudgetId, result.Suggestion.BudgetId);
    }

    [Fact]
    public async Task InvalidSuggestedAccountRequestsClarificationWithoutApplyingData()
    {
        var data = new SuggestionData { Response = $$$"""{"status":"complete","message":"Pronto","suggestion":{"date":"2026-08-24","description":"Compra","lines":[{"accountId":"{{{Guid.NewGuid()}}}","debit":10,"credit":0},{"accountId":"{{{SuggestionData.BankAccountId}}}","debit":0,"credit":10}]}}""" };
        var service = new JournalEntrySuggestionService(data, data, data, data, data, data, data);
        var result = await service.SuggestAsync(new("Uma compra", []));
        Assert.False(result.IsComplete);
        Assert.Null(result.Suggestion);
        Assert.Contains("conta", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents the SuggestionData type.
    /// </summary>
    private sealed class SuggestionData : ILLMService, IAccountService, ICategoryService, IFinancialGroupService, IBudgetService, IApplicationSettingsService, IJournalEntryService
    {
        public static readonly Guid BankAccountId = Guid.NewGuid(); public static readonly Guid ExpenseAccountId = Guid.NewGuid(); public static readonly Guid CategoryId = Guid.NewGuid(); public static readonly Guid GroupId = Guid.NewGuid(); public static readonly Guid BudgetId = Guid.NewGuid();
        public string Response { get; set; } = "{}"; public string Provider => "Teste"; public string Model => "mistral-small-latest"; public bool IsConfigured => true;
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default) => Task.FromResult(new LlmCompletionDto(Response, Model, null, null));
        Task<ApplicationSettingsDto> IApplicationSettingsService.GetAsync(CancellationToken cancellationToken) => Task.FromResult(new ApplicationSettingsDto(Model, "https://api.mistral.ai/v1/", 1024, .2, "Assistant", 12, 200, 10, DenariusAI.Application.Configuration.ApplicationSettingsDefaults.JournalSuggestionPrompt, 10, "Prompt de extração", "Prompt de classificação"));
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountDto>>([new(BankAccountId, "Conta principal", null, AccountType.BankAccount, 0, 100, "EUR", true, null), new(ExpenseAccountId, "Despesas", null, AccountType.Cash, 0, 0, "EUR", true, CategoryId)]);
        Task<AccountDto?> IAccountService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoryDto>>([new(CategoryId, GroupId, "Eletricidade", null, true, 1)]);
        Task<CategoryDto?> ICategoryService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<IReadOnlyList<FinancialGroupDto>> IFinancialGroupService.ListAsync(bool activeOnly, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FinancialGroupDto>>([new(GroupId, "Despesas Correntes", null, FinancialGroupKind.Expense, true, 1)]);
        Task<FinancialGroupDto?> IFinancialGroupService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<IReadOnlyList<JournalEntrySummaryDto>> IJournalEntryService.ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JournalEntrySummaryDto>>([]);
        Task<JournalEntryDetailsDto?> IJournalEntryService.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<JournalEntryDetailsDto?>(null);
        Task<JournalEntryResultDto> IJournalEntryService.CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task IJournalEntryService.UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task IJournalEntryService.CancelAsync(Guid id, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<MonthlySummaryDto> IJournalEntryService.GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken) => throw new NotSupportedException();        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetPeriodDto>>([new(BudgetId, 2026, 8)]);
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveCategoryDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
