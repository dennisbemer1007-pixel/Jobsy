using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/kompas")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateKompasController : ControllerBase
{
    private readonly ICandidateKompasService _kompas;
    private readonly IUserLookupService _users;

    public CandidateKompasController(ICandidateKompasService kompas, IUserLookupService users)
    {
        _kompas = kompas;
        _users = users;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CandidateKompasDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            return Ok(await _kompas.GetAsync(user.Id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
