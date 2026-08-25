using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Provides seed data for the structural elements of the application, including financial groups, categories, accounts, journal entries, budgets, and reminders.
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
    /// Gets the collection of demonstration reminders.
    /// </summary>
    public static object[] Reminders =>
    [
        Reminder(1, "Confirmar a próxima capitalização dos Certificados de Aforro", new DateOnly(2026, 8, 28), 7),
        Reminder(2, "Rever e renovar o seguro automóvel", new DateOnly(2026, 9, 15), 15),
        Reminder(3, "Preparar o orçamento familiar do próximo ano", new DateOnly(2026, 12, 15), 30)
    ];

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
    /// Gets the collection of demonstration accounts.
    /// </summary>
    public static Account[] Accounts =>
    [
        new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "Conta à Ordem — Demonstração",
            Description = "Conta bancária principal do cenário de demonstração.",
            AccountType = AccountType.BankAccount,
            InitialBalance = 1850m,
            Currency = "EUR",
            CategoryId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            CreatedAt = SeedDate
        },
        Account(2, "Conta Poupança — Demonstração", "Poupança familiar do cenário de demonstração.", AccountType.Savings, 4200m, 2),
        Account(3, "Dinheiro — Demonstração", "Carteira de numerário do cenário de demonstração.", AccountType.Cash, 120m, 4),
        Account(4, "Rendimentos — Demonstração", "Contrapartida contabilística dos rendimentos.", AccountType.Income, 0m, 10),
        Account(5, "Despesas — Demonstração", "Contrapartida contabilística das despesas.", AccountType.Expense, 0m, 33)
    ];

    /// <summary>
    /// Gets the collection of demonstration journal entries.
    /// </summary>
    public static object[] JournalEntries => CreateJournalEntries();

    /// <summary>
    /// Gets the collection of journal entry lines for all demonstration entries.
    /// </summary>
    public static object[] JournalEntryLines => CreateJournalEntryLines();

    /// <summary>
    /// Gets the collection of budgets for 8 months of the demonstration period.
    /// </summary>
    public static Budget[] Budgets => Enumerable.Range(1, 8).Select(month => new Budget { Id = BudgetId(month), Year = 2026, Month = month, CreatedAt = SeedDate, CreatedBy = "demo-seed" }).ToArray();

    /// <summary>
    /// Gets the collection of budget lines for all demonstration budgets.
    /// </summary>
    public static BudgetLine[] BudgetLines => Enumerable.Range(1, 8).SelectMany(month => new[]
    {
        BudgetLine(month, 1, 30, 780m), BudgetLine(month, 2, 31, 40m), BudgetLine(month, 3, 32, 85m),
        BudgetLine(month, 4, 33, 320m), BudgetLine(month, 5, 34, 140m), BudgetLine(month, 6, 35, 90m),
        BudgetLine(month, 7, 37, 110m), BudgetLine(month, 8, 38, 120m), BudgetLine(month, 9, 39, 35m)
    }).ToArray();

    /// <summary>
    /// Gets the collection of reconciliations for demonstration journal entries.
    /// </summary>
    public static Reconciliation[] Reconciliations => Enumerable.Range(1, 8).SelectMany(month => Enumerable.Range(1, 6).Select(slot => Reconciliation(((month - 1) * 6) + slot, EntryId(month, slot)))).ToArray();

    /// <summary>
    /// Creates the demonstration journal entries for 8 months with 9 entries per month.
    /// </summary>
    /// <returns>An array of anonymous objects representing journal entries.</returns>
    private static object[] CreateJournalEntries() => Enumerable.Range(1, 8).SelectMany(month => new[]
    {
        Entry(month, 1, 1, "Salário mensal", $"SAL-2026-{month:D2}"), Entry(month, 2, 3, "Renda da casa", $"RENDA-{month:D2}"),
        Entry(month, 3, 6, "Compras de supermercado", $"SUPER-{month:D2}"), Entry(month, 4, 8, "Fatura de eletricidade", $"ELEC-{month:D2}"),
        Entry(month, 5, 9, "Fatura de água", $"AGUA-{month:D2}"), Entry(month, 6, 12, "Passe e combustível", $"TRANSP-{month:D2}"),
        Entry(month, 7, 15, "Transferência para poupança", $"POUP-{month:D2}"), Entry(month, 8, 20, "Lazer em família", $"LAZER-{month:D2}"),
        Entry(month, 9, 24, "Trabalho ocasional", $"EXTRA-{month:D2}")
    }).ToArray();

    /// <summary>
    /// Creates the journal entry lines (debits and credits) for all demonstration entries.
    /// </summary>
    /// <returns>An array of anonymous objects representing journal entry lines.</returns>
    private static object[] CreateJournalEntryLines() => Enumerable.Range(1, 8).SelectMany(month =>
    {
        var values = new[] { 2650m, 780m, 210m + month * 4m, 62m + month, 28m + month, 95m, 250m, 70m + month * 3m, 180m + month * 10m };
        return new[]
        {
            Line(month,1,1,1,values[0],0,null), Line(month,1,2,4,0,values[0],10),
            Line(month,2,1,5,values[1],0,30), Line(month,2,2,1,0,values[1],null),
            Line(month,3,1,5,values[2],0,33), Line(month,3,2,1,0,values[2],null),
            Line(month,4,1,5,values[3],0,32), Line(month,4,2,1,0,values[3],null),
            Line(month,5,1,5,values[4],0,31), Line(month,5,2,1,0,values[4],null),
            Line(month,6,1,5,values[5],0,34), Line(month,6,2,1,0,values[5],null),
            Line(month,7,1,2,values[6],0,2), Line(month,7,2,1,0,values[6],1100),
            Line(month,8,1,5,values[7],0,38), Line(month,8,2,1,0,values[7],null),
            Line(month,9,1,1,values[8],0,null), Line(month,9,2,4,0,values[8],20)
        };
    }).ToArray();

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

    /// <summary>
    /// Creates an account entity.
    /// </summary>
    /// <param name="id">The account identifier number.</param>
    /// <param name="name">The account name.</param>
    /// <param name="description">The account description.</param>
    /// <param name="type">The account type.</param>
    /// <param name="balance">The initial balance.</param>
    /// <param name="categoryCode">The category code.</param>
    /// <returns>A new <see cref="Account"/> instance.</returns>
    private static Account Account(int id, string name, string description, AccountType type, decimal balance, int categoryCode) => new() { Id = Guid.Parse($"30000000-0000-0000-0000-{id:D12}"), Name = name, Description = description, AccountType = type, InitialBalance = balance, Currency = "EUR", CategoryId = Guid.Parse($"20000000-0000-0000-0000-{categoryCode:D12}"), CreatedAt = SeedDate };

    /// <summary>
    /// Creates a journal entry anonymous object.
    /// </summary>
    /// <param name="month">The month number (1-12).</param>
    /// <param name="slot">The entry slot within the month.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="description">The entry description.</param>
    /// <param name="reference">The entry reference code.</param>
    /// <returns>An anonymous object representing a journal entry.</returns>
    private static object Entry(int month, int slot, int day, string description, string reference) => new { Id = EntryId(month, slot), BudgetId = (Guid?)null, Date = new DateOnly(2026, month, day), Description = description, Reference = reference, Notes = $"Dados de demonstração — {month:D2}/2026", Status = JournalEntryStatus.Active, CancelledAt = (DateTimeOffset?)null, CancelledBy = (string?)null, CreatedAt = SeedDate, CreatedBy = "demo-seed", UpdatedAt = (DateTimeOffset?)null, UpdatedBy = (string?)null };

    /// <summary>
    /// Creates a journal entry line anonymous object (debit or credit).
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <param name="slot">The entry slot within the month.</param>
    /// <param name="side">The line side (1 for debit, 2 for credit).</param>
    /// <param name="accountId">The account identifier number.</param>
    /// <param name="debit">The debit amount.</param>
    /// <param name="credit">The credit amount.</param>
    /// <param name="categoryCode">The optional category code.</param>
    /// <returns>An anonymous object representing a journal entry line.</returns>
    private static object Line(int month, int slot, int side, int accountId, decimal debit, decimal credit, int? categoryCode) => new { Id = Guid.Parse($"50000000-0000-0000-0000-{(((month - 1) * 18) + ((slot - 1) * 2) + side):D12}"), JournalEntryId = EntryId(month, slot), AccountId = Guid.Parse($"30000000-0000-0000-0000-{accountId:D12}"), CategoryId = categoryCode.HasValue ? Guid.Parse($"20000000-0000-0000-0000-{categoryCode.Value:D12}") : (Guid?)null, Debit = debit, Credit = credit, Description = (string?)null, CreatedAt = SeedDate, CreatedBy = "demo-seed", UpdatedAt = (DateTimeOffset?)null, UpdatedBy = (string?)null };

    /// <summary>
    /// Creates a budget line entity.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <param name="slot">The line slot within the budget.</param>
    /// <param name="categoryCode">The category code.</param>
    /// <param name="amount">The budgeted amount.</param>
    /// <returns>A new <see cref="BudgetLine"/> instance.</returns>
    private static BudgetLine BudgetLine(int month, int slot, int categoryCode, decimal amount) => new() { Id = Guid.Parse($"70000000-0000-0000-0000-{(((month - 1) * 9) + slot):D12}"), BudgetId = BudgetId(month), CategoryId = Guid.Parse($"20000000-0000-0000-0000-{categoryCode:D12}"), Amount = amount, CreatedAt = SeedDate, CreatedBy = "demo-seed" };

    /// <summary>
    /// Creates a reconciliation entity.
    /// </summary>
    /// <param name="id">The reconciliation identifier number.</param>
    /// <param name="entryId">The journal entry identifier.</param>
    /// <returns>A new <see cref="Reconciliation"/> instance.</returns>
    private static Reconciliation Reconciliation(int id, Guid entryId) => new() { Id = Guid.Parse($"80000000-0000-0000-0000-{id:D12}"), JournalEntryId = entryId, Status = ReconciliationStatus.Reconciled, ReconciledAt = SeedDate.AddDays(id), ReconciledBy = "demo-seed", CreatedAt = SeedDate, CreatedBy = "demo-seed" };

    /// <summary>
    /// Generates a journal entry identifier based on month and slot.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <param name="slot">The entry slot within the month.</param>
    /// <returns>A unique identifier for the journal entry.</returns>
    private static Guid EntryId(int month, int slot) => Guid.Parse($"40000000-0000-0000-0000-{(((month - 1) * 9) + slot):D12}");

    /// <summary>
    /// Generates a budget identifier based on the month.
    /// </summary>
    /// <param name="month">The month number.</param>
    /// <returns>A unique identifier for the budget.</returns>
    private static Guid BudgetId(int month) => Guid.Parse($"60000000-0000-0000-0000-{month:D12}");

    /// <summary>
    /// Creates a reminder anonymous object.
    /// </summary>
    /// <param name="id">The reminder identifier number.</param>
    /// <param name="text">The reminder text.</param>
    /// <param name="eventDate">The date of the event.</param>
    /// <param name="noticeDays">The number of days before the event to send notice.</param>
    /// <returns>An anonymous object representing a reminder.</returns>
    private static object Reminder(int id, string text, DateOnly eventDate, int noticeDays) => new { Id = Guid.Parse($"90000000-0000-0000-0000-{id:D12}"), Text = text, EventDate = eventDate, NoticeDays = noticeDays, CreatedAt = SeedDate, CreatedBy = "demo-seed", UpdatedAt = (DateTimeOffset?)null, UpdatedBy = (string?)null };
}
