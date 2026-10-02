using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

/// <summary>
/// DSA notice and action: anyone can report a vacancy or a company page (public-pages 06).
/// The answer is always the same shape, so the endpoint cannot be used to probe what exists.
/// </summary>
[ApiController]
[Route("api/reports")]
[AllowAnonymous]
[EnableRateLimiting("report")]
public sealed class ReportsController : ControllerBase
{
    private readonly IContentReportService _reports;
    private readonly IUserLookupService _users;

    public ReportsController(IContentReportService reports, IUserLookupService users)
    {
        _reports = reports;
        _users = users;
    }

    [HttpPost]
    public async Task<ActionResult<ContentReportResponse>> Create(
        [FromBody] ContentReportRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || !Enum.IsDefined(request.Reason))
        {
            return BadRequest(new { code = "invalid_report" });
        }

        Guid? reporterUserId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            reporterUserId = user?.Id;
        }

        var result = await _reports.SubmitAsync(
            new ContentReportSubmission(
                request.Type,
                request.Id,
                request.Reason,
                request.Details,
                request.Email,
                reporterUserId,
                request.Language),
            cancellationToken);

        return Ok(new ContentReportResponse(true, result.EmailConfirmationSent));
    }

    /// <summary>Name of the reported page, so <c>/melden</c> can show what the visitor is reporting.</summary>
    [HttpGet("target")]
    public async Task<ActionResult<ContentReportTargetResponse>> Target(
        [FromQuery] string? type,
        [FromQuery] string? id,
        CancellationToken cancellationToken)
    {
        var label = await _reports.DescribeTargetAsync(type, id, cancellationToken);
        return Ok(new ContentReportTargetResponse(label));
    }
}

public sealed record ContentReportRequest(
    string? Type,
    string? Id,
    ContentReportReason Reason,
    string? Details,
    string? Email,
    string? Language);

/// <summary>Deliberately free of ids and target data — the visitor only learns it was accepted.</summary>
public sealed record ContentReportResponse(bool Accepted, bool EmailConfirmationSent);

/// <summary>Null when the page is not public; the form shows a generic label then.</summary>
public sealed record ContentReportTargetResponse(string? Label);
