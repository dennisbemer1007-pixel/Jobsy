using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

/// <summary>
/// Teacher (and assigned SchoolAdmin per D2) endpoints. Unassigned class → 404.
/// </summary>
[ApiController]
[Route("api/teacher/classes")]
[Authorize(Policy = JobsyPolicies.RequireSchoolStaff)]
[SchoolsFeatureGate]
public sealed class TeacherClassesController : ControllerBase
{
    private readonly ITeacherPortalService _portal;

    public TeacherClassesController(ITeacherPortalService portal) => _portal = portal;

    [HttpGet("~/api/teacher/classes")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignedClassDto>>> ListMine(
        CancellationToken cancellationToken)
    {
        var rows = await _portal.ListAssignedClassesAsync(User, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{classId:guid}/overview")]
    public async Task<ActionResult<TeacherClassOverviewDto>> Overview(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var dto = await _portal.GetOverviewAsync(User, classId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{classId:guid}/codes")]
    public async Task<ActionResult<IReadOnlyList<TeacherCodeRowDto>>> Codes(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var rows = await _portal.ListCodesAsync(User, classId, cancellationToken);
        return rows is null ? NotFound() : Ok(rows);
    }

    [HttpGet("{classId:guid}/group")]
    public async Task<ActionResult<TeacherGroupInsightsDto>> Group(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var dto = await _portal.GetGroupAsync(User, classId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{classId:guid}/dreamjobs")]
    public async Task<ActionResult<TeacherDreamJobsDto>> DreamJobs(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var dto = await _portal.GetDreamJobsAsync(User, classId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{classId:guid}/codes/{codeId:guid}")]
    public async Task<ActionResult<TeacherCodeDetailDto>> CodeDetail(
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        var dto = await _portal.GetCodeDetailAsync(User, classId, codeId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{classId:guid}/codes/{codeId:guid}/replace")]
    public async Task<ActionResult<SchoolPortalCodeRowDto>> ReplaceCode(
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        var (row, error) = await _portal.ReplaceCodeAsync(User, classId, codeId, cancellationToken);
        if (error == "not_found" || row is null)
        {
            return NotFound();
        }

        return Ok(row);
    }

    [HttpPost("{classId:guid}/test-window")]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> TestWindow(
        Guid classId,
        [FromBody] TestWindowRequest request,
        CancellationToken cancellationToken)
    {
        var (detail, error, code) = await _portal.SetTestWindowAsync(
            User, classId, request.Action, request.ClosesOn, cancellationToken);
        if (code == "not_found")
        {
            return NotFound();
        }

        if (code is "processor_agreement_missing" or "parental_info_missing")
        {
            return Conflict(new
            {
                error = code,
                message = error
            });
        }

        if (detail is null)
        {
            return BadRequest(new { error = code ?? "validation", message = error });
        }

        return Ok(detail);
    }

    [HttpGet("{classId:guid}/codelist.pdf")]
    public async Task<IActionResult> CodeListPdf(Guid classId, CancellationToken cancellationToken)
    {
        var (bytes, fileName, error) = await _portal.BuildCodeListPdfAsync(User, classId, cancellationToken);
        if (error == "not_found" || bytes is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("{classId:guid}/codelist.csv")]
    public async Task<IActionResult> CodeListCsv(Guid classId, CancellationToken cancellationToken)
    {
        var (bytes, fileName, error) = await _portal.BuildCodeListCsvAsync(User, classId, cancellationToken);
        if (error == "not_found" || bytes is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpPost("{classId:guid}/login-pause/clear")]
    public async Task<IActionResult> ClearLoginPause(
        Guid classId,
        [FromServices] IPupilPortalService pupilPortal,
        CancellationToken cancellationToken)
    {
        var (ok, error, status) = await pupilPortal.ClearLoginPauseAsync(User, classId, cancellationToken);
        if (!ok)
        {
            return StatusCode(status, error);
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(new { ok = true });
    }
}
