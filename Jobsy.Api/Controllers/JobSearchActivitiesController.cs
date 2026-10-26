using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/job-search-activities")]
[Authorize(Roles = JobsyRoles.Candidate)]
[RequiresFeature(PlatformFeature.Employers)]
public sealed class JobSearchActivitiesController : ControllerBase
{
    private readonly IJobSearchActivityService _activities;
    private readonly IPlatformFeatureService _features;
    private readonly IUserLookupService _users;

    public JobSearchActivitiesController(
        IJobSearchActivityService activities,
        IPlatformFeatureService features,
        IUserLookupService users)
    {
        _activities = activities;
        _features = features;
        _users = users;
    }

    [HttpGet]
    [RequiresFeature(PlatformFeature.ActivityLog)]
    public async Task<ActionResult<JobSearchActivityOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var user = await ResolveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var snap = await _features.GetAsync(cancellationToken);
        var culture = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault() ?? "nl";
        var overview = await _activities.GetOverviewAsync(
            user.Id,
            snap.AfterMatchEnabled,
            culture,
            cancellationToken);
        return Ok(overview);
    }

    [HttpPost]
    [RequiresFeature(PlatformFeature.ActivityLog)]
    public async Task<ActionResult<JobSearchActivityItemDto>> AddManual(
        [FromBody] CreateManualJobSearchActivityRequest request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var culture = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault() ?? "nl";
        try
        {
            var item = await _activities.AddManualAsync(user.Id, request, culture, cancellationToken);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("agreement")]
    [RequiresFeature(PlatformFeature.ActivityLog)]
    public async Task<IActionResult> UpdateAgreement(
        [FromBody] UpdateActivityAgreementRequest request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await _activities.UpdateAgreementAsync(user.Id, request.ActivitiesPerPeriod, cancellationToken);
        return NoContent();
    }

    [HttpGet("period.pdf")]
    [RequiresFeature(PlatformFeature.ActivityLog)]
    public async Task<IActionResult> DownloadPeriodPdf(CancellationToken cancellationToken)
    {
        var user = await ResolveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var snap = await _features.GetAsync(cancellationToken);
        var culture = Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault() ?? "nl";
        var bytes = await _activities.BuildPeriodPdfAsync(
            user.Id,
            snap.AfterMatchEnabled,
            culture,
            cancellationToken);
        return File(bytes, "application/pdf", "lobsy-sollicitatieactiviteiten.pdf");
    }

    private async Task<Jobsy.Core.Entities.User?> ResolveCandidateAsync(CancellationToken cancellationToken)
    {
        return await _users.FindByPrincipalAsync(User, cancellationToken);
    }
}
