namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Last outcome of acceptatie sollicitaties demo ensure on API startup (read-only diagnostic for /health).
/// </summary>
public static class AcceptatieSollicitatiesDemoStartupState
{
    private static readonly object Gate = new();
    private static string? _outcome;
    private static DateTime? _atUtc;

    public static void Record(string outcome)
    {
        lock (Gate)
        {
            _outcome = outcome;
            _atUtc = DateTime.UtcNow;
        }
    }

    public static AcceptatieSollicitatiesDemoHealthSnapshot Snapshot()
    {
        lock (Gate)
        {
            return new AcceptatieSollicitatiesDemoHealthSnapshot(_outcome, _atUtc);
        }
    }
}

public sealed record AcceptatieSollicitatiesDemoHealthSnapshot(string? Outcome, DateTime? AtUtc);
