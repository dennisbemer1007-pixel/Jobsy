using Jobsy.Api.Authorization;
using Jobsy.Api.Filters;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer-phase2")]
[RequiresFeature(PlatformFeature.EmployerPhase2)]
[Authorize]
public sealed class EmployerPhase2Controller : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly IEmployerPhase2Service _phase2;

    public EmployerPhase2Controller(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        IEmployerPhase2Service phase2)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
        _phase2 = phase2;
    }

    [HttpGet("applications/{applicationId:guid}/context")]
    public async Task<ActionResult<EmployerPhase2ApplicationContextDto>> GetApplicationContext(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (!_companyAuth.IsAdmin(User) && !_companyAuth.IsEmployer(User))
        {
            return Forbid();
        }

        var application = await LoadApplicationAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (!await CanAccessAsync(application, cancellationToken))
        {
            return Forbid();
        }

        var placement = await _db.ApplicationPlacements.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationId == applicationId, cancellationToken);
        var facts = EmployerApplicationFacts.Build(application)
            .Select(f => new EmployerPhase2FactDto(f.Label, f.Value, f.FitsWell))
            .ToList();

        return Ok(new EmployerPhase2ApplicationContextDto(
            application.Id,
            application.Status,
            application.Vacancy.IntermediaryCompanyId is not null,
            placement is not null,
            placement?.EmploymentMode,
            placement?.AcceptCostTokens,
            facts));
    }

    [HttpPost("applications/{applicationId:guid}/accept")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRolesWithAdmin)]
    public async Task<ActionResult<EmployerPhase2AcceptResult>> Accept(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (!await CanAccessAsync(application, cancellationToken))
        {
            return Forbid();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var result = await _phase2.AcceptApplicationAsync(applicationId, actor.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                "insufficient_tokens" => BadRequest(new { code = result.ErrorCode, message = result.UserMessage, balance = result.BalanceAfter }),
                "feature_disabled" => NotFound(new { type = FeatureGateFilter.FeatureDisabledType }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.UserMessage })
            };
        }

        return Ok(result);
    }

    [HttpPost("applications/{applicationId:guid}/employment-mode")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRolesWithAdmin)]
    public async Task<ActionResult<EmployerPhase2PlacementResult>> ChooseEmploymentMode(
        Guid applicationId,
        [FromBody] ChooseEmploymentModeRequest request,
        CancellationToken cancellationToken)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (!await CanAccessAsync(application, cancellationToken))
        {
            return Forbid();
        }

        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        var result = await _phase2.ChooseEmploymentModeAsync(
            applicationId,
            request.Mode,
            actor.Id,
            cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.UserMessage });
        }

        return Ok(result);
    }

    [HttpGet("wallet")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRolesWithAdmin + "," + JobsyRoles.RegionalManager)]
    public async Task<ActionResult<EmployerPhase2WalletDto>> GetWallet(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
        {
            return BadRequest(new { message = "companyId is verplicht." });
        }

        if (!_companyAuth.IsAdmin(User))
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            if (accessible is not null && !accessible.Contains(companyId))
            {
                return Forbid();
            }
        }

        return Ok(await _phase2.GetWalletAsync(companyId, cancellationToken));
    }

    private async Task<Application?> LoadApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
        => await _db.Applications
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

    private async Task<bool> CanAccessAsync(Application application, CancellationToken cancellationToken)
    {
        if (_companyAuth.IsAdmin(User))
        {
            return true;
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is null)
        {
            return true;
        }

        if (accessible.Contains(application.Vacancy.CompanyId))
        {
            return true;
        }

        return application.Vacancy.IntermediaryCompanyId is Guid intermediaryId
               && accessible.Contains(intermediaryId);
    }
}

public sealed record EmployerPhase2FactDto(string Label, string Value, bool FitsWell);

public sealed record EmployerPhase2ApplicationContextDto(
    Guid ApplicationId,
    ApplicationStatus Status,
    bool IsStaffingAgencyVacancy,
    bool HasPlacement,
    PlacementEmploymentMode? EmploymentMode,
    decimal? AcceptCostTokens,
    IReadOnlyList<EmployerPhase2FactDto> Facts);

public sealed record ChooseEmploymentModeRequest(PlacementEmploymentMode Mode);
