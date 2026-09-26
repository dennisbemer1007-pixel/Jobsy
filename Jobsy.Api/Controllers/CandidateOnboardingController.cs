using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/onboarding")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateOnboardingController : ControllerBase
{
    private readonly ICandidateOnboardingService _onboarding;
    private readonly ICandidateCareerPlanService _careerPlans;
    private readonly IUserLookupService _users;

    public CandidateOnboardingController(
        ICandidateOnboardingService onboarding,
        ICandidateCareerPlanService careerPlans,
        IUserLookupService users)
    {
        _onboarding = onboarding;
        _careerPlans = careerPlans;
        _users = users;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CandidateOnboardingStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _onboarding.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateOnboardingStateDto>> SaveProgress(
        [FromBody] CandidateOnboardingProgressRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (request.CurrentStep is < 1 or > 10)
        {
            return BadRequest(new { message = "Stap moet tussen 1 en 10 liggen." });
        }

        return Ok(await _onboarding.SaveProgressAsync(user.Id, request, cancellationToken));
    }

    [HttpPost("complete")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateOnboardingStateDto>> Complete(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _onboarding.CompleteAsync(user.Id, cancellationToken));
    }

    /// <summary>Saves dream job title without regenerating the career plan.</summary>
    [HttpPut("dream-job")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> SaveDreamJob(
        [FromBody] OnboardingDreamJobRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        await _careerPlans.SaveDreamAsync(user.Id, request.DreamTitle, cancellationToken);
        return NoContent();
    }
}

public sealed record OnboardingDreamJobRequest(string? DreamTitle);
