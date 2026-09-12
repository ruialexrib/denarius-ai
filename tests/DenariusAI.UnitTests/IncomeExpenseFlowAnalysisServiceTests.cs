using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using DenariusAI.Application.Services;

namespace DenariusAI.UnitTests;

/// <summary>
/// Verifies deterministic interpretation rules for income, expense and account flows.
/// </summary>
public sealed class IncomeExpenseFlowAnalysisServiceTests
{
    /// <summary>
    /// Verifies equivalent-period comparisons, concentration and material category changes.
    /// </summary>
    [Fact]
    public async Task GetAsyncBuildsExecutiveMetricsAndFindings()
    {
        var expenseCategoryId = Guid.NewGuid();
        var incomeCategoryId = Guid.NewGuid();
        var data = new IncomeExpenseFlowDataDto(
            new(2026, 8, 1),
            new(2026, 8, 31),
            new(2026, 7, 1),
            new(2026, 7, 31),
            2000m,
            1500m,
            1600m,
            1000m,
            [],
            [new(incomeCategoryId, "Salário", 2000m, 1600m, 100m, 25m)],
            [],
            [new(expenseCategoryId, "Habitação", 750m, 500m, 50m, 50m)],
            [new(Guid.NewGuid(), "Conta à Ordem", 2000m, 1500m, 500m, 600m)],
            [new(2026, 8, 2000m, 1500m, 500m)],
            []);
        var service = new IncomeExpenseFlowAnalysisService(new StubRepository(data));

        var result = await service.GetAsync(new(2026, 8, 1), new(2026, 8, 31));

        Assert.Equal(2000m, result.IncomeMetric.CurrentValue);
        Assert.Equal(25m, result.IncomeMetric.PercentageChange);
        Assert.Equal(1500m, result.ExpenseMetric.CurrentValue);
        Assert.Equal(50m, result.ExpenseMetric.PercentageChange);
        Assert.Equal(500m, result.BalanceMetric.CurrentValue);
        Assert.Contains(result.Findings, item => item.Title == "Despesa concentrada numa categoria");
        Assert.Contains(result.Findings, item => item.Title == "Mudança relevante no padrão de despesa");
        Assert.Contains(result.Findings, item => item.Title == "Mudança relevante numa fonte de rendimento");
    }

    /// <summary>
    /// Verifies undefined percentage comparisons when the prior value is zero.
    /// </summary>
    [Fact]
    public async Task GetAsyncDoesNotInventPercentageWhenComparisonIsZero()
    {
        var data = new IncomeExpenseFlowDataDto(
            new(2026, 8, 1),
            new(2026, 8, 31),
            new(2026, 7, 1),
            new(2026, 7, 31),
            100m,
            0m,
            0m,
            0m,
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var service = new IncomeExpenseFlowAnalysisService(new StubRepository(data));

        var result = await service.GetAsync(new(2026, 8, 1), new(2026, 8, 31));

        Assert.Null(result.IncomeMetric.PercentageChange);
        Assert.Null(result.BalanceMetric.PercentageChange);
        Assert.Contains(result.Findings, item => item.Title == "O período gerou saldo positivo");
    }

    /// <summary>
    /// Supplies fixed repository data to the application service.
    /// </summary>
    /// <param name="data">Data returned to the service.</param>
    private sealed class StubRepository(IncomeExpenseFlowDataDto data) : IIncomeExpenseFlowRepository
    {
        /// <inheritdoc />
        public Task<IncomeExpenseFlowDataDto> GetAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default) => Task.FromResult(data);
    }
}
