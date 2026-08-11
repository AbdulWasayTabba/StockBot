using Microsoft.EntityFrameworkCore;
using StockApp.Api.Data;

namespace StockApp.Api.Services;

// Checks every hour, but only actually scans while US markets are open
// (weekdays, 9:30am-4:00pm Eastern) — no point analyzing stale after-hours
// prices. Requires the backend process to stay running continuously; it only
// scans while `dotnet run` is active, not just while the browser tab is open.
public class AgentPollingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AgentPollingBackgroundService> _logger;

    private static readonly string[] DefaultWatchlist = {
        "AAPL", "MSFT", "GOOGL", "AMZN", "NVDA", "META", "TSLA", "AVGO",
        "ADBE", "CRM", "ORCL", "CSCO", "AMD", "INTC", "QCOM", "TXN",
        "COST", "PEP", "NKE", "MCD", "SBUX", "HD", "LOW",
        "JNJ", "PFE", "MRK", "ABBV", "UNH",
        "XOM", "CVX",
        "BA", "CAT", "HON", "UPS",
    };

    public AgentPollingBackgroundService(IServiceProvider services, ILogger<AgentPollingBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    private static bool IsMarketOpen()
    {
        TimeZoneInfo eastern;
        try
        {
            eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); // Windows ID
        }
        catch (TimeZoneNotFoundException)
        {
            eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); // Linux/Mac ID fallback
        }

        var nowEastern = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastern);

        if (nowEastern.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        var marketOpen = nowEastern.Date.AddHours(9).AddMinutes(30);
        var marketClose = nowEastern.Date.AddHours(16);

        // NOTE: doesn't account for US market holidays (Thanksgiving, July 4th,
        // etc.) — those days it'll still "try" but Alpaca/FMP simply won't have
        // fresh data, so results will just look stale rather than causing errors.
        return nowEastern >= marketOpen && nowEastern <= marketClose;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsMarketOpen())
            {
                _logger.LogInformation("Market closed — skipping this cycle");
            }
            else
            {
                try
                {
                    _logger.LogInformation("Starting watchlist scan of {Count} symbols", DefaultWatchlist.Length);

                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var agent = scope.ServiceProvider.GetRequiredService<IAgentClient>();
                    var executor = scope.ServiceProvider.GetRequiredService<ITradeExecutionService>();

                    var users = await db.Users.Include(u => u.RiskProfile).ToListAsync(stoppingToken);

                    foreach (var user in users)
                    {
                        var riskLabel = (user.RiskProfile?.Appetite ?? Models.RiskAppetite.Moderate).ToString();
                        var signals = await agent.GetSignalsAsync(riskLabel, DefaultWatchlist);

                        foreach (var signal in signals)
                            await executor.HandleSignalAsync(user, signal);
                    }

                    _logger.LogInformation("Watchlist scan complete");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent polling cycle failed");
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
