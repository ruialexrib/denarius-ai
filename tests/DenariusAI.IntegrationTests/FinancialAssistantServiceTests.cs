using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Enums;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies bounded financial context and safe provider failures.</summary>
public sealed class FinancialAssistantServiceTests
{
    /// <summary>Verifies configured instructions and structured financial data ground the response.</summary>
    [Fact]
    public async Task AskAsyncGroundsQuestionInStructuredFinancialContext()
    {
        var services = new AssistantServices();
        var assistant = new FinancialAssistantService(services, services, services, services, services, services, services, services);

        var result = await assistant.AskAsync(new("Quanto gastei?", [new("user", "Olá") ]));

        Assert.Equal("Resposta fundamentada", result.Answer);
        Assert.Equal(1, result.TransactionCount);
        Assert.Contains(services.Messages, message => message.Role == "system" && message.Content.StartsWith("Prompt configurado"));
        Assert.Contains(services.Messages, message => message.Role == "user" && message.Content.StartsWith("FINANCIAL_CONTEXT_JSON:") && message.Content.Contains("recentTransactions"));
        Assert.Equal("Quanto gastei?", services.Messages.Last().Content);
    }

    /// <summary>Verifies an unavailable provider is never called.</summary>
    [Fact]
    public async Task AskAsyncRejectsUnavailableProviderWithoutCallingIt()
    {
        var services = new AssistantServices { IsConfigured = false };
        var assistant = new FinancialAssistantService(services, services, services, services, services, services, services, services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => assistant.AskAsync(new("Teste", [])));
        Assert.Empty(services.Messages);
    }

    /// <summary>Verifies a greeting performs no financial queries and sends no previous financial history.</summary>
    [Fact]
    public async Task GreetingDoesNotLoadFinancialData()
    {
        var data = new AssistantServices();
        var service = new FinancialAssistantService(data, data, data, data, data, data, data, data);
        var result = await service.AskAsync(new("Olá!", [new("assistant", "private financial history")]));
        Assert.Equal(0, data.DataCalls);
        Assert.Equal(0, result.TransactionCount);
        Assert.Equal(2, data.Messages.Count);
        Assert.DoesNotContain(data.Messages, item => item.Content.Contains("private financial history"));
    }

    /// <summary>Verifies large collections and history fit the complete message budget.</summary>
    [Fact]
    public async Task LargeContextIsBoundedAndClearlyPartial()
    {
        var data = new AssistantServices { TransactionVolume = 500 };
        var service = new FinancialAssistantService(data, data, data, data, data, data, data, data);
        var result = await service.AskAsync(new("Que movimentos paguei?", Enumerable.Range(0, 40)
            .Select(_ => new AssistantChatMessageDto("user", new string('x', 10000))).ToList()));
        Assert.InRange(AiContextBudget.Measure(data.Messages), 1, 12000);
        Assert.InRange(result.TransactionCount, 1, 12);
        var context = data.Messages.Single(item => item.Content.StartsWith("FINANCIAL_CONTEXT_JSON:")).Content;
        Assert.Contains("\"available\":500", context);
        Assert.Contains("\"partial\":true", context);
        Assert.DoesNotContain("savingsCertificates", context);
        Assert.DoesNotContain("currentYearAnalytics", context);
        Assert.Equal(2, data.DataCalls);
    }

    /// <summary>Verifies a 413 causes only one smaller retry.</summary>
    [Fact]
    public async Task RetriesOversizedRequestOnceWithLessContext()
    {
        var data = new AssistantServices { TransactionVolume = 500, OversizedResponses = 1 };
        var service = new FinancialAssistantService(data, data, data, data, data, data, data, data);
        await service.AskAsync(new("Que movimentos paguei?", [new("user", new string('x', 1000))]));
        Assert.Equal(2, data.Requests.Count);
        Assert.True(AiContextBudget.Measure(data.Requests[1]) < AiContextBudget.Measure(data.Requests[0]));
        Assert.True(AiContextBudget.Measure(data.Requests[1]) <= 6000);
    }

    /// <summary>Verifies persistent 413 errors return useful feedback without an unbounded retry loop.</summary>
    [Fact]
    public async Task PersistentOversizeReturnsActionableFeedback()
    {
        var data = new AssistantServices { OversizedResponses = 10 };
        var service = new FinancialAssistantService(data, data, data, data, data, data, data, data);
        var result = await service.AskAsync(new("Olá", []));
        Assert.Single(data.Requests);
        Assert.Contains("limite de contexto", result.Answer);
    }

    /// <summary>Verifies oversized mandatory instructions are never silently cut or transmitted.</summary>
    [Fact]
    public async Task OversizedPromptFailsBeforeCallingProvider()
    {
        var data = new AssistantServices { Prompt = new string('x', 13000) };
        var service = new FinancialAssistantService(data, data, data, data, data, data, data, data);
        var result = await service.AskAsync(new("Olá", []));
        Assert.Empty(data.Requests);
        Assert.Contains("prompts", result.Answer);
    }

    /// <summary>Supplies deterministic financial data and records outgoing model requests.</summary>
    private sealed class AssistantServices : ILLMService, IAccountService, IJournalEntryService, IBudgetService, IReconciliationService, IDashboardService, IAnalyticsService, IApplicationSettingsService
    {
        private static readonly Guid Id = Guid.NewGuid();
        public string Provider => "Teste";
        public string Model => "mistral-small-latest";
        public bool IsConfigured { get; set; } = true;
        public IReadOnlyCollection<LlmMessageDto> Messages { get; private set; } = [];
        /// <summary>Gets or sets the number of simulated oversized responses.</summary>
        public int OversizedResponses { get; set; }
        /// <summary>Gets the outgoing requests.</summary>
        public List<IReadOnlyCollection<LlmMessageDto>> Requests { get; } = [];
        /// <summary>Gets or sets the transaction volume.</summary>
        public int TransactionVolume { get; set; } = 1;
        /// <summary>Gets the number of financial data queries.</summary>
        public int DataCalls { get; private set; }
        /// <summary>Gets or sets the assistant prompt.</summary>
        public string Prompt { get; set; } = "Prompt configurado";
        /// <inheritdoc />
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Messages = messages; Requests.Add(messages);
            if (Requests.Count <= OversizedResponses) throw new HttpRequestException("Too large", null, System.Net.HttpStatusCode.RequestEntityTooLarge);
            return Task.FromResult(new LlmCompletionDto("Resposta fundamentada", Model, 10, 5));
        }
        /// <summary>Records a financial data access.</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="value">The deterministic result.</param>
        /// <returns>The supplied result.</returns>
        private Task<T> Read<T>(T value) { DataCalls++; return Task.FromResult(value); }
        /// <inheritdoc />
        Task<ApplicationSettingsDto> IApplicationSettingsService.GetAsync(CancellationToken cancellationToken) => Task.FromResult(new ApplicationSettingsDto(Model, "https://api.mistral.ai/v1/", 1024, .2, Prompt, 12, 200, 10, "Sugestão", 10, "Prompt de extração", "Prompt de classificação"));
        /// <inheritdoc />
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) => Read<IReadOnlyList<AccountDto>>([new(Id, "Banco", null, AccountType.BankAccount, 0m, 100m, "EUR", true, null)]);
        /// <inheritdoc />
        Task<AccountDto?> IAccountService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<JournalEntrySummaryDto>>(Enumerable.Range(0, TransactionVolume).Select(index => new JournalEntrySummaryDto(Id, DateOnly.FromDateTime(DateTime.Today), "Compra " + index + new string('x', 2000), null, 10m, 10m, JournalEntryStatus.Active, ReconciliationStatus.Unreconciled)).ToList());
        /// <inheritdoc />
        Task<JournalEntryDetailsDto?> IJournalEntryService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default) => Read(new MonthlySummaryDto(100m, 10m));
        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default) => Read<IReadOnlyList<BudgetExecutionItemDto>>([new(Id, "Alimentação", 50m, 10m)]);
        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<BudgetPeriodDto>>([]);
        /// <inheritdoc />
        public Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default) => Read<IReadOnlyList<ReconciliationItemDto>>([new(Id, DateOnly.FromDateTime(DateTime.Today), "Compra", null, "Banco", 10m, 10m, ReconciliationStatus.Unreconciled, null, null)]);
        /// <inheritdoc />
        public Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default) => Read(new DashboardDto(year, month, 100m, 100m, 100m, 10m, 90m, 50m, 10m, 1, [], []));
        /// <inheritdoc />
        public Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default) => Read(new AnalyticsDto(100m, 10m, 0m, 0m, 100m, [], [], [], []));
        /// <inheritdoc />
        public Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <inheritdoc />
        public Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
