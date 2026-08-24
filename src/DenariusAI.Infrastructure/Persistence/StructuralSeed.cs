using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Infrastructure.Persistence;

internal static class StructuralSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid AssetsId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CurrentIncomeId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ExtraIncomeId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid CurrentExpensesId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid ExtraExpensesId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    public static FinancialGroup[] Groups =>
    [
        Group(AssetsId, "Património e Poupanças", FinancialGroupKind.Asset, 1),
        Group(CurrentIncomeId, "Rendimentos Correntes", FinancialGroupKind.Income, 2),
        Group(ExtraIncomeId, "Rendimentos Extra", FinancialGroupKind.Income, 3),
        Group(CurrentExpensesId, "Despesas Correntes", FinancialGroupKind.Expense, 4),
        Group(ExtraExpensesId, "Despesas Extra", FinancialGroupKind.Expense, 5)
    ];

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

    public static Account[] Accounts =>
    [
        new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "Conta à Ordem — Demonstração",
            Description = "Conta bancária principal do cenário de demonstração.",
            AccountType = AccountType.BankAccount,
            InitialBalance = 0m,
            Currency = "EUR",
            CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            CreatedAt = SeedDate
        },
        Account(2, "Conta Poupança — Demonstração", "Poupança familiar do cenário de demonstração.", AccountType.Savings, 500m, 2),
        Account(3, "Dinheiro — Demonstração", "Carteira de numerário do cenário de demonstração.", AccountType.Cash, 100m, 4),
        Account(4, "Rendimentos — Demonstração", "Contrapartida contabilística dos rendimentos.", AccountType.Income, 0m, 10),
        Account(5, "Despesas — Demonstração", "Contrapartida contabilística das despesas.", AccountType.Expense, 0m, 33)
    ];

    public static object[] JournalEntries =>
    [
        Entry(1, 1, "Salário mensal", "REC-JUL-001"), Entry(2, 3, "Renda da casa", "PAG-JUL-001"),
        Entry(3, 5, "Compras de supermercado", "TALAO-1842"), Entry(4, 7, "Fatura de eletricidade", "ELEC-0726"),
        Entry(5, 8, "Fatura de água", "AGUA-0726"), Entry(6, 10, "Transferência para poupança", "TRF-POUP"),
        Entry(7, 12, "Levantamento ATM", "ATM-1208"), Entry(8, 15, "Jantar em família", "REST-1508"),
        Entry(9, 18, "Trabalho ocasional", "FREELANCE-07"), Entry(10, 20, "Viagem de verão", "VIAGEM-2026")
    ];

    public static object[] JournalEntryLines =>
    [
        Line(1, 1, 1, 2500m, 0m, null), Line(2, 1, 4, 0m, 2500m, 10),
        Line(3, 2, 5, 750m, 0m, 30), Line(4, 2, 1, 0m, 750m, null),
        Line(5, 3, 5, 180m, 0m, 33), Line(6, 3, 1, 0m, 180m, null),
        Line(7, 4, 5, 65m, 0m, 32), Line(8, 4, 1, 0m, 65m, null),
        Line(9, 5, 5, 32m, 0m, 31), Line(10, 5, 1, 0m, 32m, null),
        Line(11, 6, 2, 300m, 0m, 2), Line(12, 6, 1, 0m, 300m, 1100),
        Line(13, 7, 3, 100m, 0m, 4), Line(14, 7, 1, 0m, 100m, 4),
        Line(15, 8, 5, 80m, 0m, 38), Line(16, 8, 1, 0m, 80m, null),
        Line(17, 9, 1, 350m, 0m, null), Line(18, 9, 4, 0m, 350m, 20),
        Line(19, 10, 5, 450m, 0m, 50), Line(20, 10, 1, 0m, 450m, null)
    ];

    public static Budget[] Budgets => [new() { Id = Guid.Parse("60000000-0000-0000-0000-000000000001"), Year = 2026, Month = 7, CreatedAt = SeedDate, CreatedBy = "demo-seed" }];

    public static BudgetLine[] BudgetLines =>
    [
        BudgetLine(1, 30, 700m), BudgetLine(2, 31, 35m), BudgetLine(3, 32, 70m), BudgetLine(4, 33, 250m),
        BudgetLine(5, 34, 150m), BudgetLine(6, 35, 75m), BudgetLine(7, 37, 100m), BudgetLine(8, 38, 60m),
        BudgetLine(9, 39, 30m), BudgetLine(10, 50, 300m)
    ];

    public static Reconciliation[] Reconciliations =>
    [
        Reconciliation(1, 1), Reconciliation(2, 2), Reconciliation(3, 4), Reconciliation(4, 5)
    ];

    private static FinancialGroup Group(Guid id, string name, FinancialGroupKind kind, int order) => new() { Id = id, Name = name, Kind = kind, SortOrder = order, CreatedAt = SeedDate };
    private static Category Category(Guid groupId, int code, string name, int order) => new() { Id = Guid.Parse($"20000000-0000-0000-0000-{code:D12}"), FinancialGroupId = groupId, Name = name, SortOrder = order, CreatedAt = SeedDate };
    private static Account Account(int id, string name, string description, AccountType type, decimal balance, int categoryCode) => new() { Id = Guid.Parse($"30000000-0000-0000-0000-{id:D12}"), Name = name, Description = description, AccountType = type, InitialBalance = balance, Currency = "EUR", CategoryId = Guid.Parse($"20000000-0000-0000-0000-{categoryCode:D12}"), CreatedAt = SeedDate };
    private static object Entry(int id, int day, string description, string reference) => new { Id = Guid.Parse($"40000000-0000-0000-0000-{id:D12}"), BudgetId = (Guid?)Guid.Parse("60000000-0000-0000-0000-000000000001"), Date = new DateOnly(2026, 7, day), Description = description, Reference = reference, Notes = "Dados de demonstração — julho 2026", Status = JournalEntryStatus.Active, CancelledAt = (DateTimeOffset?)null, CancelledBy = (string?)null, CreatedAt = SeedDate, CreatedBy = "demo-seed", UpdatedAt = (DateTimeOffset?)null, UpdatedBy = (string?)null };
    private static object Line(int id, int entryId, int accountId, decimal debit, decimal credit, int? categoryCode) => new { Id = Guid.Parse($"50000000-0000-0000-0000-{id:D12}"), JournalEntryId = Guid.Parse($"40000000-0000-0000-0000-{entryId:D12}"), AccountId = Guid.Parse($"30000000-0000-0000-0000-{accountId:D12}"), CategoryId = categoryCode.HasValue ? Guid.Parse($"20000000-0000-0000-0000-{categoryCode.Value:D12}") : (Guid?)null, Debit = debit, Credit = credit, Description = (string?)null, CreatedAt = SeedDate, CreatedBy = "demo-seed", UpdatedAt = (DateTimeOffset?)null, UpdatedBy = (string?)null };
    private static BudgetLine BudgetLine(int id, int categoryCode, decimal amount) => new() { Id = Guid.Parse($"70000000-0000-0000-0000-{id:D12}"), BudgetId = Guid.Parse("60000000-0000-0000-0000-000000000001"), CategoryId = Guid.Parse($"20000000-0000-0000-0000-{categoryCode:D12}"), Amount = amount, CreatedAt = SeedDate, CreatedBy = "demo-seed" };
    private static Reconciliation Reconciliation(int id, int entryId) => new() { Id = Guid.Parse($"80000000-0000-0000-0000-{id:D12}"), JournalEntryId = Guid.Parse($"40000000-0000-0000-0000-{entryId:D12}"), Status = ReconciliationStatus.Reconciled, ReconciledAt = SeedDate.AddDays(id), ReconciledBy = "demo-seed", CreatedAt = SeedDate, CreatedBy = "demo-seed" };
}
