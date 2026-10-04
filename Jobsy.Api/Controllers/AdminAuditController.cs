using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminAuditController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IAdminAuditContext _auditContext;
    private readonly IConfiguration _configuration;

    public AdminAuditController(
        JobsyDbContext db,
        IAdminAuditContext auditContext,
        IConfiguration configuration)
    {
        _db = db;
        _auditContext = auditContext;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<AdminAuditPageDto>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        [FromQuery] Guid? actor,
        [FromQuery] string? result,
        [FromQuery] string? targetType,
        [FromQuery] string? targetId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = BuildFilteredQuery(from, to, action, actor, result, targetType, targetId, q);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var actorIds = rows.Where(r => r.ActorUserId is Guid).Select(r => r.ActorUserId!.Value).Distinct().ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => ShortAdminName(u.FullName), cancellationToken);

        var items = rows.Select(e => ToItem(e, e.ActorUserId is Guid id && names.TryGetValue(id, out var n) ? n : ActorFallback(e))).ToList();
        return Ok(new AdminAuditPageDto(items, page, pageSize, total));
    }

    [HttpGet("export")]
    [AdminAudit(AdminAuditKeys.ExportCreate, TargetType = AdminAuditKeys.TargetTypes.Export)]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action,
        [FromQuery] Guid? actor,
        [FromQuery] string? result,
        [FromQuery] string? targetType,
        [FromQuery] string? targetId,
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(from, to, action, actor, result, targetType, targetId, q);
        var rows = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(50_000)
            .ToListAsync(cancellationToken);

        _auditContext.TargetLabel = "audit-csv";
        _auditContext.SetDetailsObject(new { kind = "audit", rowCount = rows.Count });

        var sb = new StringBuilder();
        sb.AppendLine("OccurredAtUtc,ActorUserId,ActorRole,ActorKind,Action,TargetType,TargetId,TargetLabel,Reason,Result,CorrelationId");
        foreach (var e in rows)
        {
            sb.Append(Csv(e.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',')
              .Append(Csv(e.ActorUserId?.ToString("D") ?? "")).Append(',')
              .Append(Csv(e.ActorRole)).Append(',')
              .Append(Csv(e.ActorKind)).Append(',')
              .Append(Csv(e.Action)).Append(',')
              .Append(Csv(e.TargetType)).Append(',')
              .Append(Csv(e.TargetId)).Append(',')
              .Append(Csv(e.TargetLabel)).Append(',')
              .Append(Csv(e.Reason ?? "")).Append(',')
              .Append(Csv(e.Result)).Append(',')
              .Append(Csv(e.CorrelationId))
              .AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"lobsy-audit-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminAuditSummaryDto>> Summary(CancellationToken cancellationToken = default)
    {
        var retentionDays = _configuration.GetValue(
            "Privacy:AdminAuditRetentionDays",
            PrivacyConstants.AdminAuditRetentionDays);
        var accessDays = _configuration.GetValue(
            "Privacy:PersonalDataAccessLogRetentionDays",
            PrivacyConstants.PersonalDataAccessLogRetentionDays);

        var lastRetention = await _db.AdminAuditEvents.AsNoTracking()
            .Where(e => e.Action == AdminAuditKeys.PrivacyRetentionRun)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(e => new { e.OccurredAtUtc, e.DetailsJson })
            .FirstOrDefaultAsync(cancellationToken);

        var adminUsers = await _db.Users.AsNoTracking()
            .Where(u => u.Role == Core.Enums.UserRole.Admin && u.IsActive)
            .Select(u => new { u.Id, u.AuthenticatorEnabled })
            .ToListAsync(cancellationToken);
        var withMfa = adminUsers.Count(u => u.AuthenticatorEnabled);

        return Ok(new AdminAuditSummaryDto(
            MaskingEnabled: true,
            AdminAuditRetentionDays: retentionDays,
            PersonalDataAccessLogRetentionDays: accessDays,
            PlatformLogRetentionDays: PrivacyConstants.PlatformLogRetentionDays,
            LastRetentionRunUtc: lastRetention?.OccurredAtUtc,
            LastRetentionDetailsJson: lastRetention?.DetailsJson,
            AdminsWithMfa: withMfa,
            AdminsTotal: adminUsers.Count,
            FailedAdminLogins24h: null));
    }

    [HttpGet("mfa-overview")]
    public async Task<ActionResult<AdminMfaOverviewDto>> MfaOverview(CancellationToken cancellationToken = default)
    {
        var privileged = new[]
        {
            Core.Enums.UserRole.Admin,
            Core.Enums.UserRole.BranchManager,
            Core.Enums.UserRole.RegionalManager,
            Core.Enums.UserRole.EnterpriseManager,
            Core.Enums.UserRole.Intermediary
        };
        var privilegedSet = privileged.ToHashSet();

        var users = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && privilegedSet.Contains(u.Role))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                u.AuthenticatorEnabled
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var lastSeen = await _db.UserDeviceSessions.AsNoTracking()
            .Where(s => userIds.Contains(s.UserId) && s.RevokedAtUtc == null)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(s => s.LastUsedAtUtc) })
            .ToDictionaryAsync(x => x.UserId, x => (DateTime?)x.Last, cancellationToken);

        var byRole = privileged
            .Select(role =>
            {
                var roleUsers = users.Where(u => u.Role == role.ToString()).ToList();
                var enrolled = roleUsers.Count(u => u.AuthenticatorEnabled);
                return new AdminMfaRoleCountDto(
                    role.ToString(),
                    Enrolled: enrolled,
                    NotEnrolled: roleUsers.Count - enrolled,
                    ViaIdp: 0,
                    Total: roleUsers.Count);
            })
            .OrderBy(r => r.Role)
            .ToList();

        var without = users
            .Where(u => !u.AuthenticatorEnabled)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                Last = lastSeen.GetValueOrDefault(u.Id)
            })
            .OrderByDescending(u => u.Last)
            .Take(50)
            .Select(u => new AdminMfaMissingDto(
                u.Id,
                PersonalDataMasker.MaskName(u.FullName),
                PersonalDataMasker.MaskEmail(u.Email),
                u.Role,
                u.Last))
            .ToList();

        var recent = await _db.AdminAuditEvents.AsNoTracking()
            .Where(e => e.Action == AdminAuditKeys.UserMfaReset
                        || e.Action == AdminAuditKeys.SupportAccessGrant
                        || e.Action == AdminAuditKeys.SupportAccessRevoke)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var recentDtos = recent.Select(e => ToItem(e, ActorFallback(e))).ToList();
        return Ok(new AdminMfaOverviewDto(byRole, without, recentDtos));
    }

    private IQueryable<AdminAuditEvent> BuildFilteredQuery(
        DateTime? from,
        DateTime? to,
        string? action,
        Guid? actor,
        string? result,
        string? targetType,
        string? targetId,
        string? q)
    {
        var query = _db.AdminAuditEvents.AsNoTracking().AsQueryable();
        if (from is DateTime f)
        {
            query = query.Where(e => e.OccurredAtUtc >= f.ToUniversalTime());
        }

        if (to is DateTime t)
        {
            query = query.Where(e => e.OccurredAtUtc <= t.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var keys = action.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(e => keys.Contains(e.Action));
        }

        if (actor is Guid actorId)
        {
            query = query.Where(e => e.ActorUserId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            var r = result.Trim().ToLowerInvariant();
            query = query.Where(e => e.Result == r);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var tt = targetType.Trim();
            query = query.Where(e => e.TargetType == tt);
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            var tid = targetId.Trim();
            query = query.Where(e => e.TargetId == tid);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e =>
                e.TargetLabel.ToLower().Contains(term)
                || e.CorrelationId.ToLower().Contains(term)
                || (e.Reason != null && e.Reason.ToLower().Contains(term))
                || e.Action.ToLower().Contains(term));
        }

        return query;
    }

    private static AdminAuditItemDto ToItem(AdminAuditEvent e, string actorName) =>
        new(e.Id, e.OccurredAtUtc, e.ActorUserId, e.ActorRole, e.ActorKind, actorName,
            e.Action, e.TargetType, e.TargetId, e.TargetLabel, e.Reason, e.DetailsJson,
            e.Result, e.CorrelationId);

    private static string ActorFallback(AdminAuditEvent e) =>
        e.ActorKind switch
        {
            AdminAuditKeys.ActorKinds.System => "Systeem",
            AdminAuditKeys.ActorKinds.Self => "Zelf",
            _ => string.IsNullOrWhiteSpace(e.ActorRole) ? "Onbekend" : e.ActorRole
        };

    private static string ShortAdminName(string fullName)
    {
        var parts = (fullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "Onbekend";
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        return $"{parts[0]} {parts[^1][0]}.";
    }

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
