using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class MfaTrustedDeviceService : IMfaTrustedDeviceService
{
    public static readonly TimeSpan TrustLifetime = TimeSpan.FromDays(30);

    private readonly JobsyDbContext _db;

    public MfaTrustedDeviceService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<(string RawToken, MfaTrustedDevice Row)> CreateAsync(
        Guid userId,
        string? userAgent,
        int sessionVersion,
        CancellationToken cancellationToken = default)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var row = new MfaTrustedDevice
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(TrustLifetime),
            UserAgentSummary = SummarizeUserAgent(userAgent),
            SessionVersionAtCreate = sessionVersion
        };
        _db.MfaTrustedDevices.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return (raw, row);
    }

    public async Task<bool> TryValidateAsync(
        Guid userId,
        string? rawToken,
        int sessionVersion,
        bool authenticatorEnabled,
        CancellationToken cancellationToken = default)
    {
        if (!authenticatorEnabled || string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var hash = HashToken(rawToken.Trim());
        var now = DateTime.UtcNow;
        var row = await _db.MfaTrustedDevices
            .FirstOrDefaultAsync(
                d => d.UserId == userId
                     && d.TokenHash == hash
                     && d.RevokedAtUtc == null
                     && d.ExpiresAtUtc > now,
                cancellationToken);
        if (row is null)
        {
            return false;
        }

        if (row.SessionVersionAtCreate != sessionVersion)
        {
            return false;
        }

        row.LastUsedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _db.MfaTrustedDevices
            .Where(d => d.UserId == userId && d.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.RevokedAtUtc = now;
        }

        if (rows.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.MfaTrustedDevices.AsNoTracking()
            .CountAsync(
                d => d.UserId == userId && d.RevokedAtUtc == null && d.ExpiresAtUtc > now,
                cancellationToken);
    }

    public static string HashToken(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    public static string SummarizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var ua = userAgent.Trim();
        if (ua.Length > 120)
        {
            ua = ua[..120];
        }

        return ua;
    }
}
