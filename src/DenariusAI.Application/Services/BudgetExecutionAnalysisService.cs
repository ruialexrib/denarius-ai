using System.Globalization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>
/// Calculates monthly budget execution, deviations and deterministic risk indicators.
/// </summary>
/// <param name="budgetService">Service that supplies authoritative budget execution data.</param>
public sealed class BudgetExecutionAnalysisService(
    IBudgetService budgetService) : IBudgetExecutionAnalysisService
{
    private const decimal NearLimitExecutionPercentage = 85m;
    private const decimal MaterialVarianceAmount = 25m;
    private const decimal MaterialVariancePercentage = 10m;
    private const int HistoryMonths = 6;
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-PT");

    /// <inheritdoc />
    public async Task<BudgetExecutionAnalysisDto> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 2200 || month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "O período orçamental é inválido.");

        var periods = await budgetService.ListPeriodsAsync(cancellationToken);
        var hasBudget = periods.Any(item => item.Year == year && item.Month == month);
        var execution = await budgetService.GetExecutionAsync(year, month, cancellationToken);
        var categories = execution
            .Select(AnalyseCategory)
            .Where(item => item.Budgeted != 0m || item.Actual != 0m)
            .OrderByDescending(Relevance)
            .ThenBy(item => item.FinancialGroupName)
            .ThenBy(item => item.CategoryName)
            .ToList();

        var totalBudgeted = categories.Sum(item => item.Budgeted);
        var totalActual = categories.Sum(item => item.Actual);
        var totalVariance = totalActual - totalBudgeted;
        decimal? overallExecution = totalBudgeted == 0m
            ? null
            : decimal.Round(totalActual / totalBudgeted * 100m, 1);

        var overBudgetAmount = categories
            .Where(item => item.Status is BudgetExecutionStatus.OverBudget or BudgetExecutionStatus.Unbudgeted)
            .Sum(item => item.Status == BudgetExecutionStatus.Unbudgeted ? item.Actual : Math.Max(item.Variance, 0m));

        var trend = await BuildTrendAsync(year, month, cancellationToken);
        var findings = BuildFindings(categories, trend, totalBudgeted, totalActual, totalVariance, overallExecution);

        return new(
            year,
            month,
            hasBudget,
            totalBudgeted,
            totalActual,
            totalVariance,
            overallExecution,
            overBudgetAmount,
            categories.Count(item => item.Status == BudgetExecutionStatus.OverBudget),
            categories.Count(item => item.Status == BudgetExecutionStatus.NearLimit),
            categories.Count(item => item.Status == BudgetExecutionStatus.Unbudgeted),
            categories,
            trend,
            findings);
    }

    /// <summary>
    /// Converts one authoritative execution item into a classified analysis row.
    /// </summary>
    /// <param name="item">Authoritative category execution item.</param>
    /// <returns>The analysed category row.</returns>
    private static BudgetExecutionCategoryAnalysisDto AnalyseCategory(BudgetExecutionItemDto item)
    {
        decimal? relativeVariance = item.Budgeted == 0m
            ? null
            : decimal.Round(item.Variance / item.Budgeted * 100m, 1);

        var status = item.Budgeted == 0m && item.Actual > 0m
            ? BudgetExecutionStatus.Unbudgeted
            : item.Budgeted > 0m && item.Actual == 0m
                ? BudgetExecutionStatus.NoExecution
                : item.Budgeted > 0m && item.Actual > item.Budgeted
                    ? BudgetExecutionStatus.OverBudget
                    : item.ExecutionPercentage >= NearLimitExecutionPercentage
                        ? BudgetExecutionStatus.NearLimit
                        : BudgetExecutionStatus.WithinBudget;

        return new(
            item.CategoryId,
            item.CategoryName,
            item.FinancialGroupId,
            item.FinancialGroupName,
            item.Budgeted,
            item.Actual,
            item.Variance,
            relativeVariance,
            item.ExecutionPercentage,
            status,
            StatusLabel(status));
    }

    /// <summary>
    /// Builds recent monthly budget totals ending in the selected period.
    /// </summary>
    /// <param name="year">Selected year.</param>
    /// <param name="month">Selected month.</param>
    /// <param name="cancellationToken">Token used to cancel repository queries.</param>
    /// <returns>Six months of deterministic budget history.</returns>
    private async Task<IReadOnlyList<BudgetExecutionTrendDto>> BuildTrendAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var selected = new DateOnly(year, month, 1);
        var result = new List<BudgetExecutionTrendDto>();
        for (var offset = HistoryMonths - 1; offset >= 0; offset--)
        {
            var period = selected.AddMonths(-offset);
            var rows = await budgetService.GetExecutionAsync(period.Year, period.Month, cancellationToken);
            var relevant = rows.Where(item => item.Budgeted != 0m || item.Actual != 0m).ToList();
            var budgeted = relevant.Sum(item => item.Budgeted);
            var actual = relevant.Sum(item => item.Actual);
            result.Add(new(
                period.Year,
                period.Month,
                budgeted,
                actual,
                actual - budgeted,
                budgeted == 0m ? null : decimal.Round(actual / budgeted * 100m, 1)));
        }

        return result;
    }

    /// <summary>
    /// Creates the most material budget execution findings without duplicating spending-pattern analysis.
    /// </summary>
    /// <param name="categories">Classified category execution.</param>
    /// <param name="trend">Recent budget execution history.</param>
    /// <param name="totalBudgeted">Total planned amount.</param>
    /// <param name="totalActual">Total executed amount.</param>
    /// <param name="totalVariance">Total variance.</param>
    /// <param name="overallExecution">Overall execution percentage.</param>
    /// <returns>Up to six prioritised budget findings.</returns>
    private static IReadOnlyList<BudgetExecutionFindingDto> BuildFindings(
        IReadOnlyList<BudgetExecutionCategoryAnalysisDto> categories,
        IReadOnlyList<BudgetExecutionTrendDto> trend,
        decimal totalBudgeted,
        decimal totalActual,
        decimal totalVariance,
        decimal? overallExecution)
    {
        var findings = new List<BudgetExecutionFindingDto>();

        if (totalBudgeted == 0m && totalActual == 0m)
        {
            findings.Add(new("info", "Sem execução orçamental", "Não existem valores planeados nem executados no período selecionado."));
            return findings;
        }

        if (totalBudgeted > 0m && totalVariance > 0m)
        {
            findings.Add(new(
                "negative",
                "Orçamento global ultrapassado",
                $"A execução atingiu {Percentage(overallExecution)} e excede o planeado em {Money(totalVariance)}."));
        }
        else if (totalBudgeted > 0m)
        {
            findings.Add(new(
                overallExecution >= NearLimitExecutionPercentage ? "warning" : "positive",
                "Execução global dentro do orçamento",
                $"Foram executados {Money(totalActual)} de {Money(totalBudgeted)} planeados ({Percentage(overallExecution)})."));
        }

        var largestOverrun = categories
            .Where(item => item.Status == BudgetExecutionStatus.OverBudget
                && IsMaterial(item))
            .OrderByDescending(item => item.Variance)
            .FirstOrDefault();
        if (largestOverrun is not null)
        {
            findings.Add(new(
                "negative",
                "Maior desvio acima do orçamento",
                $"{largestOverrun.CategoryName} excede o planeado em {Money(largestOverrun.Variance)} ({SignedPercentage(largestOverrun.RelativeVariancePercentage)}).",
                largestOverrun.CategoryId));
        }

        var unbudgeted = categories
            .Where(item => item.Status == BudgetExecutionStatus.Unbudgeted)
            .OrderByDescending(item => item.Actual)
            .FirstOrDefault();
        if (unbudgeted is not null)
        {
            findings.Add(new(
                "warning",
                "Despesa sem valor planeado",
                $"{unbudgeted.CategoryName} regista {Money(unbudgeted.Actual)} de execução sem montante orçamentado.",
                unbudgeted.CategoryId));
        }

        var nearLimit = categories
            .Where(item => item.Status == BudgetExecutionStatus.NearLimit)
            .OrderByDescending(item => item.ExecutionPercentage)
            .FirstOrDefault();
        if (nearLimit is not null)
        {
            findings.Add(new(
                "warning",
                "Categoria próxima do limite",
                $"{nearLimit.CategoryName} já executou {Percentage(nearLimit.ExecutionPercentage)} do respetivo orçamento.",
                nearLimit.CategoryId));
        }

        var largestUnder = categories
            .Where(item => item.Budgeted > 0m && item.Variance < 0m && IsMaterial(item))
            .OrderBy(item => item.Variance)
            .FirstOrDefault();
        if (largestUnder is not null)
        {
            findings.Add(new(
                "info",
                "Maior valor ainda por executar",
                $"{largestUnder.CategoryName} está {Money(Math.Abs(largestUnder.Variance))} abaixo do montante planeado.",
                largestUnder.CategoryId));
        }

        var priorOverruns = trend
            .Take(Math.Max(0, trend.Count - 1))
            .Count(item => item.Budgeted > 0m && item.Actual > item.Budgeted);
        if (priorOverruns >= 2)
        {
            findings.Add(new(
                "warning",
                "Desvios globais repetidos",
                $"O orçamento global foi ultrapassado em {priorOverruns} dos {Math.Max(0, trend.Count - 1)} meses anteriores apresentados."));
        }

        return findings.Take(6).ToList();
    }

    /// <summary>
    /// Determines whether a category deviation is material enough for executive findings.
    /// </summary>
    /// <param name="item">Category execution row.</param>
    /// <returns>True when amount and percentage thresholds are met.</returns>
    private static bool IsMaterial(BudgetExecutionCategoryAnalysisDto item) =>
        Math.Abs(item.Variance) >= MaterialVarianceAmount
        && (!item.RelativeVariancePercentage.HasValue
            || Math.Abs(item.RelativeVariancePercentage.Value) >= MaterialVariancePercentage);

    /// <summary>
    /// Calculates a stable ranking score that prioritises overspends and relevant deviations.
    /// </summary>
    /// <param name="item">Category execution row.</param>
    /// <returns>Ranking score.</returns>
    private static decimal Relevance(BudgetExecutionCategoryAnalysisDto item) =>
        item.Status switch
        {
            BudgetExecutionStatus.Unbudgeted => 400000m + item.Actual,
            BudgetExecutionStatus.OverBudget => 300000m + Math.Abs(item.Variance),
            BudgetExecutionStatus.NearLimit => 200000m + (item.ExecutionPercentage ?? 0m),
            BudgetExecutionStatus.NoExecution => 100000m + item.Budgeted,
            _ => Math.Abs(item.Variance)
        };

    /// <summary>
    /// Gets the European Portuguese label for one execution status.
    /// </summary>
    /// <param name="status">Execution status.</param>
    /// <returns>User-facing label.</returns>
    private static string StatusLabel(BudgetExecutionStatus status) => status switch
    {
        BudgetExecutionStatus.WithinBudget => "Dentro do orçamento",
        BudgetExecutionStatus.NearLimit => "Próximo do limite",
        BudgetExecutionStatus.OverBudget => "Acima do orçamento",
        BudgetExecutionStatus.Unbudgeted => "Sem orçamento",
        BudgetExecutionStatus.NoExecution => "Sem execução",
        _ => "Indeterminado"
    };

    /// <summary>
    /// Formats one euro amount for deterministic findings.
    /// </summary>
    /// <param name="value">Amount to format.</param>
    /// <returns>Portuguese euro text.</returns>
    private static string Money(decimal value) => $"{value.ToString("N2", PortugueseCulture)} €";

    /// <summary>
    /// Formats an optional percentage.
    /// </summary>
    /// <param name="value">Percentage value.</param>
    /// <returns>Percentage text or not-applicable marker.</returns>
    private static string Percentage(decimal? value) =>
        value.HasValue ? $"{value.Value.ToString("N1", PortugueseCulture)}%" : "—";

    /// <summary>
    /// Formats an optional signed percentage.
    /// </summary>
    /// <param name="value">Percentage value.</param>
    /// <returns>Signed percentage text or not-applicable marker.</returns>
    private static string SignedPercentage(decimal? value) =>
        value.HasValue
            ? $"{(value.Value > 0m ? "+" : string.Empty)}{value.Value.ToString("N1", PortugueseCulture)}%"
            : "—";
}
