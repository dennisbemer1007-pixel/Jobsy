namespace Jobsy.Core.Rules;

/// <summary>Production defaults and bounds for authenticated candidate apply rate limits.</summary>
public static class CandidateApplyRateLimitRules
{
    public const int DefaultPerMinute = 5;
    public const int DefaultPerHour = 30;
    public const int DefaultPerDay = 100;

    public const int MinLimit = 1;
    public const int MaxPerMinute = 60;
    public const int MaxPerHour = 200;
    public const int MaxPerDay = 500;

    public static int ClampPerMinute(int value) => Math.Clamp(value, MinLimit, MaxPerMinute);

    public static int ClampPerHour(int value) => Math.Clamp(value, MinLimit, MaxPerHour);

    public static int ClampPerDay(int value) => Math.Clamp(value, MinLimit, MaxPerDay);

    public static int ResolvePerMinute(int? stored)
        => stored is > 0 ? ClampPerMinute(stored.Value) : DefaultPerMinute;

    public static int ResolvePerHour(int? stored)
        => stored is > 0 ? ClampPerHour(stored.Value) : DefaultPerHour;

    public static int ResolvePerDay(int? stored)
        => stored is > 0 ? ClampPerDay(stored.Value) : DefaultPerDay;
}
