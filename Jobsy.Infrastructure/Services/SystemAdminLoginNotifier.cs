using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SystemAdminLoginNotifier
{
    private readonly ISystemAdminSettings _settings;
    private readonly IAdminAuditLog _audit;
    private readonly ITransactionalMailer? _mailer;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<SystemAdminLoginNotifier> _logger;

    public SystemAdminLoginNotifier(
        ISystemAdminSettings settings,
        IAdminAuditLog audit,
        ITransactionalMailer? mailer,
        IPlatformFeatureService features,
        ILogger<SystemAdminLoginNotifier> logger)
    {
        _settings = settings;
        _audit = audit;
        _mailer = mailer;
        _features = features;
        _logger = logger;
    }

    public async Task NotifySuccessfulLoginAsync(
        User user,
        string authMethod,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsSystemAdminAccount)
        {
            return;
        }

        var occurredAt = DateTime.UtcNow;
        var details = JsonSerializer.Serialize(new
        {
            authMethod,
            ip = ipAddress,
            userId = user.Id
        });

        await _audit.WriteAsync(
            new AdminAuditEntry(
                AdminAuditKeys.AuthSystemAdminLogin,
                AdminAuditKeys.TargetTypes.User,
                user.Id.ToString("D"),
                TargetLabel: "system-admin",
                Reason: "break-glass login",
                DetailsJson: details,
                Result: AdminAuditKeys.Results.Success,
                ActorUserId: user.Id,
                ActorRole: user.Role.ToString(),
                ActorKind: AdminAuditKeys.ActorKinds.Admin,
                IpAddress: ipAddress,
                OccurredAtUtc: occurredAt),
            cancellationToken);

        if (_mailer is null)
        {
            return;
        }

        try
        {
            var features = await _features.GetAsync(cancellationToken);
            var mail = TransactionalEmails.SystemAdminLogin(
                features.PublicWebBaseUrl,
                occurredAt,
                authMethod,
                ipAddress);

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_settings.Email))
            {
                recipients.Add(_settings.Email!);
            }

            if (!string.IsNullOrWhiteSpace(_settings.NotifyEmail))
            {
                recipients.Add(_settings.NotifyEmail!);
            }

            if (recipients.Count == 0 && !string.IsNullOrWhiteSpace(user.Email))
            {
                recipients.Add(user.Email);
            }

            foreach (var to in recipients)
            {
                await _mailer.SendAsync(
                    mail,
                    to,
                    new EmailSendOptions(BypassSuppression: true),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SystemAdmin login notification mail failed for user {UserId}", user.Id);
        }
    }
}
