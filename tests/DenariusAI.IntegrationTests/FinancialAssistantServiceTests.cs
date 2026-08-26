using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;
using DenariusAI.Domain.Enums;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for FinancialAssistantServiceTests.
/// </summary>
public sealed class FinancialAssistantServiceTests
{
    [Fact]
    public async Task AskAsyncGroundsQuestionInStructuredFinancialContext()
    {
        var services = new AssistantServices();
        var assistant = new FinancialAssistantService(services, services, services, services, services, services, services, services);

        var result = await assistant.AskAsync(new("Quanto gastei?", [new("user", "Olá") ]));

        Assert.Equal("Resposta fundamentada", result.Answer);
        Assert.Equal(1, result.TransactionCount);
        Assert.Contains(services.Messages, message => message.Role == "system" && message.Content.Contains("recentTransactions"));
        Assert.Equal("Quanto gastei?", services.Messages.Last().Content);
    }

    [Fact]
    public async Task AskAsyncRejectsUnavailableProviderWithoutCallingIt()
    {
        var services = new AssistantServices { IsConfigured = false };
        var assistant = new FinancialAssistantService(services, services, services, services, services, services, services, services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => assistant.AskAsync(new("Teste", [])));
        Assert.Empty(services.Messages);
    }

    private sealed class AssistantServices : ILLMService, IAccountService, IJournalEntryService, IBudgetService, IReconciliationService, IDashboardService, IAnalyticsService, IApplicationSettingsService
    {
        private static readonly Guid Id = Guid.NewGuid();
        public string Provider => "Teste";
        public string Model => "mistral-small-latest";
        public bool IsConfigured { get; set; } = true;
        public IReadOnlyCollection<LlmMessageDto> Messages { get; private set; } = [];
        public Task<LlmCompletionDto> CompleteAsync(IReadOnlyCollection<LlmMessageDto> messages, CancellationToken cancellationToken = default) { Messages = messages; return Task.FromResult(new LlmCompletionDto("Resposta fundamentada", Model, 10, 5)); }
        Task<ApplicationSettingsDto> IApplicationSettingsService.GetAsync(CancellationToken cancellationToken) => Task.FromResult(new ApplicationSettingsDto(Model, "https://api.mistral.ai/v1/", 1024, .2, "Prompt configurado", 12, 200, 10, "Sugestão", 10, "Prompt de extração", "Prompt de classificação"));
        public Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountDto>>([new(Id, "Banco", null, AccountType.BankAccount, 0m, 100m, "EUR", true, null)]);
        Task<AccountDto?> IAccountService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<JournalEntrySummaryDto>>([new(Id, DateOnly.FromDateTime(DateTime.Today), "Compra", null, 10m, 10m, JournalEntryStatus.Active, ReconciliationStatus.Unreconciled)]);
        Task<JournalEntryDetailsDto?> IJournalEntryService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult(new MonthlySummaryDto(100m, 10m));
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetExecutionItemDto>>([new(Id, "Alimentação", 50m, 10m)]);
        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetPeriodDto>>([]);
        public Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReconciliationItemDto>>([new(Id, DateOnly.FromDateTime(DateTime.Today), "Compra", null, "Banco", 10m, 10m, ReconciliationStatus.Unreconciled, null, null)]);
        public Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult(new DashboardDto(year, month, 100m, 100m, 100m, 10m, 90m, 50m, 10m, 1, [], []));
        public Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default) => Task.FromResult(new AnalyticsDto(100m, 10m, 0m, 0m, 100m, [], [], [], []));
        public Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
