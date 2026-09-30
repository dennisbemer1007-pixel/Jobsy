using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Jobsy.Api.Admin;
using Jobsy.Core.Admin;
using Jobsy.Core.Features;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/registration")]
[RequiresFeature(PlatformFeature.Employers)]
public class RegistrationController : ControllerBase
{
    private readonly ICompanyRegistrationService _registration;
    private readonly IKvkService _kvk;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ICompanyVerificationService _verification;

    public RegistrationController(
        ICompanyRegistrationService registration,
        IKvkService kvk,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        JobsyDbContext db,
        IPlatformFeatureService features,
        IHostEnvironment environment,
        IConfiguration configuration,
        ICompanyVerificationService verification)
    {
        _registration = registration;
        _kvk = kvk;
        _companyAuth = companyAuth;
        _users = users;
        _db = db;
        _features = features;
        _environment = environment;
        _configuration = configuration;
        _verification = verification;
    }

    /// <summary>
    /// Public KVK establishment lookup for the registration wizard.
    /// Returns Status = Ok | NotFound | Unavailable so the UI can offer a pending-KVK fallback.
    /// </summary>
    [HttpGet("kvk/{kvkNumber}/establishments")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<KvkEstablishmentsLookupResponse>> GetEstablishments(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var lookup = await _kvk.LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        // Registration wizard needs IsInUse so claimed vestigingen show as unavailable.
        // Boolean occupancy only — never owner/contact PII (that stays behind auth/takeover).
        return Ok(new KvkEstablishmentsLookupResponse(
            lookup.Status.ToString(),
            lookup.Message,
            lookup.Establishments));
    }

    /// <summary>
    /// Full KVK profile (websites, legal form, postal/visiting addresses, vestigingen + IsInUse)
    /// fetched only when a search hit is selected.
    /// </summary>
    [HttpGet("kvk/{kvkNumber}/profile")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<KvkCompanyProfileResponse>> GetProfile(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var profile = await _kvk.GetProfileAsync(kvkNumber, cancellationToken);
        return Ok(new KvkCompanyProfileResponse(
            profile.Status.ToString(),
            profile.KvkNumber,
            profile.Name,
            profile.Address,
            profile.LegalForm,
            profile.SbiCodes,
            profile.Websites,
            profile.Establishments.Select(e => new KvkEstablishmentProfileDto(
                e.KvkNumber,
                e.EstablishmentNumber,
                e.KvkEstablishmentId,
                e.Name,
                e.Address,
                e.Latitude,
                e.Longitude,
                e.IsInUse,
                e.SbiCodes,
                e.VisitingAddress is null
                    ? null
                    : new KvkAddressLineDto(
                        e.VisitingAddress.Street,
                        e.VisitingAddress.HouseNumber,
                        e.VisitingAddress.HouseLetter,
                        e.VisitingAddress.Postcode,
                        e.VisitingAddress.Place,
                        e.VisitingAddress.FormattedLine),
                e.PostalAddress is null
                    ? null
                    : new KvkAddressLineDto(
                        e.PostalAddress.Street,
                        e.PostalAddress.HouseNumber,
                        e.PostalAddress.HouseLetter,
                        e.PostalAddress.Postcode,
                        e.PostalAddress.Place,
                        e.PostalAddress.FormattedLine))).ToList(),
            profile.Message));
    }

    /// <summary>Resolve a typed or link sales/partner referral code (soft — unknown never errors).</summary>
    [HttpGet("referral")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<object>> ResolveReferral(
        [FromQuery] string? typed,
        [FromQuery] string? link,
        [FromServices] IRegistrationReferralResolver resolver,
        CancellationToken cancellationToken)
    {
        var result = await resolver.ResolveAsync(typed, link, cancellationToken);
        return Ok(new
        {
            code = result.Code,
            source = result.Source.ToString(),
            salesManagerUserId = result.SalesManagerUserId,
            isPartnerCode = result.IsPartnerCode,
            isKnown = result.IsKnown
        });
    }

    [HttpPost]
    [AdminAuditExempt("Public/employer registration submit")]
    [AllowAnonymous]
    [EnableRateLimiting("registration-submit")]
    public async Task<ActionResult<RegistrationSubmitResponse>> Submit(
        [FromBody] SubmitRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.AcceptedRepresentation
                && request.RepresentationConsentAtUtc is null)
            {
                request = request with { RepresentationConsentAtUtc = DateTime.UtcNow };
            }

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
                    request.PartnerTrackingCode,
                    request.Password,
                    request.AllowPendingKvkVerification,
                    request.ManualEstablishmentName,
                    request.ManualEstablishmentAddress,
                    request.ManualEstablishmentNumber,
                    request.ManualLatitude,
                    request.ManualLongitude,
                    request.ManualIsIntermediarySbi,
                    request.SelectedEstablishmentIds,
                    request.SalesManagerUserId,
                    request.RepresentationConsentAtUtc,
                    request.RepresentationConsentVersion,
                    request.PreferredLoginProvider,
                    request.LocationUnknown,
                    request.CookieTrackingCode),
                cancellationToken);

            return Ok(new RegistrationSubmitResponse(
                result.RegistrationId,
                result.Status.ToString(),
                result.RequiresTakeover,
                result.Message,
                result.ActivationUrl,
                result.VerificationExpiresAt));
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

    [HttpPost("{id:guid}/confirm")]
    [AdminAuditExempt("Registration confirm OTP")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegistrationActivationResponse>> Confirm(
        Guid id,
        [FromBody] ConfirmRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _registration.ConfirmAsync(id, request.VerificationCode, cancellationToken);
            var regMeta = await _db.CompanyRegistrations.AsNoTracking()
                .Where(r => r.Id == result.RegistrationId)
                .Select(r => new { r.PreferredLoginProvider, r.KvkNumber, r.ContactEmail })
                .FirstOrDefaultAsync(cancellationToken);
            var preferred = regMeta?.PreferredLoginProvider;
            var instantlyVerified = false;
            if (!result.EmailVerifiedAwaitingTakeover
                && regMeta is not null
                && (result.OrganizationCompanyId is not null || result.BranchCompanyId is not null))
            {
                var rootId = result.OrganizationCompanyId ?? result.BranchCompanyId!.Value;
                if (_registration is CompanyRegistrationService concrete
                    && await concrete.MatchesBusinessEmailDomainAsync(regMeta.KvkNumber, regMeta.ContactEmail, cancellationToken))
                {
                    await _verification.MarkVerifiedAsync(
                        rootId,
                        CompanyVerificationMethod.BusinessEmail,
                        result.UserId,
                        "D15 domain match at registration activation",
                        cancellationToken);
                    instantlyVerified = true;
                }
            }

            instantlyVerified = instantlyVerified
                || await IsInstantlyVerifiedAsync(result.OrganizationCompanyId ?? result.BranchCompanyId, cancellationToken);
            return Ok(ToActivationResponse(result, issueSession: !result.EmailVerifiedAwaitingTakeover, preferred, instantlyVerified));
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
            return Conflict(new { message = "Bevestiging conflict — probeer opnieuw." });
        }
    }

    [HttpGet("takeovers")]
    [Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.BranchManager},{JobsyRoles.Admin}")]
    public async Task<ActionResult<IEnumerable<TakeoverInboxItemDto>>> ListTakeovers(
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var isAdmin = _companyAuth.IsAdmin(User);
        var items = await _registration.ListPendingTakeoversAsync(
            accessible ?? [],
            isAdmin,
            cancellationToken);

        return Ok(items.Select(i => new TakeoverInboxItemDto(
            i.TakeoverId,
            i.RegistrationId,
            i.TargetCompanyId,
            i.TargetCompanyName,
            i.KvkEstablishmentId,
            i.RequesterName,
            // Admin sees masked e-mail by default (AVG); enterprise/branch managers keep full view.
            isAdmin ? PersonalDataMasker.MaskEmail(i.RequesterEmail) : i.RequesterEmail,
            i.Scope.ToString(),
            i.CreatedAt)));
    }

    [HttpPost("takeovers/{id:guid}/approve")]
    [AdminAudit(AdminAuditKeys.TakeoverApprove, TargetType = AdminAuditKeys.TargetTypes.Takeover, TargetRouteKey = "id")]
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
    [AdminAudit(AdminAuditKeys.TakeoverReject, TargetType = AdminAuditKeys.TargetTypes.Takeover, TargetRouteKey = "id")]
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

    private RegistrationActivationResponse ToActivationResponse(
        RegistrationActivationResult result,
        bool issueSession = false,
        string? preferredLoginProvider = null,
        bool instantlyVerified = false)
    {
        string? sessionToken = null;
        if (issueSession && result.UserId != Guid.Empty && !string.IsNullOrWhiteSpace(result.Email))
        {
            var secret = JobsyLocalSessionToken.ResolveSigningKey(
                _configuration["JobsyAuth:LocalSessionSigningKey"],
                _configuration["JobsyAuth:DevelopmentAuthSecret"]);
            if (!string.IsNullOrWhiteSpace(secret))
            {
                sessionToken = JobsyLocalSessionToken.Create(result.Email, result.UserId, secret);
            }
        }

        return new(
            result.RegistrationId,
            result.UserId,
            result.Email,
            result.FullName,
            result.Role,
            result.CompanyId,
            result.CompanyIds,
            result.OrganizationCompanyId,
            result.BranchCompanyId,
            result.UsedChosenPassword,
            result.EmailVerifiedAwaitingTakeover,
            result.WelcomeTokenGranted,
            result.FreePublishUntil,
            sessionToken,
            instantlyVerified,
            preferredLoginProvider);
    }

    private async Task<bool> IsInstantlyVerifiedAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        if (companyId is not Guid id)
        {
            return false;
        }

        var status = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.VerificationStatus, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return false;
        }

        if (status.ParentCompanyId is Guid parentId)
        {
            return await _db.Companies.AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => c.VerificationStatus)
                .FirstOrDefaultAsync(cancellationToken) == CompanyVerificationStatus.Verified;
        }

        return status.VerificationStatus == CompanyVerificationStatus.Verified;
    }
}
