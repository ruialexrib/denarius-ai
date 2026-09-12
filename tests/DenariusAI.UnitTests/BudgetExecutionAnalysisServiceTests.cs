using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>
/// Verifies deterministic classification and materiality rules for budget execution analysis.
/// </summary>
public sealed class BudgetExecutionAnalysisServiceTests
{
    /// <summary>
    /// Verifies planned versus actual classification, prioritisation and material findings.
    /// </summary>
    [Fact]
    public async Task GetAsyncClassifiesMaterialBudgetDeviations()
    {
        var housingId = Guid.NewGuid();
        var energyId = Guid.NewGuid();
        var leisureId = Guid.NewGuid();
        var service = new BudgetExecutionAnalysisService(new StubBudgetService(
            [new(Guid.NewGuid(), 2026, 9)],
            new Dictionary<(int Year, int Month), IReadOnlyList<BudgetExecutionItemDto>>
            {
                [(2026, 9)] =
                [
                    new(housingId, "Habitação", 600m, 610m),
                    new(energyId, "Energia", 100m, 130m),
                    new(leisureId, "Lazer", 0m, 50m),
                    new(Guid.NewGuid(), "Transportes", 200m, 175m),
                    new(Guid.NewGuid(), "Seguros", 80m, 0m)
                ]
            }));

        var result = await service.GetAsync(2026, 9);

        Assert.True(result.HasBudget);
        Assert.Equal(980m, result.TotalBudgeted);
        Assert.Equal(965m, result.TotalActual);
        Assert.Equal(-15m, result.TotalVariance);
        Assert.Equal(98.5m, result.ExecutionPercentage);
        Assert.Equal(90m, result.OverBudgetAmount);
        Assert.Equal(2, result.OverBudgetCategoryCount);
        Assert.Equal(1, result.UnbudgetedCategoryCount);
        Assert.Contains(result.Categories, item => item.CategoryId == energyId && item.Status == BudgetExecutionStatus.OverBudget);
        Assert.Contains(result.Categories, item => item.CategoryId == leisureId && item.Status == BudgetExecutionStatus.Unbudgeted);
        Assert.Contains(result.Findings, item => item.Title == "Maior desvio acima do orçamento");
        Assert.Contains(result.Findings, item => item.Title == "Despesa sem valor planeado");
    }

    /// <summary>
    /// Verifies near-limit and no-execution boundaries without inventing percentages for zero budgets.
    /// </summary>
    [Fact]
    public async Task GetAsyncHandlesNearLimitAndZeroBudgetBoundaries()
    {
        var nearId = Guid.NewGuid();
        var zeroId = Guid.NewGuid();
        var service = new BudgetExecutionAnalysisService(new StubBudgetService(
            [new(Guid.NewGuid(), 2026, 9)],
            new Dictionary<(int Year, int Month), IReadOnlyList<BudgetExecutionItemDto>>
            {
                [(2026, 9)] =
                [
                    new(nearId, "Alimentação", 100m, 85m),
                    new(Guid.NewGuid(), "Comunicações", 100m, 100m),
                    new(zeroId, "Outros", 0m, 20m),
                    new(Guid.NewGuid(), "Saúde", 75m, 0m)
                ]
            }));

        var result = await service.GetAsync(2026, 9);

        Assert.Contains(result.Categories, item => item.CategoryId == nearId && item.Status == BudgetExecutionStatus.NearLimit);
        Assert.Contains(result.Categories, item => item.CategoryName == "Comunicações" && item.Status == BudgetExecutionStatus.NearLimit && item.ExecutionPercentage == 100m);
        var unbudgeted = Assert.Single(result.Categories.Where(item => item.CategoryId == zeroId));
        Assert.Null(unbudgeted.ExecutionPercentage);
        Assert.Null(unbudgeted.RelativeVariancePercentage);
        Assert.Contains(result.Findings, item => item.Title == "Categoria próxima do limite");
    }

    /// <summary>
    /// Verifies repeated aggregate overruns are identified only from historical budget execution.
    /// </summary>
    [Fact]
    public async Task GetAsyncIdentifiesRepeatedHistoricalOverruns()
    {
        var rows = new Dictionary<(int Year, int Month), IReadOnlyList<BudgetExecutionItemDto>>();
        for (var month = 4; month <= 9; month++)
        {
            rows[(2026, month)] =
            [
                new(Guid.NewGuid(), "Categoria", 100m, month is 5 or 7 ? 120m : 90m)
            ];
        }

        var service = new BudgetExecutionAnalysisService(new StubBudgetService(
            Enumerable.Range(4, 6).Select(month => new BudgetPeriodDto(Guid.NewGuid(), 2026, month)).ToList(),
            rows));

        var result = await service.GetAsync(2026, 9);

        Assert.Equal(6, result.Trend.Count);
        Assert.Contains(result.Findings, item => item.Title == "Desvios globais repetidos");
    }

    /// <summary>
    /// Supplies fixed budget periods and execution rows to the analysis service.
    /// </summary>
    private sealed class StubBudgetService(
        IReadOnlyList<BudgetPeriodDto> periods,
        IReadOnlyDictionary<(int Year, int Month), IReadOnlyList<BudgetExecutionItemDto>> execution) : IBudgetService
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(periods);

        /// <inheritdoc />
        public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(execution.GetValueOrDefault((year, month), []));

        /// <inheritdoc />
        public Task SaveAsync(
            int year,
            int month,
            IReadOnlyCollection<SaveBudgetLineDto> lines,
            string userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
