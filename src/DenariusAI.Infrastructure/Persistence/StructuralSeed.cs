using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Provides seed data for the stable structural elements of the application: financial groups and categories.
/// These records are required for the application to operate and are independent of any demonstration scenario,
/// which is created exclusively by <see cref="DemonstrationDataService"/>.
/// </summary>
internal static class StructuralSeed
{
    /// <summary>
    /// The reference date used for all seed data timestamps.
    /// </summary>
    private static readonly DateTimeOffset SeedDate = new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The unique identifier for the Assets financial group.
    /// </summary>
    private static readonly Guid AssetsId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    /// <summary>
    /// The unique identifier for the Current Income financial group.
    /// </summary>
    private static readonly Guid CurrentIncomeId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    /// <summary>
    /// The unique identifier for the Extra Income financial group.
    /// </summary>
    private static readonly Guid ExtraIncomeId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    /// <summary>
    /// The unique identifier for the Current Expenses financial group.
    /// </summary>
    private static readonly Guid CurrentExpensesId = Guid.Parse("10000000-0000-0000-0000-000000000004");

    /// <summary>
    /// The unique identifier for the Extra Expenses financial group.
    /// </summary>
    private static readonly Guid ExtraExpensesId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    /// <summary>
    /// Gets the collection of financial groups.
    /// </summary>
    public static FinancialGroup[] Groups =>
    [
        Group(AssetsId, "Património e Poupanças", FinancialGroupKind.Asset, 1),
        Group(CurrentIncomeId, "Rendimentos Correntes", FinancialGroupKind.Income, 2),
        Group(ExtraIncomeId, "Rendimentos Extra", FinancialGroupKind.Income, 3),
        Group(CurrentExpensesId, "Despesas Correntes", FinancialGroupKind.Expense, 4),
        Group(ExtraExpensesId, "Despesas Extra", FinancialGroupKind.Expense, 5)
    ];

    /// <summary>
    /// Gets the collection of financial categories organized by group.
    /// </summary>
    public static Category[] Categories =>
    [
        Category(AssetsId, 1, "Conta à Ordem", 1),
        Category(AssetsId, 2, "Conta Poupança", 2),
        Category(AssetsId, 3, "Investimentos", 3),
        Category(AssetsId, 4, "Dinheiro", 4),
        Category(CurrentIncomeId, 10, "Salário", 1),
        Category(CurrentIncomeId, 11, "Subsídios", 2),
        Category(CurrentIncomeId, 12, "Pensões", 3),
        Category(ExtraIncomeId, 20, "Trabalhos ocasionais", 1),
        Category(ExtraIncomeId, 21, "Reembolsos", 2),
        Category(ExtraIncomeId, 22, "Prémios", 3),
        Category(CurrentExpensesId, 30, "Habitação", 1),
        Category(CurrentExpensesId, 31, "Água", 2),
        Category(CurrentExpensesId, 32, "Electricidade", 3),
        Category(CurrentExpensesId, 33, "Alimentação", 4),
        Category(CurrentExpensesId, 34, "Transportes", 5),
        Category(CurrentExpensesId, 35, "Saúde", 6),
        Category(CurrentExpensesId, 36, "Educação", 7),
        Category(CurrentExpensesId, 37, "Seguros", 8),
        Category(CurrentExpensesId, 38, "Lazer", 9),
        Category(CurrentExpensesId, 39, "Subscrições", 10),
        Category(ExtraExpensesId, 50, "Viagens", 1),
        Category(ExtraExpensesId, 51, "Reparações", 2),
        Category(ExtraExpensesId, 52, "Equipamentos", 3),
        Category(ExtraExpensesId, 53, "Compras extraordinárias", 4),
        Category(AssetsId, 1100, "Constituição de Poupanças", 5),
        Category(CurrentExpensesId, 4100, "Despesas com a casa", 11),
        Category(CurrentExpensesId, 4200, "Despesas com o carro e transportes", 12),
        Category(CurrentExpensesId, 4300, "Despesas Bancárias e Seguros", 13),
        Category(CurrentExpensesId, 4400, "Despesas com o Estado e Impostos", 14),
        Category(CurrentExpensesId, 4500, "Despesas com Compras", 15),
        Category(CurrentExpensesId, 4600, "Despesas com cuidados pessoais", 16),
        Category(CurrentExpensesId, 4700, "Despesas com Estudo e Formação", 17),
        Category(CurrentExpensesId, 4800, "Caixas e Fundo de Maneio", 18)
    ];

    /// <summary>
    /// Creates a financial group entity.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The group name.</param>
    /// <param name="kind">The kind of financial group.</param>
    /// <param name="order">The sort order.</param>
    /// <returns>A new <see cref="FinancialGroup"/> instance.</returns>
    private static FinancialGroup Group(Guid id, string name, FinancialGroupKind kind, int order) => new() { Id = id, Name = name, Kind = kind, SortOrder = order, CreatedAt = SeedDate };

    /// <summary>
    /// Creates a category entity.
    /// </summary>
    /// <param name="groupId">The parent financial group identifier.</param>
    /// <param name="code">The category code used in the identifier.</param>
    /// <param name="name">The category name.</param>
    /// <param name="order">The sort order.</param>
    /// <returns>A new <see cref="Category"/> instance.</returns>
    private static Category Category(Guid groupId, int code, string name, int order) => new() { Id = Guid.Parse($"20000000-0000-0000-0000-{code:D12}"), FinancialGroupId = groupId, Name = name, SortOrder = order, CreatedAt = SeedDate };
}
