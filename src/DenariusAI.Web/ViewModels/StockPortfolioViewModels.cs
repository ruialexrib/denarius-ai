using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains editable fields for a stock holding.
/// </summary>
public sealed class StockPositionFormViewModel
{
    /// <summary>
    /// Gets or sets the position identifier.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the market ticker.
    /// </summary>
    [Required]
    [StringLength(24)]
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the company or instrument name.
    /// </summary>
    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange identifier.
    /// </summary>
    [StringLength(40)]
    public string? Exchange { get; set; }

    /// <summary>
    /// Gets or sets the trading currency.
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Gets or sets the number of shares held.
    /// </summary>
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the average acquisition cost per share.
    /// </summary>
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal AverageCost { get; set; }

    /// <summary>
    /// Gets or sets the latest known market price per share.
    /// </summary>
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the date associated with the latest known market price.
    /// </summary>
    public DateOnly PriceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>
/// Represents a stock holding in the portfolio list.
/// </summary>
/// <param name="Id">The stock position identifier.</param>
/// <param name="Ticker">The market ticker.</param>
/// <param name="Name">The company or instrument name.</param>
/// <param name="Exchange">The exchange identifier.</param>
/// <param name="Currency">The trading currency.</param>
/// <param name="Quantity">The number of shares held.</param>
/// <param name="AverageCost">The average acquisition cost per share.</param>
/// <param name="CurrentPrice">The latest known market price per share.</param>
/// <param name="PriceDate">The date of the latest known market price.</param>
/// <param name="CostValue">The total acquisition cost.</param>
/// <param name="MarketValue">The current market value.</param>
/// <param name="Gain">The unrealised gain or loss.</param>
/// <param name="GainPercent">The unrealised gain or loss percentage.</param>
public sealed record StockPositionRowViewModel(
    Guid Id,
    string Ticker,
    string Name,
    string? Exchange,
    string Currency,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    DateOnly PriceDate,
    decimal CostValue,
    decimal MarketValue,
    decimal Gain,
    decimal GainPercent);

/// <summary>
/// Contains the stock portfolio overview.
/// </summary>
/// <param name="Items">The positions displayed in the portfolio.</param>
/// <param name="TotalCost">The total acquisition cost.</param>
/// <param name="TotalMarketValue">The total current market value.</param>
/// <param name="TotalGain">The total unrealised gain or loss.</param>
public sealed record StockPortfolioIndexViewModel(
    IReadOnlyList<StockPositionRowViewModel> Items,
    decimal TotalCost,
    decimal TotalMarketValue,
    decimal TotalGain);

/// <summary>
/// Contains a dated stock price update submitted by the user.
/// </summary>
public sealed class StockPriceUpdateViewModel
{
    /// <summary>
    /// Gets or sets the stock position identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new market price.
    /// </summary>
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the date associated with the market price.
    /// </summary>
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
