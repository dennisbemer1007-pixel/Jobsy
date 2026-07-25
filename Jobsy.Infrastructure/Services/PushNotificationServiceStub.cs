using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class PushNotificationServiceStub : IPushNotificationService
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<PushNotificationServiceStub> _logger;

    public PushNotificationServiceStub(JobsyDbContext db, ILogger<PushNotificationServiceStub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Push stub → {Email}: {Title} ({DeepLink})",
            message.UserEmail, message.Title, message.DeepLink);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = message.Category ?? "Push",
            Message = $"Push to {message.UserEmail}: {message.Title}",
            DetailsJson = JsonSerializer.Serialize(new
            {
                message.UserEmail,
                message.Title,
                message.Body,
                message.DeepLink
            }),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
