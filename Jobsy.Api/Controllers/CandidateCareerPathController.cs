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
    private readonly ICandidateCareerPlanService _plans;
    private readonly ICandidateCompetencyService _competencies;
    private readonly ICandidateCareerInterestService _career;
    private readonly ICandidateCulturePersonalityService _culture;
    private readonly ICandidateValuesService _values;
    private readonly IUserLookupService _users;

    public CandidateCareerPathController(
        ICandidateCareerPlanService plans,
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

    [HttpGet]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var plan = await _plans.GetAsync(user.Id, cancellationToken);
        return plan is null ? NoContent() : Ok(HorizonCareerPathPlanDto.From(plan));
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

        try
        {
            var snapshot = await BuildDnaSnapshotAsync(user.Id, cancellationToken);
            var plan = await _plans.GenerateAndSaveAsync(user.Id, dream, snapshot, cancellationToken);
            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("steps/{stepKey}/complete")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> CompleteStep(
        string stepKey,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            var plan = await _plans.CompleteStepAsync(user.Id, stepKey, cancellationToken);
            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("steps/{stepKey}/uncomplete")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> UncompleteStep(
        string stepKey,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            var plan = await _plans.UncompleteStepAsync(user.Id, stepKey, cancellationToken);
            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("courses/owned")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> MarkCourseOwned(
        [FromBody] MarkCareerCourseOwnedRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            var plan = await _plans.MarkCourseOwnedAsync(user.Id, request.CourseName ?? "", cancellationToken);
            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<HorizonCareerProfileSnapshot> BuildDnaSnapshotAsync(Guid userId, CancellationToken cancellationToken)
    {
        var strengths = new List<string>();
        var gaps = new List<string>();

        var competency = await _competencies.GetCompletedScoresAsync(userId, cancellationToken);
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

        var career = await _career.GetCompletedScoresAsync(userId, cancellationToken);
        if (career is { IsComplete: true })
        {
            foreach (var code in CareerTestCatalog.RiasecCodes
                         .OrderByDescending(career.Get)
                         .Take(2))
            {
                strengths.Add(CareerCompassBuilder.TypeLabel(code));
            }
        }

        var culture = await _culture.GetCompletedScoresAsync(userId, cancellationToken);
        if (culture is { IsComplete: true })
        {
            foreach (var code in CulturePersonalityCatalog.CategoryCodes
                         .OrderByDescending(culture.Get)
                         .Take(2))
            {
                strengths.Add(CulturePersonalityCatalog.EverydayLabel(code));
            }
        }

        var values = await _values.GetCompletedScoresAsync(userId, cancellationToken);
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

        return new HorizonCareerProfileSnapshot(
            strengths,
            gaps,
            HasDnaSignal: strengths.Count > 0 || gaps.Count > 0);
    }
}

public sealed record HorizonCareerPathPlanRequest(string? DreamTitle);

public sealed record MarkCareerCourseOwnedRequest(string? CourseName);

public sealed record HorizonCareerPathPlanDto(
    Guid? PlanId,
    string DreamTitle,
    int MatchPercent,
    string MatchSummary,
    bool GoalReached,
    IReadOnlyList<HorizonCareerPathStepDto> Steps)
{
    public static HorizonCareerPathPlanDto From(CareerPlanView plan)
        => new(
            plan.PlanId,
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            plan.GoalReached,
            plan.Steps.Select(s => new HorizonCareerPathStepDto(
                s.StepKey,
                s.Order,
                s.Title,
                s.Status.ToString(),
                s.Summary,
                s.SkillsGap.ToList(),
                s.Courses.Select(c => new HorizonCareerCourseDto(c.Name, c.OnProfile)).ToList(),
                s.MinRequirements.ToList(),
                s.YearsExperienceNeeded,
                s.ActionLabel,
                s.ActionHref,
                s.StepMatchPercent,
                s.CoursesOnProfile,
                s.CoursesTotal)).ToList());

    /// <summary>Legacy helper for content-only generated plans (no persistence).</summary>
    public static HorizonCareerPathPlanDto From(HorizonCareerPathPlan plan)
        => new(
            null,
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            false,
            plan.Steps.Select(s => new HorizonCareerPathStepDto(
                s.Id,
                s.Order,
                s.Title,
                s.Status.ToString(),
                s.Summary,
                s.SkillsGap.ToList(),
                s.Courses.Select(c => new HorizonCareerCourseDto(c, false)).ToList(),
                s.MinRequirements.ToList(),
                s.YearsExperienceNeeded,
                s.ActionLabel,
                s.ActionHref,
                s.StepMatchPercent,
                0,
                s.Courses.Count)).ToList());
}

public sealed record HorizonCareerCourseDto(string Name, bool OnProfile);

public sealed record HorizonCareerPathStepDto(
    string Id,
    int Order,
    string Title,
    string Status,
    string Summary,
    List<string> SkillsGap,
    List<HorizonCareerCourseDto> Courses,
    List<string> MinRequirements,
    int YearsExperienceNeeded,
    string ActionLabel,
    string ActionHref,
    int StepMatchPercent,
    int CoursesOnProfile,
    int CoursesTotal);
