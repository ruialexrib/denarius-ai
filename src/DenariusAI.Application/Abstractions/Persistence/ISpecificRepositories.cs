using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;

namespace DenariusAI.Application.Abstractions.Persistence;

public interface IAccountRepository : IRepository<Account>
{
    Task<IReadOnlyList<AccountDto>> ListWithBalancesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public interface IJournalEntryRepository : IRepository<JournalEntry>
{
    Task<IReadOnlyList<JournalEntrySummaryDto>> ListSummariesAsync(CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReconciliationItemDto>> ListForReconciliationAsync(Guid? accountId, DateOnly? from, DateOnly? to, Domain.Enums.ReconciliationStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<decimal> GetAmountByGroupKindAsync(DateOnly from, DateOnly to, Domain.Enums.FinancialGroupKind kind, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetClassificationStatementAsync(Guid? groupId, Guid? categoryId, Domain.Enums.FinancialGroupKind kind, CancellationToken cancellationToken = default);
}

public interface IBudgetRepository : IRepository<Budget>
{
    Task<Budget?> GetByPeriodAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IAnalyticsRepository
{
    Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default);
}

public interface ISavingsCertificateReadRepository
{
    Task<IReadOnlyList<SavingsCertificateSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentValueAsync(DateOnly? at = null, CancellationToken cancellationToken = default);
}
