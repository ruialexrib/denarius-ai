namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a dated market price for a tracked stock position.
/// </summary>
public sealed class StockPrice
{
    /// <summary>Initializes a stock price for persistence.</summary>
    private StockPrice() { }

    /// <summary>Initializes a dated stock price.</summary>
    /// <param name="stockPositionId">The stock position identifier.</param>
    /// <param name="date">The market price date.</param>
    /// <param name="price">The market price.</param>
    public StockPrice(Guid stockPositionId, DateOnly date, decimal price)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        Id = Guid.NewGuid(); StockPositionId = stockPositionId; Date = date; Price = price;
    }

    /// <summary>Gets the price record identifier.</summary>
    public Guid Id { get; private set; }
    /// <summary>Gets the related stock position identifier.</summary>
    public Guid StockPositionId { get; private set; }
    /// <summary>Gets the market price date.</summary>
    public DateOnly Date { get; private set; }
    /// <summary>Gets the market price.</summary>
    public decimal Price { get; private set; }
    /// <summary>Gets the related stock position.</summary>
    public StockPosition StockPosition { get; private set; } = null!;
}
