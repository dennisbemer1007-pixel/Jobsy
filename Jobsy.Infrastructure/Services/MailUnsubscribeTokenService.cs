using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Infrastructure.Services;

public sealed class MailUnsubscribeTokenService : IMailUnsubscribeTokenService
{
    public const string ProtectorPurpose = "Lobsy.Mail.Unsubscribe.v1";
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(365);

    private readonly IDataProtector _protector;

    public MailUnsubscribeTokenService(IDataProtectionProvider dataProtection)
    {
        _protector = dataProtection.CreateProtector(ProtectorPurpose);
    }

    public string CreateToken(string email, string category, DateTime? issuedUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        var issued = issuedUtc ?? DateTime.UtcNow;
        var payload = JsonSerializer.Serialize(new Payload(
            EmailAddressHasher.Hash(email),
            category.Trim(),
            issued.ToUniversalTime().ToString("O")));
        var bytes = Encoding.UTF8.GetBytes(payload);
        var protectedBytes = _protector.Protect(bytes);
        return Base64UrlEncode(protectedBytes);
    }

    public bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc)
    {
        emailHash = "";
        category = "";
        issuedUtc = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var bytes = Base64UrlDecode(token.Trim());
            var unprotected = _protector.Unprotect(bytes);
            var json = Encoding.UTF8.GetString(unprotected);
            var payload = JsonSerializer.Deserialize<Payload>(json);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.EmailHash)
                || string.IsNullOrWhiteSpace(payload.Category)
                || !DateTime.TryParse(payload.IssuedUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var issued))
            {
                return false;
            }

            issued = issued.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(issued, DateTimeKind.Utc)
                : issued.ToUniversalTime();

            if (DateTime.UtcNow - issued > TokenLifetime)
            {
                return false;
            }

            emailHash = payload.EmailHash;
            category = payload.Category;
            issuedUtc = issued;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category)
    {
        var origin = JobsyPublicUrl.NormalizeOrigin(publicWebBaseUrl).TrimEnd('/');
        var token = CreateToken(email, category);
        return $"{origin}/mail/afmelden?t={Uri.EscapeDataString(token)}";
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        return Convert.FromBase64String(s);
    }

    private sealed record Payload(string EmailHash, string Category, string IssuedUtc);
}
