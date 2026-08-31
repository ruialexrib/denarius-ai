using DenariusAI.Domain.Entities;

namespace DenariusAI.UnitTests;

/// <summary>Tests stock portfolio domain behavior.</summary>
public sealed class StockPositionTests
{
    /// <summary>Verifies that stock identifiers are normalized.</summary>
    [Fact]
    public void Constructor_NormalizesIdentifiers()
    {
        var item = new StockPosition(" msft ", " Microsoft ", " nasdaq ", " usd ", 2m, 100m, 110m, new DateOnly(2026, 8, 31), new DateOnly(2024, 1, 1), true);
        Assert.Equal("MSFT", item.Ticker); Assert.Equal("Microsoft", item.Name); Assert.Equal("NASDAQ", item.Exchange); Assert.Equal("USD", item.Currency);
    }

    /// <summary>Verifies that negative market prices are rejected.</summary>
    [Fact]
    public void UpdatePrice_NegativePrice_Throws()
    {
        var item = new StockPosition("MSFT", "Microsoft", "NASDAQ", "USD", 2m, 100m, 110m, new DateOnly(2026, 8, 31), new DateOnly(2024, 1, 1), true);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdatePrice(-1m, new DateOnly(2026, 8, 31)));
    }

    /// <summary>Verifies that a tracked instrument can move independently between watchlist and portfolio.</summary>
    [Fact]
    public void SetWatchlistOnly_UpdatesTrackingMode()
    {
        var item = new StockPosition("EDP.LIS", "EDP", "EURONEXT LISBON", "EUR", 0m, 0m, 4m, new DateOnly(2026, 8, 31), new DateOnly(2024, 1, 1), true, true);

        Assert.True(item.WatchlistOnly);
        item.SetWatchlistOnly(false);
        Assert.False(item.WatchlistOnly);
    }
}
