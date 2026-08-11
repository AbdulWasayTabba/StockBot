namespace StockApp.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore] // never serialize this out to the frontend
    public string PasswordHash { get; set; } = string.Empty;

    // If true, the agent executes trades automatically.
    // If false, the agent only sends notifications and the user decides.
    public bool AutoTradeEnabled { get; set; } = false;

    public RiskProfile? RiskProfile { get; set; }
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
