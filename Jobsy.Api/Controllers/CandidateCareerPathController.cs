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
    private readonly ICandidateCareerInterestService _career;
    private readonly ICandidateCulturePersonalityService _culture;
    private readonly ICandidateValuesService _values;
    private readonly IUserLookupService _users;

    public CandidateCareerPathController(
        ICareerPathPlanGenerationService plans,
        ICandidateCompetencyService competencies,
        ICandidateCareerInterestService career,
        ICandidateCulturePersonalityService culture,
        ICandidateValuesService values,
        IUserLookupService users)
    {
        _plans = plans;
        _competencies = competencies;
        _career = career;
        _culture = culture;
        _values = values;
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

        var strengths = new List<string>();
        var gaps = new List<string>();

        var competency = await _competencies.GetCompletedScoresAsync(user.Id, cancellationToken);
        if (competency is { IsComplete: true })
        {
            foreach (var code in CompetencyTestCatalog.CategoryCodes)
            {
                var label = WhoAmIKeywords.EverydayCompetency(code);
                var score = competency.Get(code);
                if (score >= 60)
                {
                    strengths.Add(label);
                }
                else if (score is > 0 and < 50)
                {
                    gaps.Add(label);
                }
            }
        }

        var career = await _career.GetCompletedScoresAsync(user.Id, cancellationToken);
        if (career is { IsComplete: true })
        {
            foreach (var code in CareerTestCatalog.RiasecCodes
                         .OrderByDescending(career.Get)
                         .Take(2))
            {
                strengths.Add(CareerCompassBuilder.TypeLabel(code));
            }
        }

        var culture = await _culture.GetCompletedScoresAsync(user.Id, cancellationToken);
        if (culture is { IsComplete: true })
        {
            foreach (var code in CulturePersonalityCatalog.CategoryCodes
                         .OrderByDescending(culture.Get)
                         .Take(2))
            {
                strengths.Add(CulturePersonalityCatalog.EverydayLabel(code));
            }
        }

        var values = await _values.GetCompletedScoresAsync(user.Id, cancellationToken);
        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes
                         .OrderByDescending(values.Get)
                         .Take(2))
            {
                strengths.Add(SchwartzValuesCatalog.EverydayLabel(code));
            }
        }

        strengths = strengths
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        gaps = gaps
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        var snapshot = new HorizonCareerProfileSnapshot(
            strengths,
            gaps,
            HasDnaSignal: strengths.Count > 0 || gaps.Count > 0);
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
