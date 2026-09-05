using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Enums;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies bounded suggestion catalogs and accounting validation.</summary>
public sealed class JournalEntrySuggestionServiceTests
{
    /// <summary>Verifies suggestions preserve valid catalog IDs and balanced entries.</summary>
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

    /// <summary>Verifies unknown account IDs require clarification.</summary>
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

    /// <summary>Verifies a large catalog and conversation remain bounded and only three similar examples are read.</summary>
    [Fact]
    public async Task LargeCatalogUsesFewRelevantExamplesAndBoundedHistory()
    {
        var data = new SuggestionData
        {
            ExtraCategories = Enumerable.Range(0, 200).Select(index => new CategoryDto(Guid.NewGuid(), SuggestionData.GroupId,
                "Categoria " + index + new string('x', 150), null, true, index)).ToList(),
            Summaries = Enumerable.Range(0, 100).Select(_ => new JournalEntrySummaryDto(Guid.NewGuid(), new DateOnly(2026, 8, 24),
                "Eletricidade", null, 42, 42, JournalEntryStatus.Active, ReconciliationStatus.Unreconciled)).ToList()
        };
        var service = new JournalEntrySuggestionService(data, data, data, data, data, data, data);
        await service.SuggestAsync(new("Paguei eletricidade", Enumerable.Range(0, 50)
            .Select(_ => new JournalEntrySuggestionMessageDto("user", new string('x', 10000))).ToList()));
        Assert.Equal(3, data.DetailReads);
        Assert.True(AiContextBudget.Measure(data.Requests.Single()) <= 12000);
        var catalog = data.Requests.Single().Single(item => item.Content.StartsWith("CATALOG_JSON:")).Content;
        Assert.Contains(SuggestionData.CategoryId.ToString(), catalog);
        Assert.Contains("\"categories\":true", catalog);
    }

    /// <summary>Verifies a valid database ID omitted from the sent catalog cannot be accepted from model output.</summary>
    [Fact]
    public async Task RejectsCategoryOmittedFromSentCatalog()
    {
        var omitted = Guid.NewGuid();
        var data = new SuggestionData
        {
            ExtraCategories = Enumerable.Range(0, 100).Select(index => new CategoryDto(index == 99 ? omitted : Guid.NewGuid(),
                SuggestionData.GroupId, "Z" + index.ToString("D3"), null, true, index)).ToList(),
            Response = $$$"""{"status":"complete","suggestion":{"date":"2026-08-24","description":"Compra","lines":[{"accountId":"{{{SuggestionData.ExpenseAccountId}}}","categoryId":"{{{omitted}}}","debit":42,"credit":0},{"accountId":"{{{SuggestionData.BankAccountId}}}","debit":0,"credit":42}]}}"""
        };
        var service = new JournalEntrySuggestionService(data, data, data, data, data, data, data);
        var result = await service.SuggestAsync(new("Paguei eletricidade", []));
        Assert.False(result.IsComplete);
        Assert.Null(result.Suggestion);
        Assert.DoesNotContain(data.Requests.Single(), item => item.Content.Contains(omitted.ToString()));
    }

    /// <summary>Verifies repeated 413 errors stop after at most one reduced retry and never produce a proposal.</summary>
    [Fact]
    public async Task OversizedSuggestionsStopSafely()
    {
        var data = new SuggestionData { OversizedResponses = 10 };
        var service = new JournalEntrySuggestionService(data, data, data, data, data, data, data);
        var result = await service.SuggestAsync(new("Paguei eletricidade", [new("user", new string('x', 1000))]));
        Assert.InRange(data.Requests.Count, 1, 2);
        if (data.Requests.Count == 2)
            Assert.True(AiContextBudget.Measure(data.Requests[1]) < AiContextBudget.Measure(data.Requests[0]));
        Assert.False(result.IsComplete);
        Assert.Null(result.Suggestion);
        Assert.Contains("contexto", result.Message);
    }

    /// <summary>Supplies deterministic catalogs and captures model calls.</summary>
    private sealed class SuggestionData : ILLMService, IAccountService, ICategoryService, IFinancialGroupService, IBudgetService, IApplicationSettingsService, IJournalEntryService
    {
        public static readonly Guid BankAccountId = Guid.NewGuid(); public static readonly Guid ExpenseAccountId = Guid.NewGuid(); public static readonly Guid CategoryId = Guid.NewGuid(); public static readonly Guid GroupId = Guid.NewGuid(); public static readonly Guid BudgetId = Guid.NewGuid();
        public string Response { get; set; } = "{}"; public string Provider => "Teste"; public string Model => "mistral-small-latest"; public bool IsConfigured => true;
        /// <summary>Gets or sets how many oversized responses to simulate.</summary>
        public int OversizedResponses { get; set; }
        /// <summary>Gets recorded model requests.</summary>
        public List<IReadOnlyCollection<LlmMessageDto>> Requests { get; } = [];
        /// <summary>Gets or sets additional category rows.</summary>
        public List<CategoryDto> ExtraCategories { get; set; } = [];
        /// <summary>Gets or sets recent movement summaries.</summary>
        public List<JournalEntrySummaryDto> Summaries { get; set; } = [];
        /// <summary>Gets the number of detail reads.</summary>
        public int DetailReads { get; private set; }
        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(messages);
            if (Requests.Count <= OversizedResponses) throw new HttpRequestException("Too large", null, System.Net.HttpStatusCode.RequestEntityTooLarge);
            return Task.FromResult(new LlmCompletionDto(Response, Model, null, null));
        }
        /// <inheritdoc />
        Task<ApplicationSettingsDto> IApplicationSettingsService.GetAsync(CancellationToken cancellationToken) => Task.FromResult(new ApplicationSettingsDto(Model, "https://api.mistral.ai/v1/", 1024, .2, "Assistant", 12, 200, 10, DenariusAI.Application.Configuration.ApplicationSettingsDefaults.JournalSuggestionPrompt, 10, "Prompt de extração", "Prompt de classificação"));
        /// <inheritdoc />
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountDto>>([new(BankAccountId, "Conta principal", null, AccountType.BankAccount, 0, 100, "EUR", true, null), new(ExpenseAccountId, "Despesas", null, AccountType.Cash, 0, 0, "EUR", true, CategoryId)]);
        /// <inheritdoc />
        Task<AccountDto?> IAccountService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoryDto>>([new(CategoryId, GroupId, "Eletricidade", null, true, 1), .. ExtraCategories]);
        /// <inheritdoc />
        Task<CategoryDto?> ICategoryService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        Task<IReadOnlyList<FinancialGroupDto>> IFinancialGroupService.ListAsync(bool activeOnly, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FinancialGroupDto>>([new(GroupId, "Despesas Correntes", null, FinancialGroupKind.Expense, true, 1)]);
        /// <inheritdoc />
        Task<FinancialGroupDto?> IFinancialGroupService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        Task<IReadOnlyList<JournalEntrySummaryDto>> IJournalEntryService.ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JournalEntrySummaryDto>>(Summaries);
        /// <inheritdoc />
        Task<JournalEntryDetailsDto?> IJournalEntryService.GetAsync(Guid id, CancellationToken cancellationToken) { DetailReads++; return Task.FromResult<JournalEntryDetailsDto?>(null); }
        /// <inheritdoc />
        Task<JournalEntryResultDto> IJournalEntryService.CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        Task IJournalEntryService.UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        Task IJournalEntryService.CancelAsync(Guid id, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        Task<MonthlySummaryDto> IJournalEntryService.GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken) => throw new NotSupportedException();        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetPeriodDto>>([new(BudgetId, 2026, 8)]);
        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveCategoryDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task UpdateAsync(Guid id, SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
