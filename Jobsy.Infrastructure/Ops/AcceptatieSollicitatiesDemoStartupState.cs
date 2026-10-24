using System.Text.RegularExpressions;

namespace Jobsy.Infrastructure.Ops;

/// <summary>
/// Last outcome of acceptatie sollicitaties demo ensure on API startup (read-only diagnostic for /health).
/// </summary>
public static class AcceptatieSollicitatiesDemoStartupState
{
    private static readonly object Gate = new();
    private static string? _outcome;
    private static DateTime? _atUtc;

    private static readonly Regex EmailLike = new(
        @"[^\s@]+@[^\s@]+\.[^\s@]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void Record(string outcome)
    {
        lock (Gate)
        {
            _outcome = TruncateOutcome(outcome);
            _atUtc = DateTime.UtcNow;
        }
    }

    public static void RecordError(Exception ex)
    {
        var message = Sanitize(ex.Message);
        if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
        {
            message += " | " + Sanitize(ex.InnerException.Message);
        }

        var outcome = string.IsNullOrWhiteSpace(message)
            ? "error:" + ex.GetType().Name
            : $"error:{ex.GetType().Name}:{message}";
        Record(outcome);
    }

    public static AcceptatieSollicitatiesDemoHealthSnapshot Snapshot()
    {
        lock (Gate)
        {
            return new AcceptatieSollicitatiesDemoHealthSnapshot(_outcome, _atUtc);
        }
    }

    private static string Sanitize(string text)
        => EmailLike.Replace(text, "[email]").Trim();

    private static string TruncateOutcome(string outcome)
        => outcome.Length <= 200 ? outcome : outcome[..200];
}

public sealed record AcceptatieSollicitatiesDemoHealthSnapshot(string? Outcome, DateTime? AtUtc);
