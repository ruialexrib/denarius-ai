using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using DenariusAI.Mcp.Tools;
using Xunit;

namespace DenariusAI.McpTests;

public sealed class McpFinancialToolsTests
{
    private readonly ToolServices _services = new();

    [Fact]
    public async Task AllFinancialToolsReturnStructuredReadOnlyData()
    {
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AccountDto>>(await FinancialTools.GetAccounts(_services, default)));
        Assert.Equal(10m, Assert.IsType<AccountDto>(await FinancialTools.GetAccountBalance(ToolServices.AccountId.ToString(), _services, default)).Balance);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<JournalEntrySummaryDto>>(await FinancialTools.GetTransactions(null, null, 10, _services, default)));
        Assert.Equal(20m, Assert.IsType<MonthlySummaryDto>(await FinancialTools.GetMonthlySummary(2026, 7, _services, default)).Income);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<BudgetExecutionItemDto>>(await FinancialTools.GetBudgetExecution(2026, 7, _services, default)));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AnalyticsBreakdownDto>>(await FinancialTools.GetExpensesByCategory(new(2026, 7, 1), new(2026, 7, 31), _services, default)));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AnalyticsBreakdownDto>>(await FinancialTools.GetExpensesByGroup(new(2026, 7, 1), new(2026, 7, 31), _services, default)));
        Assert.NotNull(await FinancialTools.GetIncomeByPeriod(new(2026, 7, 1), new(2026, 7, 31), _services, default));
        Assert.NotNull(await FinancialTools.GetSavingsRate(new(2026, 7, 1), new(2026, 7, 31), _services, default));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ReconciliationItemDto>>(await FinancialTools.GetUnreconciledTransactions(null, null, _services, default)));
        Assert.Equal(2026, Assert.IsType<DashboardDto>(await FinancialTools.GetFinancialSummary(2026, 7, _services, default)).Year);
        Assert.Equal(10m, Assert.IsType<FinancialReportDataDto>(await FinancialTools.GetFinancialReportData(new(2026, 7, 1), new(2026, 7, 31), _services, default)).Savings);
    }

    [Fact]
    public async Task TransactionToolEnforcesSafeLimit() =>
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => FinancialTools.GetTransactions(null, null, 201, _services, default));

    private sealed class ToolServices : IAccountService, IJournalEntryService, IBudgetService, IAnalyticsService, IReconciliationService, IDashboardService, IFinancialReportDataService
    {
        public static readonly Guid AccountId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountDto>>([Account()]);
        Task<AccountDto?> IAccountService.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<AccountDto?>(id == AccountId ? Account() : null);
        public Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<JournalEntrySummaryDto>>([new(Guid.NewGuid(), new(2026, 7, 1), "Demo", null, 10m, 10m, JournalEntryStatus.Active, ReconciliationStatus.Unreconciled)]);
        public Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult(new MonthlySummaryDto(20m, 10m));
        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetPeriodDto>>([new(Guid.NewGuid(), 2026, 7)]);
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BudgetExecutionItemDto>>([new(CategoryId, "Água", 15m, 10m)]);
        public Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default) => Task.FromResult(new AnalyticsDto(20m, 10m, 0m, 0m, 100m, [new(CategoryId, "Despesas", 10m)], [new(CategoryId, "Água", 10m)], [], []));
        public Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReconciliationItemDto>>([new(Guid.NewGuid(), new(2026, 7, 1), "Demo", null, "Banco", 10m, 10m, ReconciliationStatus.Unreconciled, null, null)]);
        public Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default) => Task.FromResult(new DashboardDto(year, month, 10m, 20m, 30m, 20m, 10m, 15m, 10m, 1, [], []));
        Task<FinancialReportDataDto> IFinancialReportDataService.GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) => Task.FromResult(new FinancialReportDataDto(from, to, "EUR", 20m, 10m, 10m, 50m, 100m, [], [], [], [], [], new(0, 0, 0, [])));
        private static AccountDto Account() => new(AccountId, "Banco", null, AccountType.BankAccount, 0m, 10m, "EUR", true, null);
        public Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task<JournalEntryDetailsDto?> IJournalEntryService.GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
