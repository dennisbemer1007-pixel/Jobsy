using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class OneTimeLinkService : IOneTimeLinkService
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<OneTimeLinkService> _logger;

    public OneTimeLinkService(JobsyDbContext db, ILogger<OneTimeLinkService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OneTimeLinkCreateResult> CreateAsync(
        OneTimeLinkPurpose purpose,
        Guid? userId,
        Guid? companyId,
        string email,
        TimeSpan lifetime,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        var normalized = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Ongeldig e-mailadres.", nameof(email));
        }

        if (purpose == OneTimeLinkPurpose.SetPassword && userId is null)
        {
            throw new ArgumentException("SetPassword-link vereist een gebruiker.", nameof(userId));
        }

        if (purpose == OneTimeLinkPurpose.ApiKeyReveal && companyId is null)
        {
            throw new ArgumentException("ApiKeyReveal-link vereist een bedrijf.", nameof(companyId));
        }

        var now = DateTime.UtcNow;
        await InvalidateOlderUnusedAsync(purpose, userId, companyId, now, cancellationToken);

        var token = CreateToken();
        var row = new OneTimeLink
        {
            Id = Guid.NewGuid(),
            Purpose = purpose,
            TokenHash = VerificationCodes.Hash(token),
            UserId = userId,
            CompanyId = companyId,
            Email = normalized,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime),
            CreatedByUserId = createdByUserId
        };

        _db.OneTimeLinks.Add(row);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "OneTimeLink",
            Message = $"Created one-time link {row.Id:N} purpose={(int)purpose}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created one-time link {LinkId} purpose={Purpose}",
            row.Id,
            purpose);

        return new OneTimeLinkCreateResult(row.Id, token);
    }

    public async Task<OneTimeLinkPeekResult> PeekAsync(
        OneTimeLinkPurpose purpose,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidPeek();
        }

        var hash = VerificationCodes.Hash(token.Trim());
        var now = DateTime.UtcNow;
        var row = await _db.OneTimeLinks.AsNoTracking()
            .Include(l => l.Company)
            .FirstOrDefaultAsync(
                l => l.TokenHash == hash
                     && l.Purpose == purpose
                     && l.UsedAtUtc == null
                     && l.ExpiresAtUtc > now,
                cancellationToken);

        if (row is null)
        {
            return InvalidPeek();
        }

        return new OneTimeLinkPeekResult(
            Valid: true,
            LinkId: row.Id,
            UserId: row.UserId,
            CompanyId: row.CompanyId,
            MaskedEmail: EmailServiceStub.RedactEmail(row.Email),
            CompanyName: row.Company?.Name,
            ExpiresAtUtc: row.ExpiresAtUtc);
    }

    public async Task<OneTimeLink?> ConsumeAsync(
        OneTimeLinkPurpose purpose,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var hash = VerificationCodes.Hash(token.Trim());
        var now = DateTime.UtcNow;

        if (_db.Database.IsRelational())
        {
            var claimed = await _db.OneTimeLinks
                .Where(l => l.TokenHash == hash
                            && l.Purpose == purpose
                            && l.UsedAtUtc == null
                            && l.ExpiresAtUtc > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(l => l.UsedAtUtc, now),
                    cancellationToken);

            if (claimed == 0)
            {
                return null;
            }

            return await _db.OneTimeLinks.AsNoTracking()
                .FirstOrDefaultAsync(l => l.TokenHash == hash && l.Purpose == purpose, cancellationToken);
        }

        var row = await _db.OneTimeLinks
            .FirstOrDefaultAsync(
                l => l.TokenHash == hash
                     && l.Purpose == purpose
                     && l.UsedAtUtc == null
                     && l.ExpiresAtUtc > now,
                cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.UsedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-OneTimeLinkRules.RetentionDays);
        return await _db.OneTimeLinks
            .Where(l => (l.UsedAtUtc != null && l.UsedAtUtc < cutoff)
                        || (l.UsedAtUtc == null && l.ExpiresAtUtc < cutoff))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task InvalidateOlderUnusedAsync(
        OneTimeLinkPurpose purpose,
        Guid? userId,
        Guid? companyId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        IQueryable<OneTimeLink> query = _db.OneTimeLinks
            .Where(l => l.Purpose == purpose && l.UsedAtUtc == null);

        if (purpose == OneTimeLinkPurpose.ApiKeyReveal && companyId is Guid company)
        {
            query = query.Where(l => l.CompanyId == company);
        }
        else if (userId is Guid user)
        {
            query = query.Where(l => l.UserId == user);
        }
        else
        {
            return;
        }

        if (_db.Database.IsRelational())
        {
            await query.ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.UsedAtUtc, now),
                cancellationToken);
            return;
        }

        var rows = await query.ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.UsedAtUtc = now;
        }
    }

    private static OneTimeLinkPeekResult InvalidPeek()
        => new(false, null, null, null, null, null, null);

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    /// <summary>32 random bytes as base64url (no padding).</summary>
    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
