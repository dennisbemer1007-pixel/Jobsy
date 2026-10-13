using Jobsy.Api.Admin;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

/// <summary>
/// The maintenance switch (errors 05). Admin only, and reachable while maintenance is on so the
/// switch can always be turned back off.
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class AdminMaintenanceController : ControllerBase
{
    private readonly IPlatformFeatureService _features;
    private readonly IAdminAuditLog _audit;
    private readonly IAdminAuditContext _auditContext;
    private readonly IUserLookupService _users;

    public AdminMaintenanceController(
        IPlatformFeatureService features,
        IAdminAuditLog audit,
        IAdminAuditContext auditContext,
        IUserLookupService users)
    {
        _features = features;
        _audit = audit;
        _auditContext = auditContext;
        _users = users;
    }

    [HttpGet("maintenance")]
    public async Task<ActionResult<MaintenanceStateDto>> Get(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        return Ok(ToDto(snap));
    }

    /// <summary>
    /// Flips the switch. The action key is <c>maintenance.on</c> or <c>maintenance.off</c>, so the
    /// filter's single-action auto-write is replaced by an explicit entry.
    /// </summary>
    [HttpPut("maintenance")]
    [AdminAuditExempt("Writes maintenance.on or maintenance.off explicitly, depending on the new state")]
    public async Task<ActionResult<MaintenanceStateDto>> Put(
        [FromBody] UpdateMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        _auditContext.SuppressAutoWrite = true;

        var expectedEnd = request.ExpectedEndUtc is DateTime end
            ? DateTime.SpecifyKind(end.ToUniversalTime(), DateTimeKind.Utc)
            : (DateTime?)null;

        var snap = await _features.UpdateAsync(
            new PlatformFeatureUpdate(
                MaintenanceEnabled: request.Enabled,
                MaintenanceExpectedEndUtc: expectedEnd,
                ClearMaintenanceExpectedEndUtc: expectedEnd is null,
                MaintenanceNote: MaintenanceRules.NormalizeNote(request.Note) ?? string.Empty),
            cancellationToken);

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        await _audit.WriteAsync(
            new AdminAuditEntry(
                Action: request.Enabled ? AdminAuditKeys.MaintenanceOn : AdminAuditKeys.MaintenanceOff,
                TargetType: AdminAuditKeys.TargetTypes.Setting,
                TargetId: "MaintenanceEnabled",
                TargetLabel: "MaintenanceEnabled",
                Reason: MaintenanceRules.NormalizeNote(request.Note),
                DetailsJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    enabled = snap.MaintenanceEnabled,
                    expectedEndUtc = snap.MaintenanceExpectedEndUtc
                }),
                Result: AdminAuditKeys.Results.Success,
                ActorUserId: actor?.Id,
                ActorRole: JobsyRoles.Admin,
                CorrelationId: HttpContext?.TraceIdentifier,
                IpAddress: HttpContext?.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);

        return Ok(ToDto(snap));
    }

    private static MaintenanceStateDto ToDto(PlatformFeatureSnapshot snap) => new(
        snap.MaintenanceEnabled,
        snap.MaintenanceExpectedEndUtc,
        snap.MaintenanceNote,
        snap.UpdatedAtUtc);
}

/// <summary>Admin view of the switch. <c>Note</c> is admin-only and never public.</summary>
public sealed record MaintenanceStateDto(
    bool Enabled,
    DateTime? ExpectedEndUtc,
    string? Note,
    DateTime? UpdatedAtUtc);

public sealed class UpdateMaintenanceRequest
{
    public bool Enabled { get; set; }

    public DateTime? ExpectedEndUtc { get; set; }

    public string? Note { get; set; }
}
