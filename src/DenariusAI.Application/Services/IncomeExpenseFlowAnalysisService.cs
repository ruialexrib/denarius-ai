using System.Globalization;
using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>
/// Builds deterministic executive findings for income, expenses and account flows.
/// </summary>
/// <param name="repository">Repository that supplies authoritative classified flow data.</param>
public sealed class IncomeExpenseFlowAnalysisService(
    IIncomeExpenseFlowRepository repository) : IIncomeExpenseFlowAnalysisService
{
    private const decimal MaterialPercentageChange = 10m;
    private const decimal MaterialCategoryChange = 20m;
    private const decimal ConcentrationWeight = 40m;
    private const decimal MaterialAccountNetChange = 100m;
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-PT");

    /// <inheritdoc />
    public async Task<IncomeExpenseFlowAnalysisDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from == default || to == default || from > to)
            throw new ArgumentException("O intervalo de datas é inválido.");

        var data = await repository.GetAsync(from, to, cancellationToken);
        var incomeMetric = Metric("income", "Rendimentos", data.Income, data.PreviousIncome);
        var expenseMetric = Metric("expenses", "Despesas", data.Expenses, data.PreviousExpenses);
        var currentBalance = data.Income - data.Expenses;
        var previousBalance = data.PreviousIncome - data.PreviousExpenses;
        var balanceMetric = Metric("balance", "Saldo do período", currentBalance, previousBalance);

        return new(
            data,
            incomeMetric,
            expenseMetric,
            balanceMetric,
            BuildFindings(data, incomeMetric, expenseMetric, balanceMetric));
    }

    /// <summary>
    /// Creates one comparison metric from current and previous values.
    /// </summary>
    /// <param name="key">Stable metric key.</param>
    /// <param name="label">User-facing label.</param>
    /// <param name="current">Current-period value.</param>
    /// <param name="previous">Comparison-period value.</param>
    /// <returns>The deterministic metric.</returns>
    private static GlobalFinancialMetricDto Metric(
        string key,
        string label,
        decimal current,
        decimal previous)
    {
        var change = current - previous;
        decimal? percentage = previous == 0m
            ? null
            : decimal.Round(change / Math.Abs(previous) * 100m, 1);
        var direction = change > 0m
            ? FinancialMetricDirection.Increase
            : change < 0m
                ? FinancialMetricDirection.Decrease
                : FinancialMetricDirection.Unchanged;
        return new(key, label, current, previous, change, percentage, direction);
    }

    /// <summary>
    /// Builds prioritised deterministic findings from calculated flow data.
    /// </summary>
    /// <param name="data">Authoritative classified data.</param>
    /// <param name="income">Income metric.</param>
    /// <param name="expenses">Expense metric.</param>
    /// <param name="balance">Period balance metric.</param>
    /// <returns>Up to six executive findings.</returns>
    private static IReadOnlyList<IncomeExpenseFlowFindingDto> BuildFindings(
        IncomeExpenseFlowDataDto data,
        GlobalFinancialMetricDto income,
        GlobalFinancialMetricDto expenses,
        GlobalFinancialMetricDto balance)
    {
        var findings = new List<IncomeExpenseFlowFindingDto>();

        if (balance.CurrentValue < 0m)
        {
            findings.Add(new(
                "negative",
                "O período fechou com saldo negativo",
                $"As despesas excederam os rendimentos em {Money(Math.Abs(balance.CurrentValue))}."));
        }
        else if (balance.CurrentValue > 0m)
        {
            findings.Add(new(
                "positive",
                "O período gerou saldo positivo",
                $"Os rendimentos excederam as despesas em {Money(balance.CurrentValue)}."));
        }

        AddMetricChangeFinding(
            findings,
            income,
            "Os rendimentos aumentaram",
            "Os rendimentos diminuíram",
            positiveWhenIncrease: true);

        AddMetricChangeFinding(
            findings,
            expenses,
            "As despesas aumentaram",
            "As despesas diminuíram",
            positiveWhenIncrease: false);

        var concentratedExpense = data.ExpenseCategories
            .FirstOrDefault(item => item.Weight >= ConcentrationWeight);
        if (concentratedExpense is not null)
        {
            findings.Add(new(
                "warning",
                "Despesa concentrada numa categoria",
                $"{concentratedExpense.Name} representa {concentratedExpense.Weight.ToString("N1", PortugueseCulture)}% das despesas do período."));
        }

        var changingExpense = data.ExpenseCategories
            .Where(item => item.ChangePercentage.HasValue
                && Math.Abs(item.ChangePercentage.Value) >= MaterialCategoryChange)
            .OrderByDescending(item => Math.Abs(item.ChangePercentage!.Value))
            .FirstOrDefault();
        if (changingExpense is not null)
        {
            var direction = changingExpense.ChangePercentage!.Value > 0m ? "aumentou" : "diminuiu";
            findings.Add(new(
                changingExpense.ChangePercentage.Value > 0m ? "negative" : "positive",
                "Mudança relevante no padrão de despesa",
                $"{changingExpense.Name} {direction} {Math.Abs(changingExpense.ChangePercentage.Value).ToString("N1", PortugueseCulture)}% face ao período anterior."));
        }

        var changingIncome = data.IncomeCategories
            .Where(item => item.ChangePercentage.HasValue
                && Math.Abs(item.ChangePercentage.Value) >= MaterialCategoryChange)
            .OrderByDescending(item => Math.Abs(item.ChangePercentage!.Value))
            .FirstOrDefault();
        if (changingIncome is not null)
        {
            var direction = changingIncome.ChangePercentage!.Value > 0m ? "aumentou" : "diminuiu";
            findings.Add(new(
                changingIncome.ChangePercentage.Value > 0m ? "positive" : "negative",
                "Mudança relevante numa fonte de rendimento",
                $"{changingIncome.Name} {direction} {Math.Abs(changingIncome.ChangePercentage.Value).ToString("N1", PortugueseCulture)}% face ao período anterior."));
        }

        var accountChange = data.AccountFlows
            .Select(item => new
            {
                item.Name,
                Change = item.NetFlow - item.PreviousNetFlow,
                item.NetFlow
            })
            .Where(item => Math.Abs(item.Change) >= MaterialAccountNetChange)
            .OrderByDescending(item => Math.Abs(item.Change))
            .FirstOrDefault();
        if (accountChange is not null)
        {
            findings.Add(new(
                "info",
                "Alteração relevante no fluxo de uma conta",
                $"{accountChange.Name} apresenta um fluxo líquido de {SignedMoney(accountChange.NetFlow)}, com uma variação de {SignedMoney(accountChange.Change)} face ao período anterior."));
        }

        if (findings.Count == 0)
        {
            findings.Add(new(
                "info",
                "Sem alterações materiais detetadas",
                "Os principais rendimentos, despesas e fluxos não ultrapassaram os limiares de materialidade desta análise."));
        }

        return findings.Take(6).ToList();
    }

    /// <summary>
    /// Adds a finding when an aggregate metric exceeds the deterministic percentage threshold.
    /// </summary>
    /// <param name="findings">Destination collection.</param>
    /// <param name="metric">Metric being evaluated.</param>
    /// <param name="increaseTitle">Title for an increase.</param>
    /// <param name="decreaseTitle">Title for a decrease.</param>
    /// <param name="positiveWhenIncrease">Whether an increase is semantically positive.</param>
    private static void AddMetricChangeFinding(
        ICollection<IncomeExpenseFlowFindingDto> findings,
        GlobalFinancialMetricDto metric,
        string increaseTitle,
        string decreaseTitle,
        bool positiveWhenIncrease)
    {
        if (!metric.PercentageChange.HasValue
            || Math.Abs(metric.PercentageChange.Value) < MaterialPercentageChange)
            return;

        var increase = metric.Direction == FinancialMetricDirection.Increase;
        findings.Add(new(
            increase == positiveWhenIncrease ? "positive" : "negative",
            increase ? increaseTitle : decreaseTitle,
            $"{metric.Label}: {Money(metric.CurrentValue)} ({SignedPercentage(metric.PercentageChange.Value)} face ao período anterior)."));
    }

    /// <summary>
    /// Formats a monetary value for deterministic findings.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <returns>Portuguese formatted euro amount.</returns>
    private static string Money(decimal value) =>
        $"{value.ToString("N2", PortugueseCulture)} €";

    /// <summary>
    /// Formats a signed monetary value.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <returns>Signed Portuguese formatted euro amount.</returns>
    private static string SignedMoney(decimal value) =>
        $"{(value > 0m ? "+" : string.Empty)}{value.ToString("N2", PortugueseCulture)} €";

    /// <summary>
    /// Formats a signed percentage.
    /// </summary>
    /// <param name="value">Percentage to format.</param>
    /// <returns>Signed percentage text.</returns>
    private static string SignedPercentage(decimal value) =>
        $"{(value > 0m ? "+" : string.Empty)}{value.ToString("N1", PortugueseCulture)}%";
}
