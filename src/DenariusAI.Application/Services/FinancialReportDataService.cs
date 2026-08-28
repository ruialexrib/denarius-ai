using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

/// <summary>Calculates every numeric fact before it is supplied to an AI model.</summary>
public sealed class FinancialReportDataService(
    IAnalyticsService analyticsService,
    IAccountService accountService,
    IBudgetService budgetService,
    IReconciliationService reconciliationService) : IFinancialReportDataService
{
    public async Task<FinancialReportDataDto> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from == default || to == default || from > to) throw new ArgumentException("O intervalo de datas é inválido.");

        var analytics = await analyticsService.GetAsync(new(from, to), cancellationToken);
        var accounts = await accountService.ListAsync(false, cancellationToken);
        var accountFacts = new List<FinancialReportAccountDto>();
        foreach (var account in accounts.Where(item => item.AccountType is not AccountType.Income and not AccountType.Expense))
        {
            var statement = await accountService.GetStatementAsync(account.Id, cancellationToken);
            var lastLine = statement.Where(item => item.Date <= to)
                .OrderBy(item => item.Date).ThenBy(item => item.CreatedAt).ThenBy(item => item.LineId).LastOrDefault();
            accountFacts.Add(new(account.Id, account.Name, account.AccountType.ToString(), account.InitialBalance,
                lastLine?.Balance ?? account.InitialBalance, account.Currency, account.IsActive));
        }

        var months = new List<FinancialReportMonthDto>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var finalMonth = new DateOnly(to.Year, to.Month, 1);
        while (cursor <= finalMonth)
        {
            var trend = analytics.Trend.Single(item => item.Year == cursor.Year && item.Month == cursor.Month);
            var execution = await budgetService.GetExecutionAsync(cursor.Year, cursor.Month, cancellationToken);
            var relevantLines = execution.Where(item => item.Budgeted != 0m || item.Actual != 0m).ToList();
            var budgeted = relevantLines.Sum(item => item.Budgeted);
            var executed = relevantLines.Sum(item => item.Actual);
            months.Add(new(cursor.Year, cursor.Month, trend.Income, trend.Expenses, trend.Savings,
                budgeted, executed, executed - budgeted, relevantLines));
            cursor = cursor.AddMonths(1);
        }

        var reconciliationItems = await reconciliationService.ListAsync(from: from, to: to, cancellationToken: cancellationToken);
        var reconciliationAccounts = reconciliationItems.GroupBy(item => item.AccountNames)
            .OrderBy(group => group.Key)
            .Select(group => new FinancialReportReconciliationAccountDto(group.Key, group.Count(),
                group.Count(item => item.Status == ReconciliationStatus.Reconciled),
                group.Count(item => item.Status == ReconciliationStatus.Unreconciled))).ToList();
        var reconciliation = new FinancialReportReconciliationDto(reconciliationItems.Count,
            reconciliationItems.Count(item => item.Status == ReconciliationStatus.Reconciled),
            reconciliationItems.Count(item => item.Status == ReconciliationStatus.Unreconciled), reconciliationAccounts);

        return new(from, to, "EUR", analytics.Income, analytics.Expenses, analytics.Savings, analytics.SavingsRate,
            analytics.NetWorth, accountFacts, analytics.Groups, analytics.Categories, months,
            analytics.SavingsCertificates, reconciliation);
    }
}
