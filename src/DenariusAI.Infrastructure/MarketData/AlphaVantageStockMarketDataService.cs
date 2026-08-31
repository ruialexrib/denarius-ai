using System.Globalization;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.Extensions.Configuration;

namespace DenariusAI.Infrastructure.MarketData;

/// <summary>Retrieves global daily equity prices from the Alpha Vantage free API.</summary>
public sealed class AlphaVantageStockMarketDataService(HttpClient httpClient, IConfiguration configuration, IApplicationSettingsService settingsService) : IStockMarketDataService
{
    /// <inheritdoc />
    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["MarketData:ApiKey"]);

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockPriceObservationDto>> GetDailyHistoryAsync(string symbol, DateOnly from, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["MarketData:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Configure MarketData__ApiKey antes de recolher cotações.");
        }

        var settings = await settingsService.GetAsync(cancellationToken);
        var separator = settings.MarketDataBaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var requestUri = $"{settings.MarketDataBaseUrl}{separator}function=TIME_SERIES_DAILY&symbol={Uri.EscapeDataString(symbol.Trim())}&outputsize=compact&apikey={Uri.EscapeDataString(apiKey)}";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("Time Series (Daily)", out var timeSeries))
        {
            var message = ReadProviderMessage(document.RootElement);
            throw new InvalidOperationException(message ?? "O fornecedor não devolveu histórico para este símbolo.");
        }

        var result = new List<StockPriceObservationDto>();
        foreach (var item in timeSeries.EnumerateObject())
        {
            if (DateOnly.TryParseExact(item.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date >= from
                && item.Value.TryGetProperty("4. close", out var close)
                && decimal.TryParse(close.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                && price > 0)
            {
                result.Add(new(date, price));
            }
        }

        return result.OrderBy(x => x.Date).ToArray();
    }

    /// <summary>Reads a safe diagnostic returned by Alpha Vantage.</summary>
    /// <param name="root">The response root object.</param>
    /// <returns>A provider message without credentials, when present.</returns>
    private static string? ReadProviderMessage(JsonElement root)
    {
        foreach (var propertyName in new[] { "Error Message", "Information", "Note" })
        {
            if (root.TryGetProperty(propertyName, out var value))
            {
                return value.GetString();
            }
        }

        return null;
    }
}
