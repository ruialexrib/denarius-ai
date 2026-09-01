using DenariusAI.Application.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies deterministic financial report data aggregation.</summary>
public sealed class FinancialReportDataServiceTests
{
    /// <summary>Verifies account balances and every requested month are calculated without delegating arithmetic.</summary>
    [Fact]
    public async Task GetAsyncBuildsAccountAndMonthlyFactsForWholeRange()
    {
        await using var context = CreateContext();
        var bank = new Account { Name = "Banco", AccountType = AccountType.BankAccount, InitialBalance = 250m, Currency = "EUR", IsActive = true };
        context.Accounts.Add(bank);
        await context.SaveChangesAsync();
        var unitOfWork = new UnitOfWork(context, new AccountRepository(context), new JournalEntryRepository(context), new BudgetRepository(context));
        var service = new FinancialReportDataService(new AnalyticsService(new AnalyticsRepository(context)),
            new AccountService(unitOfWork), new BudgetService(unitOfWork), new ReconciliationService(unitOfWork));

        var report = await service.GetAsync(new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 10));

        var account = Assert.Single(report.Accounts);
        Assert.Equal(250m, account.InitialBalance);
        Assert.Equal(250m, account.BalanceAtEnd);
        Assert.Equal([1, 2, 3], report.Months.Select(month => month.Month));
        Assert.All(report.Months, month => Assert.Equal(0m, month.BudgetVariance));
        Assert.Equal(0, report.Reconciliation.Total);
    }

    /// <summary>Verifies invalid report periods are rejected before any service query is attempted.</summary>
    [Fact]
    public async Task GetAsyncRejectsInvalidPeriod()
    {
        var service = new FinancialReportDataService(null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAsync(default, new DateOnly(2026, 1, 31)));
    }

    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
