using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Infrastructure.Security;

/// <summary>
/// SHA-256 hashing for high-entropy company API keys (enables indexed equality lookup).
/// </summary>
public static class ApiKeyHasher
{
    public const string KeyPrefixLiteral = "lobsy_";

    public static string Hash(string plaintextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextKey);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintextKey.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Cryptographically random API key: lobsy_ + 32 url-safe bytes.</summary>
    public static string GeneratePlaintext()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return KeyPrefixLiteral + token;
    }

    public static string ToDisplayPrefix(string plaintextKey)
    {
        var trimmed = plaintextKey.Trim();
        if (trimmed.Length <= 12)
        {
            return trimmed[..Math.Min(4, trimmed.Length)] + "…";
        }

        return trimmed[..12] + "…";
    }
}
