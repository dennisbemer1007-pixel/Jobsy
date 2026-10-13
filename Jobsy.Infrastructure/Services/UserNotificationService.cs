using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private static readonly Regex VacancyLink = new(
        @"/vacancies/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            // Never persist bearer tokens in ActionUrl (emails may keep tokenized links separately).
            ActionUrl = Truncate(SanitizeActionUrl(request.ActionUrl), 1024),
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
        var rows = await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Min(200, take * 4))
            .ToListAsync(cancellationToken);
        var live = await KeepLiveAsync(rows, cancellationToken);
        return live.Take(take).ToList();
    }

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        return (await KeepLiveAsync(unread, cancellationToken)).Count;
    }

    private async Task<List<UserNotification>> KeepLiveAsync(
        List<UserNotification> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return rows;
        }

        var appIds = rows
            .Where(n => n.RelatedEntityType == "Application" && n.RelatedEntityId is Guid)
            .Select(n => n.RelatedEntityId!.Value)
            .Distinct()
            .ToList();
        var apps = appIds.Count == 0
            ? []
            : await _db.Applications.AsNoTracking()
                .Where(a => appIds.Contains(a.Id))
                .Select(a => new { a.Id, a.VacancyId })
                .ToListAsync(cancellationToken);
        var liveApps = apps.Select(a => a.Id).ToHashSet();
        var vacIds = rows
            .Where(n => n.RelatedEntityType == "Vacancy" && n.RelatedEntityId is Guid)
            .Select(n => n.RelatedEntityId!.Value)
            .Concat(apps.Select(a => a.VacancyId))
            .Concat(rows.SelectMany(LinkedVacancyIds))
            .Distinct()
            .ToList();
        HashSet<Guid> liveVacs = vacIds.Count == 0
            ? []
            : (await _db.Vacancies.AsNoTracking()
                .Where(v => vacIds.Contains(v.Id))
                .Select(v => v.Id)
                .ToListAsync(cancellationToken)).ToHashSet();
        var appVacancy = apps.ToDictionary(a => a.Id, a => a.VacancyId);

        return rows.Where(n => IsLive(n, liveApps, liveVacs, appVacancy)).ToList();
    }

    private static bool IsLive(
        UserNotification notification,
        HashSet<Guid> liveApps,
        HashSet<Guid> liveVacs,
        Dictionary<Guid, Guid> appVacancy)
    {
        if (notification.RelatedEntityType == "Application" && notification.RelatedEntityId is Guid appId)
        {
            if (!liveApps.Contains(appId))
            {
                return false;
            }

            if (appVacancy.TryGetValue(appId, out var vacancyId) && !liveVacs.Contains(vacancyId))
            {
                return false;
            }
        }

        if (notification.RelatedEntityType == "Vacancy"
            && notification.RelatedEntityId is Guid vacancy
            && !liveVacs.Contains(vacancy))
        {
            return false;
        }

        foreach (var linked in LinkedVacancyIds(notification))
        {
            if (!liveVacs.Contains(linked))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Guid> LinkedVacancyIds(UserNotification notification)
    {
        foreach (var raw in new[] { notification.DeepLink, notification.ActionUrl })
        {
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            var match = VacancyLink.Match(raw);
            if (match.Success && Guid.TryParse(match.Groups[1].Value, out var id))
            {
                yield return id;
            }
        }
    }

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

    /// <summary>
    /// Strip secret <c>token</c> query params from persisted in-app CTAs (AVG / session security).
    /// Safe params like <c>hiredApplicationId</c> are kept.
    /// </summary>
    public static string? SanitizeActionUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl))
        {
            return actionUrl;
        }

        var trimmed = actionUrl.Trim();
        var queryIndex = trimmed.IndexOf('?');
        if (queryIndex < 0)
        {
            return trimmed;
        }

        var path = trimmed[..queryIndex];
        var query = trimmed[(queryIndex + 1)..];
        if (string.IsNullOrWhiteSpace(query))
        {
            return path;
        }

        var kept = new List<string>();
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            var key = eq < 0 ? part : part[..eq];
            if (key.Equals("token", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            kept.Add(part);
        }

        return kept.Count == 0 ? path : path + "?" + string.Join('&', kept);
    }
}
