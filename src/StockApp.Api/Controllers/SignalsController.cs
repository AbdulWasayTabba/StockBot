using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;
using StockApp.Api.Services;

namespace StockApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignalsController : ControllerBase
{
    private readonly IAgentClient _agent;
    private readonly ITradeExecutionService _executor;
    private readonly AppDbContext _db;

    public SignalsController(IAgentClient agent, ITradeExecutionService executor, AppDbContext db)
    {
        _agent = agent;
        _executor = executor;
        _db = db;
    }

    public record RunRequest(int UserId, List<string> Symbols);

    // Manually trigger an analysis cycle for a user's chosen symbols, instead of
    // waiting for the 15-minute background poller.
    [HttpPost("run")]
    public async Task<IActionResult> Run(RunRequest req)
    {
        var user = await _db.Users.Include(u => u.RiskProfile).FirstOrDefaultAsync(u => u.Id == req.UserId);
        if (user is null) return NotFound("User not found");

        var riskLabel = (user.RiskProfile?.Appetite ?? Models.RiskAppetite.Moderate).ToString();
        var signals = await _agent.GetSignalsAsync(riskLabel, req.Symbols);

        foreach (var signal in signals)
            await _executor.HandleSignalAsync(user, signal);

        return Ok(signals);
    }
}
