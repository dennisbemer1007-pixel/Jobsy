using Jobsy.Api.Authorization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/maqqie-hours")]
[RequiresFeature(PlatformFeature.EmployerPhase2)]
public sealed class MaqqieHoursController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly IMaqqieHoursService _hours;

    public MaqqieHoursController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        IMaqqieHoursService hours)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
        _hours = hours;
    }

    /// <summary>Whether the signed-in candidate has an active Maqqie contract (nav gate).</summary>
    [HttpGet("active")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<MaqqieActiveDto>> GetActive(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var active = await _hours.CandidateHasActiveMaqqieContractAsync(user.Id, cancellationToken);
        return Ok(new MaqqieActiveDto(active));
    }

    [HttpGet("overview")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<MaqqieHoursOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var overview = await _hours.GetOverviewForCandidateAsync(user.Id, cancellationToken);
        return overview is null ? NotFound() : Ok(overview);
    }

    [HttpPut("weeks")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<ActionResult<MaqqieHoursWeekDto>> UpsertWeek(
        [FromBody] UpsertMaqqieHoursWeekRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var week = await _hours.UpsertDraftWeekAsync(
            user.Id,
            request.WeekStart,
            request.DailyHours ?? new Dictionary<int, decimal>(),
            cancellationToken);
        return week is null ? NotFound() : Ok(week);
    }

    [HttpPost("weeks/{weekId:guid}/submit")]
    [Authorize(Roles = JobsyRoles.Candidate)]
    public async Task<IActionResult> SubmitWeek(Guid weekId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var ok = await _hours.SubmitWeekAsync(user.Id, weekId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("applications/{applicationId:guid}/weeks/{weekId:guid}/approve")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRolesWithAdmin)]
    public async Task<IActionResult> EmployerApproveWeek(
        Guid applicationId,
        Guid weekId,
        CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (!_companyAuth.IsAdmin(User))
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            if (accessible is not null
                && !accessible.Contains(application.Vacancy.CompanyId)
                && !(application.Vacancy.IntermediaryCompanyId is Guid i && accessible.Contains(i)))
            {
                return Forbid();
            }
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var ok = await _hours.EmployerApproveWeekAsync(applicationId, weekId, actor.Id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record MaqqieActiveDto(bool Active);

public sealed record UpsertMaqqieHoursWeekRequest(
    DateOnly WeekStart,
    Dictionary<int, decimal>? DailyHours);
