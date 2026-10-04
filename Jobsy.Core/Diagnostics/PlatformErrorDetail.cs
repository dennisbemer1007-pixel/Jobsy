using System.Text.RegularExpressions;

namespace Jobsy.Core.Diagnostics;

/// <summary>
/// Systeemlogs detail for a new error: the message, the first stack line, and the path.
/// Never the full stack, and never an e-mail address.
/// </summary>
public static class PlatformErrorDetail
{
    private static readonly Regex Email = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Format(Exception? exception, string? path)
    {
        var message = Sanitize(exception?.Message);
        string? frame = null;
        if (!string.IsNullOrWhiteSpace(exception?.StackTrace))
        {
            frame = exception.StackTrace
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Sanitize)
                .FirstOrDefault(line => line.Length > 0 && line != "Er ging iets mis.");
        }

        var where = string.IsNullOrWhiteSpace(path) ? "" : Sanitize(path);
        return string.Join('\n', new[] { message, frame, where }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static string Sanitize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Er ging iets mis.";
        }

        var sanitized = Email.Replace(message, "[redacted]");
        return sanitized.Length > 400 ? sanitized[..400] : sanitized;
    }
}
