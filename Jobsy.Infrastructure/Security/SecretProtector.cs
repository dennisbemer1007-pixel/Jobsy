using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Infrastructure.Security;

public interface ISecretProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? protectedPayload);
}

/// <summary>
/// Encrypts integration secrets at rest via ASP.NET Data Protection.
/// Payloads are prefixed so plaintext legacy rows can still be read until re-saved.
/// </summary>
public sealed class SecretProtector : ISecretProtector
{
    public const string Prefix = "dp1:";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Jobsy.IntegrationSecrets.v1");
    }

    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return null;
        }

        var trimmed = plaintext.Trim();
        if (trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return trimmed;
        }

        return Prefix + _protector.Protect(trimmed);
    }

    public string? Unprotect(string? protectedPayload)
    {
        if (string.IsNullOrWhiteSpace(protectedPayload))
        {
            return null;
        }

        var trimmed = protectedPayload.Trim();
        if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            // Legacy plaintext row — return as-is until next upsert re-protects.
            return trimmed;
        }

        try
        {
            return _protector.Unprotect(trimmed[Prefix.Length..]);
        }
        catch
        {
            return null;
        }
    }
}
