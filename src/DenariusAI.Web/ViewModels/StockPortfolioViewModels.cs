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

    /// <summary>Gets or sets the first date to collect when importing historical prices.</summary>
    [DataType(DataType.Date)]
    public DateOnly HistoryStartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-2));

    /// <summary>Gets or sets whether time-series forecasts are enabled.</summary>
    public bool ForecastEnabled { get; set; }

    /// <summary>Gets or sets whether the instrument belongs only to the watchlist.</summary>
    public bool WatchlistOnly { get; set; }
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
    decimal GainPercent,
    DateOnly HistoryStartDate,
    bool ForecastEnabled,
    bool WatchlistOnly,
    string? ForecastModel,
    decimal? ForecastMaePercent,
    string? ForecastMessage,
    IReadOnlyList<StockForecastPointViewModel> Forecasts);

/// <summary>Represents a projected stock price at a forecast horizon.</summary>
/// <param name="Days">The forecast horizon in calendar days.</param>
/// <param name="Date">The target date.</param>
/// <param name="Price">The projected price.</param>
/// <param name="LowerPrice">The lower 95 percent confidence bound.</param>
/// <param name="UpperPrice">The upper 95 percent confidence bound.</param>
public sealed record StockForecastPointViewModel(int Days, DateOnly Date, decimal Price, decimal LowerPrice, decimal UpperPrice);

/// <summary>
/// Contains the stock portfolio overview.
/// </summary>
/// <param name="Items">The positions displayed in the portfolio.</param>
/// <param name="TotalCost">The total acquisition cost.</param>
/// <param name="TotalMarketValue">The total current market value.</param>
/// <param name="TotalGain">The total unrealised gain or loss.</param>
public sealed record StockPortfolioIndexViewModel(
    IReadOnlyList<StockPositionRowViewModel> PortfolioItems,
    IReadOnlyList<StockPositionRowViewModel> WatchlistItems,
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

/// <summary>Contains historical prices and optional forecasts for one tracked instrument.</summary>
/// <param name="Id">The stock position identifier.</param>
/// <param name="Ticker">The provider ticker.</param>
/// <param name="Name">The instrument name.</param>
/// <param name="Exchange">The exchange name.</param>
/// <param name="Currency">The trading currency.</param>
/// <param name="ForecastEnabled">Whether forecasting is enabled.</param>
/// <param name="ForecastModel">The forecasting model description.</param>
/// <param name="ForecastMaePercent">The validation mean absolute percentage error.</param>
/// <param name="ForecastMessage">The reason why a forecast is unavailable.</param>
/// <param name="History">The imported closing-price history.</param>
/// <param name="Forecasts">The requested forecast horizons.</param>
public sealed record StockHistoryViewModel(
    Guid Id,
    string Ticker,
    string Name,
    string? Exchange,
    string Currency,
    bool ForecastEnabled,
    string? ForecastModel,
    decimal? ForecastMaePercent,
    string? ForecastMessage,
    IReadOnlyList<StockHistoryPointViewModel> History,
    IReadOnlyList<StockForecastPointViewModel> Forecasts);

/// <summary>Represents one historical closing price.</summary>
/// <param name="Date">The market date.</param>
/// <param name="Price">The closing price.</param>
public sealed record StockHistoryPointViewModel(DateOnly Date, decimal Price);
