namespace Jobsy.Core.Interfaces;

public interface IMailUnsubscribeTokenService
{
    /// <summary>Create a base64url token valid for one year (no plaintext PII).</summary>
    string CreateToken(string email, string category, DateTime? issuedUtc = null);

    bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc);

    string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category);
}
