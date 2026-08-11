namespace StockApp.Api.Models;

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public bool IsShariahCompliant { get; set; }
    public string? ShariahScreenNotes { get; set; }
    public decimal LastPrice { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public enum SignalAction
{
    Buy = 0,
    Sell = 1,
    Hold = 2
}

public class TradeSignal
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public SignalAction Action { get; set; }
    public double Confidence { get; set; } // 0..1
    public string Rationale { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Populated by the Python agent
    public string SourceSummary { get; set; } = string.Empty;
}
