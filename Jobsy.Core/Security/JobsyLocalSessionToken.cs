using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>
/// HMAC-signed session proof issued after local-login / external ensure.
/// Lets Production DevelopmentAuth accept non-demo emails without opening
/// header spoofing when only the shared DevelopmentAuth secret is known.
/// Format: <c>base64url(email|userId|exp).base64url(hmac)</c>
/// </summary>
public static class JobsyLocalSessionToken
{
    /// <summary>
    /// Absolute lifetime of a token; refreshed on session-activity so idle users
    /// stay in sync with the sliding cookie without minting an 8h bearer.
    /// </summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Prefer a dedicated signing key so a leaked DevelopmentAuthSecret alone
    /// cannot forge non-demo session tokens. Falls back to DevelopmentAuthSecret for local DX.
    /// </summary>
    public static string? ResolveSigningKey(string? localSessionSigningKey, string? developmentAuthSecret)
    {
        if (!string.IsNullOrWhiteSpace(localSessionSigningKey))
        {
            return localSessionSigningKey.Trim();
        }

        return string.IsNullOrWhiteSpace(developmentAuthSecret)
            ? null
            : developmentAuthSecret.Trim();
    }

    public static string Create(string email, Guid userId, string secret, TimeSpan? lifetime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var ttl = lifetime ?? DefaultLifetime;
        var exp = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
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
        => TryReadSignedPayload(token, secret, ignoreExpiry: false, out email, out userId, out _);

    /// <summary>
    /// Signature-valid payload read for cookie refresh (allows expired tokens so
    /// session-activity can mint a new absolute expiry while the cookie is still live).
    /// </summary>
    public static bool TryReadSignedPayload(
        string? token,
        string secret,
        bool ignoreExpiry,
        out string email,
        out Guid userId,
        out long expUnix)
    {
        email = string.Empty;
        userId = Guid.Empty;
        expUnix = 0;

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
            || !long.TryParse(fields[2], out expUnix))
        {
            return false;
        }

        if (!ignoreExpiry && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
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
