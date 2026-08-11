using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly AppDbContext _db;
    public PortfolioController(AppDbContext db) => _db = db;

    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades(int userId) =>
        Ok(await _db.Trades.Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedUtc).ToListAsync());
}
