namespace DenariusAI.Application.DTOs;

public sealed record DashboardCategoryDto(string Name, decimal Actual, decimal Budgeted);
public sealed record DashboardMonthDto(int Year, int Month, decimal Income, decimal Expenses)
{
    public string Label => $"{Month:D2}/{Year}";
}

public sealed record DashboardDto(
    int Year,
    int Month,
    decimal LiquidBalance,
    decimal SavingsAndInvestments,
    decimal TotalAssets,
    decimal SavingsCertificatesValue,
    decimal SavingsCertificatesYield,
    int MaturedSavingsCertificates,
    decimal MaturedSavingsCertificatesValue,
    decimal SavingsCertificatesFutureNetInterest,
    decimal SavingsCertificatesFutureValue,
    decimal Income,
    decimal Expenses,
    decimal Budgeted,
    decimal BudgetActual,
    int UnreconciledMovements,
    IReadOnlyList<DashboardCategoryDto> Categories,
    IReadOnlyList<DashboardMonthDto> Evolution)
{
    public DashboardDto(int year, int month, decimal liquidBalance, decimal savingsAndInvestments, decimal totalAssets,
        decimal income, decimal expenses, decimal budgeted, decimal budgetActual, int unreconciledMovements,
        IReadOnlyList<DashboardCategoryDto> categories, IReadOnlyList<DashboardMonthDto> evolution)
        : this(year, month, liquidBalance, savingsAndInvestments, totalAssets, 0m, 0m, 0, 0m, 0m, 0m, income, expenses,
            budgeted, budgetActual, unreconciledMovements, categories, evolution) { }

    public decimal MonthlyResult => Income - Expenses;
    public decimal BudgetAvailable => Budgeted - BudgetActual;
    public decimal? BudgetExecution => Budgeted == 0 ? null : decimal.Round(BudgetActual / Budgeted * 100m, 1);
}
