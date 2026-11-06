using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Notifications;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class StatusNotificationDispatchGate : IStatusNotificationDispatchGate
{
    private static readonly TimeZoneInfo Amsterdam =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    private readonly JobsyDbContext _db;

    public StatusNotificationDispatchGate(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<StatusNotificationDispatchDecision> EvaluateAsync(
        Guid userId,
        string category,
        CancellationToken cancellationToken = default)
    {
        var soort = StatusNotificationCategories.SoortForCategory(category);
        var pref = await _db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Soort == soort, cancellationToken);

        var viaPush = pref?.ViaPush ?? true;
        var viaMail = pref?.ViaMail ?? true;
        var quietFrom = pref?.NietStorenVan ?? new TimeOnly(21, 0);
        var quietTo = pref?.NietStorenTot ?? new TimeOnly(8, 0);
        var maxPerDay = pref?.MaxPerDag ?? 3;

        if (soort == NotificationPreferenceSoort.Herinnering)
        {
            return new StatusNotificationDispatchDecision(viaPush, viaMail, AllowBell: true);
        }

        var inQuietHours = IsQuietHours(quietFrom, quietTo);
        var overDailyCap = await IsOverDailyCapAsync(userId, maxPerDay, cancellationToken);

        if (inQuietHours || overDailyCap)
        {
            return new StatusNotificationDispatchDecision(
                AllowPush: false,
                AllowMail: false,
                AllowBell: true);
        }

        return new StatusNotificationDispatchDecision(viaPush, viaMail, AllowBell: true);
    }

    private static bool IsQuietHours(TimeOnly from, TimeOnly to)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Amsterdam);
        var now = TimeOnly.FromDateTime(local);
        if (from > to)
        {
            return now >= from || now < to;
        }

        return now >= from && now < to;
    }

    private async Task<bool> IsOverDailyCapAsync(
        Guid userId,
        int maxPerDay,
        CancellationToken cancellationToken)
    {
        if (maxPerDay <= 0)
        {
            return true;
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Amsterdam);
        var dayStartLocal = local.Date;
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(dayStartLocal, DateTimeKind.Unspecified),
            Amsterdam);

        var count = await _db.UserNotifications.AsNoTracking()
            .CountAsync(
                n => n.UserId == userId
                     && n.CreatedAtUtc >= dayStartUtc
                     && !n.Category.StartsWith(StatusNotificationCategories.HerinneringPrefix),
                cancellationToken);

        return count >= maxPerDay;
    }
}
