namespace StockApp.Api.Models;

using System.Text.Json.Serialization;

public enum RiskAppetite
{
    Conservative = 0,
    Moderate = 1,
    Aggressive = 2
}

public class RiskProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public RiskAppetite Appetite { get; set; } = RiskAppetite.Moderate;

    // Max % of portfolio to allocate to a single position
    public decimal MaxPositionSizePercent { get; set; } = 10m;

    // Max % of portfolio value the bot may trade in a single day
    public decimal MaxDailyTurnoverPercent { get; set; } = 20m;

    // Stop loss threshold, e.g. 8 = sell if position down 8%
    public decimal StopLossPercent { get; set; } = 8m;

    // Take profit threshold
    public decimal TakeProfitPercent { get; set; } = 20m;

    // Only Shariah-compliant screened stocks are ever considered (locked true for this app)
    public bool ShariahComplianceOnly { get; set; } = true;
}
