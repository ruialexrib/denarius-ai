using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for StructuralSeedTests.
/// </summary>
public sealed class StructuralSeedTests
{
    [Fact]
    public void SeedMatchesGroupsAndCategoriesFromAnnualPlan()
    {
        using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var model = context.GetService<IDesignTimeModel>().Model;
        var groups = model.FindEntityType(typeof(FinancialGroup))!.GetSeedData();
        var categories = model.FindEntityType(typeof(Category))!.GetSeedData();
        var accounts = model.FindEntityType(typeof(Account))!.GetSeedData();
        var entries = model.FindEntityType(typeof(JournalEntry))!.GetSeedData();
        var entryLines = model.FindEntityType(typeof(JournalEntryLine))!.GetSeedData();
        var reconciliations = model.FindEntityType(typeof(Reconciliation))!.GetSeedData();

        Assert.Equal(5, groups.Count());
        Assert.Equal(33, categories.Count());
        Assert.Contains(groups, item => (string)item[nameof(FinancialGroup.Name)]! == "Património e Poupanças");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Constituição de Poupanças");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Despesas com o carro e transportes");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Caixas e Fundo de Maneio");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Água");
        Assert.Equal(5, accounts.Count());
        var demonstrationAccount = Assert.Single(accounts, item => (Guid)item[nameof(Account.Id)]! == Guid.Parse("30000000-0000-0000-0000-000000000001"));
        Assert.Equal("Conta à Ordem — Demonstração", demonstrationAccount[nameof(Account.Name)]);
        Assert.Equal("EUR", demonstrationAccount[nameof(Account.Currency)]);
        Assert.Equal(1850m, demonstrationAccount[nameof(Account.InitialBalance)]);
        Assert.Equal(72, entries.Count());
        Assert.Equal(144, entryLines.Count());
        Assert.Equal(48, reconciliations.Count());
        Assert.Equal(24, entries.Count() - reconciliations.Count());
    }
}
