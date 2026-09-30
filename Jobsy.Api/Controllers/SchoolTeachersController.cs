using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Api.Security;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/school/teachers")]
[Authorize(Policy = JobsyPolicies.RequireSchoolAdmin)]
[SchoolsFeatureGate]
public sealed class SchoolTeachersController : ControllerBase
{
    private readonly ISchoolPortalService _portal;

    public SchoolTeachersController(ISchoolPortalService portal) => _portal = portal;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchoolPortalTeacherListItemDto>>> List(
        [FromQuery] Guid? classId,
        CancellationToken cancellationToken)
    {
        var rows = await _portal.ListTeachersAsync(User, classId, cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<SchoolStaffInviteResultDto>> Invite(
        [FromBody] InviteTeacherRequest request,
        CancellationToken cancellationToken)
    {
        // No role field — Teacher only (D2).
        var (result, error, code) = await _portal.InviteTeacherAsync(User, request, cancellationToken);
        if (code == "forbidden")
        {
            return Forbid();
        }

        if (code == "email_domain_not_allowed")
        {
            return BadRequest(new { error = code, message = error });
        }

        if (result is null)
        {
            return BadRequest(new { error = code ?? "invite_failed", message = error });
        }

        return Ok(result);
    }

    [HttpPut("{teacherUserId:guid}/classes")]
    public async Task<IActionResult> AssignClasses(
        Guid teacherUserId,
        [FromBody] AssignTeacherClassesRequest request,
        CancellationToken cancellationToken)
    {
        var (ok, error) = await _portal.AssignTeacherClassesAsync(
            User, teacherUserId, request.ClassIds ?? [], cancellationToken);
        if (!ok && error == "not_found")
        {
            return NotFound();
        }

        if (!ok)
        {
            return BadRequest(new { error = "validation", message = error });
        }

        return NoContent();
    }

    [HttpPost("{teacherUserId:guid}/resend-invite")]
    public async Task<IActionResult> ResendInvite(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var (ok, error) = await _portal.ResendTeacherInviteAsync(User, teacherUserId, cancellationToken);
        if (!ok && error == "not_found")
        {
            return NotFound();
        }

        if (!ok)
        {
            return BadRequest(new { error = "invite_failed", message = error });
        }

        return NoContent();
    }

    [HttpDelete("{teacherUserId:guid}")]
    public async Task<IActionResult> Remove(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var (ok, error) = await _portal.RemoveTeacherAsync(User, teacherUserId, cancellationToken);
        if (!ok && error == "not_found")
        {
            return NotFound();
        }

        return NoContent();
    }
}
