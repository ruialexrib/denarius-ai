using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies read-only persistence facts used by specialised financial analyses.</summary>
public sealed class SpecializedFinancialAnalysisRepositoryTests
{
    /// <summary>Verifies stock history and insurance premiums are projected without modifying source records.</summary>
    [Fact]
    public async Task RepositoryReturnsStockAndInsuranceFacts()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DenariusDbContext(options);

        var today = new DateOnly(2026, 9, 12);
        var stock = new StockPosition(
            "AAA",
            "Example",
            "XETRA",
            "EUR",
            10m,
            8m,
            12m,
            today,
            today.AddYears(-1),
            false)
        {
            CreatedBy = "test"
        };
        context.StockPositions.Add(stock);
        context.StockPrices.AddRange(
            new StockPrice(stock.Id, today.AddDays(-10), 9m),
            new StockPrice(stock.Id, today, 12m));

        var policy = new InsurancePolicy(
            "Seguro",
            "Seguradora",
            "P-1",
            InsurancePolicyType.Home,
            InsurancePaymentFrequency.Annual,
            today.AddYears(-1),
            renewalDate: today.AddMonths(1))
        {
            CreatedBy = "test"
        };
        context.InsurancePolicies.Add(policy);
        context.InsurancePremiums.Add(new InsurancePremium(
            policy.Id,
            120m,
            today,
            today.AddYears(1).AddDays(-1),
            today.AddMonths(1))
        {
            CreatedBy = "test"
        });
        await context.SaveChangesAsync();

        var repository = new SpecializedFinancialAnalysisRepository(context);
        var stocks = await repository.GetStocksAsync();
        var policies = await repository.GetInsurancePoliciesAsync();

        var stockFact = Assert.Single(stocks);
        Assert.Equal(today.AddDays(-10), stockFact.FirstHistoryDate);
        Assert.Equal(9m, stockFact.FirstHistoryPrice);

        var policyFact = Assert.Single(policies);
        Assert.Equal("Seguro", policyFact.Name);
        var premiumFact = Assert.Single(policyFact.Premiums);
        Assert.Equal(120m, premiumFact.Amount);
        Assert.False(premiumFact.IsPaid);
    }
}
