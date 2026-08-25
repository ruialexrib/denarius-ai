namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents a dashboard category with actual and budgeted values.
/// </summary>
/// <param name="Name">The name of the category.</param>
/// <param name="Actual">The actual amount spent in the category.</param>
/// <param name="Budgeted">The budgeted amount for the category.</param>
public sealed record DashboardCategoryDto(string Name, decimal Actual, decimal Budgeted);

/// <summary>
/// Represents budget data for a specific month.
/// </summary>
/// <param name="Year">The year.</param>
/// <param name="Month">The month number (1-12).</param>
/// <param name="Budgeted">The total budgeted amount for the month.</param>
/// <param name="Actual">The actual amount spent in the month.</param>
public sealed record DashboardBudgetMonthDto(int Year, int Month, decimal Budgeted, decimal Actual)
{
    /// <summary>
    /// Gets a formatted label for the month in MM/YYYY format.
    /// </summary>
    public string Label => $"{Month:D2}/{Year}";
}

/// <summary>
/// Represents financial data for a specific month.
/// </summary>
/// <param name="Year">The year.</param>
/// <param name="Month">The month number (1-12).</param>
/// <param name="Income">The total income for the month.</param>
/// <param name="Expenses">The total expenses for the month.</param>
public sealed record DashboardMonthDto(int Year, int Month, decimal Income, decimal Expenses)
{
    /// <summary>
    /// Gets a formatted label for the month in MM/YYYY format.
    /// </summary>
    public string Label => $"{Month:D2}/{Year}";
}

/// <summary>
/// Represents comprehensive dashboard data including financial summaries, budgets, and savings certificates.
/// </summary>
/// <param name="Year">The year of the dashboard data.</param>
/// <param name="Month">The month number (1-12) of the dashboard data.</param>
/// <param name="LiquidBalance">The current liquid balance.</param>
/// <param name="SavingsAndInvestments">The total value of savings and investments.</param>
/// <param name="TotalAssets">The total value of all assets.</param>
/// <param name="SavingsCertificatesValue">The current value of savings certificates.</param>
/// <param name="SavingsCertificatesYield">The yield from savings certificates.</param>
/// <param name="MaturedSavingsCertificates">The number of matured savings certificates.</param>
/// <param name="MaturedSavingsCertificatesValue">The total value of matured savings certificates.</param>
/// <param name="SavingsCertificatesFutureNetInterest">The projected net interest from savings certificates.</param>
/// <param name="SavingsCertificatesFutureValue">The projected future value of savings certificates.</param>
/// <param name="Income">The total income for the period.</param>
/// <param name="Expenses">The total expenses for the period.</param>
/// <param name="Budgeted">The total budgeted amount.</param>
/// <param name="BudgetActual">The actual amount spent against the budget.</param>
/// <param name="UnreconciledMovements">The number of unreconciled financial movements.</param>
/// <param name="Categories">The list of budget categories with actual and budgeted values.</param>
/// <param name="Evolution">The list of monthly financial evolution data.</param>
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
    /// <summary>
    /// Initializes a new instance of <see cref="DashboardDto"/> with savings certificates data set to default values.
    /// </summary>
    public DashboardDto(int year, int month, decimal liquidBalance, decimal savingsAndInvestments, decimal totalAssets,
        decimal income, decimal expenses, decimal budgeted, decimal budgetActual, int unreconciledMovements,
        IReadOnlyList<DashboardCategoryDto> categories, IReadOnlyList<DashboardMonthDto> evolution)
        : this(year, month, liquidBalance, savingsAndInvestments, totalAssets, 0m, 0m, 0, 0m, 0m, 0m, income, expenses,
            budgeted, budgetActual, unreconciledMovements, categories, evolution) { }

    /// <summary>
    /// Gets or sets the budget evolution data over multiple months.
    /// </summary>
    public IReadOnlyList<DashboardBudgetMonthDto> BudgetEvolution { get; init; } = [];

    /// <summary>
    /// Gets the monthly result (income minus expenses).
    /// </summary>
    public decimal MonthlyResult => Income - Expenses;

    /// <summary>
    /// Gets the available budget (budgeted amount minus actual spending).
    /// </summary>
    public decimal BudgetAvailable => Budgeted - BudgetActual;

    /// <summary>
    /// Gets the budget execution percentage. Returns null if no budget is defined.
    /// </summary>
    public decimal? BudgetExecution => Budgeted == 0 ? null : decimal.Round(BudgetActual / Budgeted * 100m, 1);
}
