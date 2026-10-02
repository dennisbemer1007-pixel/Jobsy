using System.Security.Cryptography;

namespace Jobsy.Core.Diagnostics;

/// <summary>
/// Short, human-readable code a visitor can read out to support (E2).
/// Format: <c>LB-</c> + 4 Crockford base32 characters without 0/1/I/L/O/U so it survives
/// being spelled over the phone. Random per error; never derived from ids or request data.
/// </summary>
public static class SupportCodeGenerator
{
    public const string Prefix = "LB-";
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const int BodyLength = 4;

    public static string Create()
    {
        Span<char> body = stackalloc char[BodyLength];
        for (var i = 0; i < BodyLength; i++)
        {
            body[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return string.Concat(Prefix, new string(body));
    }

    public static bool IsValid(string? code)
    {
        if (code is null || code.Length != Prefix.Length + BodyLength)
        {
            return false;
        }

        if (!code.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = Prefix.Length; i < code.Length; i++)
        {
            if (!Alphabet.Contains(code[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
