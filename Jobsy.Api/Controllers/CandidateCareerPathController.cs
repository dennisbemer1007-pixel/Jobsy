using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/career-path")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidateCareerPathController : ControllerBase
{
    private readonly ICareerPathPlanGenerationService _plans;
    private readonly ICandidateCompetencyService _competencies;
    private readonly IUserLookupService _users;

    public CandidateCareerPathController(
        ICareerPathPlanGenerationService plans,
        ICandidateCompetencyService competencies,
        IUserLookupService users)
    {
        _plans = plans;
        _competencies = competencies;
        _users = users;
    }

    [HttpPost]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> Generate(
        [FromBody] HorizonCareerPathPlanRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var dream = (request.DreamTitle ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dream))
        {
            return BadRequest(new { message = "Vul een stip op de horizon in." });
        }

        var scores = await _competencies.GetCompletedScoresAsync(user.Id, cancellationToken);
        var hints = new List<string>();
        if (scores is { IsComplete: true })
        {
            foreach (var code in CompetencyTestCatalog.CategoryCodes)
            {
                if (scores.Get(code) >= 60)
                {
                    hints.Add(WhoAmIKeywords.EverydayCompetency(code));
                }
            }
        }

        var snapshot = new HorizonCareerProfileSnapshot(hints, hints.Count > 0);
        var plan = await _plans.GenerateAsync(dream, snapshot, cancellationToken);
        return Ok(HorizonCareerPathPlanDto.From(plan));
    }
}

public sealed record HorizonCareerPathPlanRequest(string? DreamTitle);

public sealed record HorizonCareerPathPlanDto(
    string DreamTitle,
    int MatchPercent,
    string MatchSummary,
    IReadOnlyList<HorizonCareerPathStepDto> Steps)
{
    public static HorizonCareerPathPlanDto From(HorizonCareerPathPlan plan)
        => new(
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            plan.Steps.Select(s => new HorizonCareerPathStepDto(
                s.Id,
                s.Order,
                s.Title,
                s.Status.ToString(),
                s.Summary,
                s.SkillsGap.ToList(),
                s.Courses.ToList(),
                s.MinRequirements.ToList(),
                s.YearsExperienceNeeded,
                s.ActionLabel,
                s.ActionHref,
                s.StepMatchPercent)).ToList());
}

public sealed record HorizonCareerPathStepDto(
    string Id,
    int Order,
    string Title,
    string Status,
    string Summary,
    List<string> SkillsGap,
    List<string> Courses,
    List<string> MinRequirements,
    int YearsExperienceNeeded,
    string ActionLabel,
    string ActionHref,
    int StepMatchPercent);
