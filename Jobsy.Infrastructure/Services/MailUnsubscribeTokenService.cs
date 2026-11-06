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
        => CreateToken(email, category, userId: null, epoch: 0, issuedUtc);

    public string CreateToken(string email, string category, Guid? userId, int epoch, DateTime? issuedUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentOutOfRangeException.ThrowIfNegative(epoch);

        var issued = issuedUtc ?? DateTime.UtcNow;
        var payload = JsonSerializer.Serialize(new Payload
        {
            EmailHash = EmailAddressHasher.Hash(email),
            Category = category.Trim(),
            IssuedUtc = issued.ToUniversalTime().ToString("O"),
            UserId = userId,
            Epoch = epoch
        });
        var bytes = Encoding.UTF8.GetBytes(payload);
        var protectedBytes = _protector.Protect(bytes);
        return Base64UrlEncode(protectedBytes);
    }

    public bool TryValidate(string? token, out string emailHash, out string category, out DateTime issuedUtc)
    {
        if (!TryRead(token, out var claims) || claims is null)
        {
            emailHash = "";
            category = "";
            issuedUtc = default;
            return false;
        }

        emailHash = claims.EmailHash;
        category = claims.Category;
        issuedUtc = claims.IssuedUtc;
        return true;
    }

    public bool TryRead(string? token, out MailUnsubscribeClaims? claims)
    {
        claims = null;
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

            if (payload.Epoch < 0)
            {
                return false;
            }

            claims = new MailUnsubscribeClaims(
                payload.EmailHash,
                payload.Category,
                issued,
                payload.UserId,
                payload.Epoch);
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
        => BuildUnsubscribeUrl(publicWebBaseUrl, email, category, userId: null, epoch: 0);

    public string BuildUnsubscribeUrl(string publicWebBaseUrl, string email, string category, Guid? userId, int epoch)
    {
        var origin = JobsyPublicUrl.NormalizeOrigin(publicWebBaseUrl).TrimEnd('/');
        var token = CreateToken(email, category, userId, epoch);
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

    private sealed class Payload
    {
        public string EmailHash { get; set; } = "";
        public string Category { get; set; } = "";
        public string IssuedUtc { get; set; } = "";
        public Guid? UserId { get; set; }
        public int Epoch { get; set; }
    }
}
