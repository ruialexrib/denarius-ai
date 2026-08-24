using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.DTOs;

public sealed record SavingsCertificateSummaryDto(Guid Id, DateOnly InvestmentDate, string SeriesNumber,
    string Description, decimal InvestmentValue, decimal Rate, decimal CurrentValue, DateOnly NextCapitalization)
{
    public decimal Yield => CurrentValue - InvestmentValue;
    public decimal FutureNetInterest => CurrentValue * (Rate / 100m * .72m / 4m);
    public decimal FutureValue => CurrentValue + FutureNetInterest;
}

public sealed record FinancialGroupDto(Guid Id, string Name, string? Description, FinancialGroupKind Kind, bool IsActive, int SortOrder);
public sealed record CategoryDto(Guid Id, Guid FinancialGroupId, string Name, string? Description, bool IsActive, int SortOrder);
public sealed record SaveFinancialGroupDto(string Name, string? Description, FinancialGroupKind Kind, int SortOrder);
public sealed record SaveCategoryDto(Guid FinancialGroupId, string Name, string? Description, int SortOrder);
public sealed record AccountDto(Guid Id, string Name, string? Description, AccountType AccountType, decimal InitialBalance, decimal Balance, string Currency, bool IsActive, Guid? CategoryId);
public sealed record SaveAccountDto(string Name, string? Description, AccountType AccountType, decimal InitialBalance, string Currency, Guid? CategoryId);
public sealed record JournalEntryLineInput(Guid AccountId, decimal Debit, decimal Credit, string? Description = null, Guid? CategoryId = null);
public sealed record CreateJournalEntryRequest(DateOnly Date, string Description, string? Reference, string? Notes, IReadOnlyCollection<JournalEntryLineInput> Lines, Guid? BudgetId = null);
public sealed record JournalEntryResultDto(Guid Id, DateOnly Date, string Description, decimal TotalDebit, decimal TotalCredit, JournalEntryStatus Status);
public sealed record JournalEntrySummaryDto(Guid Id, DateOnly Date, string Description, string? Reference, decimal TotalDebit, decimal TotalCredit, JournalEntryStatus Status, ReconciliationStatus ReconciliationStatus, int? BudgetYear = null, int? BudgetMonth = null, Guid? BudgetId = null)
{
    public string? BudgetName => BudgetYear.HasValue && BudgetMonth.HasValue ? $"{BudgetMonth:D2}/{BudgetYear}" : null;
}
public sealed record JournalEntryLineDto(Guid Id, Guid AccountId, string AccountName, Guid? CategoryId, string? CategoryName, decimal Debit, decimal Credit, string? Description);
public sealed record JournalEntryDetailsDto(Guid Id, DateOnly Date, string Description, string? Reference, string? Notes, JournalEntryStatus Status, DateTimeOffset? CancelledAt, string? CancelledBy, ReconciliationStatus ReconciliationStatus, IReadOnlyList<JournalEntryLineDto> Lines, Guid? BudgetId = null, string? BudgetName = null)
{
    public decimal TotalDebit => Lines.Sum(line => line.Debit);
    public decimal TotalCredit => Lines.Sum(line => line.Credit);
    public decimal Difference => TotalDebit - TotalCredit;
}
public sealed record ReconciliationItemDto(Guid JournalEntryId, DateOnly Date, string Description, string? Reference, string AccountNames, decimal Debit, decimal Credit, ReconciliationStatus Status, DateTimeOffset? ReconciledAt, string? ReconciledBy);
public sealed record SaveBudgetLineDto(Guid CategoryId, decimal Amount);
public sealed record BudgetPeriodDto(Guid Id, int Year, int Month) { public string Name => $"{Month:D2}/{Year}"; }
public sealed record BudgetExecutionItemDto(Guid CategoryId, string CategoryName, decimal Budgeted, decimal Actual, Guid FinancialGroupId = default, string FinancialGroupName = "")
{
    public decimal Variance => Actual - Budgeted;
    public decimal? ExecutionPercentage => Budgeted == 0 ? null : decimal.Round(Actual / Budgeted * 100m, 2);
}
public sealed record MonthlySummaryDto(decimal Income, decimal Expenses)
{
    public decimal Result => Income - Expenses;
}
