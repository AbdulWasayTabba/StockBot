using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;
using StockApp.Api.Models;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Users.Include(u => u.RiskProfile).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await _db.Users.Include(u => u.RiskProfile).FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id}/auto-trade")]
    public async Task<IActionResult> SetAutoTrade(int id, [FromBody] bool enabled)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.AutoTradeEnabled = enabled;
        await _db.SaveChangesAsync();
        return Ok(user);
    }
}
