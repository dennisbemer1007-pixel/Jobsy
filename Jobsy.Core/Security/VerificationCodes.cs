using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Cryptographically strong verification OTPs and constant-time compares.</summary>
public static class VerificationCodes
{
    /// <summary>Max wrong guesses before the current OTP is invalidated.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>SHA-256 / HMAC-SHA256 hex length for stored OTP hashes.</summary>
    public const int HashLength = 64;

    /// <summary>
    /// Built-in fallback pepper mixed into OTP hashes when no deploy-time secret is configured.
    /// Prefer <see cref="ConfigurePepper"/> / env <c>VerificationCodes__Pepper</c> in production.
    /// </summary>
    private const string ApplicationPepper = "Jobsy.VerificationOtp.v1";

    private static string? _configuredPepper;

    /// <summary>Optional deploy-time pepper (call once at startup from configuration).</summary>
    public static void ConfigurePepper(string? pepper)
    {
        _configuredPepper = string.IsNullOrWhiteSpace(pepper) ? null : pepper.Trim();
    }

    private static string EffectivePepper => _configuredPepper ?? ApplicationPepper;

    public static string CreateNumericCode(int digits = 6)
    {
        if (digits is < 4 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(digits));
        }

        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{digits}");
    }

    /// <summary>One-way HMAC hash for at-rest OTP storage (never persist plaintext codes).</summary>
    public static string Hash(string plaintextCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextCode);
        return HashWithPepper(plaintextCode.Trim(), EffectivePepper);
    }

    /// <summary>HMAC-SHA256 hex using the given pepper (tests / future config injection).</summary>
    public static string HashWithPepper(string plaintextCode, string pepper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextCode);
        var key = Encoding.UTF8.GetBytes(pepper ?? string.Empty);
        var data = Encoding.UTF8.GetBytes(plaintextCode.Trim());
        var bytes = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Legacy unsalted SHA-256 (pre-pepper) for verifying in-flight OTPs during rollout.</summary>
    public static string HashLegacyUnsalted(string plaintextCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextCode);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintextCode.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool FixedTimeEquals(string? expected, string? actual)
    {
        expected ??= string.Empty;
        actual ??= string.Empty;
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(actual);
        if (a.Length != b.Length)
        {
            // Still compare to keep timing flatter for wrong-length inputs.
            CryptographicOperations.FixedTimeEquals(a, a);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Compare a submitted plaintext OTP against a stored hash.
    /// Accepts peppered HMAC (current) and legacy unsalted SHA-256 (transition).
    /// </summary>
    public static bool MatchesHash(string? storedHash, string? submittedPlaintext)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(submittedPlaintext))
        {
            return false;
        }

        if (FixedTimeEquals(storedHash, Hash(submittedPlaintext)))
        {
            return true;
        }

        // Transition: configured pepper may differ from the built-in fallback used before rollout.
        if (_configuredPepper is not null
            && FixedTimeEquals(storedHash, HashWithPepper(submittedPlaintext, ApplicationPepper)))
        {
            return true;
        }

        return FixedTimeEquals(storedHash, HashLegacyUnsalted(submittedPlaintext));
    }

    /// <summary>
    /// Increments failed attempts. When the limit is reached, returns true so callers
    /// can clear the OTP and force a fresh send.
    /// </summary>
    public static bool RegisterFailedAttempt(ref int failedAttempts)
    {
        if (failedAttempts < int.MaxValue)
        {
            failedAttempts++;
        }

        return failedAttempts >= MaxFailedAttempts;
    }
}
