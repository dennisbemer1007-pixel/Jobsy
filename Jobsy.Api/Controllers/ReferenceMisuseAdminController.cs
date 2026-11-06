using System.Security.Claims;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Referee misuse reports. The list shows a masked candidate and the message.
/// Referee e-mail and phone stay off this screen.
/// </summary>
[ApiController]
[Route("api/admin/reference-misuse")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class ReferenceMisuseAdminController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;
    private readonly IAdminAuditLog _audit;
    private readonly IAdminTodoService _todo;

    public ReferenceMisuseAdminController(
        JobsyDbContext db,
        IUserLookupService users,
        IAdminAuditLog audit,
        IAdminTodoService todo)
    {
        _db = db;
        _users = users;
        _audit = audit;
        _todo = todo;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<ReferenceMisuseItemDto>>> List(CancellationToken cancellationToken)
    {
        var rows = await _db.ReferenceMisuseReports.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.CreatedAtUtc,
                r.Message,
                r.HandledAtUtc,
                Name = r.User.FullName
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(r => new ReferenceMisuseItemDto(
            r.Id,
            r.CreatedAtUtc,
            PersonalDataMasker.MaskName(r.Name),
            r.Message,
            r.HandledAtUtc is null ? "open" : "handled")).ToList());
    }

    [HttpPost("{id:guid}/handled")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> MarkHandled(Guid id, CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var row = await _db.ReferenceMisuseReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (row.HandledAtUtc is not null)
        {
            return NoContent();
        }

        row.HandledAtUtc = DateTime.UtcNow;
        row.HandledByUserId = actor.Id;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            new AdminAuditEntry(
                Action: AdminAuditKeys.ReferenceMisuseHandled,
                TargetType: "reference-misuse",
                TargetId: row.Id.ToString("D"),
                TargetLabel: "Melding misbruik referent",
                ActorUserId: actor.Id,
                ActorRole: User.FindFirstValue(ClaimTypes.Role) ?? "Admin",
                CorrelationId: HttpContext.TraceIdentifier),
            cancellationToken);
        _todo.Invalidate();
        return NoContent();
    }
}

public sealed record ReferenceMisuseItemDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string CandidateMasked,
    string? Message,
    string Status);
