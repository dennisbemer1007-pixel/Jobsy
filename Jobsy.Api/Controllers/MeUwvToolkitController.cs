using System.ComponentModel.DataAnnotations;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/uwv")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class MeUwvToolkitController : ControllerBase
{
    private readonly UwvCandidateToolkitService _toolkit;
    private readonly IUserLookupService _users;
    private readonly IFeatureFlags _features;

    public MeUwvToolkitController(
        UwvCandidateToolkitService toolkit,
        IUserLookupService users,
        IFeatureFlags features)
    {
        _toolkit = toolkit;
        _users = users;
        _features = features;
    }

    [HttpGet("toolkit")]
    public async Task<ActionResult<UwvToolkitStateDto>> Get(CancellationToken cancellationToken)
    {
        if (!await UwvOnAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _toolkit.GetStateAsync(user.Id, cancellationToken));
    }

    [HttpPut("ondernemer")]
    public async Task<ActionResult<UwvToolkitStateDto>> SaveOndernemer(
        [FromBody] SaveOndernemerRequest body,
        CancellationToken cancellationToken)
    {
        if (!await UwvOnAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var state = await _toolkit.SaveOndernemerAsync(user.Id, body.Answers, cancellationToken);
            return Ok(state);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("drijfveren")]
    public async Task<ActionResult<UwvToolkitStateDto>> SaveDrijfveren(
        [FromBody] SaveDrijfverenRequest body,
        CancellationToken cancellationToken)
    {
        if (!await UwvOnAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            var state = await _toolkit.SaveDrijfverenAsync(user.Id, body.DriverKeys, cancellationToken);
            return Ok(state);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("adviseur-share")]
    public async Task<ActionResult<UwvAdviseurShareCreatedDto>> CreateShare(
        [FromBody] CreateAdviseurShareRequest body,
        CancellationToken cancellationToken)
    {
        if (!await UwvOnAsync(cancellationToken))
        {
            return NotFound();
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(await _toolkit.CreateShareLinkAsync(
                user.Id,
                body.Sections ?? [],
                body.Language ?? "nl",
                cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<bool> UwvOnAsync(CancellationToken cancellationToken)
        => await _features.IsEnabledAsync(PlatformFeature.UwvFeatures, cancellationToken);

    public sealed record SaveOndernemerRequest([Required] int[] Answers);

    public sealed record SaveDrijfverenRequest([Required] string[] DriverKeys);

    public sealed record CreateAdviseurShareRequest(string[]? Sections, string? Language);
}
