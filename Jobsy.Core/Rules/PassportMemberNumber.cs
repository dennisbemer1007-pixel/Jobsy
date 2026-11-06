using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Stable non-PII display hash for the passport banner (never the database id).</summary>
public static class PassportMemberNumber
{
    public static string Format(Guid userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N")));
        var n = (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
        var code = (n % 100_000).ToString("D5", CultureInfo.InvariantCulture);
        return "LB-" + code;
    }

    /// <summary>Formats "SEP '26" style month from an account-start date (UTC).</summary>
    public static string FormatStartedMonth(DateTime utc, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.GetCultureInfo("nl-NL");
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        var month = culture.DateTimeFormat.GetAbbreviatedMonthName(local.Month).ToUpperInvariant();
        // Drop trailing '.' some cultures add (e.g. "sep.")
        month = month.TrimEnd('.');
        var yy = (local.Year % 100).ToString("D2", CultureInfo.InvariantCulture);
        return $"{month} '{yy}";
    }
}
