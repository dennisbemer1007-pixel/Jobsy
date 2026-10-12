using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class ProactiveMatchingRules
{
    public const string ConsentVersion = "proactive-matching-2026-01";

    public const int MinScorePercentFloor = 50;
    public const int MinScorePercentCeiling = 99;
    public const int MaxNotificationsPerWeekFloor = 1;
    public const int MaxNotificationsPerWeekCeiling = 21;

    public static int ClampMinScore(int value)
        => Math.Clamp(value, MinScorePercentFloor, MinScorePercentCeiling);

    public static int ClampMaxPerWeek(int value)
        => Math.Clamp(value, MaxNotificationsPerWeekFloor, MaxNotificationsPerWeekCeiling);

    public static bool HasValidConsent(DateTime? acceptedAt, string? version)
        => acceptedAt is not null
           && string.Equals(version, ConsentVersion, StringComparison.Ordinal);

    public static bool ShouldDeliverNow(
        ProactiveMatchingFrequency frequency,
        DateTime utcNow,
        DateTime? lastProcessedAtUtc)
    {
        if (lastProcessedAtUtc is null)
        {
            return true;
        }

        return frequency switch
        {
            ProactiveMatchingFrequency.Immediate => true,
            ProactiveMatchingFrequency.Daily => utcNow - lastProcessedAtUtc.Value >= TimeSpan.FromHours(20),
            ProactiveMatchingFrequency.Weekly => utcNow - lastProcessedAtUtc.Value >= TimeSpan.FromDays(6),
            _ => false
        };
    }

    public static DateTime WeekStartUtc(DateTime utcNow)
    {
        var d = DateOnly.FromDateTime(utcNow);
        var offset = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-offset).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}
