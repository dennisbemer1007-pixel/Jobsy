using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/who-am-i")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateWhoAmIController : ControllerBase
{
    private readonly IWhoAmIService _whoAmI;
    private readonly IUserLookupService _users;

    public CandidateWhoAmIController(IWhoAmIService whoAmI, IUserLookupService users)
    {
        _whoAmI = whoAmI;
        _users = users;
    }

    [HttpGet]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<WhoAmIStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _whoAmI.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<WhoAmIStateDto>> Save(
        [FromBody] SaveWhoAmIRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            return Ok(await _whoAmI.SetIncludeOnCvAsync(user.Id, request.IncludeOnCv, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
