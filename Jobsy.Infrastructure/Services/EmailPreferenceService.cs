using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class EmailPreferenceService : IEmailPreferenceService
{
    public static readonly IReadOnlyList<(string Key, string LabelNl, Func<UserRole, bool> Allowed)> Catalog =
    [
        ("PushBom", "Tips over vacatures bij jou in de buurt",
            r => r == UserRole.Candidate),
        ("VacancyEngagementReminder", "Herinnering als je vacature 14 dagen openstaat",
            IsEmployerRole),
        ("CompanyReEngagement", "Bericht als je een tijd niet actief was",
            IsEmployerRole)
    ];

    private readonly JobsyDbContext _db;

    public EmailPreferenceService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsOptedOutAsync(string email, string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        var hash = EmailAddressHasher.Hash(email);
        var key = category.Trim();
        return await _db.EmailOptOuts.AsNoTracking()
            .AnyAsync(o => o.EmailHash == hash && o.Category == key, cancellationToken);
    }

    public async Task OptOutAsync(string email, string category, string source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        var hash = EmailAddressHasher.Hash(email);
        var key = category.Trim();
        var src = string.IsNullOrWhiteSpace(source) ? "Page" : source.Trim();
        if (src.Length > 32)
        {
            src = src[..32];
        }

        var existing = await _db.EmailOptOuts
            .FirstOrDefaultAsync(o => o.EmailHash == hash && o.Category == key, cancellationToken);
        if (existing is not null)
        {
            existing.Source = src;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.EmailOptOuts.Add(new EmailOptOut
        {
            Id = Guid.NewGuid(),
            EmailHash = hash,
            Category = key.Length > 64 ? key[..64] : key,
            CreatedAtUtc = DateTime.UtcNow,
            Source = src
        });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique race — treat as already opted out.
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>Opt-out by precomputed hash (token payload has no plaintext e-mail).</summary>
    public async Task OptOutByHashAsync(string emailHash, string category, string source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        var hash = emailHash.Trim().ToLowerInvariant();
        var key = category.Trim();
        var src = string.IsNullOrWhiteSpace(source) ? "OneClick" : source.Trim();
        if (src.Length > 32)
        {
            src = src[..32];
        }

        var existing = await _db.EmailOptOuts
            .FirstOrDefaultAsync(o => o.EmailHash == hash && o.Category == key, cancellationToken);
        if (existing is not null)
        {
            existing.Source = src;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.EmailOptOuts.Add(new EmailOptOut
        {
            Id = Guid.NewGuid(),
            EmailHash = hash,
            Category = key.Length > 64 ? key[..64] : key,
            CreatedAtUtc = DateTime.UtcNow,
            Source = src
        });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task OptInAsync(string email, string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var hash = EmailAddressHasher.Hash(email);
        await OptInByHashAsync(hash, category, cancellationToken);
    }

    public async Task OptInByHashAsync(string emailHash, string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailHash) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var hash = emailHash.Trim().ToLowerInvariant();
        var key = category.Trim();
        var rows = await _db.EmailOptOuts
            .Where(o => o.EmailHash == hash && o.Category == key)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _db.EmailOptOuts.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailPreferenceItem>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return Array.Empty<EmailPreferenceItem>();
        }

        var hash = EmailAddressHasher.Hash(user.Email);
        var opted = await _db.EmailOptOuts.AsNoTracking()
            .Where(o => o.EmailHash == hash)
            .Select(o => o.Category)
            .ToListAsync(cancellationToken);
        var optedSet = opted.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Catalog
            .Where(c => c.Allowed(user.Role))
            .Select(c => new EmailPreferenceItem(c.Key, c.LabelNl, !optedSet.Contains(c.Key)))
            .ToList();
    }

    public static bool IsOptionalCategory(string? key)
        => EmailOptionalCategories.IsOptional(key);

    private static bool IsEmployerRole(UserRole role)
        => role is UserRole.BranchManager
            or UserRole.RegionalManager
            or UserRole.EnterpriseManager
            or UserRole.Intermediary;
}
