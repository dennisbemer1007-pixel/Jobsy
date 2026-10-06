using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>
/// Shared secret for <c>X-Jobsy-Origin-Secret</c>. Cloudflare adds it on proxied
/// browser traffic; the web service adds it on server-side calls to the API.
/// </summary>
public static class CloudflareOriginSecret
{
    public const string HeaderName = "X-Jobsy-Origin-Secret";

    /// <summary>
    /// Returns a header-safe secret, or null when unset or unsafe to put in a header.
    /// Never logs the value.
    /// </summary>
    public static string? Normalize(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        var trimmed = secret.Trim();
        if (trimmed.Contains('\r') || trimmed.Contains('\n') || trimmed.Contains('\0'))
        {
            return null;
        }

        return trimmed;
    }

    public static bool Matches(string expected, string? provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);
        return expectedBytes.Length == providedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
