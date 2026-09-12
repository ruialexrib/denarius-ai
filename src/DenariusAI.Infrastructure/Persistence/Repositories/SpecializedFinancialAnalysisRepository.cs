using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence.Repositories;

/// <summary>Reads stock and insurance facts for specialised financial analyses without modifying persisted data.</summary>
/// <param name="dbContext">Application database context.</param>
public sealed class SpecializedFinancialAnalysisRepository(DenariusDbContext dbContext)
    : ISpecializedFinancialAnalysisRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InvestmentStockFactDto>> GetStocksAsync(
        CancellationToken cancellationToken = default)
    {
        var positions = await dbContext.StockPositions.AsNoTracking()
            .OrderBy(item => item.Ticker)
            .ToListAsync(cancellationToken);
        if (positions.Count == 0) return [];

        var ids = positions.Select(item => item.Id).ToArray();
        var prices = await dbContext.StockPrices.AsNoTracking()
            .Where(item => ids.Contains(item.StockPositionId))
            .OrderBy(item => item.Date)
            .Select(item => new { item.StockPositionId, item.Date, item.Price })
            .ToListAsync(cancellationToken);
        var firstByPosition = prices
            .GroupBy(item => item.StockPositionId)
            .ToDictionary(group => group.Key, group => group.First());

        return positions.Select(item =>
        {
            firstByPosition.TryGetValue(item.Id, out var first);
            return new InvestmentStockFactDto(
                item.Id,
                item.Ticker,
                item.Name,
                item.Exchange,
                item.Currency,
                item.Quantity,
                item.AverageCost,
                item.CurrentPrice,
                item.PriceDate,
                item.WatchlistOnly,
                first?.Date,
                first?.Price);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InsurancePolicyFactDto>> GetInsurancePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        var policies = await dbContext.InsurancePolicies.AsNoTracking()
            .Include(item => item.Premiums)
            .ThenInclude(item => item.JournalEntry)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return policies.Select(policy => new InsurancePolicyFactDto(
            policy.Id,
            policy.Name,
            policy.Insurer,
            policy.Type,
            policy.PaymentFrequency,
            policy.StartDate,
            policy.EndDate,
            policy.RenewalDate,
            policy.Status,
            policy.Premiums
                .OrderBy(premium => premium.DueDate)
                .Select(premium => new InsurancePremiumFactDto(
                    premium.Id,
                    premium.PolicyId,
                    premium.Amount,
                    premium.DueDate,
                    premium.PeriodStart,
                    premium.PeriodEnd,
                    premium.IsPaid))
                .ToList())).ToList();
    }
}
