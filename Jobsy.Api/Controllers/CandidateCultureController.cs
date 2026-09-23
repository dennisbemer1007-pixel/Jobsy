using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/culture")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateCultureController : ControllerBase
{
    private readonly ICandidateCulturePersonalityService _culture;
    private readonly IUserLookupService _users;

    public CandidateCultureController(ICandidateCulturePersonalityService culture, IUserLookupService users)
    {
        _culture = culture;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<CandidateCulturePersonalityStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _culture.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateCulturePersonalityStateDto>> Save(
        [FromBody] SaveCandidateCompetenciesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var answers = new Dictionary<int, int>();
        if (request.Answers is not null)
        {
            foreach (var (key, value) in request.Answers)
            {
                if (int.TryParse(key, out var id))
                {
                    answers[id] = value;
                }
                else
                {
                    return BadRequest(new { message = "Onbekend vraagnummer in de cultuurscan." });
                }
            }
        }

        try
        {
            return Ok(await _culture.SaveAsync(user.Id, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
