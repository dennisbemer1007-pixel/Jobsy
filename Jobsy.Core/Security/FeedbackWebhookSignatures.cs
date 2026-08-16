using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

public static class FeedbackWebhookSignatures
{
    public static bool TryVerify(string? secret, ReadOnlySpan<byte> rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var provided = signatureHeader.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            provided = provided["sha256=".Length..];
        }

        if (!TryParseHex(provided, out var providedRaw))
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), rawBody);
        return providedRaw.Length == expected.Length
               && CryptographicOperations.FixedTimeEquals(providedRaw, expected);
    }

    public static string ComputeSha256Header(string secret, ReadOnlySpan<byte> rawBody)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), rawBody);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool TryParseHex(string hex, out byte[] bytes)
    {
        bytes = [];
        if (hex.Length % 2 != 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
