using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

/// <summary>
/// The moderation queue behind the admin tab "Meldingen" (public-pages 06). Reporter e-mail
/// addresses are masked before they leave the server.
/// </summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IContentReportService _reports;
    private readonly IUserLookupService _users;

    public AdminReportsController(IContentReportService reports, IUserLookupService users)
    {
        _reports = reports;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminContentReportDto>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var openOnly = status?.Trim().ToLowerInvariant() switch
        {
            "open" => true,
            "closed" or "afgehandeld" or "decided" => false,
            _ => (bool?)null
        };

        var rows = await _reports.ListAsync(openOnly, cancellationToken);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpGet("{targetType}/{targetId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AdminContentReportDto>>> ListForTarget(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        if (!TryParseTargetType(targetType, out var parsed))
        {
            return BadRequest(new { code = "invalid_target_type" });
        }

        var rows = await _reports.ListForTargetAsync(parsed, targetId, cancellationToken);
        return Ok(rows.Select(Map).ToList());
    }

    [HttpPost("decide")]
    public async Task<ActionResult<AdminContentReportDecisionDto>> Decide(
        [FromBody] AdminContentReportDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null
            || !TryParseTargetType(request.TargetType, out var targetType)
            || !TryParseDecision(request.Decision, out var decision))
        {
            return BadRequest(new { code = "invalid_decision" });
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        var result = await _reports.DecideAsync(
            new ContentReportDecisionRequest(
                targetType,
                request.TargetId,
                decision,
                request.Reason,
                actor?.Id,
                actor?.Role.ToString()),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                "not_found" => NotFound(new { code = "not_found" }),
                _ => BadRequest(new { code = result.ErrorCode })
            };
        }

        return Ok(new AdminContentReportDecisionDto(result.ClosedReportCount));
    }

    private static bool TryParseTargetType(string? value, out ContentReportTargetType targetType)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out targetType)
           && Enum.IsDefined(targetType);

    private static bool TryParseDecision(string? value, out ContentReportStatus decision)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out decision)
           && Enum.IsDefined(decision)
           && decision != ContentReportStatus.Open;

    private static AdminContentReportDto Map(ContentReportListItem item)
        => new(
            item.Id,
            item.TargetType.ToString(),
            item.TargetId,
            item.TargetKvk,
            item.TargetLabel,
            item.Reason.ToString(),
            item.Details,
            item.ReporterEmailMasked,
            item.CreatedAtUtc,
            item.Status.ToString(),
            item.DecisionReason,
            item.DecidedAtUtc,
            item.TargetReportCount);
}

public sealed record AdminContentReportDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string? TargetKvk,
    string? TargetLabel,
    string Reason,
    string? Details,
    string? ReporterEmailMasked,
    DateTime CreatedAtUtc,
    string Status,
    string? DecisionReason,
    DateTime? DecidedAtUtc,
    int TargetReportCount);

public sealed record AdminContentReportDecisionRequest(
    string? TargetType,
    Guid TargetId,
    string? Decision,
    string? Reason);

public sealed record AdminContentReportDecisionDto(int ClosedReportCount);
