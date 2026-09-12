using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Calculates savings capacity, liquidity and resource concentration for a selected interval.</summary>
/// <param name="financialReportDataService">Authoritative source for balances, monthly flows and Savings Certificates.</param>
public sealed class SavingsLiquidityAnalysisService(
    IFinancialReportDataService financialReportDataService) : ISavingsLiquidityAnalysisService
{
    /// <inheritdoc />
    public async Task<SavingsLiquidityAnalysisDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to) throw new ArgumentException("O intervalo de datas é inválido.", nameof(from));

        var days = to.DayNumber - from.DayNumber + 1;
        var comparisonTo = from.AddDays(-1);
        var comparisonFrom = comparisonTo.AddDays(-(days - 1));
        var current = await financialReportDataService.GetAsync(from, to, cancellationToken);
        var previous = await financialReportDataService.GetAsync(comparisonFrom, comparisonTo, cancellationToken);

        var liquid = current.Accounts
            .Where(account => account.IsActive
                && account.Type is "BankAccount" or "Cash" or "Savings")
            .Select(account => new SavingsLiquidityAccountDto(
                account.Id,
                account.Name,
                account.Type,
                account.Currency,
                account.BalanceAtEnd,
                null))
            .ToList();

        var eurLiquid = liquid.Where(item => item.Currency == "EUR").Sum(item => item.Balance);
        var weightedLiquid = liquid.Select(item => item with
        {
            Weight = item.Currency == "EUR" && eurLiquid != 0m
                ? decimal.Round(item.Balance / eurLiquid * 100m, 1)
                : null
        }).OrderByDescending(item => item.Currency == "EUR" ? item.Balance : decimal.MinValue)
            .ThenBy(item => item.Name)
            .ToList();

        decimal? currentSavingsRate = current.Income == 0m
            ? null
            : decimal.Round(current.Savings / current.Income * 100m, 1);
        decimal? previousSavingsRate = previous.Income == 0m
            ? null
            : decimal.Round(previous.Savings / previous.Income * 100m, 1);
        var certificatesValue = current.SavingsCertificates.Sum(item => item.CurrentValue);
        var certificatesYield = current.SavingsCertificates.Sum(item => item.Yield);

        var trend = current.Months.Select(month => new SavingsLiquidityTrendDto(
            month.Year,
            month.Month,
            month.Income,
            month.Expenses,
            month.Savings,
            month.Income == 0m ? null : decimal.Round(month.Savings / month.Income * 100m, 1)))
            .ToList();

        var largestWeight = weightedLiquid
            .Where(item => item.Currency == "EUR")
            .Select(item => item.Weight)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .DefaultIfEmpty()
            .Max();
        decimal? largestWeightValue = eurLiquid == 0m ? null : largestWeight;

        var findings = BuildFindings(
            current.Savings,
            currentSavingsRate,
            previous.Savings,
            eurLiquid,
            largestWeightValue,
            weightedLiquid.Count(item => item.Currency != "EUR"),
            certificatesValue);

        return new(
            from,
            to,
            comparisonFrom,
            comparisonTo,
            current.Savings,
            currentSavingsRate,
            previous.Savings,
            previousSavingsRate,
            eurLiquid,
            weightedLiquid.Where(item => item.Type == "Savings" && item.Currency == "EUR").Sum(item => item.Balance),
            certificatesValue,
            certificatesYield,
            largestWeightValue,
            weightedLiquid.Count(item => item.Currency != "EUR"),
            weightedLiquid,
            trend,
            findings);
    }

    /// <summary>Builds prioritised deterministic findings for savings capacity and liquidity.</summary>
    /// <param name="savings">Savings in the selected period.</param>
    /// <param name="savingsRate">Savings rate when meaningful.</param>
    /// <param name="previousSavings">Savings in the immediately previous equivalent period.</param>
    /// <param name="eurLiquidity">Immediate EUR liquidity.</param>
    /// <param name="largestWeight">Largest EUR liquid-account concentration.</param>
    /// <param name="nonEurAccounts">Number of liquid accounts excluded from the EUR aggregate.</param>
    /// <param name="certificateValue">Current Savings Certificate value.</param>
    /// <returns>Prioritised findings.</returns>
    private static IReadOnlyList<SpecializedAnalysisFindingDto> BuildFindings(
        decimal savings,
        decimal? savingsRate,
        decimal previousSavings,
        decimal eurLiquidity,
        decimal? largestWeight,
        int nonEurAccounts,
        decimal certificateValue)
    {
        var findings = new List<SpecializedAnalysisFindingDto>();
        if (savings > 0m)
        {
            findings.Add(new(
                "positive",
                "Capacidade de poupança positiva",
                $"O período gerou uma poupança de {savings:N2} €{(savingsRate.HasValue ? $" ({savingsRate.Value:N1}% dos rendimentos)." : ".")}"));
        }
        else if (savings < 0m)
        {
            findings.Add(new(
                "negative",
                "Consumo acima dos rendimentos",
                $"As despesas excederam os rendimentos em {Math.Abs(savings):N2} € no período selecionado."));
        }

        var change = savings - previousSavings;
        if (Math.Abs(change) >= 25m)
        {
            findings.Add(new(
                change > 0m ? "positive" : "warning",
                "Alteração da poupança face ao período anterior",
                $"A poupança {(change > 0m ? "aumentou" : "diminuiu")} {Math.Abs(change):N2} € face ao intervalo imediatamente anterior."));
        }

        if (eurLiquidity <= 0m)
        {
            findings.Add(new("warning", "Liquidez EUR sem margem positiva", "As contas líquidas em EUR não apresentam uma margem positiva na data final do período."));
        }
        else if (largestWeight >= 70m)
        {
            findings.Add(new(
                "warning",
                "Liquidez concentrada",
                $"A maior conta líquida representa {largestWeight.Value:N1}% da liquidez EUR acompanhada."));
        }

        if (certificateValue > 0m)
        {
            findings.Add(new(
                "info",
                "Poupança aplicada em Certificados de Aforro",
                $"Os Certificados de Aforro acompanhados totalizam atualmente {certificateValue:N2} €.",
                "SavingsCertificates",
                "Index"));
        }

        if (nonEurAccounts > 0)
        {
            findings.Add(new(
                "info",
                "Existem saldos noutras moedas",
                $"{nonEurAccounts} conta(s) líquida(s) noutra moeda são apresentadas separadamente e não são convertidas para o total EUR."));
        }

        return findings.Take(6).ToList();
    }
}
