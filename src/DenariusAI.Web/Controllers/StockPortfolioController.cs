using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>Manages tracked stock holdings and their market values.</summary>
[Authorize]
public sealed class StockPortfolioController(DenariusDbContext dbContext, IStockForecastService forecastService, IStockMarketDataService marketDataService, ILogger<StockPortfolioController> logger) : Controller
{
    /// <summary>Displays the current stock portfolio with filters and independent portfolio/watchlist pagination.</summary><param name="search">Optional ticker or instrument-name filter.</param><param name="currency">Optional trading-currency filter.</param><param name="exchange">Optional exchange filter.</param><param name="portfolioPage">Portfolio page number.</param><param name="watchlistPage">Watchlist page number.</param><param name="pageSize">Number of items per section page.</param><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    public async Task<IActionResult> Index(string? search, string? currency, string? exchange, int portfolioPage = 1, int watchlistPage = 1, int pageSize = PaginationViewModel.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        pageSize = PaginationViewModel.NormalizePageSize(pageSize);
        var positions = await dbContext.StockPositions.AsNoTracking().OrderBy(x => x.Ticker).ToListAsync(cancellationToken);
        var positionIds = positions.Select(x => x.Id).ToArray();
        var history = await dbContext.StockPrices.AsNoTracking().Where(x => positionIds.Contains(x.StockPositionId)).OrderBy(x => x.Date).ToListAsync(cancellationToken);
        var historyByPosition = history.ToLookup(x => x.StockPositionId);
        var rows = positions.Select(position => ToRow(position, historyByPosition[position.Id])).ToList();
        var completePortfolio = rows.Where(x => !x.WatchlistOnly).ToList();
        var currencies = rows.Select(x => x.Currency).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        var exchanges = rows.Select(x => x.Exchange).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();

        IEnumerable<StockPositionRowViewModel> filteredRows = rows;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filteredRows = filteredRows.Where(x => x.Ticker.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(currency)) filteredRows = filteredRows.Where(x => string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exchange)) filteredRows = filteredRows.Where(x => string.Equals(x.Exchange, exchange, StringComparison.OrdinalIgnoreCase));

        var filtered = filteredRows.ToList();
        var portfolioRows = filtered.Where(x => !x.WatchlistOnly).ToList();
        var watchlistRows = filtered;
        var portfolioPagination = PaginationViewModel.Create(portfolioRows.Count, portfolioPage, pageSize);
        var watchlistPagination = PaginationViewModel.Create(watchlistRows.Count, watchlistPage, pageSize);
        var portfolioItems = portfolioRows.Skip((portfolioPagination.Page - 1) * pageSize).Take(pageSize).ToList();
        var watchlistItems = watchlistRows.Skip((watchlistPagination.Page - 1) * pageSize).Take(pageSize).ToList();

        return View(new StockPortfolioIndexViewModel(portfolioItems, watchlistItems, completePortfolio.Sum(x => x.CostValue), completePortfolio.Sum(x => x.MarketValue), completePortfolio.Sum(x => x.Gain), search, currency, exchange, currencies, exchanges, portfolioPagination, watchlistPagination));
    }

    /// <summary>Displays the imported price evolution and configured forecasts for one instrument.</summary><param name="id">The stock position identifier.</param><param name="cancellationToken">Token used to cancel database access.</param><returns>The history page or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var position = await dbContext.StockPositions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (position is null) return NotFound();
        var prices = await dbContext.StockPrices.AsNoTracking().Where(x => x.StockPositionId == id).OrderBy(x => x.Date).ToListAsync(cancellationToken);
        var forecast = position.ForecastEnabled ? forecastService.Forecast(prices.Select(x => new StockPriceObservationDto(x.Date, x.Price)).ToArray()) : null;
        return View(new StockHistoryViewModel(position.Id, position.Ticker, position.Name, position.Exchange, position.Currency, position.ForecastEnabled, forecast?.Model, forecast?.ValidationMaePercent, forecast?.Message, prices.Select(x => new StockHistoryPointViewModel(x.Date, x.Price)).ToArray(), forecast?.Points.Select(x => new StockForecastPointViewModel(x.Days, x.Date, x.Price, x.LowerPrice, x.UpperPrice)).ToArray() ?? []));
    }

    /// <summary>Displays the form for a new stock holding.</summary><returns>The stock form.</returns>
    [HttpGet] public IActionResult Create() => View("Form", new StockPositionFormViewModel());

    /// <summary>Creates a stock holding.</summary><param name="model">Submitted holding data.</param><param name="cancellationToken">Cancellation token.</param><returns>The form on failure or portfolio on success.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockPositionFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateMarketAnalysis(model); if (!ModelState.IsValid) return View("Form", model);
        var ticker = model.Ticker.Trim().ToUpperInvariant(); var exchange = string.IsNullOrWhiteSpace(model.Exchange) ? null : model.Exchange.Trim().ToUpperInvariant();
        if (await dbContext.StockPositions.AnyAsync(x => x.Ticker == ticker && x.Exchange == exchange, cancellationToken)) { ModelState.AddModelError(nameof(model.Ticker), "Esta ação já existe no portfólio para o mercado indicado."); return View("Form", model); }
        var position = new StockPosition(ticker, model.Name, exchange, model.Currency, model.Quantity, model.AverageCost, model.CurrentPrice, model.PriceDate, model.HistoryStartDate, model.ForecastEnabled, model.WatchlistOnly) { CreatedBy = UserId() };
        dbContext.StockPositions.Add(position); dbContext.StockPrices.Add(new StockPrice(position.Id, model.PriceDate, model.CurrentPrice)); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Ação adicionada ao portfólio."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Displays the edit form for a stock holding.</summary><param name="id">Position identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The stock form or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var position = await dbContext.StockPositions.FindAsync([id], cancellationToken); if (position is null) return NotFound();
        return View("Form", new StockPositionFormViewModel { Id=position.Id,Ticker=position.Ticker,Name=position.Name,Exchange=position.Exchange,Currency=position.Currency,Quantity=position.Quantity,AverageCost=position.AverageCost,CurrentPrice=position.CurrentPrice,PriceDate=position.PriceDate,HistoryStartDate=position.HistoryStartDate,ForecastEnabled=position.ForecastEnabled,WatchlistOnly=position.WatchlistOnly });
    }

    /// <summary>Updates a stock holding.</summary><param name="id">Position identifier.</param><param name="model">Submitted holding data.</param><param name="cancellationToken">Cancellation token.</param><returns>The form on failure or portfolio on success.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StockPositionFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest(); ValidateMarketAnalysis(model); if (!ModelState.IsValid) return View("Form", model);
        var position = await dbContext.StockPositions.FindAsync([id], cancellationToken); if (position is null) return NotFound();
        var ticker=model.Ticker.Trim().ToUpperInvariant(); var exchange=string.IsNullOrWhiteSpace(model.Exchange)?null:model.Exchange.Trim().ToUpperInvariant();
        if(await dbContext.StockPositions.AnyAsync(candidate=>candidate.Id!=id&&candidate.Ticker==ticker&&candidate.Exchange==exchange,cancellationToken)){ModelState.AddModelError(nameof(model.Ticker),"Esta ação já existe no portfólio para o mercado indicado.");return View("Form",model);}
        var priceChanged=position.CurrentPrice!=model.CurrentPrice||position.PriceDate!=model.PriceDate; position.Update(ticker,model.Name,exchange,model.Currency,model.Quantity,model.AverageCost,model.CurrentPrice,model.PriceDate); position.ConfigureMarketAnalysis(model.HistoryStartDate,model.ForecastEnabled); position.SetWatchlistOnly(model.WatchlistOnly); position.UpdatedBy=UserId();
        if(priceChanged&&!await dbContext.StockPrices.AnyAsync(price=>price.StockPositionId==id&&price.Date==model.PriceDate,cancellationToken))dbContext.StockPrices.Add(new StockPrice(id,model.PriceDate,model.CurrentPrice));
        await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"]="Posição atualizada."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Registers a new market price for a position.</summary><param name="model">Price update.</param><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(StockPriceUpdateViewModel model,CancellationToken cancellationToken)
    {
        if(!ModelState.IsValid){TempData["ErrorMessage"]="Indique um preço válido.";return RedirectToAction(nameof(Index));} var position=await dbContext.StockPositions.FindAsync([model.Id],cancellationToken);if(position is null)return NotFound();position.UpdatePrice(model.Price,model.Date);position.UpdatedBy=UserId();var history=await dbContext.StockPrices.SingleOrDefaultAsync(price=>price.StockPositionId==model.Id&&price.Date==model.Date,cancellationToken);if(history is not null)dbContext.StockPrices.Remove(history);dbContext.StockPrices.Add(new StockPrice(model.Id,model.Date,model.Price));await dbContext.SaveChangesAsync(cancellationToken);TempData["SuccessMessage"]=$"Cotação de {position.Ticker} atualizada.";return RedirectToAction(nameof(Index));
    }

    /// <summary>Imports the configured daily market history for a tracked instrument.</summary><param name="id">The stock position identifier.</param><param name="cancellationToken">Token used to cancel the provider request.</param><returns>The refreshed portfolio view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportHistory(Guid id,CancellationToken cancellationToken)
    {
        var position=await dbContext.StockPositions.FindAsync([id],cancellationToken);if(position is null)return NotFound();try{var imported=await marketDataService.GetDailyHistoryAsync(position.Ticker,position.HistoryStartDate,cancellationToken);if(imported.Count==0){TempData["ErrorMessage"]=$"Não foram encontradas cotações para {position.Ticker} desde {position.HistoryStartDate:dd/MM/yyyy}.";return RedirectToAction(nameof(Index));}var existing=await dbContext.StockPrices.Where(x=>x.StockPositionId==id&&x.Date>=position.HistoryStartDate).ToDictionaryAsync(x=>x.Date,cancellationToken);foreach(var observation in imported){if(existing.TryGetValue(observation.Date,out var previous))dbContext.StockPrices.Remove(previous);dbContext.StockPrices.Add(new StockPrice(id,observation.Date,observation.Price));}var latest=imported[^1];position.UpdatePrice(latest.Price,latest.Date);position.UpdatedBy=UserId();await dbContext.SaveChangesAsync(cancellationToken);TempData["SuccessMessage"]=$"Foram recolhidas {imported.Count} cotações de {position.Ticker}.";return RedirectToAction(nameof(History),new{id});}catch(Exception exception)when(exception is HttpRequestException or InvalidOperationException or TaskCanceledException){logger.LogWarning(exception,"Stock history import failed for position {PositionId}.",id);TempData["ErrorMessage"]=exception is InvalidOperationException?exception.Message:"Não foi possível contactar o fornecedor de cotações.";}return RedirectToAction(nameof(Index));
    }

    /// <summary>Removes a stock holding and its price history.</summary><param name="id">Position identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id,CancellationToken cancellationToken){var position=await dbContext.StockPositions.FindAsync([id],cancellationToken);if(position is null)return NotFound();dbContext.StockPositions.Remove(position);await dbContext.SaveChangesAsync(cancellationToken);TempData["SuccessMessage"]="Ação removida do portfólio.";return RedirectToAction(nameof(Index));}

    /// <summary>Gets the authenticated user identifier.</summary><returns>The user identifier.</returns>
    private string UserId()=>User.FindFirstValue(ClaimTypes.NameIdentifier)??throw new InvalidOperationException("Utilizador não identificado.");
    /// <summary>Validates the historical collection settings submitted for a stock.</summary><param name="model">The submitted stock form.</param>
    private void ValidateMarketAnalysis(StockPositionFormViewModel model){if(model.HistoryStartDate>DateOnly.FromDateTime(DateTime.Today))ModelState.AddModelError(nameof(model.HistoryStartDate),"A data inicial do histórico não pode estar no futuro.");}

    /// <summary>Creates a deterministic portfolio row.</summary><param name="position">Stock position.</param><param name="history">Historical prices for the position.</param><returns>Calculated portfolio values and history metrics.</returns>
    private StockPositionRowViewModel ToRow(StockPosition position,IEnumerable<StockPrice> history)
    {
        var historyPoints=history.OrderBy(x=>x.Date).ToArray();var cost=position.Quantity*position.AverageCost;var market=position.Quantity*position.CurrentPrice;var gain=market-cost;var percent=cost==0?0:gain/cost*100m;
        var forecast=position.ForecastEnabled?forecastService.Forecast(historyPoints.Select(x=>new StockPriceObservationDto(x.Date,x.Price)).ToArray()):null;
        var first=historyPoints.FirstOrDefault();var minimum=historyPoints.Length==0?null:historyPoints.MinBy(x=>x.Price);var maximum=historyPoints.Length==0?null:historyPoints.MaxBy(x=>x.Price);decimal? periodChange=first is null||first.Price==0?null:(position.CurrentPrice-first.Price)/first.Price*100m;
        return new StockPositionRowViewModel(position.Id,position.Ticker,position.Name,position.Exchange,position.Currency,position.Quantity,position.AverageCost,position.CurrentPrice,position.PriceDate,cost,market,gain,percent,position.HistoryStartDate,position.ForecastEnabled,position.WatchlistOnly,forecast?.Model,forecast?.ValidationMaePercent,forecast?.Message,forecast?.Points.Select(x=>new StockForecastPointViewModel(x.Days,x.Date,x.Price,x.LowerPrice,x.UpperPrice)).ToArray()??[],periodChange,first?.Date,minimum?.Price,minimum?.Date,maximum?.Price,maximum?.Date);
    }
}
