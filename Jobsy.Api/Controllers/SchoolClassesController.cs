using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Api.Security;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/school/classes")]
[Authorize(Policy = JobsyPolicies.RequireSchoolAdmin)]
[SchoolsFeatureGate]
public sealed class SchoolClassesController : ControllerBase
{
    private readonly ISchoolPortalService _portal;

    public SchoolClassesController(ISchoolPortalService portal) => _portal = portal;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchoolPortalClassListItemDto>>> List(
        [FromQuery] int? schoolYearStart,
        CancellationToken cancellationToken)
    {
        var rows = await _portal.ListClassesAsync(User, schoolYearStart, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{classId:guid}")]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> Get(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var detail = await _portal.GetClassAsync(User, classId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> Create(
        [FromBody] CreateSchoolClassRequest request,
        CancellationToken cancellationToken)
    {
        var (detail, error) = await _portal.CreateClassAsync(User, request, cancellationToken);
        if (error == "forbidden")
        {
            return Forbid();
        }

        if (detail is null)
        {
            return BadRequest(new { error = "validation", message = error });
        }

        return CreatedAtAction(nameof(Get), new { classId = detail.Id }, detail);
    }

    [HttpPut("{classId:guid}")]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> Update(
        Guid classId,
        [FromBody] UpdateSchoolClassRequest request,
        CancellationToken cancellationToken)
    {
        var (detail, error) = await _portal.UpdateClassAsync(User, classId, request, cancellationToken);
        if (error == "not_found" || detail is null && error == "not_found")
        {
            return NotFound();
        }

        if (detail is null)
        {
            return BadRequest(new { error = "validation", message = error });
        }

        return Ok(detail);
    }

    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> Delete(
        Guid classId,
        [FromQuery] string confirmName,
        CancellationToken cancellationToken)
    {
        var (ok, error) = await _portal.DeleteClassAsync(User, classId, confirmName ?? "", cancellationToken);
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

    [HttpGet("{classId:guid}/codes")]
    public async Task<ActionResult<IReadOnlyList<SchoolPortalCodeRowDto>>> ListCodes(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var detail = await _portal.GetClassAsync(User, classId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail.Codes);
    }

    [HttpPost("{classId:guid}/codes")]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> AddCodes(
        Guid classId,
        [FromBody] AddCodesRequest request,
        CancellationToken cancellationToken)
    {
        var (detail, error) = await _portal.AddCodesAsync(User, classId, request.Count, cancellationToken);
        if (error == "not_found")
        {
            return NotFound();
        }

        if (detail is null)
        {
            return BadRequest(new { error = "validation", message = error });
        }

        return Ok(detail);
    }

    [HttpPost("{classId:guid}/codes/{codeId:guid}/replace")]
    public async Task<ActionResult<SchoolPortalCodeRowDto>> ReplaceCode(
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        var (row, error) = await _portal.ReplaceCodeAsync(User, classId, codeId, cancellationToken);
        if (error == "not_found")
        {
            return NotFound();
        }

        return Ok(row);
    }

    [HttpDelete("{classId:guid}/codes/{codeId:guid}")]
    public async Task<IActionResult> DeleteCode(
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        var (ok, error) = await _portal.DeleteCodeAsync(User, classId, codeId, cancellationToken);
        if (!ok && error == "not_found")
        {
            return NotFound();
        }

        return NoContent();
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

    [HttpPost("{classId:guid}/parental-confirmation")]
    public async Task<ActionResult<SchoolPortalClassDetailDto>> ParentalConfirmation(
        Guid classId,
        [FromBody] ParentalConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var (detail, error, code) = await _portal.SetParentalConfirmationAsync(
            User, classId, request.Confirmed, cancellationToken);
        if (code == "not_found")
        {
            return NotFound();
        }

        if (detail is null)
        {
            return Conflict(new { error = code, message = error });
        }

        return Ok(detail);
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
                message = error,
                fixHref = code == "parental_info_missing"
                    ? $"/school/klassen/{classId}"
                    : "/school/privacy"
            });
        }

        if (detail is null)
        {
            return BadRequest(new { error = code ?? "validation", message = error });
        }

        return Ok(detail);
    }

    [HttpGet("{classId:guid}/results")]
    public async Task<ActionResult<SchoolPortalResultsDto>> Results(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var (results, error) = await _portal.GetResultsAsync(User, classId, cancellationToken);
        if (error == "not_found" || results is null)
        {
            return NotFound();
        }

        return Ok(results);
    }
}
