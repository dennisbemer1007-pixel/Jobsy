using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Privacy;

/// <summary>
/// HMAC-signed analytics-consent proof. Plain <c>analytics</c> is only accepted
/// outside Production (tests / local DX). Format: <c>analytics.{expUnix}.{hexHmac}</c>.
/// </summary>
public static class CookieConsentToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string Create(string secret, DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var exp = (now ?? DateTimeOffset.UtcNow).Add(Lifetime).ToUnixTimeSeconds();
        var payload = $"{CookieConsentNames.AnalyticsValue}.{exp}";
        return $"{payload}.{SignHex(payload, secret)}";
    }

    public static bool IsValid(string? token, string? secret)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var parts = token.Split('.', 3, StringSplitOptions.None);
        if (parts.Length != 3
            || !parts[0].Equals(CookieConsentNames.AnalyticsValue, StringComparison.OrdinalIgnoreCase)
            || !long.TryParse(parts[1], out var expUnix))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
        {
            return false;
        }

        var payload = $"{CookieConsentNames.AnalyticsValue}.{expUnix}";
        var expected = SignHex(payload, secret);
        var actual = parts[2];
        if (expected.Length != actual.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(actual));
    }

    public static bool AllowsAnalyticsChoice(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        var value = stored.Trim();
        return value.Equals(CookieConsentNames.AnalyticsValue, StringComparison.OrdinalIgnoreCase)
               || value.StartsWith(CookieConsentNames.AnalyticsValue + ".", StringComparison.OrdinalIgnoreCase);
    }

    private static string SignHex(string payload, string secret)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
}
