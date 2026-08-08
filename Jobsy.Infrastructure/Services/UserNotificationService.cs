using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private readonly JobsyDbContext _db;

    public UserNotificationService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<UserNotification> CreateAsync(
        NotificationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = Truncate(request.Title, 256) ?? string.Empty,
            Body = Truncate(request.Body, 4000) ?? string.Empty,
            Category = Truncate(request.Category, 64) ?? string.Empty,
            DeepLink = Truncate(request.DeepLink, 1024),
            ActionLabel = Truncate(request.ActionLabel, 128),
            ActionUrl = Truncate(request.ActionUrl, 1024),
            RelatedEntityType = Truncate(request.RelatedEntityType, 64),
            RelatedEntityId = request.RelatedEntityId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.UserNotifications.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task CreateForEmailAsync(
        string userEmail,
        string title,
        string body,
        string category,
        string? deepLink = null,
        string? actionLabel = null,
        string? actionUrl = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return;
        }

        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Email.ToLower() == userEmail.Trim().ToLower())
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return;
        }

        await CreateAsync(
            new NotificationCreateRequest(
                userId.Value,
                title,
                body,
                category,
                deepLink,
                actionLabel,
                actionUrl,
                relatedEntityType,
                relatedEntityId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotification>> ListForUserAsync(
        Guid userId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        return await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.UserNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<UserNotification?> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        if (!row.IsRead)
        {
            row.IsRead = true;
            row.ReadAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return row;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, now),
                cancellationToken);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= max ? value : value[..max];
    }
}
