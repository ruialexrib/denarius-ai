using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

public sealed class FinancialDataResetTests
{
    [Fact]
    public async Task ResetRemovesFinancialDataAndKeepsConfiguration()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();
        var category = await context.Categories.FirstAsync();
        var budget = new Budget { Year = 2027, Month = 1 };
        budget.Lines.Add(new BudgetLine { CategoryId = category.Id, Amount = 10m });
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        var groupCount = await context.FinancialGroups.CountAsync();
        var categoryCount = await context.Categories.CountAsync();

        var result = await new FinancialDataResetService(context).ResetAsync();

        Assert.True(result.Accounts > 0);
        Assert.True(result.JournalEntries > 0);
        Assert.Empty(context.Accounts);
        Assert.Empty(context.JournalEntries);
        Assert.Empty(context.JournalEntryLines);
        Assert.Empty(context.Reconciliations);
        Assert.Empty(context.Budgets);
        Assert.Empty(context.BudgetLines);
        Assert.Equal(groupCount, await context.FinancialGroups.CountAsync());
        Assert.Equal(categoryCount, await context.Categories.CountAsync());
    }

    [Fact]
    public async Task DemonstrationDataCanBeLoadedAfterResetButIsNotDuplicated()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();
        await new FinancialDataResetService(context).ResetAsync();

        var first = await new DemonstrationDataService(context).LoadAsync();
        var second = await new DemonstrationDataService(context).LoadAsync();

        Assert.True(first.Loaded);
        Assert.Equal(5, first.Accounts);
        Assert.Equal(72, first.JournalEntries);
        Assert.Equal(8, first.Budgets);
        Assert.False(second.Loaded);
        Assert.Equal(5, await context.Accounts.CountAsync());
        Assert.Equal(72, await context.JournalEntries.CountAsync());
        Assert.Equal(144, await context.JournalEntryLines.CountAsync());
        Assert.Equal(48, await context.Reconciliations.CountAsync());
        Assert.Equal(8, await context.Budgets.CountAsync());
        Assert.Equal(72, await context.BudgetLines.CountAsync());
        Assert.Equal(3, await context.SavingsCertificates.CountAsync());
    }
}
