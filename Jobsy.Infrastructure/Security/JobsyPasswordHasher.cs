using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Infrastructure.Security;

/// <summary>
/// PBKDF2-SHA256 password hashing (100k iterations). Stored format:
/// <c>PBKDF2$&lt;iter&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c>
/// </summary>
public static class JobsyPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int DefaultIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return $"{Prefix}${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        // Legacy plaintext (pre-migration): constant-time compare, then caller should rehash.
        if (!storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            return FixedTimeEquals(storedHash, password);
        }

        var parts = storedHash.Split('$', 4);
        if (parts.Length != 4
            || !int.TryParse(parts[1], out var iterations)
            || iterations < 10_000)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static bool NeedsRehash(string storedHash)
        => string.IsNullOrWhiteSpace(storedHash)
           || !storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal);

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(actual);
        if (a.Length != b.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
