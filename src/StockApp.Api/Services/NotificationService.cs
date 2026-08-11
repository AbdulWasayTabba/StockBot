using StockApp.Api.Data;
using StockApp.Api.Models;

namespace StockApp.Api.Services;

public interface INotificationService
{
    Task NotifyAsync(User user, string symbol, SignalAction action, string message);
}

// Persists an in-app notification. Wire up email/SMS/push here later
// (e.g. SendGrid, Twilio, or a web-push subscription) — kept simple for now.
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(User user, string symbol, SignalAction action, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Symbol = symbol,
            SuggestedAction = action,
            Message = message
        });
        await _db.SaveChangesAsync();
    }
}
