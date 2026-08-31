using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a stock holding tracked in the investment portfolio.
/// </summary>
public sealed class StockPosition : AuditableEntity
{
    /// <summary>
    /// Initializes a stock position for persistence.
    /// </summary>
    private StockPosition() { }

    /// <summary>
    /// Initializes a stock position.
    /// </summary>
    /// <param name="ticker">The market ticker.</param>
    /// <param name="name">The company or instrument name.</param>
    /// <param name="exchange">The exchange or market identifier.</param>
    /// <param name="currency">The trading currency.</param>
    /// <param name="quantity">The number of shares held.</param>
    /// <param name="averageCost">The average acquisition cost per share.</param>
    /// <param name="currentPrice">The latest known market price per share.</param>
    /// <param name="priceDate">The date of the latest known price.</param>
    public StockPosition(string ticker, string name, string? exchange, string currency, decimal quantity, decimal averageCost, decimal currentPrice, DateOnly priceDate)
        => Update(ticker, name, exchange, currency, quantity, averageCost, currentPrice, priceDate);

    /// <summary>Gets the market ticker.</summary>
    public string Ticker { get; private set; } = string.Empty;
    /// <summary>Gets the company or instrument name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the exchange or market identifier.</summary>
    public string? Exchange { get; private set; }
    /// <summary>Gets the trading currency.</summary>
    public string Currency { get; private set; } = "EUR";
    /// <summary>Gets the number of shares currently held.</summary>
    public decimal Quantity { get; private set; }
    /// <summary>Gets the average acquisition cost per share.</summary>
    public decimal AverageCost { get; private set; }
    /// <summary>Gets the latest known market price per share.</summary>
    public decimal CurrentPrice { get; private set; }
    /// <summary>Gets the date of the latest known market price.</summary>
    public DateOnly PriceDate { get; private set; }

    /// <summary>
    /// Updates the holding and its latest known price.
    /// </summary>
    /// <param name="ticker">The market ticker.</param>
    /// <param name="name">The company or instrument name.</param>
    /// <param name="exchange">The exchange or market identifier.</param>
    /// <param name="currency">The trading currency.</param>
    /// <param name="quantity">The number of shares held.</param>
    /// <param name="averageCost">The average acquisition cost per share.</param>
    /// <param name="currentPrice">The latest known market price per share.</param>
    /// <param name="priceDate">The date of the latest known price.</param>
    public void Update(string ticker, string name, string? exchange, string currency, decimal quantity, decimal averageCost, decimal currentPrice, DateOnly priceDate)
    {
        if (string.IsNullOrWhiteSpace(ticker)) throw new ArgumentException("O ticker é obrigatório.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("O nome é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("A moeda é obrigatória.", nameof(currency));
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (averageCost < 0) throw new ArgumentOutOfRangeException(nameof(averageCost));
        if (currentPrice < 0) throw new ArgumentOutOfRangeException(nameof(currentPrice));
        Ticker = ticker.Trim().ToUpperInvariant(); Name = name.Trim(); Exchange = string.IsNullOrWhiteSpace(exchange) ? null : exchange.Trim().ToUpperInvariant(); Currency = currency.Trim().ToUpperInvariant(); Quantity = quantity; AverageCost = averageCost; CurrentPrice = currentPrice; PriceDate = priceDate;
    }

    /// <summary>Updates only the latest market price.</summary>
    /// <param name="price">The new market price.</param>
    /// <param name="date">The price date.</param>
    public void UpdatePrice(decimal price, DateOnly date)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        CurrentPrice = price; PriceDate = date;
    }
}
