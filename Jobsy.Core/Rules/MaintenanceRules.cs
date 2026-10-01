namespace Jobsy.Core.Rules;

/// <summary>
/// Shared contract for the maintenance switch (errors 05, decision E8): how long a visitor is
/// asked to wait, and how long an admin note may be.
/// </summary>
public static class MaintenanceRules
{
    /// <summary>ProblemDetails / Web client error code for a maintenance rejection.</summary>
    public const string Code = "maintenance";

    /// <summary>Used when no expected end time is set, or when it already passed.</summary>
    public const int DefaultRetryAfterSeconds = 300;

    public const int MinRetryAfterSeconds = 60;

    public const int MaxRetryAfterSeconds = 3600;

    /// <summary>Upper bound for the <c>meta http-equiv="refresh"</c> on the 503 page.</summary>
    public const int MaxMetaRefreshSeconds = 300;

    public const int NoteMaxLength = 200;

    /// <summary>Seconds the state is allowed to be stale in the Web host.</summary>
    public const int StatePollSeconds = 15;

    /// <summary>
    /// Memory-cache key for the API's maintenance snapshot, so the middleware does not query the
    /// database on every request. Invalidated the moment the switch is written.
    /// </summary>
    public const string CacheKey = "jobsy.maintenance";

    /// <summary>How long the API may serve a cached snapshot (the write invalidates it anyway).</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Seconds until <paramref name="expectedEndUtc"/>, clamped to 60–3600. A missing or past
    /// end time falls back to <see cref="DefaultRetryAfterSeconds"/>.
    /// </summary>
    public static int RetryAfterSeconds(DateTime? expectedEndUtc, DateTime nowUtc)
    {
        if (expectedEndUtc is not DateTime end)
        {
            return DefaultRetryAfterSeconds;
        }

        var remaining = DateTime.SpecifyKind(end, DateTimeKind.Utc) - DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (remaining <= TimeSpan.Zero)
        {
            return DefaultRetryAfterSeconds;
        }

        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return Math.Clamp(seconds, MinRetryAfterSeconds, MaxRetryAfterSeconds);
    }

    /// <summary>Trims an admin note to <see cref="NoteMaxLength"/>; blank becomes null.</summary>
    public static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length <= NoteMaxLength ? trimmed : trimmed[..NoteMaxLength];
    }
}
