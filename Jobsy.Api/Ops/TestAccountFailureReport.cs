using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Api.Ops;

/// <summary>
/// Secret-free CLI failure text: exception type, message, and inner messages.
/// Never includes stack traces, passwords, or connection strings.
/// </summary>
public static partial class TestAccountFailureReport
{
    public static string Format(Exception exception, string? step = null, string? accountKey = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var sb = new StringBuilder();
        sb.Append("Failed:");
        if (!string.IsNullOrWhiteSpace(step))
        {
            sb.Append(" step=").Append(SanitizeLabel(step));
        }

        if (!string.IsNullOrWhiteSpace(accountKey))
        {
            sb.Append(" account=").Append(SanitizeLabel(accountKey));
        }

        AppendException(sb, exception, depth: 0);
        return sb.ToString();
    }

    internal static string Redact(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var redacted = PostgresUri().Replace(message, "postgres://[redacted]");
        return KeyValueSecret().Replace(redacted, "$1=[redacted]");
    }

    private static void AppendException(StringBuilder sb, Exception exception, int depth)
    {
        if (depth > 8)
        {
            return;
        }

        sb.AppendLine();
        sb.Append(depth == 0 ? "  " : "  inner: ");
        sb.Append(exception.GetType().Name);
        sb.Append(": ");
        sb.Append(Redact(exception.Message));

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                AppendException(sb, inner, depth + 1);
            }

            return;
        }

        if (exception.InnerException is not null)
        {
            AppendException(sb, exception.InnerException, depth + 1);
        }
    }

    private static string SanitizeLabel(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            trimmed = trimmed[..64];
        }

        return Redact(trimmed).Replace('\r', ' ').Replace('\n', ' ');
    }

    [GeneratedRegex(@"postgres(?:ql)?://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PostgresUri();

    [GeneratedRegex(
        @"\b(Password|Pwd|Username|User\s*Id|Host|Server)\s*=\s*[^;\s]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueSecret();
}
