using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockApp.Api.Services;

public class AgentSignalDto
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("action")] public string Action { get; set; } = "hold"; // "buy" | "sell" | "hold"
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("rationale")] public string Rationale { get; set; } = string.Empty;
    [JsonPropertyName("shariah_compliant")] public bool ShariahCompliant { get; set; }
}

public class ComplianceDto
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("compliant")] public bool Compliant { get; set; }
    [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
}

public interface IAgentClient
{
    Task<List<AgentSignalDto>> GetSignalsAsync(string riskAppetite, IEnumerable<string> watchlist);
    Task<ComplianceDto> GetComplianceAsync(string symbol);
}

// Calls the Python FastAPI agent (see /agent) which does the actual news/financial analysis.
public class AgentClient : IAgentClient
{
    private readonly HttpClient _http;

    public AgentClient(HttpClient http, IConfiguration config)
    {
        var baseUrl = config["PythonAgent:BaseUrl"] ?? "http://localhost:8001";
        http.BaseAddress = new Uri(baseUrl);
        _http = http;
    }

    public async Task<List<AgentSignalDto>> GetSignalsAsync(string riskAppetite, IEnumerable<string> watchlist)
    {
        var payload = new { risk_appetite = riskAppetite, symbols = watchlist };
        var response = await _http.PostAsJsonAsync("/analyze", payload);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<AgentSignalDto>>();
        return result ?? new List<AgentSignalDto>();
    }

    public async Task<ComplianceDto> GetComplianceAsync(string symbol)
    {
        var response = await _http.GetAsync($"/shariah/{symbol}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ComplianceDto>();
        return result ?? new ComplianceDto { Symbol = symbol, Compliant = false, Notes = "No data" };
    }
}
