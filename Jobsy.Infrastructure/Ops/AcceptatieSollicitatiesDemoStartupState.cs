using System.Text.RegularExpressions;
using Npgsql;

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
        var constraint = FindPostgresConstraintName(ex);
        var message = Sanitize(ex.Message);
        if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
        {
            message += " | " + Sanitize(ex.InnerException.Message);
        }

        Record(FormatErrorOutcome(ex.GetType().Name, constraint, message));
    }

    internal static string FormatErrorOutcome(string exceptionTypeName, string? postgresConstraint, string message)
    {
        message = message.Trim();
        if (postgresConstraint is not null)
        {
            return string.IsNullOrWhiteSpace(message)
                ? $"error:{exceptionTypeName}:{postgresConstraint}"
                : $"error:{exceptionTypeName}:{postgresConstraint}:{message}";
        }

        return string.IsNullOrWhiteSpace(message)
            ? "error:" + exceptionTypeName
            : $"error:{exceptionTypeName}:{message}";
    }

    internal static string? FindPostgresConstraintName(Exception ex)
    {
        for (Exception? cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is PostgresException pg && !string.IsNullOrWhiteSpace(pg.ConstraintName))
            {
                return pg.ConstraintName;
            }
        }

        return null;
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
