using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PassportPartnersController : ControllerBase
{
    private readonly IPassportPartnerService _partners;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;

    public PassportPartnersController(
        IPassportPartnerService partners,
        IUserLookupService users,
        JobsyDbContext db)
    {
        _partners = partners;
        _users = users;
        _db = db;
    }

    [HttpGet("passport-partners/codes/{code}")]
    [AllowAnonymous]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult> ResolveCode(string code, CancellationToken cancellationToken)
    {
        var summary = await _partners.ResolveCodeAsync(code, cancellationToken);
        if (summary is null)
        {
            return NotFound(new { error = "unknown_code" });
        }

        return Ok(new
        {
            summary.DisplayName,
            summary.Type,
            summary.BranchLabel,
            logoUrl = summary.HasLogo ? $"/api/passport-partners/{summary.PartnerId}/logo" : null
        });
    }

    [HttpGet("passport-partners/{id:guid}/logo")]
    [AllowAnonymous]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult> Logo(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.PassportPartners.AsNoTracking()
            .Where(p => p.Id == id && p.IsActive && p.LogoPng != null)
            .Select(p => new { p.LogoPng, p.LogoContentType })
            .FirstOrDefaultAsync(cancellationToken);
        if (row?.LogoPng is not { Length: > 0 })
        {
            return NotFound();
        }

        return File(row.LogoPng, row.LogoContentType ?? "image/png");
    }

    [HttpGet("me/passport-partners")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Mine(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var links = await _db.PassportPartnerCandidateLinks.AsNoTracking()
            .Include(l => l.PassportPartner)
            .Where(l => l.CandidateUserId == user.Id)
            .OrderByDescending(l => l.StartedAtUtc)
            .Select(l => new
            {
                l.Id,
                l.PassportPartnerId,
                PartnerName = l.PassportPartner!.DisplayName,
                l.Source,
                l.StartedAtUtc,
                l.ConsentGivenAtUtc,
                l.ConsentVersion,
                l.ContactConsentAtUtc,
                l.ReconfirmDueAtUtc,
                l.SuspendedAtUtc,
                l.RevokedAtUtc,
                l.RevokedReason
            })
            .ToListAsync(cancellationToken);
        return Ok(links);
    }

    [HttpPost("me/passport-partners")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Attach([FromBody] AttachPartnerCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _partners.AttachByCodeAsync(
            user.Id,
            request.Code ?? "",
            PassportPartnerLinkSource.AddedCode,
            cancellationToken);
        return result.Ok ? Ok(new { linkId = result.LinkId }) : Map(result);
    }

    [HttpPost("me/passport-partners/{linkId:guid}/consent")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Consent(
        Guid linkId,
        [FromBody] PartnerConsentRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _partners.GiveConsentAsync(
            user.Id,
            linkId,
            request.ConsentVersion,
            request.ContactConsent,
            request.ConfirmAdult,
            cancellationToken);
        return result.Ok ? Ok(new { linkId = result.LinkId }) : Map(result);
    }

    [HttpPost("me/passport-partners/{linkId:guid}/revoke")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Revoke(Guid linkId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _partners.RevokeAsync(user.Id, linkId, PassportPartnerRevokeReason.Candidate, cancellationToken);
        return result.Ok ? Ok(new { linkId = result.LinkId }) : Map(result);
    }

    [HttpPost("me/passport-partners/{linkId:guid}/reconfirm")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [RequiresFeature(PlatformFeature.PassportPartners)]
    public async Task<ActionResult> Reconfirm(Guid linkId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _partners.ReconfirmAsync(user.Id, linkId, cancellationToken);
        return result.Ok ? Ok(new { linkId = result.LinkId }) : Map(result);
    }

    private ActionResult Map(PassportLinkMutation result)
        => result.Error is "not_found" or "unknown_code" or "feature_disabled"
            ? NotFound(new { error = result.Error == "feature_disabled" ? "feature_disabled" : result.Error })
            : BadRequest(new { error = result.Error });
}

public sealed record AttachPartnerCodeRequest(string? Code);

public sealed record PartnerConsentRequest(string? ConsentVersion, bool ContactConsent, bool ConfirmAdult);
