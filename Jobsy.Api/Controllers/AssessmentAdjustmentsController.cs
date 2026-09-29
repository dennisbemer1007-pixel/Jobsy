using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Models;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/assessments")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class AssessmentAdjustmentsController : ControllerBase
{
    private readonly IAssessmentAdjustmentService _adjustments;
    private readonly IAssessmentRetakeService _retakes;
    private readonly IUserLookupService _users;
    private readonly ISampleAssessmentReportPdfService _samplePdf;

    public AssessmentAdjustmentsController(
        IAssessmentAdjustmentService adjustments,
        IAssessmentRetakeService retakes,
        IUserLookupService users,
        ISampleAssessmentReportPdfService samplePdf)
    {
        _adjustments = adjustments;
        _retakes = retakes;
        _users = users;
        _samplePdf = samplePdf;
    }

    [HttpGet("{kind}/sample-report.pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SampleReport(
        string kind,
        [FromQuery] string? lang,
        CancellationToken cancellationToken = default)
    {
        if (!AssessmentKindLabels.TryParse(kind, out var k))
        {
            return BadRequest(new { message = "Unknown assessment kind." });
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var pdf = await _samplePdf.RenderSampleAsync(k, lang, cancellationToken);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HttpGet("{kind}/sample-report-preview")]
    [ProducesResponseType(typeof(SampleAssessmentReportPreview), StatusCodes.Status200OK)]
    public async Task<ActionResult<SampleAssessmentReportPreview>> SampleReportPreview(
        string kind,
        [FromQuery] string? lang,
        CancellationToken cancellationToken = default)
    {
        if (!AssessmentKindLabels.TryParse(kind, out var k))
        {
            return BadRequest(new { message = "Unknown assessment kind." });
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _samplePdf.RenderSamplePreviewAsync(k, lang, cancellationToken));
    }

    [HttpGet("{kind}/adjustments")]
    [ProducesResponseType(typeof(AssessmentAdjustmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssessmentAdjustmentDto>> Get(
        string kind,
        [FromQuery] string variant = "quick",
        CancellationToken cancellationToken = default)
    {
        if (!TryParse(kind, variant, out var k, out var v))
        {
            return BadRequest(new { message = "Unknown assessment kind or variant." });
        }

        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _adjustments.GetAsync(user.Id, k, v, cancellationToken));
    }

    [HttpPost("{kind}/retake")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartRetake(
        string kind,
        [FromQuery] string variant = "quick",
        CancellationToken cancellationToken = default)
    {
        if (!TryParse(kind, variant, out var k, out var v))
        {
            return BadRequest(new { message = "Unknown assessment kind or variant." });
        }

        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var attempt = await _retakes.StartAsync(user.Id, k, v, cancellationToken);
            return Ok(new
            {
                attemptId = attempt.Id,
                status = attempt.Status,
                startedAtUtc = attempt.StartedAtUtc
            });
        }
        catch (AssessmentAdjustmentLimitException ex)
        {
            return Conflict(new { code = ex.Code, remaining = ex.Remaining, max = ex.Max });
        }
    }

    [HttpPost("{kind}/retake/{attemptId:guid}/abandon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AbandonRetake(
        string kind,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        _ = kind; // route segment retained for URL symmetry with other retake endpoints
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await _retakes.AbandonAsync(user.Id, attemptId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{kind}/history")]
    public async Task<IActionResult> History(
        string kind,
        [FromQuery] string variant = "quick",
        CancellationToken cancellationToken = default)
    {
        if (!TryParse(kind, variant, out var k, out var v))
        {
            return BadRequest(new { message = "Unknown assessment kind or variant." });
        }

        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var rows = await _retakes.ListHistoryAsync(user.Id, k, v, cancellationToken);
        return Ok(rows.Select(a => new
        {
            a.Id,
            a.Status,
            a.StartedAtUtc,
            a.CompletedAtUtc
        }));
    }

    private Task<Core.Entities.User?> CurrentUserAsync(CancellationToken cancellationToken)
        => _users.FindByPrincipalAsync(User, cancellationToken);

    private static bool TryParse(string kind, string variant, out AssessmentKind k, out AssessmentVariant v)
    {
        v = string.Equals(variant, "deep", StringComparison.OrdinalIgnoreCase)
            ? AssessmentVariant.Deep
            : AssessmentVariant.Quick;
        return AssessmentKindLabels.TryParse(kind, out k);
    }
}
