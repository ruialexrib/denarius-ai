using System.Security.Claims;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

/// <summary>Manages tracked stock holdings and their market values.</summary>
[Authorize]
public sealed class StockPortfolioController(DenariusDbContext dbContext) : Controller
{
    /// <summary>Displays the current stock portfolio.</summary><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var positions = await dbContext.StockPositions.AsNoTracking().OrderBy(x => x.Ticker).ToListAsync(cancellationToken);
        var rows = positions.Select(ToRow).ToList();
        return View(new StockPortfolioIndexViewModel(rows, rows.Sum(x => x.CostValue), rows.Sum(x => x.MarketValue), rows.Sum(x => x.Gain)));
    }

    /// <summary>Displays the form for a new stock holding.</summary><returns>The stock form.</returns>
    [HttpGet] public IActionResult Create() => View("Form", new StockPositionFormViewModel());

    /// <summary>Creates a stock holding.</summary><param name="model">Submitted holding data.</param><param name="cancellationToken">Cancellation token.</param><returns>The form on failure or portfolio on success.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockPositionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var ticker = model.Ticker.Trim().ToUpperInvariant(); var exchange = string.IsNullOrWhiteSpace(model.Exchange) ? null : model.Exchange.Trim().ToUpperInvariant();
        if (await dbContext.StockPositions.AnyAsync(x => x.Ticker == ticker && x.Exchange == exchange, cancellationToken)) { ModelState.AddModelError(nameof(model.Ticker), "Esta ação já existe no portfólio para o mercado indicado."); return View("Form", model); }
        var position = new StockPosition(ticker, model.Name, exchange, model.Currency, model.Quantity, model.AverageCost, model.CurrentPrice, model.PriceDate) { CreatedBy = UserId() };
        dbContext.StockPositions.Add(position); dbContext.StockPrices.Add(new StockPrice(position.Id, model.PriceDate, model.CurrentPrice)); await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Ação adicionada ao portfólio."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Displays the edit form for a stock holding.</summary><param name="id">Position identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The stock form or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) { var x=await dbContext.StockPositions.FindAsync([id],cancellationToken); if(x is null)return NotFound(); return View("Form", new StockPositionFormViewModel{Id=x.Id,Ticker=x.Ticker,Name=x.Name,Exchange=x.Exchange,Currency=x.Currency,Quantity=x.Quantity,AverageCost=x.AverageCost,CurrentPrice=x.CurrentPrice,PriceDate=x.PriceDate}); }

    /// <summary>Updates a stock holding.</summary><param name="id">Position identifier.</param><param name="model">Submitted holding data.</param><param name="cancellationToken">Cancellation token.</param><returns>The form on failure or portfolio on success.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StockPositionFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest(); if (!ModelState.IsValid) return View("Form", model); var x=await dbContext.StockPositions.FindAsync([id],cancellationToken); if(x is null)return NotFound();
        var ticker=model.Ticker.Trim().ToUpperInvariant(); var exchange=string.IsNullOrWhiteSpace(model.Exchange)?null:model.Exchange.Trim().ToUpperInvariant(); if(await dbContext.StockPositions.AnyAsync(p=>p.Id!=id&&p.Ticker==ticker&&p.Exchange==exchange,cancellationToken)){ModelState.AddModelError(nameof(model.Ticker),"Esta ação já existe no portfólio para o mercado indicado.");return View("Form",model);}
        var priceChanged=x.CurrentPrice!=model.CurrentPrice||x.PriceDate!=model.PriceDate; x.Update(ticker,model.Name,exchange,model.Currency,model.Quantity,model.AverageCost,model.CurrentPrice,model.PriceDate); x.UpdatedBy=UserId(); if(priceChanged&&!await dbContext.StockPrices.AnyAsync(p=>p.StockPositionId==id&&p.Date==model.PriceDate,cancellationToken))dbContext.StockPrices.Add(new StockPrice(id,model.PriceDate,model.CurrentPrice)); await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"]="Posição atualizada."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Registers a new market price for a position.</summary><param name="model">Price update.</param><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(StockPriceUpdateViewModel model, CancellationToken cancellationToken)
    {
        if(!ModelState.IsValid){TempData["ErrorMessage"]="Indique um preço válido.";return RedirectToAction(nameof(Index));} var x=await dbContext.StockPositions.FindAsync([model.Id],cancellationToken); if(x is null)return NotFound(); x.UpdatePrice(model.Price,model.Date); x.UpdatedBy=UserId(); var history=await dbContext.StockPrices.SingleOrDefaultAsync(p=>p.StockPositionId==model.Id&&p.Date==model.Date,cancellationToken); if(history is null)dbContext.StockPrices.Add(new StockPrice(model.Id,model.Date,model.Price)); else { dbContext.StockPrices.Remove(history); dbContext.StockPrices.Add(new StockPrice(model.Id,model.Date,model.Price)); } await dbContext.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"]=$"Cotação de {x.Ticker} atualizada."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Removes a stock holding and its price history.</summary><param name="id">Position identifier.</param><param name="cancellationToken">Cancellation token.</param><returns>The portfolio view.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id,CancellationToken cancellationToken){var x=await dbContext.StockPositions.FindAsync([id],cancellationToken);if(x is null)return NotFound();dbContext.StockPositions.Remove(x);await dbContext.SaveChangesAsync(cancellationToken);TempData["SuccessMessage"]="Ação removida do portfólio.";return RedirectToAction(nameof(Index));}

    /// <summary>Gets the authenticated user identifier.</summary><returns>The user identifier.</returns>
    private string UserId()=>User.FindFirstValue(ClaimTypes.NameIdentifier)??throw new InvalidOperationException("Utilizador não identificado.");
    /// <summary>Creates a deterministic portfolio row.</summary><param name="x">Stock position.</param><returns>Calculated portfolio values.</returns>
    private static StockPositionRowViewModel ToRow(StockPosition x){var cost=x.Quantity*x.AverageCost;var market=x.Quantity*x.CurrentPrice;var gain=market-cost;var percent=cost==0?0:gain/cost*100m;return new(x.Id,x.Ticker,x.Name,x.Exchange,x.Currency,x.Quantity,x.AverageCost,x.CurrentPrice,x.PriceDate,cost,market,gain,percent);}
}
