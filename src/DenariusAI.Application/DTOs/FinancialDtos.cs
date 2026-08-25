using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents a summary of a savings certificate investment.
/// </summary>
/// <param name="Id">The unique identifier of the savings certificate.</param>
/// <param name="InvestmentDate">The date when the investment was made.</param>
/// <param name="SeriesNumber">The series number of the certificate.</param>
/// <param name="Description">A description of the certificate.</param>
/// <param name="InvestmentValue">The initial investment amount.</param>
/// <param name="Rate">The interest rate as a percentage.</param>
/// <param name="CurrentValue">The current value of the investment.</param>
/// <param name="NextCapitalization">The date of the next interest capitalization.</param>
public sealed record SavingsCertificateSummaryDto(Guid Id, DateOnly InvestmentDate, string SeriesNumber,
    string Description, decimal InvestmentValue, decimal Rate, decimal CurrentValue, DateOnly NextCapitalization)
{
    /// <summary>
    /// Gets the yield of the investment (current value minus initial investment).
    /// </summary>
    public decimal Yield => CurrentValue - InvestmentValue;
    
    /// <summary>
    /// Gets the estimated net interest for the next period (after 28% tax).
    /// </summary>
    public decimal FutureNetInterest => CurrentValue * (Rate / 100m * .72m / 4m);
    
    /// <summary>
    /// Gets the estimated future value including the next period's interest.
    /// </summary>
    public decimal FutureValue => CurrentValue + FutureNetInterest;
}

/// <summary>
/// Represents a financial group (e.g., Income, Expenses).
/// </summary>
public sealed record FinancialGroupDto(Guid Id, string Name, string? Description, FinancialGroupKind Kind, bool IsActive, int SortOrder);

/// <summary>
/// Represents a category within a financial group.
/// </summary>
public sealed record CategoryDto(Guid Id, Guid FinancialGroupId, string Name, string? Description, bool IsActive, int SortOrder);

/// <summary>
/// Data transfer object for creating or updating a financial group.
/// </summary>
public sealed record SaveFinancialGroupDto(string Name, string? Description, FinancialGroupKind Kind, int SortOrder);

/// <summary>
/// Data transfer object for creating or updating a category.
/// </summary>
public sealed record SaveCategoryDto(Guid FinancialGroupId, string Name, string? Description, int SortOrder);

/// <summary>
/// Represents an account with its balance and properties.
/// </summary>
public sealed record AccountDto(Guid Id, string Name, string? Description, AccountType AccountType, decimal InitialBalance, decimal Balance, string Currency, bool IsActive, Guid? CategoryId);

/// <summary>
/// Represents a line in an account statement showing transaction details and running balance.
/// </summary>
public sealed record AccountStatementLineDto(Guid JournalEntryId, Guid LineId, DateOnly Date, DateTimeOffset CreatedAt, string Description, string? Reference, string? LineDescription, string? CategoryName, decimal Debit, decimal Credit, decimal Balance, ReconciliationStatus ReconciliationStatus);

/// <summary>
/// Represents a line in a classification statement grouping transactions by category.
/// </summary>
public sealed record ClassificationStatementLineDto(Guid JournalEntryId, Guid LineId, DateOnly Date, DateTimeOffset CreatedAt, string Description, string? Reference, string AccountName, string CategoryName, decimal Debit, decimal Credit, decimal Balance);

/// <summary>
/// Data transfer object for creating or updating an account.
/// </summary>
public sealed record SaveAccountDto(string Name, string? Description, AccountType AccountType, decimal InitialBalance, string Currency, Guid? CategoryId);

/// <summary>
/// Represents a single line in a journal entry with account and amount details.
/// </summary>
public sealed record JournalEntryLineInput(Guid AccountId, decimal Debit, decimal Credit, string? Description = null, Guid? CategoryId = null);

/// <summary>
/// Request object for creating a new journal entry with multiple lines.
/// </summary>
public sealed record CreateJournalEntryRequest(DateOnly Date, string Description, string? Reference, string? Notes, IReadOnlyCollection<JournalEntryLineInput> Lines, Guid? BudgetId = null);

/// <summary>
/// Represents the result of a journal entry operation.
/// </summary>
public sealed record JournalEntryResultDto(Guid Id, DateOnly Date, string Description, decimal TotalDebit, decimal TotalCredit, JournalEntryStatus Status);

/// <summary>
/// Represents a summary view of a journal entry.
/// </summary>
public sealed record JournalEntrySummaryDto(Guid Id, DateOnly Date, string Description, string? Reference, decimal TotalDebit, decimal TotalCredit, JournalEntryStatus Status, ReconciliationStatus ReconciliationStatus, int? BudgetYear = null, int? BudgetMonth = null, Guid? BudgetId = null, string MovementType = "Transferência")
{
    /// <summary>
    /// Gets the budget name formatted as MM/YYYY if year and month are available.
    /// </summary>
    public string? BudgetName => BudgetYear.HasValue && BudgetMonth.HasValue ? $"{BudgetMonth:D2}/{BudgetYear}" : null;
}

/// <summary>
/// Represents a line within a journal entry showing account and category details.
/// </summary>
public sealed record JournalEntryLineDto(Guid Id, Guid AccountId, string AccountName, Guid? CategoryId, string? CategoryName, decimal Debit, decimal Credit, string? Description);

/// <summary>
/// Represents detailed information about a journal entry including all its lines.
/// </summary>
public sealed record JournalEntryDetailsDto(Guid Id, DateOnly Date, string Description, string? Reference, string? Notes, JournalEntryStatus Status, DateTimeOffset? CancelledAt, string? CancelledBy, ReconciliationStatus ReconciliationStatus, IReadOnlyList<JournalEntryLineDto> Lines, Guid? BudgetId = null, string? BudgetName = null)
{
    /// <summary>
    /// Gets the sum of all debit amounts in the entry.
    /// </summary>
    public decimal TotalDebit => Lines.Sum(line => line.Debit);
    
    /// <summary>
    /// Gets the sum of all credit amounts in the entry.
    /// </summary>
    public decimal TotalCredit => Lines.Sum(line => line.Credit);
    
    /// <summary>
    /// Gets the difference between total debits and credits (should be zero for balanced entries).
    /// </summary>
    public decimal Difference => TotalDebit - TotalCredit;
}

/// <summary>
/// Represents an item in the reconciliation process.
/// </summary>
public sealed record ReconciliationItemDto(Guid JournalEntryId, DateOnly Date, string Description, string? Reference, string AccountNames, decimal Debit, decimal Credit, ReconciliationStatus Status, DateTimeOffset? ReconciledAt, string? ReconciledBy);

/// <summary>
/// Data transfer object for creating or updating a budget line.
/// </summary>
public sealed record SaveBudgetLineDto(Guid CategoryId, decimal Amount);

/// <summary>
/// Represents a budget period (month/year).
/// </summary>
public sealed record BudgetPeriodDto(Guid Id, int Year, int Month) 
{ 
    /// <summary>
    /// Gets the budget period name formatted as MM/YYYY.
    /// </summary>
    public string Name => $"{Month:D2}/{Year}"; 
}

/// <summary>
/// Represents budget execution data comparing budgeted vs actual amounts for a category.
/// </summary>
public sealed record BudgetExecutionItemDto(Guid CategoryId, string CategoryName, decimal Budgeted, decimal Actual, Guid FinancialGroupId = default, string FinancialGroupName = "")
{
    /// <summary>
    /// Gets the variance between actual and budgeted amounts.
    /// </summary>
    public decimal Variance => Actual - Budgeted;
    
    /// <summary>
    /// Gets the execution percentage (actual/budgeted * 100). Returns null if budgeted is zero.
    /// </summary>
    public decimal? ExecutionPercentage => Budgeted == 0 ? null : decimal.Round(Actual / Budgeted * 100m, 2);
}

/// <summary>
/// Represents a monthly summary of income and expenses.
/// </summary>
public sealed record MonthlySummaryDto(decimal Income, decimal Expenses)
{
    /// <summary>
    /// Gets the net result (income minus expenses).
    /// </summary>
    public decimal Result => Income - Expenses;
}
