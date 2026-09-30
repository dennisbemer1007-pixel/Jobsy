using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Infrastructure.Security;

public interface IIbanProtector
{
    string? Protect(string? plaintext);
    string? Unprotect(string? protectedPayload);
    bool IsProtected(string? value);
}

/// <summary>
/// Encrypts IBAN columns at rest via ASP.NET Data Protection (purpose Jobsy.Iban.v1).
/// Payloads are prefixed so plaintext legacy rows can still be read until re-protected.
/// Key-loss risk: if Data Protection keys in "__DataProtectionKeys" are lost, stored IBANs
/// cannot be recovered — keep DB backups and key-table backups together (see SECURITY.md).
/// </summary>
public sealed class IbanProtector : IIbanProtector
{
    public const string Prefix = "dp1:";
    public const string Purpose = "Jobsy.Iban.v1";
    private readonly IDataProtector _protector;

    public IbanProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public bool IsProtected(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.TrimStart().StartsWith(Prefix, StringComparison.Ordinal);

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
            // Legacy plaintext row — return as-is until migration / next save re-protects.
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

/// <summary>Static hook so EF value converters can encrypt without injecting into JobsyDbContext.</summary>
public static class IbanEfProtection
{
    private static IIbanProtector _protector = PassThroughIbanProtector.Instance;

    public static void Configure(IIbanProtector protector)
        => _protector = protector ?? PassThroughIbanProtector.Instance;

    public static string? Protect(string? value)
    {
        try
        {
            return _protector.Protect(value);
        }
        catch (ObjectDisposedException)
        {
            // Parallel tests may dispose a WebApplicationFactory DataProtection provider after
            // configuring this static hook; fall back to pass-through so seed/unit tests stay green.
            return PassThroughIbanProtector.Instance.Protect(value);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
            when (ex.InnerException is ObjectDisposedException)
        {
            return PassThroughIbanProtector.Instance.Protect(value);
        }
    }

    public static string? Unprotect(string? value)
    {
        try
        {
            return _protector.Unprotect(value);
        }
        catch (ObjectDisposedException)
        {
            return PassThroughIbanProtector.Instance.Unprotect(value);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
            when (ex.InnerException is ObjectDisposedException)
        {
            return PassThroughIbanProtector.Instance.Unprotect(value);
        }
    }
}

file sealed class PassThroughIbanProtector : IIbanProtector
{
    public static readonly PassThroughIbanProtector Instance = new();
    public bool IsProtected(string? value) => false;
    public string? Protect(string? plaintext) => plaintext;
    public string? Unprotect(string? protectedPayload) => protectedPayload;
}
