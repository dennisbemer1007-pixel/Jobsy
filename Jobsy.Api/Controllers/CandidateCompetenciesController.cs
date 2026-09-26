using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateCompetenciesController : ControllerBase
{
    private readonly ICandidateCompetencyService _competencies;
    private readonly IUserLookupService _users;

    public CandidateCompetenciesController(
        ICandidateCompetencyService competencies,
        IUserLookupService users)
    {
        _competencies = competencies;
        _users = users;
    }

    [HttpGet("competencies")]
    public async Task<ActionResult<CandidateCompetencyStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _competencies.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut("competencies")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateCompetencyStateDto>> Save(
        [FromBody] SaveCandidateCompetenciesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }
        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { message = CandidateConsentRules.ParentalConsentRequiredMessage });
        }
        if (!CandidateConsentRules.HasCurrentTestAiConsent(user))
        {
            return BadRequest(new { message = CandidateConsentRules.TestConsentRequiredMessage });
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
                    return BadRequest(new { message = "Onbekend vraagnummer in de competentietest." });
                }
            }
        }

        try
        {
            return Ok(await _competencies.SaveAsync(user.Id, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("matched-vacancies")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<CandidateMatchedVacancyDto>>> GetMatchedVacancies(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _competencies.GetTopMatchesAsync(user.Id, cancellationToken));
    }
}
