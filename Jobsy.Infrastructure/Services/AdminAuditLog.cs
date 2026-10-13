using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class AdminAuditLog : IAdminAuditLog
{
    public const int MaxTargetLabelLength = 200;
    public const int MaxReasonLength = 500;
    public const int MaxDetailsJsonBytes = 4096;
    public const int MaxActionLength = 128;
    public const int MaxTargetTypeLength = 64;
    public const int MaxTargetIdLength = 128;
    public const int MaxActorRoleLength = 64;
    public const int MaxCorrelationIdLength = 64;

    private readonly JobsyDbContext _db;
    private readonly ILogger<AdminAuditLog> _logger;
    private readonly string _ipSalt;

    public AdminAuditLog(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger<AdminAuditLog> logger)
    {
        _db = db;
        _logger = logger;
        _ipSalt = configuration["Privacy:IpHashSalt"]
                  ?? configuration["VerificationCodes:Pepper"]
                  ?? configuration["JobsyAuth:DevelopmentAuthSecret"]
                  ?? "jobsy-ip-hash-fallback";
    }

    public async Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            _db.AdminAuditEvents.Add(BuildRow(entry));
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist AdminAuditEvent for action {Action} target {TargetType}/{TargetId}",
                entry.Action,
                entry.TargetType,
                entry.TargetId);
            // Host (Api/Web) wires Sentry; LogError surfaces the failure there for CaptureException.
            _logger.LogError(ex, "AdminAuditEvent persistence failed (action {Action})", entry.Action);
        }
    }

    public void Stage(AdminAuditEntry entry)
    {
        _db.AdminAuditEvents.Add(BuildRow(entry));
    }

    private AdminAuditEvent BuildRow(AdminAuditEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Action))
        {
            throw new ArgumentException("Action is required.", nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.TargetType))
        {
            throw new ArgumentException("TargetType is required.", nameof(entry));
        }

        var details = NormalizeDetails(entry.DetailsJson);
        var result = NormalizeResult(entry.Result);
        var kind = NormalizeActorKind(entry.ActorKind);

        return new AdminAuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = entry.OccurredAtUtc ?? DateTime.UtcNow,
            ActorUserId = entry.ActorUserId,
            ActorRole = Truncate(
                string.IsNullOrWhiteSpace(entry.ActorRole) ? "Unknown" : entry.ActorRole.Trim(),
                MaxActorRoleLength),
            ActorKind = kind,
            Action = Truncate(entry.Action.Trim(), MaxActionLength),
            TargetType = Truncate(entry.TargetType.Trim(), MaxTargetTypeLength),
            TargetId = Truncate((entry.TargetId ?? string.Empty).Trim(), MaxTargetIdLength),
            TargetLabel = Truncate((entry.TargetLabel ?? string.Empty).Trim(), MaxTargetLabelLength),
            Reason = string.IsNullOrWhiteSpace(entry.Reason)
                ? null
                : Truncate(entry.Reason.Trim(), MaxReasonLength),
            DetailsJson = details,
            Result = result,
            CorrelationId = Truncate(
                string.IsNullOrWhiteSpace(entry.CorrelationId)
                    ? Guid.NewGuid().ToString("N")
                    : entry.CorrelationId.Trim(),
                MaxCorrelationIdLength),
            IpHash = HashIp(entry.IpAddress)
        };
    }

    private static string? NormalizeDetails(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return null;
        }

        var trimmed = detailsJson.Trim();
        var bytes = Encoding.UTF8.GetByteCount(trimmed);
        if (bytes > MaxDetailsJsonBytes)
        {
            // Keep valid JSON shape when truncating large payloads.
            using var doc = JsonDocument.Parse(trimmed);
            var compact = JsonSerializer.Serialize(doc.RootElement);
            if (Encoding.UTF8.GetByteCount(compact) <= MaxDetailsJsonBytes)
            {
                return compact;
            }

            return Truncate(compact, MaxDetailsJsonBytes / 2);
        }

        return trimmed;
    }

    private static string NormalizeResult(string? result)
    {
        var r = (result ?? AdminAuditKeys.Results.Success).Trim().ToLowerInvariant();
        return r switch
        {
            AdminAuditKeys.Results.Denied => AdminAuditKeys.Results.Denied,
            AdminAuditKeys.Results.Failed => AdminAuditKeys.Results.Failed,
            _ => AdminAuditKeys.Results.Success
        };
    }

    private static string NormalizeActorKind(string? kind)
    {
        var k = (kind ?? AdminAuditKeys.ActorKinds.Admin).Trim().ToLowerInvariant();
        return k switch
        {
            AdminAuditKeys.ActorKinds.System => AdminAuditKeys.ActorKinds.System,
            AdminAuditKeys.ActorKinds.Self => AdminAuditKeys.ActorKinds.Self,
            _ => AdminAuditKeys.ActorKinds.Admin
        };
    }

    private string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_ipSalt + "|" + ip.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
