using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/training/partners")]
public sealed class TrainingPartnersController : ControllerBase
{
    private readonly ITrainingPartnerService _partners;
    private readonly IUserLookupService _users;
    private readonly IFeatureFlags _flags;

    public TrainingPartnersController(
        ITrainingPartnerService partners,
        IUserLookupService users,
        IFeatureFlags flags)
    {
        _partners = partners;
        _users = users;
        _flags = flags;
    }

    [HttpGet("career-step")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> CareerStep(
        [FromQuery] string? stepTitle,
        [FromQuery] string? gaps,
        CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var gapList = string.IsNullOrWhiteSpace(gaps)
            ? []
            : gaps.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var rows = await _partners.ListForCareerStepAsync(user.Id, stepTitle, gapList, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("learn-grow")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> LearnGrow(
        [FromQuery] string? q,
        [FromQuery] string? subject,
        [FromQuery] bool freeOnly,
        [FromQuery] string? format,
        [FromQuery] string? duration,
        CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var page = await _partners.GetLearnGrowPageAsync(
            user.Id,
            new TrainingLearnGrowQuery(q, subject, freeOnly, format, duration),
            cancellationToken);
        return Ok(page);
    }

    [HttpGet("competency-growth")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> CompetencyGrowth(CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _partners.GetCompetencyGrowthAsync(user.Id, cancellationToken));
    }

    [HttpGet("progress")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _partners.GetProgressAsync(user.Id, cancellationToken));
    }

    [HttpPost("offers/{offerId:guid}/saved")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> ToggleSaved(Guid offerId, CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await _partners.ToggleSavedAsync(user.Id, offerId, cancellationToken);
        return NoContent();
    }

    [HttpPost("offers/{offerId:guid}/progress")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> SetProgress(
        Guid offerId,
        [FromBody] TrainingProgressRequest body,
        CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await _partners.SetInProgressAsync(user.Id, offerId, body.Current, body.Total, cancellationToken);
        return NoContent();
    }

    [HttpPost("webhook/certificate")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> PartnerCertificateWebhook(
        [FromBody] PartnerCertificateWebhook body,
        CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
        {
            return NotFound();
        }

        await _partners.VerifyEnrollmentFromPartnerAsync(
            body.PartnerExternalId,
            body.CandidateHash,
            body.CertificateTitle,
            cancellationToken);
        return Ok();
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListAdmin(CancellationToken cancellationToken)
        => Ok(await _partners.ListPartnersAdminAsync(cancellationToken));

    [HttpPut("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertAdmin(
        [FromBody] TrainingPartnerAdminUpsert body,
        CancellationToken cancellationToken)
        => Ok(await _partners.UpsertPartnerAdminAsync(body, cancellationToken));

    [HttpPost("admin/{providerId:guid}/import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportAdmin(Guid providerId, CancellationToken cancellationToken)
    {
        await _partners.ImportProviderAsync(providerId, cancellationToken);
        return NoContent();
    }

    private async Task<bool> EnabledAsync(CancellationToken cancellationToken)
        => await _flags.IsEnabledAsync(PlatformFeature.TrainingPartners, cancellationToken);
}

public sealed record TrainingProgressRequest(int Current, int Total);

public sealed record PartnerCertificateWebhook(
    string PartnerExternalId,
    string CandidateHash,
    string CertificateTitle);
