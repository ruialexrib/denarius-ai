using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

public sealed class DashboardService(
    IAccountService accountService,
    IJournalEntryService journalEntryService,
    IBudgetService budgetService,
    IReconciliationService reconciliationService,
    DenariusAI.Application.Abstractions.Persistence.ISavingsCertificateReadRepository? savingsRepository = null) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 9999 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var accounts = await accountService.ListAsync(activeOnly: true, cancellationToken);
        var summary = await journalEntryService.GetMonthlySummaryAsync(year, month, cancellationToken);
        var execution = await budgetService.GetExecutionAsync(year, month, cancellationToken);
        var unreconciled = await reconciliationService.ListAsync(status: ReconciliationStatus.Unreconciled, cancellationToken: cancellationToken);
        var certificates = savingsRepository is null ? [] : await savingsRepository.ListAsync(cancellationToken);
        var certificateValue = certificates.Sum(item => item.CurrentValue);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var maturedCertificates = certificates.Where(item => item.NextCapitalization <= today).ToList();

        var liquidTypes = new[] { AccountType.BankAccount, AccountType.Cash };
        var savingTypes = new[] { AccountType.Savings, AccountType.TermDeposit, AccountType.Investment, AccountType.OtherAsset };
        var assetTypes = liquidTypes.Concat(savingTypes).ToHashSet();
        var categories = execution.Where(item => item.Actual != 0 || item.Budgeted != 0)
            .OrderByDescending(item => item.Actual).ThenBy(item => item.CategoryName)
            .Select(item => new DashboardCategoryDto(item.CategoryName, item.Actual, item.Budgeted)).ToList();

        var evolution = new List<DashboardMonthDto>();
        var budgetEvolution = new List<DashboardBudgetMonthDto>();
        for (var monthNumber = 1; monthNumber <= 12; monthNumber++)
        {
            var item = await journalEntryService.GetMonthlySummaryAsync(year, monthNumber, cancellationToken);
            evolution.Add(new(year, monthNumber, item.Income, item.Expenses));
            var monthExecution = monthNumber == month ? execution : await budgetService.GetExecutionAsync(year, monthNumber, cancellationToken);
            budgetEvolution.Add(new(year, monthNumber, monthExecution.Sum(value => value.Budgeted), monthExecution.Sum(value => value.Actual)));
        }

        return new DashboardDto(year, month,
            accounts.Where(item => liquidTypes.Contains(item.AccountType)).Sum(item => item.Balance),
            accounts.Where(item => savingTypes.Contains(item.AccountType)).Sum(item => item.Balance) + certificateValue,
            accounts.Where(item => assetTypes.Contains(item.AccountType)).Sum(item => item.Balance) + certificateValue,
            certificateValue, certificates.Sum(item => item.Yield), maturedCertificates.Count,
            maturedCertificates.Sum(item => item.CurrentValue), certificates.Sum(item => item.FutureNetInterest),
            certificates.Sum(item => item.FutureValue),
            summary.Income, summary.Expenses,
            execution.Sum(item => item.Budgeted), execution.Sum(item => item.Actual), unreconciled.Count,
            categories, evolution) { BudgetEvolution = budgetEvolution };
    }
}
