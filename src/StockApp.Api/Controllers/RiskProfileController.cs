using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;
using StockApp.Api.Models;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/risk-profile")]
public class RiskProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    public RiskProfileController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(int userId)
    {
        var profile = await _db.RiskProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(int userId, RiskProfile input)
    {
        var user = await _db.Users.Include(u => u.RiskProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound("User not found");

        if (user.RiskProfile is null)
        {
            input.UserId = userId;
            input.ShariahComplianceOnly = true; // locked: this app only ever trades Shariah-screened stocks
            _db.RiskProfiles.Add(input);
        }
        else
        {
            user.RiskProfile.Appetite = input.Appetite;
            user.RiskProfile.MaxPositionSizePercent = input.MaxPositionSizePercent;
            user.RiskProfile.MaxDailyTurnoverPercent = input.MaxDailyTurnoverPercent;
            user.RiskProfile.StopLossPercent = input.StopLossPercent;
            user.RiskProfile.TakeProfitPercent = input.TakeProfitPercent;
        }

        await _db.SaveChangesAsync();
        return Ok(user.RiskProfile ?? input);
    }
}
