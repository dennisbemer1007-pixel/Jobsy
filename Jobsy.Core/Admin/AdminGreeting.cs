namespace Jobsy.Core.Admin;

/// <summary>Time-of-day greeting in Europe/Amsterdam (Goedemorgen / Goedemiddag / Goedenavond).</summary>
public static class AdminGreeting
{
    private static readonly TimeZoneInfo Amsterdam =
        ResolveAmsterdam();

    public static string GreetingKey(DateTime utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            Amsterdam);
        var hour = local.Hour;
        if (hour < 12)
        {
            return "AdminDash.Greeting.Morning";
        }

        if (hour < 18)
        {
            return "AdminDash.Greeting.Afternoon";
        }

        return "AdminDash.Greeting.Evening";
    }

    public static DateTime LocalNow(DateTime? utcNow = null)
        => TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow ?? DateTime.UtcNow, DateTimeKind.Utc),
            Amsterdam);

    private static TimeZoneInfo ResolveAmsterdam()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
