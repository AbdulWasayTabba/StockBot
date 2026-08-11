using Microsoft.AspNetCore.Mvc;
using StockApp.Api.Services;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IAlpacaService _alpaca;
    private readonly IAgentClient _agent;

    public MarketDataController(IAlpacaService alpaca, IAgentClient agent)
    {
        _alpaca = alpaca;
        _agent = agent;
    }

    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol)
    {
        var price = await _alpaca.GetLastPriceAsync(symbol.ToUpper());
        return price is null ? NotFound() : Ok(new { symbol, price });
    }

    [HttpGet("history/{symbol}")]
    public async Task<IActionResult> GetHistory(string symbol, [FromQuery] int days = 90)
    {
        var bars = await _alpaca.GetBarsAsync(symbol.ToUpper(), days);
        return Ok(bars);
    }

    [HttpGet("compliance/{symbol}")]
    public async Task<IActionResult> GetCompliance(string symbol)
    {
        var result = await _agent.GetComplianceAsync(symbol.ToUpper());
        return Ok(result);
    }

    [HttpGet("account")]
    public async Task<IActionResult> GetAccount() => Ok(await _alpaca.GetAccountAsync());

    [HttpGet("buying-power")]
    public async Task<IActionResult> GetBuyingPower() => Ok(await _alpaca.GetBuyingPowerAsync());
}
