using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DenariusAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Service responsible for loading demonstration data into the database.
/// </summary>
/// <param name="dbContext">The database context for data operations.</param>
/// <param name="userManager">Optional user manager for creating demonstration users.</param>
public sealed class DemonstrationDataService(DenariusDbContext dbContext, UserManager<ApplicationUser>? userManager = null) : IDemonstrationDataService
{
    /// <summary>
    /// Loads demonstration data into the database if no data exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing information about the loaded data.</returns>
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
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Set<ApplicationUser>()
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(user => user.DemonstrationDataAcknowledgedAt, (DateTimeOffset?)null), cancellationToken);
        }
        else
        {
            foreach (var user in await dbContext.Set<ApplicationUser>().ToListAsync(cancellationToken))
                user.DemonstrationDataAcknowledgedAt = null;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureUsersAsync(cancellationToken);

        return new(true, accounts.Length, entries.Length, budgets.Length);
    }

    /// <summary>
    /// Creates an array of sample savings certificates for demonstration purposes.
    /// </summary>
    /// <returns>An array of savings certificates.</returns>
    private static SavingsCertificate[] CreateSavingsCertificates() =>
    [
        Certificate(1, new DateOnly(2023, 3, 15), "E-2023-001842", "Poupança familiar", 2500m, 2.50m, 2684.72m, new DateOnly(2026, 9, 15)),
        Certificate(2, new DateOnly(2024, 7, 2), "F-2024-007316", "Fundo de emergência", 5000m, 2.50m, 5218.34m, new DateOnly(2026, 10, 2)),
        Certificate(3, new DateOnly(2025, 11, 21), "F-2025-014908", "Objetivos de longo prazo", 1500m, 2.00m, 1521.66m, new DateOnly(2026, 8, 21))
    ];

    /// <summary>
    /// Creates a savings certificate with the specified parameters.
    /// </summary>
    /// <param name="id">The certificate identifier.</param>
    /// <param name="date">The certificate date.</param>
    /// <param name="number">The certificate number.</param>
    /// <param name="description">The certificate description.</param>
    /// <param name="investment">The investment amount.</param>
    /// <param name="rate">The interest rate.</param>
    /// <param name="currentValue">The current value of the certificate.</param>
    /// <param name="nextCapitalization">The next capitalization date.</param>
    /// <returns>A configured savings certificate.</returns>
    private static SavingsCertificate Certificate(int id, DateOnly date, string number, string description,
        decimal investment, decimal rate, decimal currentValue, DateOnly nextCapitalization) =>
        new(date, number, description, investment, rate, currentValue, nextCapitalization)
        {
            Id = Id("70000000", id), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };

    /// <summary>
    /// Creates sample journal entries for demonstration purposes covering 8 months.
    /// </summary>
    /// <returns>An array of journal entries.</returns>
    private static JournalEntry[] CreateEntries()
    {
        var entries = new List<JournalEntry>();
        for (var month = 1; month <= 8; month++)
        {
            var values = new[] { 2650m, 780m, 210m + month * 4m, 62m + month, 28m + month, 95m, 250m, 70m + month * 3m, 180m + month * 10m };
            entries.Add(Entry(month,1,1,"Salário mensal",$"SAL-2026-{month:D2}",(1,values[0],0,null),(4,0,values[0],10)));
            entries.Add(Entry(month,2,3,"Renda da casa",$"RENDA-{month:D2}",(5,values[1],0,30),(1,0,values[1],null)));
            entries.Add(Entry(month,3,6,"Compras de supermercado",$"SUPER-{month:D2}",(5,values[2],0,33),(1,0,values[2],null)));
            entries.Add(Entry(month,4,8,"Fatura de eletricidade",$"ELEC-{month:D2}",(5,values[3],0,32),(1,0,values[3],null)));
            entries.Add(Entry(month,5,9,"Fatura de água",$"AGUA-{month:D2}",(5,values[4],0,31),(1,0,values[4],null)));
            entries.Add(Entry(month,6,12,"Passe e combustível",$"TRANSP-{month:D2}",(5,values[5],0,34),(1,0,values[5],null)));
            entries.Add(Entry(month,7,15,"Transferência para poupança",$"POUP-{month:D2}",(2,values[6],0,2),(1,0,values[6],1100)));
            entries.Add(Entry(month,8,20,"Lazer em família",$"LAZER-{month:D2}",(5,values[7],0,38),(1,0,values[7],null)));
            entries.Add(Entry(month,9,24,"Trabalho ocasional",$"EXTRA-{month:D2}",(1,values[8],0,null),(4,0,values[8],20)));
        }
        return entries.ToArray();
    }

    /// <summary>
    /// Creates a journal entry with two lines (debit and credit).
    /// </summary>
    /// <param name="month">The month of the entry.</param>
    /// <param name="slot">The slot number within the month.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="description">The entry description.</param>
    /// <param name="reference">The entry reference.</param>
    /// <param name="first">The first line details (account, debit, credit, category).</param>
    /// <param name="second">The second line details (account, debit, credit, category).</param>
    /// <returns>A configured journal entry.</returns>
    private static JournalEntry Entry(int month, int slot, int day, string description, string reference,
        (int Account, decimal Debit, decimal Credit, int? Category) first,
        (int Account, decimal Debit, decimal Credit, int? Category) second)
    {
        var entryId = ((month - 1) * 9) + slot;
        var entry = new JournalEntry(new DateOnly(2026, month, day), description, reference, $"Dados de demonstração — {month:D2}/2026")
        {
            Id = Id("40000000", entryId), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };
        entry.AssignBudget(Id("60000000", month));
        var firstLine = entry.AddLine(Id("30000000", first.Account), first.Debit, first.Credit, categoryId: first.Category.HasValue ? Id("20000000", first.Category.Value) : null);
        var secondLine = entry.AddLine(Id("30000000", second.Account), second.Debit, second.Credit, categoryId: second.Category.HasValue ? Id("20000000", second.Category.Value) : null);
        firstLine.Id = Id("50000000", (entryId * 2) - 1);
        secondLine.Id = Id("50000000", entryId * 2);
        firstLine.CreatedAt = secondLine.CreatedAt = entry.CreatedAt;
        firstLine.CreatedBy = secondLine.CreatedBy = "demo-seed";
        entry.EnsureBalanced();
        return entry;
    }

    /// <summary>
    /// Ensures demonstration users exist in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task EnsureUsersAsync(CancellationToken cancellationToken = default)
    {
        if (userManager is null) return;
        foreach (var (email, name) in new[] { ("demo.familia@denarius.local", "Membro da família — Demo"), ("demo.consulta@denarius.local", "Consulta financeira — Demo") })
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await userManager.FindByEmailAsync(email) is not null) continue;
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
            var password = $"Demo!{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}aA1";
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Demonstration user could not be created: {string.Join("; ", result.Errors.Select(error => error.Code))}");
            var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.User);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Demonstration user role could not be assigned: {string.Join("; ", roleResult.Errors.Select(error => error.Code))}");
        }
    }

    /// <summary>
    /// Generates a GUID by combining a prefix with a numeric value.
    /// </summary>
    /// <param name="prefix">The prefix for the GUID.</param>
    /// <param name="value">The numeric value to include in the GUID.</param>
    /// <returns>A generated GUID.</returns>
    private static Guid Id(string prefix, int value) => Guid.Parse($"{prefix}-0000-0000-0000-{value:D12}");
}
