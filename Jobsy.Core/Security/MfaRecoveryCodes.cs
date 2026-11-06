using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Recovery-code generation and hashing (auth 04). Old 16-hex codes still verify via Normalize.</summary>
public static class MfaRecoveryCodes
{
    public const int CodeCount = 10;
    public const int CodeLength = 8;
    public const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string[] Generate()
    {
        var codes = new string[CodeCount];
        for (var i = 0; i < CodeCount; i++)
        {
            var chars = new char[CodeLength];
            for (var c = 0; c < CodeLength; c++)
            {
                chars[c] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }

            codes[i] = new string(chars);
        }

        return codes;
    }

    public static string FormatGrouped(string code)
    {
        var n = Normalize(code);
        if (n.Length == CodeLength)
        {
            return $"{n[..4]}-{n[4..]}";
        }

        return n;
    }

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return code.Trim().ToUpperInvariant()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    public static string Hash(string code)
    {
        var normalized = Normalize(code);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
