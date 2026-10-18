using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Localization;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class EmailLanguageResolver : IEmailLanguageResolver
{
    private readonly JobsyDbContext _db;

    public EmailLanguageResolver(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<EmailCulture> ResolveAsync(
        EmailRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        return recipient switch
        {
            EmailRecipient.Requester r => EmailCulture.ForLanguage(r.Language),
            EmailRecipient.User u => await FromUserIdAsync(u.UserId, cancellationToken),
            EmailRecipient.ParentOf p => await FromUserIdAsync(p.ChildUserId, cancellationToken),
            EmailRecipient.Address a => await FromAddressAsync(a.Email, cancellationToken),
            _ => EmailCulture.Nl
        };
    }

    private async Task<EmailCulture> FromUserIdAsync(Guid userId, CancellationToken ct)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(ct);
        return EmailCulture.ForLanguage(ReadLanguage(json));
    }

    private async Task<EmailCulture> FromAddressAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmailCulture.Nl;
        }

        var normalized = email.Trim().ToLowerInvariant();
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Email != null && u.Email.ToLower() == normalized)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(ct);
        return EmailCulture.ForLanguage(ReadLanguage(json));
    }

    internal static string? ReadLanguage(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("language", out var langEl)
                && langEl.ValueKind == JsonValueKind.String)
            {
                var raw = langEl.GetString();
                return string.IsNullOrWhiteSpace(raw) ? null : JobsyLanguages.Normalize(raw);
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
