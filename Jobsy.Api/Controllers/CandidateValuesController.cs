using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/values")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateValuesController : ControllerBase
{
    private readonly ICandidateValuesService _values;
    private readonly IUserLookupService _users;

    public CandidateValuesController(ICandidateValuesService values, IUserLookupService users)
    {
        _values = values;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<CandidateValuesStateDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _values.GetAsync(user.Id, cancellationToken));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateValuesStateDto>> Save(
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
            return TestSaveErrors.ParentalConsent();
        }
        if (!CandidateConsentRules.HasCurrentTestAiConsent(user))
        {
            return TestSaveErrors.TestConsent();
        }

        var answers = new Dictionary<int, int>();
        if (request.Answers is not null)
        {
            foreach (var (key, value) in request.Answers)
            {
                if (int.TryParse(key, out var id))
                {
                    if (value is < 1 or > 5)
                    {
                        return TestSaveErrors.InvalidAnswer();
                    }

                    answers[id] = value;
                }
                else
                {
                    return TestSaveErrors.UnknownQuestion();
                }
            }
        }

        try
        {
            return Ok(await _values.SaveAsync(user.Id, answers, request.Complete, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return await TestSaveErrors.LogAsync(this, ex, TestSaveErrors.FromException(ex), "TestSave", cancellationToken);
        }
        catch (AssessmentAdjustmentLimitException ex)
        {
            return Conflict(new { code = ex.Code, remaining = ex.Remaining, max = ex.Max });
        }
    }
}
