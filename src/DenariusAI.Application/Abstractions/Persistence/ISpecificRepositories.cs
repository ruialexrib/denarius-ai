using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>
/// Repository interface for Account entity operations.
/// Provides methods for querying accounts with balance calculations and statement generation.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Lists accounts with their current balances.
    /// </summary>
    /// <param name="activeOnly">When true, returns only active accounts. Default is false.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of accounts with balance information.</returns>
    Task<IReadOnlyList<AccountDto>> ListWithBalancesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current balance for a specific account.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The calculated balance amount.</returns>
    Task<decimal> GetBalanceAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the account statement with all transaction lines.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of statement lines.</returns>
    Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid accountId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for JournalEntry entity operations.
/// Provides methods for querying journal entries with various filters and aggregations.
/// </summary>
public interface IJournalEntryRepository : IRepository<JournalEntry>
{
    /// <summary>
    /// Lists journal entry summaries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of journal entry summaries.</returns>
    Task<IReadOnlyList<JournalEntrySummaryDto>> ListSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a journal entry with all related details (lines, categories, etc.).
    /// </summary>
    /// <param name="id">The unique identifier of the journal entry.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The journal entry with details, or null if not found.</returns>
    Task<JournalEntry?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists journal entries for reconciliation purposes with optional filtering.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="from">Optional start date filter.</param>
    /// <param name="to">Optional end date filter.</param>
    /// <param name="status">Optional reconciliation status filter.</param>
    /// <param name="search">Optional text search filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of reconciliation items.</returns>
    Task<IReadOnlyList<ReconciliationItemDto>> ListForReconciliationAsync(Guid? accountId, DateOnly? from, DateOnly? to, Domain.Enums.ReconciliationStatus? status, string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total amount for a specific financial group kind within a date range.
    /// </summary>
    /// <param name="from">Start date of the period.</param>
    /// <param name="to">End date of the period.</param>
    /// <param name="kind">The financial group kind (Income, Expense, etc.).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The aggregated amount.</returns>
    Task<decimal> GetAmountByGroupKindAsync(DateOnly from, DateOnly to, Domain.Enums.FinancialGroupKind kind, CancellationToken cancellationToken = default);

    /// <summary>Calculates the total amount associated with a budget period, regardless of movement date.</summary>
    Task<decimal> GetAmountByBudgetAndGroupKindAsync(int year, int month, Domain.Enums.FinancialGroupKind kind, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the classification statement for a specific group, category, and financial kind.
    /// </summary>
    /// <param name="groupId">Optional financial group filter.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="kind">The financial group kind.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of classification statement lines.</returns>
    Task<IReadOnlyList<ClassificationStatementLineDto>> GetClassificationStatementAsync(Guid? groupId, Guid? categoryId, Domain.Enums.FinancialGroupKind kind, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Budget entity operations.
/// Provides methods for querying budgets by period and execution data.
/// </summary>
public interface IBudgetRepository : IRepository<Budget>
{
    /// <summary>
    /// Gets the budget for a specific period.
    /// </summary>
    /// <param name="year">The year of the budget period.</param>
    /// <param name="month">The month of the budget period.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The budget for the specified period, or null if not found.</returns>
    Task<Budget?> GetByPeriodAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the budget execution data for a specific period.
    /// </summary>
    /// <param name="year">The year of the budget period.</param>
    /// <param name="month">The month of the budget period.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of budget execution items.</returns>
    Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>Gets persisted income and expense execution for the explicitly selected import budget.</summary>
    /// <param name="budgetId">The selected budget identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Active category totals, or an empty list if the budget does not exist.</returns>
    Task<IReadOnlyList<BudgetExecutionItemDto>> GetCategoryExecutionAsync(Guid budgetId, CancellationToken cancellationToken = default);

}

/// <summary>
/// Repository interface for analytics operations.
/// Provides methods for querying aggregated financial analytics data.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Gets analytics data based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for analytics.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Analytics data matching the filter criteria.</returns>
    Task<AnalyticsDto> GetAsync(AnalyticsFilterDto filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for SavingsCertificate queries.
/// Provides methods for querying savings certificates and calculating current values.
/// </summary>
public interface ISavingsCertificateReadRepository
{
    /// <summary>
    /// Lists all savings certificates with summary information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A read-only list of savings certificate summaries.</returns>
    Task<IReadOnlyList<SavingsCertificateSummaryDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total current value of all savings certificates.
    /// </summary>
    /// <param name="at">Optional date to calculate value at. If null, uses current date.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The total current value.</returns>
    Task<decimal> GetCurrentValueAsync(DateOnly? at = null, CancellationToken cancellationToken = default);
}
