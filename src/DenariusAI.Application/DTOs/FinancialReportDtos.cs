namespace DenariusAI.Application.DTOs;

/// <summary>Authoritative, pre-calculated financial facts supplied to report generators.</summary>
public sealed record FinancialReportDataDto(
    DateOnly From,
    DateOnly To,
    string Currency,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal SavingsRate,
    decimal NetWorth,
    IReadOnlyList<FinancialReportAccountDto> Accounts,
    IReadOnlyList<AnalyticsBreakdownDto> ExpenseGroups,
    IReadOnlyList<AnalyticsBreakdownDto> ExpenseCategories,
    IReadOnlyList<FinancialReportMonthDto> Months,
    IReadOnlyList<SavingsCertificateSummaryDto> SavingsCertificates,
    FinancialReportReconciliationDto Reconciliation);

public sealed record FinancialReportAccountDto(Guid Id, string Name, string Type, decimal InitialBalance, decimal BalanceAtEnd, string Currency, bool IsActive);

public sealed record FinancialReportMonthDto(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal Budgeted,
    decimal BudgetExecuted,
    decimal BudgetVariance,
    IReadOnlyList<BudgetExecutionItemDto> BudgetLines);

public sealed record FinancialReportReconciliationDto(
    int Total,
    int Reconciled,
    int Unreconciled,
    IReadOnlyList<FinancialReportReconciliationAccountDto> Accounts);

public sealed record FinancialReportReconciliationAccountDto(string Account, int Total, int Reconciled, int Unreconciled);
