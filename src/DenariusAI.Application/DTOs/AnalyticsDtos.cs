namespace DenariusAI.Application.DTOs;

public sealed record AnalyticsFilterDto(DateOnly From, DateOnly To, Guid? GroupId = null, Guid? CategoryId = null, Guid? AccountId = null);
public sealed record AnalyticsBreakdownDto(Guid Id, string Name, decimal Amount);
public sealed record AnalyticsTrendDto(int Year, int Month, decimal Income, decimal Expenses, decimal NetWorth)
{
    public string Label => $"{Month:D2}/{Year}";
    public decimal Savings => Income - Expenses;
}
public sealed record AnalyticsDto(decimal Income, decimal Expenses, decimal PreviousIncome, decimal PreviousExpenses, decimal NetWorth,
    decimal SavingsCertificatesValue, decimal SavingsCertificatesYield, IReadOnlyList<SavingsCertificateSummaryDto> SavingsCertificates,
    IReadOnlyList<AnalyticsBreakdownDto> Groups, IReadOnlyList<AnalyticsBreakdownDto> Categories,
    IReadOnlyList<AnalyticsBreakdownDto> Accounts, IReadOnlyList<AnalyticsTrendDto> Trend)
{
    public AnalyticsDto(decimal income, decimal expenses, decimal previousIncome, decimal previousExpenses, decimal netWorth,
        IReadOnlyList<AnalyticsBreakdownDto> groups, IReadOnlyList<AnalyticsBreakdownDto> categories,
        IReadOnlyList<AnalyticsBreakdownDto> accounts, IReadOnlyList<AnalyticsTrendDto> trend)
        : this(income, expenses, previousIncome, previousExpenses, netWorth, 0m, 0m, [], groups, categories, accounts, trend) { }

    public decimal Savings => Income - Expenses;
    public decimal PreviousSavings => PreviousIncome - PreviousExpenses;
    public decimal SavingsRate => Income == 0m ? 0m : decimal.Round(Savings / Income * 100m, 1);
    public decimal PreviousSavingsRate => PreviousIncome == 0m ? 0m : decimal.Round(PreviousSavings / PreviousIncome * 100m, 1);
    public decimal? IncomeChange => PreviousIncome == 0m ? null : decimal.Round((Income - PreviousIncome) / PreviousIncome * 100m, 1);
    public decimal? ExpensesChange => PreviousExpenses == 0m ? null : decimal.Round((Expenses - PreviousExpenses) / PreviousExpenses * 100m, 1);
    public decimal SavingsChange => Savings - PreviousSavings;
    public decimal SavingsCertificatesFutureNetInterest => SavingsCertificates.Sum(item => item.FutureNetInterest);
    public decimal SavingsCertificatesFutureValue => SavingsCertificates.Sum(item => item.FutureValue);
}
