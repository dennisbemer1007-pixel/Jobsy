using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Small RFC 6238 TOTP implementation. Seeds are Base32 encoded and 160 bits long.</summary>
public static class TotpAuthenticator
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int Digits = 6;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(30);

    public static string GenerateSecret()
        => ToBase32(RandomNumberGenerator.GetBytes(20));

    public static bool VerifyCode(string? secret, string? code, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(secret)
            || string.IsNullOrWhiteSpace(code)
            || code.Length != Digits
            || !code.All(char.IsAsciiDigit))
        {
            return false;
        }

        byte[] key;
        try
        {
            key = FromBase32(secret);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var counter = (long)Math.Floor((utcNow - DateTime.UnixEpoch).TotalSeconds / Period.TotalSeconds);
        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = ComputeCode(key, counter + offset);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected),
                    Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    public static string BuildProvisioningUri(string accountName, string secret)
        => $"otpauth://totp/Lobsy:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer=Lobsy&algorithm=SHA1&digits={Digits}&period=30";

    private static string ComputeCode(byte[] key, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24)
                    | (hash[offset + 1] << 16)
                    | (hash[offset + 2] << 8)
                    | hash[offset + 3];
        return (value % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ToBase32(byte[] bytes)
    {
        var result = new StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                result.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            result.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return result.ToString();
    }

    private static byte[] FromBase32(string text)
    {
        var buffer = 0;
        var bits = 0;
        var result = new List<byte>();
        foreach (var character in text.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant())
        {
            var value = Base32Alphabet.IndexOf(character);
            if (value < 0)
            {
                throw new ArgumentException("Ongeldige authenticatorcode.");
            }

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                result.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        return result.ToArray();
    }
}
