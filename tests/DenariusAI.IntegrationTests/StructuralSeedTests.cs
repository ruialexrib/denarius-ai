using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies that only stable structural data (financial groups and categories) is part of the EF Core model
/// seed data, and that demonstration financial records are no longer baked into the schema.
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

        Assert.Equal(5, groups.Count());
        Assert.Equal(33, categories.Count());
        Assert.Contains(groups, item => (string)item[nameof(FinancialGroup.Name)]! == "Património e Poupanças");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Constituição de Poupanças");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Despesas com o carro e transportes");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Caixas e Fundo de Maneio");
        Assert.Contains(categories, item => (string)item[nameof(Category.Name)]! == "Água");
    }

    /// <summary>
    /// Verifies that demonstration financial records are no longer part of the EF Core model seed data, so
    /// that a database created from the full migration history starts with structural data only and
    /// <see cref="DemonstrationDataService"/> remains the single source of truth for the demonstration scenario.
    /// </summary>
    [Fact]
    public void ModelSeedDataDoesNotIncludeDemonstrationRecords()
    {
        using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var model = context.GetService<IDesignTimeModel>().Model;

        Assert.Empty(model.FindEntityType(typeof(Account))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(JournalEntry))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(JournalEntryLine))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(Reconciliation))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(Budget))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(BudgetLine))!.GetSeedData());
        Assert.Empty(model.FindEntityType(typeof(Reminder))!.GetSeedData());
    }

    /// <summary>
    /// Verifies that a freshly created database (mirroring the outcome of replaying the full migration
    /// history) contains structural data but no financial demonstration records until
    /// <see cref="DemonstrationDataService"/> explicitly loads them.
    /// </summary>
    [Fact]
    public async Task FreshDatabaseHasStructuralDataWithoutDemonstrationRecords()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();

        Assert.Equal(5, await context.FinancialGroups.CountAsync());
        Assert.Equal(33, await context.Categories.CountAsync());
        Assert.Empty(context.Accounts);
        Assert.Empty(context.JournalEntries);
        Assert.Empty(context.JournalEntryLines);
        Assert.Empty(context.Reconciliations);
        Assert.Empty(context.Budgets);
        Assert.Empty(context.BudgetLines);
        Assert.Empty(context.Reminders);
    }
}
