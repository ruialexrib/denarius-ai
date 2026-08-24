using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface IFinancialGroupService
{
    Task<IReadOnlyList<FinancialGroupDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<FinancialGroupDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClassificationStatementLineDto>>([]);
    Task UpdateAsync(Guid id, SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClassificationStatementLineDto>>([]);
    Task UpdateAsync(Guid id, SaveCategoryDto input, string userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountStatementLineDto>>([]);
    Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

public interface IJournalEntryService
{
    Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<JournalEntryDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default);
    Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default);
}

public interface IReconciliationService
{
    Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, Domain.Enums.ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
    Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default);
    Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default);
}

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default);
}
