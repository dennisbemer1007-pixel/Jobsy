using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/registration")]
public class RegistrationController : ControllerBase
{
    private readonly ICompanyRegistrationService _registration;
    private readonly IKvkService _kvk;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly IHostEnvironment _environment;

    public RegistrationController(
        ICompanyRegistrationService registration,
        IKvkService kvk,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        JobsyDbContext db,
        IPlatformFeatureService features,
        IHostEnvironment environment)
    {
        _registration = registration;
        _kvk = kvk;
        _companyAuth = companyAuth;
        _users = users;
        _db = db;
        _features = features;
        _environment = environment;
    }

    /// <summary>
    /// Public KVK establishment lookup for the registration wizard (includes IsInUse).
    /// </summary>
    [HttpGet("kvk/{kvkNumber}/establishments")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<IEnumerable<KvkEstablishmentResult>>> GetEstablishments(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var items = await _kvk.GetEstablishmentsAsync(kvkNumber, cancellationToken);
        // Hide occupancy to anonymous callers (same redaction as KvkController).
        return Ok(items.Select(i => i with { IsInUse = false }));
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<RegistrationSubmitResponse>> Submit(
        [FromBody] SubmitRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registration.SubmitAsync(
                new RegistrationSubmitRequest(
                    request.KvkNumber,
                    request.KvkEstablishmentId,
                    request.Scope,
                    request.ContactName,
                    request.ContactEmail,
                    request.ContactPhone,
                    request.AcceptedTerms,
                    request.ConsentVersion,
                    request.SalesManagerTrackingCode,
                    request.Password),
                cancellationToken);

            return Ok(new RegistrationSubmitResponse(
                result.RegistrationId,
                result.Status.ToString(),
                result.RequiresTakeover,
                result.Message,
                result.ActivationUrl));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Registratie conflict — probeer opnieuw." });
        }
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegistrationActivationResponse>> Activate(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registration.ActivateAsync(token, cancellationToken);
            return Ok(new RegistrationActivationResponse(
                result.RegistrationId,
                result.UserId,
                result.Email,
                result.FullName,
                result.Role,
                result.CompanyId,
                result.CompanyIds,
                // Never echo credentials outside Development — temp password is e-mailed only.
                _environment.IsDevelopment() && !result.UsedChosenPassword
                    ? result.TemporaryPassword
                    : null,
                result.OrganizationCompanyId,
                result.BranchCompanyId,
                result.UsedChosenPassword));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Activatie conflict — vestiging is mogelijk al bezet." });
        }
    }

    [HttpGet("takeovers")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<IEnumerable<TakeoverInboxItemDto>>> ListTakeovers(
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var items = await _registration.ListPendingTakeoversAsync(
            accessible ?? [],
            _companyAuth.IsAdmin(User),
            cancellationToken);

        return Ok(items.Select(i => new TakeoverInboxItemDto(
            i.TakeoverId,
            i.RegistrationId,
            i.TargetCompanyId,
            i.TargetCompanyName,
            i.KvkEstablishmentId,
            i.RequesterName,
            i.RequesterEmail,
            i.Scope.ToString(),
            i.CreatedAt)));
    }

    [HttpPost("takeovers/{id:guid}/approve")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<TakeoverDecisionResponse>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            var result = await _registration.ApproveTakeoverAsync(
                id,
                actor.Id,
                actor.Role,
                accessible,
                _companyAuth.IsAdmin(User),
                cancellationToken);

            return Ok(new TakeoverDecisionResponse(
                result.TakeoverId,
                result.Status.ToString(),
                result.Message,
                result.OrganizationCompanyId,
                result.BranchCompanyId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Overname conflict — probeer opnieuw." });
        }
    }

    [HttpPost("takeovers/{id:guid}/reject")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<TakeoverDecisionResponse>> Reject(
        Guid id,
        [FromBody] RejectTakeoverRequest? request,
        CancellationToken cancellationToken)
    {
        var actor = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (actor is null)
        {
            return Unauthorized();
        }

        try
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            var result = await _registration.RejectTakeoverAsync(
                id,
                actor.Id,
                accessible,
                _companyAuth.IsAdmin(User),
                request?.Note,
                cancellationToken);

            return Ok(new TakeoverDecisionResponse(
                result.TakeoverId,
                result.Status.ToString(),
                result.Message,
                result.OrganizationCompanyId,
                result.BranchCompanyId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Development-only helper: latest activation stub URL for an email.
    /// Requires Admin auth — never anonymous.
    /// </summary>
    [HttpGet("stub-activation")]
    [Authorize(Roles = JobsyRoles.Admin)]
    public async Task<ActionResult<object>> StubActivation(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            var featuresGate = await _features.GetAsync(cancellationToken);
            if (!featuresGate.ExposeRegistrationActivationLinks)
            {
                return NotFound();
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email is verplicht." });
        }

        var normalized = email.Trim().ToLowerInvariant();
        var reg = await _db.CompanyRegistrations
            .AsNoTracking()
            .Where(r => r.ContactEmail == normalized
                        && r.Status == CompanyRegistrationStatus.PendingActivation
                        && r.ActivationToken != "")
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (reg is null)
        {
            return NotFound(new { message = "Geen openstaande activatie gevonden." });
        }

        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = features.PublicWebBaseUrl.TrimEnd('/');
        return Ok(new
        {
            reg.Id,
            reg.ContactEmail,
            ActivationUrl = $"{baseUrl}/register/activate?token={Uri.EscapeDataString(reg.ActivationToken)}"
        });
    }
}
