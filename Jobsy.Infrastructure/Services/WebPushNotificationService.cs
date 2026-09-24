using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Sends system Web Push notifications to stored browser subscriptions.
/// Falls back to platform-log only when no subscriptions exist.
/// </summary>
public sealed class WebPushNotificationService : IPushNotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly WebPushVapidKeyProvider _vapid;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        JobsyDbContext db,
        WebPushVapidKeyProvider vapid,
        ILogger<WebPushNotificationService> logger)
    {
        _db = db;
        _vapid = vapid;
        _logger = logger;
    }

    public async Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        var redactedTo = EmailServiceStub.RedactEmail(message.UserEmail);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = message.Category ?? "Push",
            Message = $"Push to {redactedTo}: {message.Title}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                To = redactedTo,
                message.Title,
                BodyLength = message.Body?.Length ?? 0,
                message.DeepLink
            }),
            CreatedAt = DateTime.UtcNow
        });

        if (string.IsNullOrWhiteSpace(message.UserEmail))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var email = message.UserEmail.Trim().ToLowerInvariant();
        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Email.ToLower() == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var subscriptions = await _db.WebPushSubscriptions
            .Where(s => s.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogInformation("No Web Push subscriptions for {Email}; in-app/log only.", redactedTo);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var (subject, publicKey, privateKey) = _vapid.GetKeys();
        var client = new WebPushClient();
        var payload = JsonSerializer.Serialize(new
        {
            title = message.Title,
            body = message.Body,
            url = string.IsNullOrWhiteSpace(message.DeepLink) ? "/" : message.DeepLink,
            tag = message.Category ?? "lobsy",
            category = message.Category
        }, JsonOptions);

        var stale = new List<WebPushSubscription>();
        foreach (var sub in subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, new VapidDetails(subject, publicKey, privateKey));
                sub.LastUsedAtUtc = DateTime.UtcNow;
                sub.UpdatedAtUtc = DateTime.UtcNow;
            }
            catch (WebPushException ex) when ((int)ex.StatusCode is 404 or 410)
            {
                _logger.LogInformation("Removing stale Web Push endpoint for {Email}.", redactedTo);
                stale.Add(sub);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web Push send failed for {Email}.", redactedTo);
            }
        }

        if (stale.Count > 0)
        {
            _db.WebPushSubscriptions.RemoveRange(stale);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
