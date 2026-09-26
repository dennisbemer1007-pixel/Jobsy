using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class DeviceSessionService : IDeviceSessionService
{
    private readonly JobsyDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeviceSessionService> _logger;

    public DeviceSessionService(
        JobsyDbContext db,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<DeviceSessionService> logger)
    {
        _db = db;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DeviceSessionCreateResult> CreateAsync(
        Guid userId,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var raw = DeviceRefreshToken.Generate();
        var now = DateTime.UtcNow;
        var session = new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = DeviceRefreshToken.Hash(raw),
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            ExpiresAtUtc = now.Add(DeviceSessionRules.Lifetime),
            UserAgent = Truncate(userAgent, 512),
            DeviceName = DeviceNameFormatter.FromUserAgent(userAgent)
        };
        _db.UserDeviceSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        CacheCurrentToken(session.Id, raw);
        return new DeviceSessionCreateResult(
            session.Id,
            session.FamilyId,
            raw,
            session.ExpiresAtUtc,
            session.DeviceName);
    }

    public async Task<DeviceSessionRotateResult?> RotateAsync(
        string refreshToken,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var hash = DeviceRefreshToken.Hash(refreshToken);
        var now = DateTime.UtcNow;

        // Serialize rotations for the same hash to avoid false theft detection across tabs.
        // InMemory provider (tests) does not support relational transactions.
        IDisposable? tx = null;
        if (_db.Database.IsRelational())
        {
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
        var session = await _db.UserDeviceSessions
            .FirstOrDefaultAsync(
                s => s.RefreshTokenHash == hash || s.PreviousRefreshTokenHash == hash,
                cancellationToken);

        if (session is null)
        {
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel)
            {
                await rel.RollbackAsync(cancellationToken);
            }

            return null;
        }

        if (session.RevokedAtUtc is not null)
        {
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel2)
            {
                await rel2.RollbackAsync(cancellationToken);
            }

            return null;
        }

        if (session.ExpiresAtUtc <= now)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = DeviceSessionRules.RevokeReasonExpired;
            await _db.SaveChangesAsync(cancellationToken);
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel3)
            {
                await rel3.CommitAsync(cancellationToken);
            }

            return null;
        }

        var matchedCurrent = string.Equals(session.RefreshTokenHash, hash, StringComparison.Ordinal);
        var matchedPrevious = !matchedCurrent
            && string.Equals(session.PreviousRefreshTokenHash, hash, StringComparison.Ordinal)
            && session.PreviousTokenGraceUntilUtc is DateTime grace
            && grace > now;

        if (!matchedCurrent && !matchedPrevious)
        {
            // Token reuse outside the grace window → revoke the whole family.
            await RevokeFamilyInternalAsync(
                session.FamilyId,
                DeviceSessionRules.RevokeReasonTokenReuse,
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel4)
            {
                await rel4.CommitAsync(cancellationToken);
            }

            _logger.LogWarning(
                "Device refresh token reuse detected; revoked family {FamilyId} for user {UserId}",
                session.FamilyId,
                session.UserId);
            return null;
        }

        string newRaw;
        if (matchedPrevious)
        {
            // Concurrent tab/request still holding the pre-rotation token: return the same
            // newly issued token without rotating again (avoids false theft detection).
            if (!_cache.TryGetValue(CurrentTokenCacheKey(session.Id), out newRaw!)
                || string.IsNullOrWhiteSpace(newRaw))
            {
                newRaw = DeviceRefreshToken.Generate();
                session.PreviousRefreshTokenHash = session.RefreshTokenHash;
                session.PreviousTokenGraceUntilUtc = now.Add(DeviceSessionRules.RotationGraceWindow);
                session.RefreshTokenHash = DeviceRefreshToken.Hash(newRaw);
                CacheCurrentToken(session.Id, newRaw);
            }
        }
        else
        {
            newRaw = DeviceRefreshToken.Generate();
            session.PreviousRefreshTokenHash = session.RefreshTokenHash;
            session.PreviousTokenGraceUntilUtc = now.Add(DeviceSessionRules.RotationGraceWindow);
            session.RefreshTokenHash = DeviceRefreshToken.Hash(newRaw);
            CacheCurrentToken(session.Id, newRaw);
        }

        session.ExpiresAtUtc = now.Add(DeviceSessionRules.Lifetime);
        if (now - session.LastUsedAtUtc >= DeviceSessionRules.LastUsedWriteThrottle)
        {
            session.LastUsedAtUtc = now;
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            session.UserAgent = Truncate(userAgent, 512);
            session.DeviceName = DeviceNameFormatter.FromUserAgent(userAgent);
        }

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = DeviceSessionRules.RevokeReasonAdminBlock;
            await _db.SaveChangesAsync(cancellationToken);
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel5)
            {
                await rel5.CommitAsync(cancellationToken);
            }

            return null;
        }

        var minVersion = await _db.PlatformFeatureSettings.AsNoTracking()
            .Select(s => (int?)s.MinimumSessionVersion)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        if (user.SessionVersion < minVersion)
        {
            await RevokeFamilyInternalAsync(
                session.FamilyId,
                DeviceSessionRules.RevokeReasonLogoutAll,
                cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel6)
            {
                await rel6.CommitAsync(cancellationToken);
            }

            return null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction rel7)
        {
            await rel7.CommitAsync(cancellationToken);
        }

        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid home && !companyIds.Contains(home))
        {
            companyIds.Insert(0, home);
        }

        var showHowTo = user.Role == UserRole.Candidate && user.CandidateHowToCompletedAt is null;
        var hasApps = await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.CandidateUserId == user.Id, cancellationToken);
        var hasSales = user.CompanyId is Guid cid
            && await _db.Companies.AsNoTracking()
                .AnyAsync(c => c.Id == cid && c.ReferredBySalesManagerUserId != null, cancellationToken);

        return new DeviceSessionRotateResult(
            user.Id,
            session.Id,
            newRaw,
            session.ExpiresAtUtc,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds,
            showHowTo,
            hasApps,
            hasSales,
            user.SessionVersion,
            CreateLocalSessionToken(user.Email, user.Id));
        }
        finally
        {
            tx?.Dispose();
        }
    }

    public async Task<IReadOnlyList<DeviceSessionListItem>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.UserDeviceSessions.AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .OrderByDescending(s => s.LastUsedAtUtc)
            .Select(s => new DeviceSessionListItem(
                s.Id,
                s.DeviceName ?? "Apparaat",
                s.LastUsedAtUtc,
                s.CreatedAtUtc,
                s.ExpiresAtUtc,
                false))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        Guid userId,
        Guid deviceSessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.UserDeviceSessions
            .FirstOrDefaultAsync(s => s.Id == deviceSessionId && s.UserId == userId, cancellationToken);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return false;
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        session.RevokedReason = reason;
        await RemovePushForDeviceAsync(session.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllAsync(
        Guid userId,
        string reason,
        bool bumpSessionVersion = true,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await _db.UserDeviceSessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedReason = reason;
        }

        var push = await _db.WebPushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
        if (push.Count > 0)
        {
            _db.WebPushSubscriptions.RemoveRange(push);
        }

        if (bumpSessionVersion)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is not null)
            {
                user.SessionVersion++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TouchLastUsedAsync(Guid deviceSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _db.UserDeviceSessions
            .FirstOrDefaultAsync(s => s.Id == deviceSessionId && s.RevokedAtUtc == null, cancellationToken);
        if (session is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now - session.LastUsedAtUtc < DeviceSessionRules.LastUsedWriteThrottle)
        {
            return;
        }

        session.LastUsedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeviceHandoffCreateResult> CreateHandoffAsync(
        Guid userId,
        bool rememberDevice,
        string? returnUrl,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var raw = DeviceRefreshToken.Generate();
        var now = DateTime.UtcNow;
        _db.DeviceLoginHandoffs.Add(new DeviceLoginHandoff
        {
            Id = Guid.NewGuid(),
            CodeHash = DeviceRefreshToken.Hash(raw),
            UserId = userId,
            RememberDevice = rememberDevice,
            ReturnUrl = Truncate(returnUrl, 2048),
            UserAgent = Truncate(userAgent, 512),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(DeviceSessionRules.HandoffLifetime)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new DeviceHandoffCreateResult(raw, now.Add(DeviceSessionRules.HandoffLifetime));
    }

    public async Task<DeviceHandoffExchangeResult?> ExchangeHandoffAsync(
        string code,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var hash = DeviceRefreshToken.Hash(code);
        var now = DateTime.UtcNow;
        var handoff = await _db.DeviceLoginHandoffs
            .FirstOrDefaultAsync(h => h.CodeHash == hash, cancellationToken);
        if (handoff is null || handoff.UsedAtUtc is not null || handoff.ExpiresAtUtc <= now)
        {
            return null;
        }

        handoff.UsedAtUtc = now;

        var user = await _db.Users
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Id == handoff.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        DeviceSessionCreateResult? device = null;
        if (handoff.RememberDevice)
        {
            device = await CreateAsync(user.Id, userAgent ?? handoff.UserAgent, cancellationToken);
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var companyIds = user.CompanyMemberships.Select(m => m.CompanyId).Distinct().ToList();
        if (user.CompanyId is Guid home && !companyIds.Contains(home))
        {
            companyIds.Insert(0, home);
        }

        var showHowTo = user.Role == UserRole.Candidate && user.CandidateHowToCompletedAt is null;
        var hasApps = await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.CandidateUserId == user.Id, cancellationToken);
        var hasSales = user.CompanyId is Guid cid
            && await _db.Companies.AsNoTracking()
                .AnyAsync(c => c.Id == cid && c.ReferredBySalesManagerUserId != null, cancellationToken);

        return new DeviceHandoffExchangeResult(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.CompanyId,
            companyIds,
            showHowTo,
            hasApps,
            hasSales,
            user.SessionVersion,
            CreateLocalSessionToken(user.Email, user.Id),
            handoff.ReturnUrl,
            device);
    }

    public async Task<SessionValiditySnapshot> GetSessionValidityAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var version = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SessionVersion)
            .FirstOrDefaultAsync(cancellationToken);
        var min = await _db.PlatformFeatureSettings.AsNoTracking()
            .Select(s => (int?)s.MinimumSessionVersion)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        return new SessionValiditySnapshot(version, min);
    }

    public async Task IncrementSessionVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.SessionVersion++;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeFamilyInternalAsync(
        Guid familyId,
        string reason,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var family = await _db.UserDeviceSessions
            .Where(s => s.FamilyId == familyId && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var row in family)
        {
            row.RevokedAtUtc = now;
            row.RevokedReason = reason;
            await RemovePushForDeviceAsync(row.Id, cancellationToken);
        }
    }

    private async Task RemovePushForDeviceAsync(Guid deviceSessionId, CancellationToken cancellationToken)
    {
        var push = await _db.WebPushSubscriptions
            .Where(s => s.DeviceSessionId == deviceSessionId)
            .ToListAsync(cancellationToken);
        if (push.Count > 0)
        {
            _db.WebPushSubscriptions.RemoveRange(push);
        }
    }

    private string? CreateLocalSessionToken(string email, Guid userId)
    {
        var secret = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        return JobsyLocalSessionToken.Create(email, userId, secret);
    }

    private void CacheCurrentToken(Guid sessionId, string rawToken)
    {
        _cache.Set(
            CurrentTokenCacheKey(sessionId),
            rawToken,
            DeviceSessionRules.RotationGraceWindow + TimeSpan.FromSeconds(5));
    }

    private static string CurrentTokenCacheKey(Guid sessionId) => $"device-session:current:{sessionId:N}";

    private static string? Truncate(string? value, int max)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= max ? value.Trim() : value.Trim()[..max];
}
