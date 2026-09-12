using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Services;

/// <summary>Calculates tracked stock and Savings Certificate value, performance and concentration.</summary>
/// <param name="repository">Read-only source for persisted stock facts.</param>
/// <param name="analyticsService">Authoritative source for Savings Certificate values.</param>
public sealed class InvestmentPortfolioAnalysisService(
    ISpecializedFinancialAnalysisRepository repository,
    IAnalyticsService analyticsService) : IInvestmentPortfolioAnalysisService
{
    /// <inheritdoc />
    public async Task<InvestmentPortfolioAnalysisDto> GetAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var stockFacts = await repository.GetStocksAsync(cancellationToken);
        var owned = stockFacts.Where(item => !item.WatchlistOnly && item.Quantity > 0m).ToList();
        var analytics = await analyticsService.GetAsync(new(asOf.AddYears(-1), asOf), cancellationToken);

        var currencyMarketTotals = owned
            .GroupBy(item => item.Currency)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity * item.CurrentPrice));

        var stocks = owned.Select(item =>
        {
            var cost = item.Quantity * item.AverageCost;
            var market = item.Quantity * item.CurrentPrice;
            var gain = market - cost;
            var marketTotal = currencyMarketTotals.GetValueOrDefault(item.Currency);
            return new InvestmentStockAnalysisDto(
                item.Id,
                item.Ticker,
                item.Name,
                item.Currency,
                cost,
                market,
                gain,
                cost == 0m ? null : decimal.Round(gain / cost * 100m, 1),
                item.FirstHistoryPrice is > 0m
                    ? decimal.Round((item.CurrentPrice - item.FirstHistoryPrice.Value) / item.FirstHistoryPrice.Value * 100m, 1)
                    : null,
                item.PriceDate,
                marketTotal == 0m ? null : decimal.Round(market / marketTotal * 100m, 1));
        }).OrderByDescending(item => item.MarketValue).ToList();

        var currencySummaries = stocks.GroupBy(item => item.Currency)
            .Select(group =>
            {
                var cost = group.Sum(item => item.CostValue);
                var market = group.Sum(item => item.MarketValue);
                var gain = market - cost;
                return new InvestmentCurrencySummaryDto(
                    group.Key,
                    cost,
                    market,
                    gain,
                    cost == 0m ? null : decimal.Round(gain / cost * 100m, 1),
                    group.Max(item => item.CurrencyWeight));
            })
            .OrderByDescending(item => item.Currency == "EUR")
            .ThenBy(item => item.Currency)
            .ToList();

        var certificates = analytics.SavingsCertificates
            .Select(item => new InvestmentCertificateAnalysisDto(
                item.Id,
                item.SeriesNumber,
                item.Description,
                item.InvestmentValue,
                item.CurrentValue,
                item.Yield,
                item.InvestmentValue == 0m ? null : decimal.Round(item.Yield / item.InvestmentValue * 100m, 1),
                item.NextCapitalization))
            .OrderByDescending(item => item.CurrentValue)
            .ToList();

        var eurStocks = currencySummaries.FirstOrDefault(item => item.Currency == "EUR");
        var eurStockCost = eurStocks?.CostValue ?? 0m;
        var eurStockMarket = eurStocks?.MarketValue ?? 0m;
        var eurStockGain = eurStocks?.Gain ?? 0m;
        var certificateInvestment = certificates.Sum(item => item.InvestmentValue);
        var certificateValue = certificates.Sum(item => item.CurrentValue);
        var certificateYield = certificates.Sum(item => item.Yield);
        var findings = BuildFindings(stocks, currencySummaries, certificateValue, certificateYield);

        return new(
            asOf,
            eurStockCost,
            eurStockMarket,
            eurStockGain,
            eurStockCost == 0m ? null : decimal.Round(eurStockGain / eurStockCost * 100m, 1),
            certificateInvestment,
            certificateValue,
            certificateYield,
            eurStockMarket + certificateValue,
            stocks.Count,
            certificates.Count,
            stocks.Count(item => item.Currency != "EUR"),
            stocks,
            currencySummaries,
            certificates,
            findings);
    }

    /// <summary>Builds deterministic findings without treating model forecasts as realised performance.</summary>
    /// <param name="stocks">Analysed stock positions.</param>
    /// <param name="currencies">Portfolio totals by trading currency.</param>
    /// <param name="certificateValue">Current Savings Certificate value.</param>
    /// <param name="certificateYield">Accumulated Savings Certificate yield.</param>
    /// <returns>Prioritised findings.</returns>
    private static IReadOnlyList<SpecializedAnalysisFindingDto> BuildFindings(
        IReadOnlyList<InvestmentStockAnalysisDto> stocks,
        IReadOnlyList<InvestmentCurrencySummaryDto> currencies,
        decimal certificateValue,
        decimal certificateYield)
    {
        var findings = new List<SpecializedAnalysisFindingDto>();
        var eur = currencies.FirstOrDefault(item => item.Currency == "EUR");
        if (eur is not null && eur.CostValue > 0m)
        {
            findings.Add(new(
                eur.Gain >= 0m ? "positive" : "negative",
                "Desempenho das ações em EUR",
                $"As posições em EUR apresentam {(eur.Gain >= 0m ? "um ganho" : "uma perda")} não realizado(a) de {Math.Abs(eur.Gain):N2} € ({eur.ReturnPercentage:N1}%).",
                "StockPortfolio",
                "Index"));
        }

        var concentrated = currencies
            .Where(item => item.LargestPositionWeight >= 50m)
            .OrderByDescending(item => item.LargestPositionWeight)
            .FirstOrDefault();
        if (concentrated is not null)
        {
            findings.Add(new(
                "warning",
                "Concentração elevada numa posição",
                $"Na componente de ações em {concentrated.Currency}, a maior posição representa {concentrated.LargestPositionWeight:N1}% do valor acompanhado."));
        }

        if (certificateValue > 0m)
        {
            findings.Add(new(
                certificateYield >= 0m ? "positive" : "warning",
                "Certificados de Aforro no património financeiro",
                $"Os Certificados de Aforro totalizam {certificateValue:N2} € e acumulam {certificateYield:N2} € de rendimento.",
                "SavingsCertificates",
                "Index"));
        }

        var stale = stocks.Where(item => item.PriceDate < DateOnly.FromDateTime(DateTime.Today).AddDays(-7)).ToList();
        if (stale.Count > 0)
        {
            findings.Add(new(
                "warning",
                "Cotações que podem estar desatualizadas",
                $"{stale.Count} posição(ões) têm uma cotação com mais de 7 dias; o valor de mercado depende da última cotação guardada.",
                "StockPortfolio",
                "Index"));
        }

        var nonEur = stocks.Count(item => item.Currency != "EUR");
        if (nonEur > 0)
        {
            findings.Add(new(
                "info",
                "Exposição noutras moedas",
                $"{nonEur} posição(ões) estão denominadas fora do EUR. Não é aplicada conversão cambial nem agregação artificial ao total EUR."));
        }

        return findings.Take(6).ToList();
    }
}
