using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class WebPushSubscriptionService : IWebPushSubscriptionService
{
    private readonly JobsyDbContext _db;

    public WebPushSubscriptionService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(Guid userId, WebPushSubscriptionInput input, CancellationToken cancellationToken = default)
    {
        var endpoint = NormalizeEndpoint(input.Endpoint);
        if (endpoint is null
            || string.IsNullOrWhiteSpace(input.P256dh)
            || string.IsNullOrWhiteSpace(input.Auth))
        {
            throw new ArgumentException("Invalid Web Push subscription.");
        }

        var existing = await _db.WebPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            _db.WebPushSubscriptions.Add(new WebPushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = endpoint,
                P256dh = input.P256dh.Trim(),
                Auth = input.Auth.Trim(),
                UserAgent = Truncate(input.UserAgent, 512),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.UserId = userId;
            existing.P256dh = input.P256dh.Trim();
            existing.Auth = input.Auth.Trim();
            existing.UserAgent = Truncate(input.UserAgent, 512);
            existing.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEndpoint(endpoint);
        if (normalized is null)
        {
            return;
        }

        var rows = await _db.WebPushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == normalized)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _db.WebPushSubscriptions.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.WebPushSubscriptions.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        _db.WebPushSubscriptions.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebPushSubscriptionRecord>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WebPushSubscriptions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => new WebPushSubscriptionRecord(s.Id, s.Endpoint, s.CreatedAtUtc, s.LastUsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        endpoint = endpoint.Trim();
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || endpoint.Length > 2048)
        {
            return null;
        }

        return endpoint;
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= max ? value : value[..max];
}
