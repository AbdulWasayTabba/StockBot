using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(int userId) =>
        Ok(await _db.Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedUtc).ToListAsync());

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int userId, int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(n);
    }
}
