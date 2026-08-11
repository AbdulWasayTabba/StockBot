using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StockApp.Api.Models;

namespace StockApp.Api.Services;

public interface IAlpacaService
{
    Task<string?> PlaceOrderAsync(string symbol, decimal qty, SignalAction side);
    Task<decimal> GetBuyingPowerAsync();
    Task<decimal?> GetLastPriceAsync(string symbol);
    Task<List<PriceBar>> GetBarsAsync(string symbol, int days);
    Task<AccountInfo> GetAccountAsync();
}

public record PriceBar(DateTime Time, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
public record AccountInfo(decimal Equity, decimal BuyingPower, decimal Cash, decimal LastEquity);

// Thin wrapper around Alpaca's REST trading API (paper endpoint by default).
// Docs: https://docs.alpaca.markets/
public class AlpacaService : IAlpacaService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public AlpacaService(HttpClient http, IConfiguration config)
    {
        _config = config;
        var baseUrl = _config["Alpaca:BaseUrl"] ?? "https://paper-api.alpaca.markets";
        http.BaseAddress = new Uri(baseUrl);
        http.DefaultRequestHeaders.Add("APCA-API-KEY-ID", _config["Alpaca:ApiKeyId"]);
        http.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", _config["Alpaca:ApiSecretKey"]);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http = http;
    }

    public async Task<string?> PlaceOrderAsync(string symbol, decimal qty, SignalAction side)
    {
        var payload = new
        {
            symbol,
            qty = qty.ToString(),
            side = side == SignalAction.Buy ? "buy" : "sell",
            type = "market",
            time_in_force = "day"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/v2/orders", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Alpaca order failed: {response.StatusCode} - {err}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString();
    }

    public async Task<decimal> GetBuyingPowerAsync()
    {
        var response = await _http.GetAsync("/v2/account");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return decimal.Parse(doc.RootElement.GetProperty("buying_power").GetString()!);
    }

    public async Task<AccountInfo> GetAccountAsync()
    {
        var response = await _http.GetAsync("/v2/account");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        return new AccountInfo(
            Equity: decimal.Parse(root.GetProperty("equity").GetString()!),
            BuyingPower: decimal.Parse(root.GetProperty("buying_power").GetString()!),
            Cash: decimal.Parse(root.GetProperty("cash").GetString()!),
            LastEquity: decimal.Parse(root.GetProperty("last_equity").GetString()!)
        );
    }

    public async Task<decimal?> GetLastPriceAsync(string symbol)
    {
        // Uses the market data API (data.alpaca.markets) — separate host in production;
        // for simplicity this assumes it's proxied/allowed via the same key.
        var response = await _http.GetAsync($"https://data.alpaca.markets/v2/stocks/{symbol}/trades/latest");
        if (!response.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("trade", out var trade) && trade.TryGetProperty("p", out var p))
            return p.GetDecimal();
        return null;
    }

    public async Task<List<PriceBar>> GetBarsAsync(string symbol, int days)
    {
        var start = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
        var url = $"https://data.alpaca.markets/v2/stocks/{symbol}/bars" +
                  $"?timeframe=1Day&start={start}&limit={days}&adjustment=raw&feed=iex";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<PriceBar>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bars = new List<PriceBar>();
        if (!doc.RootElement.TryGetProperty("bars", out var barsEl)) return bars;

        foreach (var b in barsEl.EnumerateArray())
        {
            bars.Add(new PriceBar(
                Time: b.GetProperty("t").GetDateTime(),
                Open: b.GetProperty("o").GetDecimal(),
                High: b.GetProperty("h").GetDecimal(),
                Low: b.GetProperty("l").GetDecimal(),
                Close: b.GetProperty("c").GetDecimal(),
                Volume: b.GetProperty("v").GetInt64()
            ));
        }
        return bars;
    }
}
