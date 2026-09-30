using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
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
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<HorizonCareerPathPlanDto?>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        var plan = await _plans.GetAsync(user.Id, cancellationToken);
        if (plan is null)
        {
            // Explicit JSON null with 200 so clients do not treat this as 204 NoContent.
            return new JsonResult(null);
        }

        return Ok(HorizonCareerPathPlanDto.From(plan));
    }

    [HttpGet("dream-options")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CareerDreamOptionsDto>> DreamOptions(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        var options = await _plans.GetDreamOptionsAsync(user.Id, q, cancellationToken);
        return Ok(CareerDreamOptionsDto.From(options));
    }

    [HttpGet("archived")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<ArchivedCareerPlanDto>>> Archived(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        var archived = await _plans.ListArchivedAsync(user.Id, cancellationToken);
        return Ok(archived.Select(ArchivedCareerPlanDto.From).ToList());
    }

    [HttpPost("archived/{planId:guid}/restore")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<HorizonCareerPathPlanDto>> RestoreArchived(
        Guid planId,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        var plan = await _plans.RestoreArchivedAsync(user.Id, planId, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { code = CareerPlanErrorCodes.NotFound });
        }

        return Ok(HorizonCareerPathPlanDto.From(plan));
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
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        var catalogKey = string.IsNullOrWhiteSpace(request.CatalogKey) ? null : request.CatalogKey.Trim();
        string? freeText = null;
        if (catalogKey is null)
        {
            freeText = CareerDreamText.Sanitize(request.FreeText ?? request.DreamTitle);
            if (freeText is null)
            {
                return BadRequest(new { code = CareerPlanErrorCodes.DreamTextInvalid });
            }
        }

        try
        {
            var snapshot = await BuildSnapshotAsync(user.Id, cancellationToken);
            var plan = await _plans.GenerateAndSaveAsync(
                user.Id,
                freeText,
                snapshot,
                catalogKey: catalogKey,
                dreamSource: catalogKey is not null ? CareerDreamSources.Catalog : CareerDreamSources.FreeText,
                planLanguage: null,
                force: request.Force,
                cancellationToken);
            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (CareerPlanException ex)
        {
            return ProblemFromException(ex);
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
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        try
        {
            var plan = await _plans.CompleteStepAsync(user.Id, stepKey, cancellationToken);
            if (plan is null)
            {
                return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
            }

            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (CareerPlanException ex)
        {
            return ProblemFromException(ex);
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
            return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
        }

        try
        {
            var plan = await _plans.UncompleteStepAsync(user.Id, stepKey, cancellationToken);
            if (plan is null)
            {
                return NotFound(new { code = CareerPlanErrorCodes.NoPlan });
            }

            return Ok(HorizonCareerPathPlanDto.From(plan));
        }
        catch (CareerPlanException ex)
        {
            return ProblemFromException(ex);
        }
    }

    /// <summary>
    /// Removed self-claim (D2): candidates prove courses via the passport, not by naming a course here.
    /// Kept as a 410 stub for one release so old clients get a clear error instead of a 404/500.
    /// </summary>
    [Obsolete("Removed in Carrière 01 (D2). Candidates claim courses via the passport. Remove this stub in 06.")]
    [HttpPost("courses/claim")]
    [EnableRateLimiting("public-write")]
    public ActionResult ClaimCourse()
        => StatusCode(StatusCodes.Status410Gone, new { code = CareerPlanErrorCodes.UsePassportProof });

    private ActionResult ProblemFromException(CareerPlanException ex)
    {
        var status = ex.Code switch
        {
            CareerPlanErrorCodes.CompletePreviousFirst => StatusCodes.Status409Conflict,
            CareerPlanErrorCodes.UndoLastFirst => StatusCodes.Status409Conflict,
            CareerPlanErrorCodes.GenerationInProgress => StatusCodes.Status409Conflict,
            CareerPlanErrorCodes.GenerationLimit => StatusCodes.Status429TooManyRequests,
            CareerPlanErrorCodes.DreamTextInvalid => StatusCodes.Status400BadRequest,
            CareerPlanErrorCodes.StepNotFound => StatusCodes.Status404NotFound,
            CareerPlanErrorCodes.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        if (ex.RetryAfterUtc is { } retryAfter)
        {
            return StatusCode(status, new { code = ex.Code, retryAfterUtc = retryAfter });
        }

        return StatusCode(status, new { code = ex.Code });
    }

    private async Task<HorizonCareerProfileSnapshot> BuildSnapshotAsync(Guid userId, CancellationToken cancellationToken)
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

/// <summary><paramref name="DreamTitle"/> stays for legacy clients; new clients send <see cref="FreeText"/> or <see cref="CatalogKey"/>.</summary>
public sealed record HorizonCareerPathPlanRequest(string? CatalogKey, string? FreeText, bool Force = false, string? DreamTitle = null);

public sealed record HorizonCareerPathPlanDto(
    string DreamTitle,
    bool FromAi,
    string PlanLanguage,
    string DreamFitBand,
    bool GoalReached,
    int CarriedOverCount,
    IReadOnlyList<HorizonCareerPathStepDto> Steps)
{
    public static HorizonCareerPathPlanDto From(HorizonCareerPathPlanView plan)
        => new(
            plan.DreamTitle,
            plan.FromAi,
            plan.PlanLanguage,
            plan.DreamFitBand,
            plan.GoalReached,
            plan.CarriedOverCount,
            plan.Steps.Select(s => new HorizonCareerPathStepDto(
                s.Id,
                s.Order,
                s.Title,
                s.Status,
                s.Summary,
                s.SkillsGap.ToList(),
                s.Courses.Select(c => c.Name).ToList(),
                s.Courses.Select(c => new HorizonCareerCourseDto(c.Name, c.OnProfile)).ToList(),
                s.MinRequirements.ToList(),
                s.YearsExperienceNeeded,
                s.StepFitBand,
                s.HeldBack,
                s.MatchedCourseCount,
                s.ActionKinds.ToList())).ToList());
}

public sealed record HorizonCareerPathStepDto(
    string Id,
    int Order,
    string Title,
    string Status,
    string Summary,
    List<string> SkillsGap,
    List<string> Courses,
    List<HorizonCareerCourseDto> CourseStatuses,
    List<string> MinRequirements,
    int YearsExperienceNeeded,
    string StepFitBand,
    bool HeldBack,
    int MatchedCourseCount,
    List<string> ActionKinds);

public sealed record HorizonCareerCourseDto(string Name, bool OnProfile);

public sealed record CareerDreamOptionDto(string? CatalogKey, string Title, string? ReasonKey)
{
    public static CareerDreamOptionDto From(CareerDreamOptionView view)
        => new(view.CatalogKey, view.Title, view.ReasonKey);
}

public sealed record CareerDreamOptionsDto(
    IReadOnlyList<CareerDreamOptionDto> Suggestions,
    IReadOnlyList<CareerDreamOptionDto> Results)
{
    public static CareerDreamOptionsDto From(CareerDreamOptionsView view)
        => new(
            view.Suggestions.Select(CareerDreamOptionDto.From).ToList(),
            view.Results.Select(CareerDreamOptionDto.From).ToList());
}

public sealed record ArchivedCareerPlanDto(
    Guid PlanId,
    string DreamTitle,
    DateTime ArchivedAtUtc,
    int CompletedSteps,
    int TotalSteps,
    DateTime ExpiresAtUtc)
{
    public static ArchivedCareerPlanDto From(ArchivedCareerPlanView view)
        => new(view.PlanId, view.DreamTitle, view.ArchivedAtUtc, view.CompletedSteps, view.TotalSteps, view.ExpiresAtUtc);
}
