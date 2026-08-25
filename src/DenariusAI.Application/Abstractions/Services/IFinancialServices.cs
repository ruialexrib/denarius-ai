using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Service for managing financial groups (account classifications).
/// </summary>
public interface IFinancialGroupService
{
    /// <summary>
    /// Lists all financial groups.
    /// </summary>
    /// <param name="activeOnly">Filter only active groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of financial groups.</returns>
    Task<IReadOnlyList<FinancialGroupDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific financial group by ID.
    /// </summary>
    /// <param name="id">Financial group identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Financial group details or null if not found.</returns>
    Task<FinancialGroupDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new financial group.
    /// </summary>
    /// <param name="input">Financial group data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created financial group identifier.</returns>
    Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the statement for a financial group showing all related transactions.
    /// </summary>
    /// <param name="id">Financial group identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of statement lines.</returns>
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClassificationStatementLineDto>>([]);
    
    /// <summary>
    /// Updates an existing financial group.
    /// </summary>
    /// <param name="id">Financial group identifier.</param>
    /// <param name="input">Updated financial group data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid id, SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates or deactivates a financial group.
    /// </summary>
    /// <param name="id">Financial group identifier.</param>
    /// <param name="isActive">New active status.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing categories within financial groups.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Lists all categories, optionally filtered by group.
    /// </summary>
    /// <param name="groupId">Optional financial group filter.</param>
    /// <param name="activeOnly">Filter only active categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of categories.</returns>
    Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific category by ID.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category details or null if not found.</returns>
    Task<CategoryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="input">Category data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created category identifier.</returns>
    Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the statement for a category showing all related transactions.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of statement lines.</returns>
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClassificationStatementLineDto>>([]);
    
    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="input">Updated category data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid id, SaveCategoryDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates or deactivates a category.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="isActive">New active status.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing financial accounts (bank accounts, cash, credit cards, etc.).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Lists all accounts.
    /// </summary>
    /// <param name="activeOnly">Filter only active accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of accounts.</returns>
    Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific account by ID.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account details or null if not found.</returns>
    Task<AccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the statement for an account showing all transactions.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of account statement lines.</returns>
    Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountStatementLineDto>>([]);
    
    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="input">Account data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created account identifier.</returns>
    Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="input">Updated account data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates or deactivates an account.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="isActive">New active status.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing journal entries (financial transactions).
/// </summary>
public interface IJournalEntryService
{
    /// <summary>
    /// Lists all journal entries with summary information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of journal entry summaries.</returns>
    Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about a specific journal entry.
    /// </summary>
    /// <param name="id">Journal entry identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Journal entry details or null if not found.</returns>
    Task<JournalEntryDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    /// <param name="request">Journal entry data with all movements.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing created journal entry identifier and validation messages.</returns>
    Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing journal entry.
    /// </summary>
    /// <param name="id">Journal entry identifier.</param>
    /// <param name="request">Updated journal entry data.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a journal entry.
    /// </summary>
    /// <param name="id">Journal entry identifier.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets monthly summary of transactions.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="month">Month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Monthly summary with totals and breakdown.</returns>
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing budgets and tracking budget execution.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Lists all budget periods.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of budget periods.</returns>
    Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets budget execution for a specific period, comparing planned vs actual.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="month">Month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of budget execution items per category.</returns>
    Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves or updates budget for a specific period.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="month">Month (1-12).</param>
    /// <param name="lines">Budget lines per category.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for bank reconciliation operations.
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// Lists transactions for reconciliation with filters.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="from">Start date filter.</param>
    /// <param name="to">End date filter.</param>
    /// <param name="status">Reconciliation status filter.</param>
    /// <param name="search">Text search filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of reconciliation items.</returns>
    Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, Domain.Enums.ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a journal entry as reconciled.
    /// </summary>
    /// <param name="journalEntryId">Journal entry identifier.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Undoes reconciliation of a journal entry.
    /// </summary>
    /// <param name="journalEntryId">Journal entry identifier.</param>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for generating dashboard data and KPIs.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets dashboard data for a specific period with key metrics and charts.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="month">Month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard data with KPIs and visualizations.</returns>
    Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for financial analytics and reporting.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets analytics data based on specified filters.
    /// </summary>
    /// <param name="filter">Analytics filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics data with charts and reports.</returns>
    Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default);
}
