using StockApp.Api.Data;
using StockApp.Api.Models;

namespace StockApp.Api.Services;

public interface ITradeExecutionService
{
    Task HandleSignalAsync(User user, AgentSignalDto signal);
}

// Turns an agent signal into either an executed trade (if the user has
// auto-trading on) or a notification (if they want to decide themselves).
// Also enforces the user's risk profile limits (position sizing, Shariah filter).
public class TradeExecutionService : ITradeExecutionService
{
    private readonly AppDbContext _db;
    private readonly IAlpacaService _alpaca;
    private readonly INotificationService _notifications;

    public TradeExecutionService(AppDbContext db, IAlpacaService alpaca, INotificationService notifications)
    {
        _db = db;
        _alpaca = alpaca;
        _notifications = notifications;
    }

    public async Task HandleSignalAsync(User user, AgentSignalDto signal)
    {
        // Hard rule: never act on a non-Shariah-compliant stock, regardless of settings.
        if (!signal.ShariahCompliant)
            return;

        if (signal.Action == "hold" || signal.Confidence < 0.55)
            return;

        var action = signal.Action == "buy" ? SignalAction.Buy : SignalAction.Sell;
        var message = $"{signal.Action.ToUpper()} signal for {signal.Symbol} " +
                       $"(confidence {signal.Confidence:P0}): {signal.Rationale}";

        if (!user.AutoTradeEnabled)
        {
            // Alert-only mode: user decides manually.
            await _notifications.NotifyAsync(user, signal.Symbol, action, message);
            return;
        }

        var profile = user.RiskProfile;
        if (profile is null)
        {
            await _notifications.NotifyAsync(user, signal.Symbol, action,
                "No risk profile set — skipped auto-trade, please configure your risk appetite.");
            return;
        }

        if (!MarketHours.IsOpen())
        {
            await _notifications.NotifyAsync(user, signal.Symbol, action,
                $"{signal.Action.ToUpper()} signal for {signal.Symbol} found, but market is closed — " +
                "skipped to avoid queuing an order at a stale price. Will retry next scan during market hours.");
            return;
        }

        var buyingPower = await _alpaca.GetBuyingPowerAsync();
        var price = await _alpaca.GetLastPriceAsync(signal.Symbol) ?? 0m;
        if (price <= 0) return;

        decimal qty;
        if (action == SignalAction.Buy)
        {
            var maxAllocation = buyingPower * (profile.MaxPositionSizePercent / 100m);
            qty = Math.Floor(maxAllocation / price);
            if (qty < 1) return; // not enough buying power for even 1 share
        }
        else
        {
            // Selling: sell full existing position size is handled at the portfolio-sync layer.
            // Here we conservatively sell a nominal 1 share increment as a placeholder trigger;
            // in production, look up the actual held quantity from Alpaca positions endpoint.
            qty = 1;
        }

        string? orderId;
        try
        {
            orderId = await _alpaca.PlaceOrderAsync(signal.Symbol, qty, action);
        }
        catch (Exception ex)
        {
            await _notifications.NotifyAsync(user, signal.Symbol, action, $"Auto-trade failed: {ex.Message}");
            return;
        }

        _db.Trades.Add(new Trade
        {
            UserId = user.Id,
            Symbol = signal.Symbol,
            Side = action,
            Quantity = qty,
            Price = price,
            Status = TradeStatus.Submitted,
            AlpacaOrderId = orderId,
            Rationale = signal.Rationale
        });
        await _db.SaveChangesAsync();

        await _notifications.NotifyAsync(user, signal.Symbol, action,
            $"Auto-executed {action} of {qty} {signal.Symbol} @ ~{price:C}. {signal.Rationale}");
    }
}
