using System.Globalization;
using System.Text;

namespace Jobsy.Core.Security;

/// <summary>Normalize + peppered hash for mail preference keys (no plaintext storage).</summary>
public static class EmailAddressHasher
{
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>64-char lowercase hex HMAC using the VerificationCodes pepper.</summary>
    public static string Hash(string email)
        => VerificationCodes.Hash(Normalize(email));

    public static string HashNormalized(string normalizedEmail)
        => VerificationCodes.Hash(normalizedEmail);
}
