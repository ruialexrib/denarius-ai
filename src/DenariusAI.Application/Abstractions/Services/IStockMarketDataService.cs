using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Imports daily stock prices from the configured market-data provider.</summary>
public interface IStockMarketDataService
{
    /// <summary>Gets a value indicating whether the provider credential is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Gets daily closing prices for a provider symbol from the requested date.</summary>
    /// <param name="symbol">The symbol understood by the configured provider.</param>
    /// <param name="from">The earliest date to include.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The available daily closing-price observations.</returns>
    Task<IReadOnlyList<StockPriceObservationDto>> GetDailyHistoryAsync(string symbol, DateOnly from, CancellationToken cancellationToken = default);
}
