using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Cryptographically strong verification OTPs and constant-time compares.</summary>
public static class VerificationCodes
{
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
}
