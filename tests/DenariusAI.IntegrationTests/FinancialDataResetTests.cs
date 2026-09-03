using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies the lifecycle of resettable financial data and the complete demonstration scenario.
/// </summary>
public sealed class FinancialDataResetTests
{
    /// <summary>
    /// Verifies that a financial reset removes transactional data while retaining shared configuration.
    /// </summary>
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

    /// <summary>
    /// Verifies that demonstration data covers every user-facing area and cannot be loaded twice.
    /// </summary>
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
        Assert.Equal(3, await context.StockPositions.CountAsync());
        Assert.Equal(24, await context.StockPrices.CountAsync());
        Assert.Equal(2, await context.Warranties.CountAsync());
        Assert.Equal(2, await context.Reminders.CountAsync(reminder => reminder.WarrantyId != null));
        Assert.Equal(2, await context.Correspondence.CountAsync());
        Assert.Equal(4, await context.CorrespondenceMetadata.CountAsync());
        Assert.Equal(3, await context.InsurancePolicies.CountAsync());
        Assert.Equal(5, await context.InsurancePremiums.CountAsync());
        Assert.Single(context.InsurancePolicyAttachments);
        Assert.Single(context.InsurancePremiumAttachments);
        Assert.All(context.Warranties, warranty => Assert.False(string.IsNullOrWhiteSpace(warranty.DocumentBase64)));
        Assert.All(context.Correspondence, item => Assert.False(string.IsNullOrWhiteSpace(item.DocumentBase64)));
    }
}
