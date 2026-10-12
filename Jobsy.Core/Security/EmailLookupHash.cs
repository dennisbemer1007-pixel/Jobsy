namespace Jobsy.Core.Security;

/// <summary>
/// Blind index for e-mail lookup (login, uniqueness). Uses the VerificationCodes pepper via
/// <see cref="EmailAddressHasher"/>.
/// </summary>
public static class EmailLookupHash
{
    public static string Compute(string email) => EmailAddressHasher.Hash(email);

    public static string? ComputeOrNull(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : Compute(email);
}
