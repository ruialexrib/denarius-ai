using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

public sealed class DemonstrationDataService(DenariusDbContext dbContext) : IDemonstrationDataService
{
    public async Task<DemonstrationDataLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Accounts.AnyAsync(cancellationToken)
            || await dbContext.JournalEntries.AnyAsync(cancellationToken)
            || await dbContext.Budgets.AnyAsync(cancellationToken)
            || await dbContext.SavingsCertificates.AnyAsync(cancellationToken))
        {
            return new(false, 0, 0, 0);
        }

        var accounts = StructuralSeed.Accounts;
        var entries = CreateEntries();
        var budgets = StructuralSeed.Budgets;

        dbContext.Accounts.AddRange(accounts);
        dbContext.JournalEntries.AddRange(entries);
        dbContext.Budgets.AddRange(budgets);
        dbContext.BudgetLines.AddRange(StructuralSeed.BudgetLines);
        dbContext.Reconciliations.AddRange(StructuralSeed.Reconciliations);
        dbContext.SavingsCertificates.AddRange(CreateSavingsCertificates());
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(true, accounts.Length, entries.Length, budgets.Length);
    }

    private static SavingsCertificate[] CreateSavingsCertificates() =>
    [
        Certificate(1, new DateOnly(2023, 3, 15), "E-2023-001842", "Poupança familiar", 2500m, 2.50m, 2684.72m, new DateOnly(2026, 9, 15)),
        Certificate(2, new DateOnly(2024, 7, 2), "F-2024-007316", "Fundo de emergência", 5000m, 2.50m, 5218.34m, new DateOnly(2026, 10, 2)),
        Certificate(3, new DateOnly(2025, 11, 21), "F-2025-014908", "Objetivos de longo prazo", 1500m, 2.00m, 1521.66m, new DateOnly(2026, 8, 21))
    ];

    private static SavingsCertificate Certificate(int id, DateOnly date, string number, string description,
        decimal investment, decimal rate, decimal currentValue, DateOnly nextCapitalization) =>
        new(date, number, description, investment, rate, currentValue, nextCapitalization)
        {
            Id = Id("70000000", id), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };

    private static JournalEntry[] CreateEntries()
    {
        return
        [
            Entry(1, 1, "Salário mensal", "REC-JUL-001", (1, 2500m, 0m, null), (4, 0m, 2500m, 10)),
            Entry(2, 3, "Renda da casa", "PAG-JUL-001", (5, 750m, 0m, 30), (1, 0m, 750m, null)),
            Entry(3, 5, "Compras de supermercado", "TALAO-1842", (5, 180m, 0m, 33), (1, 0m, 180m, null)),
            Entry(4, 7, "Fatura de eletricidade", "ELEC-0726", (5, 65m, 0m, 32), (1, 0m, 65m, null)),
            Entry(5, 8, "Fatura de água", "AGUA-0726", (5, 32m, 0m, 31), (1, 0m, 32m, null)),
            Entry(6, 10, "Transferência para poupança", "TRF-POUP", (2, 300m, 0m, 2), (1, 0m, 300m, 1100)),
            Entry(7, 12, "Levantamento ATM", "ATM-1208", (3, 100m, 0m, 4), (1, 0m, 100m, 4)),
            Entry(8, 15, "Jantar em família", "REST-1508", (5, 80m, 0m, 38), (1, 0m, 80m, null)),
            Entry(9, 18, "Trabalho ocasional", "FREELANCE-07", (1, 350m, 0m, null), (4, 0m, 350m, 20)),
            Entry(10, 20, "Viagem de verão", "VIAGEM-2026", (5, 450m, 0m, 50), (1, 0m, 450m, null))
        ];
    }

    private static JournalEntry Entry(int id, int day, string description, string reference,
        (int Account, decimal Debit, decimal Credit, int? Category) first,
        (int Account, decimal Debit, decimal Credit, int? Category) second)
    {
        var entry = new JournalEntry(new DateOnly(2026, 7, day), description, reference, "Dados de demonstração — julho 2026")
        {
            Id = Id("40000000", id), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };
        entry.AssignBudget(Guid.Parse("60000000-0000-0000-0000-000000000001"));
        var firstLine = entry.AddLine(Id("30000000", first.Account), first.Debit, first.Credit, categoryId: first.Category.HasValue ? Id("20000000", first.Category.Value) : null);
        var secondLine = entry.AddLine(Id("30000000", second.Account), second.Debit, second.Credit, categoryId: second.Category.HasValue ? Id("20000000", second.Category.Value) : null);
        firstLine.Id = Id("50000000", (id * 2) - 1);
        secondLine.Id = Id("50000000", id * 2);
        firstLine.CreatedAt = secondLine.CreatedAt = entry.CreatedAt;
        firstLine.CreatedBy = secondLine.CreatedBy = "demo-seed";
        entry.EnsureBalanced();
        return entry;
    }

    private static Guid Id(string prefix, int value) => Guid.Parse($"{prefix}-0000-0000-0000-{value:D12}");
}
