using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Infrastructure.Security;

/// <summary>
/// PBKDF2-SHA256 password hashing (600k iterations). Stored format:
/// <c>PBKDF2$&lt;iter&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c>
/// </summary>
public static class JobsyPasswordHasher
{
    private const string Prefix = "PBKDF2";
    public const int DefaultIterations = 600_000;
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

        if (!storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            return false;
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
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return true;
        }

        var parts = storedHash.Split('$', 4);
        return parts.Length != 4
               || !string.Equals(parts[0], Prefix, StringComparison.Ordinal)
               || !int.TryParse(parts[1], out var iterations)
               || iterations < DefaultIterations;
    }
}
