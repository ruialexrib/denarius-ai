using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies persistence of stock portfolio positions and their price history.
/// </summary>
public sealed class StockPortfolioPersistenceTests
{
    /// <summary>
    /// Verifies that a stock position and its dated market price survive a database round trip.
    /// </summary>
    [Fact]
    public async Task PositionAndPriceHistoryCanBePersisted()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DenariusDbContext(options);
        var priceDate = new DateOnly(2026, 8, 31);
        var historyStartDate = new DateOnly(2024, 1, 1);
        var position = new StockPosition("MSFT", "Microsoft", "NASDAQ", "USD", 2m, 100m, 110m, priceDate, historyStartDate, true);
        var price = new StockPrice(position.Id, priceDate, 110m);

        context.StockPositions.Add(position);
        context.StockPrices.Add(price);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var storedPrice = await context.StockPrices
            .Include(item => item.StockPosition)
            .SingleAsync();
        Assert.Equal(position.Id, storedPrice.StockPositionId);
        Assert.Equal("MSFT", storedPrice.StockPosition.Ticker);
        Assert.Equal(priceDate, storedPrice.Date);
        Assert.Equal(110m, storedPrice.Price);
        Assert.Equal(historyStartDate, storedPrice.StockPosition.HistoryStartDate);
        Assert.True(storedPrice.StockPosition.ForecastEnabled);
    }
}
