namespace StockApp.Api.Models;

using System.Text.Json.Serialization;

public enum TradeStatus
{
    Pending = 0,
    Submitted = 1,
    Filled = 2,
    Rejected = 3,
    Cancelled = 4
}

public class Trade
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public SignalAction Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public TradeStatus Status { get; set; } = TradeStatus.Pending;
    public string? AlpacaOrderId { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public SignalAction SuggestedAction { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
