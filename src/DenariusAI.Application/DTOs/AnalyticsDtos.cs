namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents filter criteria for analytics queries.
/// </summary>
/// <param name="From">The start date for the analytics period.</param>
/// <param name="To">The end date for the analytics period.</param>
/// <param name="GroupId">Optional identifier to filter by transaction group.</param>
/// <param name="CategoryId">Optional identifier to filter by transaction category.</param>
/// <param name="AccountId">Optional identifier to filter by account.</param>
public sealed record AnalyticsFilterDto(DateOnly From, DateOnly To, Guid? GroupId = null, Guid? CategoryId = null, Guid? AccountId = null);

/// <summary>
/// Represents a breakdown item in analytics with its total amount.
/// </summary>
/// <param name="Id">The unique identifier of the breakdown item.</param>
/// <param name="Name">The display name of the breakdown item.</param>
/// <param name="Amount">The total amount for this breakdown item.</param>
public sealed record AnalyticsBreakdownDto(Guid Id, string Name, decimal Amount);

/// <summary>
/// Represents financial trend data for a specific month.
/// </summary>
/// <param name="Year">The year of the trend data.</param>
/// <param name="Month">The month of the trend data.</param>
/// <param name="Income">The total income for the period.</param>
/// <param name="Expenses">The total expenses for the period.</param>
/// <param name="NetWorth">The net worth at the end of the period.</param>
public sealed record AnalyticsTrendDto(int Year, int Month, decimal Income, decimal Expenses, decimal NetWorth)
{
    /// <summary>
    /// Gets the formatted label for this trend period (MM/YYYY).
    /// </summary>
    public string Label => $"{Month:D2}/{Year}";
    
    /// <summary>
    /// Gets the savings amount (Income - Expenses) for this period.
    /// </summary>
    public decimal Savings => Income - Expenses;
}

/// <summary>
/// Represents comprehensive analytics data including income, expenses, savings certificates, and trends.
/// </summary>
/// <param name="Income">The total income for the current period.</param>
/// <param name="Expenses">The total expenses for the current period.</param>
/// <param name="PreviousIncome">The total income for the previous period.</param>
/// <param name="PreviousExpenses">The total expenses for the previous period.</param>
/// <param name="NetWorth">The current net worth.</param>
/// <param name="SavingsCertificatesValue">The total value of savings certificates.</param>
/// <param name="SavingsCertificatesYield">The yield from savings certificates.</param>
/// <param name="SavingsCertificates">The collection of savings certificate summaries.</param>
/// <param name="Groups">The breakdown of transactions by group.</param>
/// <param name="Categories">The breakdown of transactions by category.</param>
/// <param name="Accounts">The breakdown of transactions by account.</param>
/// <param name="Trend">The historical trend data.</param>
public sealed record AnalyticsDto(decimal Income, decimal Expenses, decimal PreviousIncome, decimal PreviousExpenses, decimal NetWorth,
    decimal SavingsCertificatesValue, decimal SavingsCertificatesYield, IReadOnlyList<SavingsCertificateSummaryDto> SavingsCertificates,
    IReadOnlyList<AnalyticsBreakdownDto> Groups, IReadOnlyList<AnalyticsBreakdownDto> Categories,
    IReadOnlyList<AnalyticsBreakdownDto> Accounts, IReadOnlyList<AnalyticsTrendDto> Trend)
{
    /// <summary>
    /// Initializes a new instance of <see cref="AnalyticsDto"/> without savings certificate data.
    /// </summary>
    public AnalyticsDto(decimal income, decimal expenses, decimal previousIncome, decimal previousExpenses, decimal netWorth,
        IReadOnlyList<AnalyticsBreakdownDto> groups, IReadOnlyList<AnalyticsBreakdownDto> categories,
        IReadOnlyList<AnalyticsBreakdownDto> accounts, IReadOnlyList<AnalyticsTrendDto> trend)
        : this(income, expenses, previousIncome, previousExpenses, netWorth, 0m, 0m, [], groups, categories, accounts, trend) { }

    /// <summary>
    /// Gets the current savings amount (Income - Expenses).
    /// </summary>
    public decimal Savings => Income - Expenses;
    
    /// <summary>
    /// Gets the previous period savings amount (PreviousIncome - PreviousExpenses).
    /// </summary>
    public decimal PreviousSavings => PreviousIncome - PreviousExpenses;
    
    /// <summary>
    /// Gets the current savings rate as a percentage of income.
    /// </summary>
    public decimal SavingsRate => Income == 0m ? 0m : decimal.Round(Savings / Income * 100m, 1);
    
    /// <summary>
    /// Gets the previous period savings rate as a percentage of income.
    /// </summary>
    public decimal PreviousSavingsRate => PreviousIncome == 0m ? 0m : decimal.Round(PreviousSavings / PreviousIncome * 100m, 1);
    
    /// <summary>
    /// Gets the percentage change in income compared to the previous period, or null if previous income is zero.
    /// </summary>
    public decimal? IncomeChange => PreviousIncome == 0m ? null : decimal.Round((Income - PreviousIncome) / PreviousIncome * 100m, 1);
    
    /// <summary>
    /// Gets the percentage change in expenses compared to the previous period, or null if previous expenses are zero.
    /// </summary>
    public decimal? ExpensesChange => PreviousExpenses == 0m ? null : decimal.Round((Expenses - PreviousExpenses) / PreviousExpenses * 100m, 1);
    
    /// <summary>
    /// Gets the absolute change in savings compared to the previous period.
    /// </summary>
    public decimal SavingsChange => Savings - PreviousSavings;
    
    /// <summary>
    /// Gets the total future net interest from all savings certificates.
    /// </summary>
    public decimal SavingsCertificatesFutureNetInterest => SavingsCertificates.Sum(item => item.FutureNetInterest);
    
    /// <summary>
    /// Gets the total future value of all savings certificates.
    /// </summary>
    public decimal SavingsCertificatesFutureValue => SavingsCertificates.Sum(item => item.FutureValue);
}
