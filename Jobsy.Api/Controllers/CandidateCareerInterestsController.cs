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
public sealed class CandidateCareerInterestsController : ControllerBase
{
    private readonly ICandidateCareerInterestService _career;
    private readonly IUserLookupService _users;

    public CandidateCareerInterestsController(
        ICandidateCareerInterestService career,
        IUserLookupService users)
    {
        _career = career;
        _users = users;
    }

    [HttpGet("career-interests")]
    public async Task<ActionResult<CandidateCareerInterestStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _career.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut("career-interests")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateCareerInterestStateDto>> Save(
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
                    return BadRequest(new { message = "Onbekend vraagnummer in de beroepentest." });
                }
            }
        }

        try
        {
            return Ok(await _career.SaveAsync(user.Id, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
