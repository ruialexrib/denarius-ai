using System.ComponentModel;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using ModelContextProtocol.Server;

namespace DenariusAI.Mcp.Tools;

/// <summary>
/// Represents the FinancialTools type.
/// </summary>
[McpServerToolType]
public static class FinancialTools
{
    [McpServerTool(Name = "get_accounts"), Description("Lists financial accounts and their calculated balances.")]
    public static async Task<object> GetAccounts(IAccountService service, CancellationToken cancellationToken) =>
        await service.ListAsync(cancellationToken: cancellationToken);

    [McpServerTool(Name = "get_account_balance"), Description("Gets one financial account and its calculated balance.")]
    public static async Task<object> GetAccountBalance([Description("Account identifier in UUID format.")] string accountId, IAccountService service, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(accountId, out var parsedAccountId)) throw new ArgumentException("Account identifier must be a valid UUID.", nameof(accountId));
        return await service.GetAsync(parsedAccountId, cancellationToken) ?? throw new ArgumentException("Account was not found.", nameof(accountId));
    }

    [McpServerTool(Name = "get_transactions"), Description("Lists transactions, optionally filtered by dates, with a safe result limit.")]
    public static async Task<object> GetTransactions(DateOnly? from, DateOnly? to,
        [Description("Maximum records between 1 and 200.")] int limit, IJournalEntryService service, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 200.");
        if (from.HasValue && to.HasValue && from > to) throw new ArgumentException("The date interval is invalid.");
        var entries = await service.ListAsync(cancellationToken);
        return entries.Where(item => (!from.HasValue || item.Date >= from) && (!to.HasValue || item.Date <= to)).Take(limit).ToList();
    }

    [McpServerTool(Name = "get_monthly_summary"), Description("Returns income, expenses and result for a calendar month.")]
    public static async Task<object> GetMonthlySummary(int year, int month, IJournalEntryService service, CancellationToken cancellationToken) =>
        await service.GetMonthlySummaryAsync(year, month, cancellationToken);

    [McpServerTool(Name = "get_budget_execution"), Description("Returns budgeted, actual, variance and execution values by category for a month.")]
    public static async Task<object> GetBudgetExecution(int year, int month, IBudgetService service, CancellationToken cancellationToken) =>
        await service.GetExecutionAsync(year, month, cancellationToken);

    [McpServerTool(Name = "get_expenses_by_category"), Description("Returns expenses aggregated by category for a date interval.")]
    public static async Task<object> GetExpensesByCategory(DateOnly from, DateOnly to, IAnalyticsService service, CancellationToken cancellationToken) =>
        (await service.GetAsync(new(from, to), cancellationToken)).Categories;

    [McpServerTool(Name = "get_expenses_by_group"), Description("Returns expenses aggregated by financial group for a date interval.")]
    public static async Task<object> GetExpensesByGroup(DateOnly from, DateOnly to, IAnalyticsService service, CancellationToken cancellationToken) =>
        (await service.GetAsync(new(from, to), cancellationToken)).Groups;

    [McpServerTool(Name = "get_income_by_period"), Description("Returns total income for a date interval.")]
    public static async Task<object> GetIncomeByPeriod(DateOnly from, DateOnly to, IAnalyticsService service, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(new(from, to), cancellationToken);
        return new { from, to, amount = result.Income, currency = "EUR" };
    }

    [McpServerTool(Name = "get_savings_rate"), Description("Returns income, expenses, savings and savings rate for a date interval.")]
    public static async Task<object> GetSavingsRate(DateOnly from, DateOnly to, IAnalyticsService service, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(new(from, to), cancellationToken);
        return new { from, to, result.Income, result.Expenses, result.Savings, result.SavingsRate, currency = "EUR" };
    }

    [McpServerTool(Name = "get_unreconciled_transactions"), Description("Lists unreconciled banking transactions for an optional date interval.")]
    public static async Task<object> GetUnreconciledTransactions(DateOnly? from, DateOnly? to, IReconciliationService service, CancellationToken cancellationToken) =>
        await service.ListAsync(from: from, to: to, status: ReconciliationStatus.Unreconciled, cancellationToken: cancellationToken);

    [McpServerTool(Name = "get_financial_summary"), Description("Returns the principal financial indicators and six-month evolution for a selected month.")]
    public static async Task<object> GetFinancialSummary(int year, int month, IDashboardService service, CancellationToken cancellationToken) =>
        await service.GetAsync(year, month, cancellationToken);
}
