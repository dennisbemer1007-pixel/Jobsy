namespace Jobsy.Core.Interfaces;

/// <summary>Claims inside a signed unsubscribe token. No plaintext e-mail.</summary>
public sealed record MailUnsubscribeClaims(
    string EmailHash,
    string Category,
    DateTime IssuedUtc,
    Guid? UserId,
    int Epoch);

public interface IMailUnsubscribeTokenService
{
    /// <summary>Create a base64url token valid for one year (no plaintext PII).</summary>
    string CreateToken(string email, string category, DateTime? issuedUtc = null);

    /// <summary>
    /// Same token, bound to a user and a revocation epoch. Bumping the epoch on the user
    /// makes older links stop working.
    /// </summary>
    string CreateToken(string email, string category, Guid? userId, int epoch, DateTime? issuedUtc = null)
        => CreateToken(email, category, issuedUtc);

    bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc);

    bool TryRead(string? token, out MailUnsubscribeClaims? claims)
    {
        claims = null;
        if (!TryValidate(token, out var emailHash, out var category, out var issuedUtc))
        {
            return false;
        }

        claims = new MailUnsubscribeClaims(emailHash, category, issuedUtc, null, 0);
        return true;
    }

    string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category);

    string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category, Guid? userId, int epoch)
        => BuildUnsubscribeUrl(publicWebBaseUrl, email, category);
}
