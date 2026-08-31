using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>Contains editable fields for a stock holding.</summary>
public sealed class StockPositionFormViewModel
{
    /// <summary>Gets or sets the position identifier.</summary> public Guid? Id { get; set; }
    /// <summary>Gets or sets the market ticker.</summary> [Required, StringLength(24)] public string Ticker { get; set; } = string.Empty;
    /// <summary>Gets or sets the company or instrument name.</summary> [Required, StringLength(160)] public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the exchange identifier.</summary> [StringLength(40)] public string? Exchange { get; set; }
    /// <summary>Gets or sets the trading currency.</summary> [Required, StringLength(3, MinimumLength = 3)] public string Currency { get; set; } = "EUR";
    /// <summary>Gets or sets the number of shares held.</summary> [Range(typeof(decimal), "0", "999999999999")] public decimal Quantity { get; set; }
    /// <summary>Gets or sets the average acquisition cost.</summary> [Range(typeof(decimal), "0", "999999999999")] public decimal AverageCost { get; set; }
    /// <summary>Gets or sets the latest known price.</summary> [Range(typeof(decimal), "0", "999999999999")] public decimal CurrentPrice { get; set; }
    /// <summary>Gets or sets the latest known price date.</summary> public DateOnly PriceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>Represents a stock holding in the portfolio list.</summary>
public sealed record StockPositionRowViewModel(Guid Id, string Ticker, string Name, string? Exchange, string Currency, decimal Quantity, decimal AverageCost, decimal CurrentPrice, DateOnly PriceDate, decimal CostValue, decimal MarketValue, decimal Gain, decimal GainPercent);

/// <summary>Contains the stock portfolio overview.</summary>
public sealed record StockPortfolioIndexViewModel(IReadOnlyList<StockPositionRowViewModel> Items, decimal TotalCost, decimal TotalMarketValue, decimal TotalGain);

/// <summary>Contains a dated price update.</summary>
public sealed class StockPriceUpdateViewModel
{
    /// <summary>Gets or sets the stock position identifier.</summary> public Guid Id { get; set; }
    /// <summary>Gets or sets the new price.</summary> [Range(typeof(decimal), "0", "999999999999")] public decimal Price { get; set; }
    /// <summary>Gets or sets the price date.</summary> public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
