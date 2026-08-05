using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Infrastructure.Security;

/// <summary>
/// HMAC-signed session proof issued after local-login / external ensure.
/// Lets Production DevelopmentAuth accept non-demo emails without opening
/// header spoofing when only the shared secret is known.
/// Format: <c>base64url(email|userId|exp).base64url(hmac)</c>
/// </summary>
public static class JobsyLocalSessionToken
{
    public static string Create(string email, Guid userId, string secret, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var payload = $"{email.Trim().ToLowerInvariant()}|{userId:N}|{exp}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var sig = Sign(payloadBytes, secret);
        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(sig)}";
    }

    public static bool TryValidate(
        string? token,
        string secret,
        out string email,
        out Guid userId)
    {
        email = string.Empty;
        userId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        byte[] payloadBytes;
        byte[] sigBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            sigBytes = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = Sign(payloadBytes, secret);
        if (expected.Length != sigBytes.Length
            || !CryptographicOperations.FixedTimeEquals(expected, sigBytes))
        {
            return false;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var fields = payload.Split('|', 3);
        if (fields.Length != 3
            || !Guid.TryParseExact(fields[1], "N", out userId)
            || !long.TryParse(fields[2], out var expUnix))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
        {
            return false;
        }

        email = fields[0];
        return !string.IsNullOrWhiteSpace(email);
    }

    private static byte[] Sign(byte[] payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        return HMACSHA256.HashData(key, payload);
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
