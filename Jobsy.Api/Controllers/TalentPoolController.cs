using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/talent")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class TalentPoolController : ControllerBase
{
    private readonly ITalentPoolService _talent;
    private readonly IUserLookupService _users;
    private readonly ICompanyAuthorizationService _authz;

    public TalentPoolController(
        ITalentPoolService talent,
        IUserLookupService users,
        ICompanyAuthorizationService authz)
    {
        _talent = talent;
        _users = users;
        _authz = authz;
    }

    [HttpGet("search")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<AnonymousTalentCardDto>>> Search(
        [FromQuery] string? tags,
        [FromQuery] int? maxTravelMinutes,
        [FromQuery] string? transport,
        [FromQuery] string? availability,
        [FromQuery] string? drivingLicense,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Reject age filters explicitly (equal treatment / AVG).
        if (Request.Query.ContainsKey("minAge")
            || Request.Query.ContainsKey("maxAge")
            || Request.Query.ContainsKey("age")
            || Request.Query.ContainsKey("dateOfBirth"))
        {
            return BadRequest(new { message = "Leeftijdsfilters zijn niet toegestaan." });
        }

        var (companyId, _) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null)
        {
            return Forbid();
        }

        var transportMode = TransportMode.Bike;
        if (!string.IsNullOrWhiteSpace(transport)
            && Enum.TryParse<TransportMode>(transport, ignoreCase: true, out var parsed))
        {
            transportMode = parsed;
        }

        var tagList = string.IsNullOrWhiteSpace(tags)
            ? null
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        var results = await _talent.SearchAsync(
            companyId.Value,
            new TalentPoolSearchQuery(tagList, maxTravelMinutes, transportMode, availability, drivingLicense, take),
            cancellationToken);
        return Ok(results);
    }

    [HttpPost("unlock")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<TalentContactRequestDto>> Unlock(
        [FromBody] TalentUnlockRequest body,
        CancellationToken cancellationToken = default)
    {
        var (companyId, user) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null || user is null)
        {
            return Forbid();
        }

        try
        {
            return Ok(await _talent.UnlockAsync(
                companyId.Value,
                user.Id,
                body.CandidateUserId,
                body.Message ?? "",
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{requestId:guid}/withdraw")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<TalentContactRequestDto>> Withdraw(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, user) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null || user is null)
        {
            return Forbid();
        }

        try
        {
            return Ok(await _talent.WithdrawAndRefundAsync(
                companyId.Value, user.Id, requestId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<TalentContactRequestDto>>> ListRequests(
        CancellationToken cancellationToken = default)
    {
        var (companyId, _) = await ResolveCompanyAsync(cancellationToken);
        if (companyId is null)
        {
            return Forbid();
        }

        return Ok(await _talent.ListForEmployerAsync(companyId.Value, cancellationToken));
    }

    private async Task<(Guid? CompanyId, Core.Entities.User? User)> ResolveCompanyAsync(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return (null, null);
        }

        var accessible = await _authz.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null || accessible.Count == 0)
        {
            return (null, user);
        }

        var companyId = user.CompanyId is Guid home && accessible.Contains(home)
            ? home
            : accessible.First();
        return (companyId, user);
    }
}

public sealed record TalentUnlockRequest(Guid CandidateUserId, string? Message);

[ApiController]
[Route("api/me/talent-contacts")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateTalentContactsController : ControllerBase
{
    private readonly ITalentPoolService _talent;
    private readonly IUserLookupService _users;

    public CandidateTalentContactsController(ITalentPoolService talent, IUserLookupService users)
    {
        _talent = talent;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TalentContactRequestDto>>> List(
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _talent.ListForCandidateAsync(user.Id, cancellationToken));
    }

    [HttpPost("{requestId:guid}/respond")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<TalentContactRequestDto>> Respond(
        Guid requestId,
        [FromBody] TalentRespondRequest body,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _talent.CandidateRespondAsync(
                user.Id, requestId, body.Accept, body.AlreadyPlaced, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed record TalentRespondRequest(bool Accept, bool AlreadyPlaced = false);
