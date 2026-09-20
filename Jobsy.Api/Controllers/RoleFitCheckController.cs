using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class RoleFitCheckController : ControllerBase
{
    private readonly IRoleFitCheckService _fit;
    private readonly IUserLookupService _users;

    public RoleFitCheckController(IRoleFitCheckService fit, IUserLookupService users)
    {
        _fit = fit;
        _users = users;
    }

    [HttpGet("role-fit")]
    public async Task<ActionResult<RoleFitCheckStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _fit.GetAsync(user.Id, cancellationToken));
    }

    [HttpPost("role-fit")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<RoleFitCheckStateDto>> Evaluate(
        [FromBody] RoleFitCheckRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            return Ok(await _fit.EvaluateAsync(user.Id, request.JobTitle, cancellationToken));
        }
        catch (RoleFitLockedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record RoleFitCheckRequest(string? JobTitle);
