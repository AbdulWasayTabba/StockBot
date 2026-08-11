namespace StockApp.Api.Services;

public static class MarketHours
{
    public static bool IsOpen()
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

        // NOTE: doesn't account for US market holidays — see caveat in
        // AgentPollingBackgroundService.
        return nowEastern >= marketOpen && nowEastern <= marketClose;
    }
}
