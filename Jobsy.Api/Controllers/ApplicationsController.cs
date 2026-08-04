using System.Net;
using Jobsy.Api.Models;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly IEmailService _email;
    private readonly IPushNotificationService _push;
    private readonly IPlatformFeatureService _features;

    public ApplicationsController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        IEmailService email,
        IPushNotificationService push,
        IPlatformFeatureService features)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
        _email = email;
        _push = push;
        _features = features;
    }

    [HttpGet]
    [Authorize(Policy = JobsyPolicies.RequireAdminOrEmployer)]
    public async Task<ActionResult<IEnumerable<EmployerApplicationDto>>> GetForManagedCompanies(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Applications
            .AsNoTracking()
            .Where(a => a.EmailVerifiedAt != null)
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(a => accessible.Contains(a.Vacancy.CompanyId));
        }

        var rows = await query
            .OrderByDescending(a => a.MatchPercent ?? -1)
            .ThenBy(a => a.EstimatedTravelMinutes)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.VacancyId,
                VacancyTitle = a.Vacancy.Title,
                CompanyName = a.Vacancy.Company.Name,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.CreatedAt,
                a.Status,
                a.RespondedAt,
                a.CandidateCity,
                a.DistanceKm,
                a.PreferencesSummary,
                a.CandidateName,
                a.CandidateEmail,
                a.CandidateAddress,
                a.WorkPermitConfirmed,
                a.SnapshotAvailabilityJson,
                a.SnapshotDrivingLicenses,
                a.SnapshotEducations,
                a.SnapshotAboutMe,
                a.CandidateEmployerCount,
                a.MatchPercent,
                a.MatchBreakdownJson,
                a.ViaSafetyNet,
                a.Motivation
            })
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(a =>
        {
            var revealed = a.Status is ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting or ApplicationStatus.Hired;
            return new EmployerApplicationDto(
                a.Id,
                a.VacancyId,
                a.VacancyTitle,
                a.CompanyName,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.CreatedAt,
                a.Status.ToString(),
                a.RespondedAt,
                revealed ? a.CandidateCity : null,
                revealed ? a.DistanceKm : null,
                ApplicationPreferenceRedaction.RedactForEmployer(a.PreferencesSummary, revealed),
                revealed ? a.CandidateName : null,
                revealed ? a.CandidateEmail : null,
                revealed ? a.CandidateAddress : null,
                revealed,
                revealed && a.WorkPermitConfirmed,
                revealed ? a.SnapshotAvailabilityJson : null,
                revealed ? a.SnapshotDrivingLicenses : null,
                revealed ? a.SnapshotEducations : null,
                revealed ? a.SnapshotAboutMe : null,
                revealed ? a.CandidateEmployerCount : 0,
                a.MatchPercent,
                a.MatchBreakdownJson,
                a.ViaSafetyNet,
                // Motivation is candidate-authored for the employer — show after verified apply (pre-accept OK).
                a.Motivation,
                LegalEligible: true);
        }));
    }

    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult<ApplyResultDto>> Apply(
        [FromBody] ApplyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ApplyCoreAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "Sollicitatie mislukt door een serverfout. Herstart de API na de laatste update of controleer of migrations zijn toegepast."
            });
        }
    }

    private async Task<ActionResult<ApplyResultDto>> ApplyCoreAsync(
        ApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.AcceptedTerms)
        {
            return BadRequest(new { message = "Je moet akkoord gaan met de gebruiksvoorwaarden en privacyverklaring." });
        }
        if (!request.WorkPermitConfirmed)
        {
            return BadRequest(new { message = "Je moet bevestigen dat je wettelijk in Nederland mag werken." });
        }

        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
                .ThenInclude(c => c.ParentCompany)
            .Include(v => v.ExclusivitySetting!)
                .ThenInclude(s => s.Educations)
            .FirstOrDefaultAsync(v => v.Id == request.VacancyId, cancellationToken);

        if (vacancy is null)
        {
            return NotFound();
        }

        if (vacancy.Kind == VacancyKind.Internship
            && vacancy.ExclusivitySetting is { } exclusivity
            && ExclusivityRules.RequiresApplicantExtras(exclusivity))
        {
            var exclusivityError = ExclusivityRules.ValidateApplicantExtras(
                exclusivity,
                request.StudentNumber,
                request.SchoolEmail,
                request.StudyProgram);
            if (exclusivityError is not null)
            {
                return BadRequest(new { message = exclusivityError });
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var applicationCount = await _db.Applications.CountAsync(
            a => a.VacancyId == vacancy.Id && a.EmailVerifiedAt != null,
            cancellationToken);
        if (!VacancyVisibilityRules.CanAcceptApplications(vacancy, today, applicationCount))
        {
            return BadRequest(new { message = "Deze vacature neemt geen sollicitaties meer aan." });
        }

        var candidate = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (candidate is null || !candidate.IsActive)
        {
            return Unauthorized(new { message = "Inloggegevens incompleet; solliciteren niet mogelijk." });
        }

        var existing = await _db.Applications.FirstOrDefaultAsync(
            a => a.VacancyId == vacancy.Id
                 && (a.CandidateUserId == candidate.Id
                     || a.CandidateEmail.ToLower() == candidate.Email.ToLower()),
            cancellationToken);
        if (existing is not null
            && existing.EmailVerifiedAt is not null
            && ApplicationRules.IsSameCandidate(candidate.Id, candidate.Email, existing.CandidateUserId, existing.CandidateEmail))
        {
            return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
        }

        var preferences = MeController.ParsePreferences(candidate.PreferencesJson);
        var requirementError = ApplicationRequirementRules.ValidateHardRequirements(
            vacancy.RequiredDrivingLicense,
            vacancy.RequiredEducation,
            vacancy.MinimumEmployers,
            preferences.DrivingLicenses,
            preferences.Educations,
            preferences.Employers?.Count ?? 0);
        if (requirementError is not null)
        {
            return BadRequest(new { message = requirementError });
        }

        var ageYears = AgeRules.AgeYearsFromDateOfBirth(candidate.DateOfBirth)
                       ?? preferences.AgeYears;
        if (ageYears is null && string.IsNullOrWhiteSpace(request.VerificationCode))
        {
            // DOB required before OTP for youth-labor + wage integrity.
            return BadRequest(new
            {
                message = "Vul eerst je geboortedatum in op je profiel voordat je solliciteert."
            });
        }

        var matchInput = MatchingProfileMapper.BuildInput(
            vacancy,
            preferences,
            request.EstimatedTravelMinutes,
            ageYears);
        var match = MatchScoreCalculator.Calculate(matchInput);
        if (!match.LegalEligible)
        {
            return BadRequest(new { message = YouthLaborRules.FriendlyBlockMessage });
        }

        var matchJson = JsonSerializer.Serialize(match);
        var isVerificationAttempt = !string.IsNullOrWhiteSpace(request.VerificationCode);

        // Gulden Middenweg: hold OTP until candidate confirms safety net.
        if (!isVerificationAttempt
            && GuldenMiddenwegRules.RequiresSafetyNetConfirmation(match)
            && !request.ConfirmLowMatchSafetyNet)
        {
            var placeholder = new ApplicationDto(
                existing?.Id ?? Guid.Empty,
                vacancy.Id,
                vacancy.Title,
                vacancy.Company.Name,
                candidate.FullName,
                candidate.Email,
                request.PreferredTransport,
                request.EstimatedTravelMinutes,
                existing?.CreatedAt ?? DateTime.UtcNow,
                ApplicationStatus.Pending.ToString());
            return Ok(new ApplyResultDto(
                placeholder,
                ConfirmationEmailQueued: false,
                AuthenticatorStubUsed: false,
                RequiresVerification: false,
                VerificationCodeSent: false,
                DirectContact: null,
                RequiresSafetyNetConfirmation: true,
                MatchPercent: match.TotalPercent,
                MatchBreakdownJson: matchJson,
                SafetyNetMessage:
                $"Je matchscore is {match.TotalPercent}%. Pas je profiel aan voor een betere match, of ga toch door (vangnet)."));
        }

        var authenticatorStubUsed = false;
        if (request.UseAuthenticator)
        {
            var features = await _features.GetAsync(cancellationToken);
            if (!features.AuthenticatorEnabled)
            {
                return BadRequest(new { message = "Authenticator is nog niet beschikbaar (stub-flag uit)." });
            }

            authenticatorStubUsed = true;
        }

        double? distanceKm = null;
        if (candidate.HomeLocation is not null && vacancy.Location is not null)
        {
            distanceKm = Math.Round(
                GeoDistance.HaversineKm(
                    new GeoPoint(candidate.HomeLocation.Latitude, candidate.HomeLocation.Longitude),
                    new GeoPoint(vacancy.Location.Latitude, vacancy.Location.Longitude)),
                1);
        }

        var application = existing ?? new Core.Entities.Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancy.Id,
            CandidateUserId = candidate.Id,
            CandidateName = candidate.FullName,
            CandidateEmail = candidate.Email,
            CreatedAt = DateTime.UtcNow,
            Status = ApplicationStatus.Pending
        };

        application.CandidateCity = ExtractCityHint(candidate);
        application.PreferredTransport = request.PreferredTransport;
        application.EstimatedTravelMinutes = request.EstimatedTravelMinutes;
        application.DistanceKm = distanceKm;
        // Compact summary only — full prefs JSON easily exceeds varchar(1024) and broke Apply.
        application.PreferencesSummary = BuildCompactPreferencesSummary(preferences);
        application.ConsentAcceptedAt = DateTime.UtcNow;
        // Server stamps consent version — client-supplied values are ignored (AVG integrity).
        application.ConsentVersion = PrivacyConstants.CurrentConsentVersion;
        application.WorkPermitConfirmed = request.WorkPermitConfirmed;
        application.SnapshotAvailabilityJson = Truncate(
            preferences.Availability is null ? null : JsonSerializer.Serialize(preferences.Availability),
            2048);
        application.SnapshotDrivingLicenses = Truncate(
            preferences.DrivingLicenses is null ? null : string.Join(", ", preferences.DrivingLicenses),
            512);
        application.SnapshotEducations = Truncate(
            preferences.Educations is null ? null : string.Join(", ", preferences.Educations),
            512);
        application.SnapshotAboutMe = Truncate(preferences.AboutMe, 1024);
        application.CandidateEmployerCount = preferences.Employers?.Count ?? 0;
        application.Motivation = Truncate(string.IsNullOrWhiteSpace(request.Motivation) ? null : request.Motivation.Trim(), 500);
        application.MatchPercent = match.TotalPercent;
        application.MatchBreakdownJson = Truncate(matchJson, 4000);
        application.ViaSafetyNet = GuldenMiddenwegRules.RequiresSafetyNetConfirmation(match)
                                   && request.ConfirmLowMatchSafetyNet;

        if (vacancy.Kind == VacancyKind.Internship
            && vacancy.ExclusivitySetting is { } excl
            && ExclusivityRules.RequiresApplicantExtras(excl))
        {
            application.StudentNumber = Truncate(request.StudentNumber?.Trim(), 64);
            application.SchoolEmail = Truncate(request.SchoolEmail?.Trim().ToLowerInvariant(), 256);
            application.StudyProgram = Truncate(request.StudyProgram?.Trim(), 256);
            application.StudyYear = Truncate(
                string.IsNullOrWhiteSpace(request.StudyYear) ? null : request.StudyYear.Trim(),
                64);
            application.ExclusivityValidationStatus = "Ok";
        }
        else
        {
            application.StudentNumber = null;
            application.SchoolEmail = null;
            application.StudyProgram = null;
            application.StudyYear = null;
            application.ExclusivityValidationStatus = vacancy.Kind == VacancyKind.Internship
                ? "NotApplicable"
                : null;
        }

        if (!isVerificationAttempt)
        {
            var code = VerificationCodes.CreateNumericCode();
            application.EmailVerificationCode = VerificationCodes.Hash(code);
            application.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
            // Do not reset failed attempts on resend — prevents lockout bypass via spam resend.
            if (existing is null || existing.EmailVerificationFailedAttempts >= VerificationCodes.MaxFailedAttempts)
            {
                application.EmailVerificationFailedAttempts = 0;
            }

            application.EmailVerifiedAt = null;
            application.Status = ApplicationStatus.Pending;
            if (existing is null)
            {
                _db.Applications.Add(application);
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                if (detail.Contains("23505", StringComparison.Ordinal)
                    || detail.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
                }

                return BadRequest(new { message = "Sollicitatie kon niet worden opgeslagen. Probeer het opnieuw." });
            }

            try
            {
                await SendVerificationCodeAsync(candidate, vacancy, code, cancellationToken);
            }
            catch
            {
                // Email stub/provider failures must not fail the apply after save.
            }

            // Draft is stored for code validation but is not a candidate-facing application status
            // (Sollicitaties only lists verified apps). RequiresVerification drives the UI.
            var pendingDto = new ApplicationDto(
                application.Id,
                vacancy.Id,
                vacancy.Title,
                vacancy.Company.Name,
                application.CandidateName,
                application.CandidateEmail,
                application.PreferredTransport,
                application.EstimatedTravelMinutes,
                application.CreatedAt,
                application.Status.ToString(),
                null);
            return Ok(new ApplyResultDto(
                pendingDto,
                ConfirmationEmailQueued: false,
                authenticatorStubUsed,
                RequiresVerification: true,
                VerificationCodeSent: true,
                DirectContact: null,
                RequiresSafetyNetConfirmation: false,
                MatchPercent: match.TotalPercent,
                MatchBreakdownJson: matchJson));
        }

        if (existing is null)
        {
            return BadRequest(new { message = "Start eerst met Verzenden om een verificatiecode te ontvangen." });
        }

        if (existing.EmailVerifiedAt is not null)
        {
            return BadRequest(new { message = "Deze sollicitatie is al bevestigd." });
        }

        if (string.IsNullOrWhiteSpace(existing.EmailVerificationCode)
            || existing.EmailVerificationExpiresAt is null
            || existing.EmailVerificationExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Verificatiecode verlopen. Klik opnieuw op Verzenden." });
        }

        if (existing.EmailVerificationFailedAttempts >= VerificationCodes.MaxFailedAttempts)
        {
            existing.EmailVerificationCode = null;
            existing.EmailVerificationExpiresAt = null;
            await _db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = "Te veel onjuiste pogingen. Vraag een nieuwe verificatiecode aan." });
        }

        if (!VerificationCodes.MatchesHash(existing.EmailVerificationCode, request.VerificationCode?.Trim()))
        {
            var attempts = existing.EmailVerificationFailedAttempts;
            var lockedOut = VerificationCodes.RegisterFailedAttempt(ref attempts);
            existing.EmailVerificationFailedAttempts = attempts;
            if (lockedOut)
            {
                existing.EmailVerificationCode = null;
                existing.EmailVerificationExpiresAt = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return BadRequest(new
            {
                message = lockedOut
                    ? "Te veel onjuiste pogingen. Vraag een nieuwe verificatiecode aan."
                    : "Onjuiste verificatiecode."
            });
        }

        existing.EmailVerifiedAt = DateTime.UtcNow;
        existing.EmailVerificationCode = null;
        existing.EmailVerificationExpiresAt = null;
        existing.EmailVerificationFailedAttempts = 0;
        existing.WorkPermitConfirmed = request.WorkPermitConfirmed;
        await _db.SaveChangesAsync(cancellationToken);

        var deepLink = await BuildDeepLinkAsync("/candidate/applications", cancellationToken);
        var name = Html(candidate.FullName);
        var title = Html(vacancy.Title);
        var company = Html(vacancy.Company.Name);
        await _email.SendAsync(new EmailMessage(
            candidate.Email,
            $"Sollicitatie bevestigd: {vacancy.Title}",
            $"""
             <p>Hoi {name},</p>
             <p>Je sollicitatie op <strong>{title}</strong> bij {company} is ontvangen.</p>
             <p><a href="{Html(deepLink)}">Bekijk je sollicitaties</a></p>
             {(authenticatorStubUsed ? "<p><em>Authenticator stub: verificatie gesimuleerd.</em></p>" : "")}
             """,
            "ApplicationConfirmation"), cancellationToken);

        await NotifyEmployersOfNewApplicationAsync(vacancy, existing, cancellationToken);

        var dto = new ApplicationDto(
            existing.Id,
            vacancy.Id,
            vacancy.Title,
            vacancy.Company.Name,
            existing.CandidateName,
            existing.CandidateEmail,
            existing.PreferredTransport,
            existing.EstimatedTravelMinutes,
            existing.CreatedAt,
            existing.Status.ToString(),
            existing.RespondedAt);

        return Ok(new ApplyResultDto(
            dto,
            ConfirmationEmailQueued: true,
            authenticatorStubUsed,
            DirectContact: ToDirectContactDto(vacancy),
            RequiresSafetyNetConfirmation: false,
            MatchPercent: existing.MatchPercent ?? match.TotalPercent,
            MatchBreakdownJson: existing.MatchBreakdownJson ?? matchJson));
    }

    /// <summary>
    /// Re-fetch employer direct-contact options after a verified application (never public).
    /// </summary>
    [HttpGet("by-vacancy/{vacancyId:guid}/direct-contact")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<EmployerDirectContactDto>> GetDirectContactForVacancy(
        Guid vacancyId,
        CancellationToken cancellationToken)
    {
        var candidate = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (candidate is null || !candidate.IsActive)
        {
            return Unauthorized();
        }

        var hasVerifiedApplication = await _db.Applications.AsNoTracking().AnyAsync(
            a => a.VacancyId == vacancyId
                 && a.EmailVerifiedAt != null
                 && (a.CandidateUserId == candidate.Id
                     || a.CandidateEmail.ToLower() == candidate.Email.ToLower()),
            cancellationToken);
        if (!hasVerifiedApplication)
        {
            return NotFound(new { message = "Geen bevestigde sollicitatie op deze vacature." });
        }

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
                .ThenInclude(c => c.ParentCompany)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var dto = ToDirectContactDto(vacancy);
        if (dto is null)
        {
            return Ok(new EmployerDirectContactDto(
                Available: false,
                OfferMail: false,
                OfferPhone: false,
                OfferWhatsApp: false));
        }

        return Ok(dto);
    }

    private static EmployerDirectContactDto? ToDirectContactDto(Core.Entities.Vacancy vacancy)
    {
        var effective = EmployerContactPreferenceRules.Resolve(
            vacancy.Company,
            vacancy,
            vacancy.Company.ParentCompany);
        if (!effective.Available)
        {
            return null;
        }

        return new EmployerDirectContactDto(
            Available: true,
            OfferMail: effective.OfferMail,
            OfferPhone: effective.OfferPhone,
            OfferWhatsApp: effective.OfferWhatsApp,
            Email: effective.Email,
            Phone: effective.Phone,
            WhatsAppUrl: effective.WhatsAppUrl);
    }

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var application = await _db.Applications
            .FirstOrDefaultAsync(a => a.Id == id && a.CandidateUserId == user.Id, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        // Only verified "Open" (Pending) applications can be withdrawn — drafts awaiting a code cannot.
        if (!ApplicationRules.CanCandidateWithdraw(application.Status, application.EmailVerifiedAt))
        {
            return BadRequest(new { message = "Alleen open sollicitaties kunnen worden ingetrokken." });
        }

        _db.Applications.Remove(application);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/react")]
    [Authorize(Roles = JobsyRoles.ApplicationReactRoles)]
    public async Task<ActionResult<EmployerApplicationDto>> React(
        Guid id,
        [FromBody] ReactToApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected))
        {
            return BadRequest(new { message = "Status moet Accepted of Rejected zijn." });
        }

        var application = await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }
        if (application.EmailVerifiedAt is null)
        {
            return BadRequest(new { message = "Sollicitatie is nog niet geverifieerd." });
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null && !accessible.Contains(application.Vacancy.CompanyId))
        {
            return Forbid();
        }

        if (!ApplicationRules.CanEmployerReact(application.Status))
        {
            return BadRequest(new { message = "Op deze sollicitatie is al gereageerd." });
        }

        var respondedAt = DateTime.UtcNow;
        var updated = await _db.Applications
            .Where(a => a.Id == id && a.Status == ApplicationStatus.Pending)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.Status, request.Status)
                    .SetProperty(a => a.RespondedAt, respondedAt),
                cancellationToken);

        if (updated == 0)
        {
            return BadRequest(new { message = "Op deze sollicitatie is al gereageerd." });
        }

        application.Status = request.Status;
        application.RespondedAt = respondedAt;

        var statusLabel = request.Status == ApplicationStatus.Accepted ? "positief" : "afwijzing";
        var deepLink = await BuildDeepLinkAsync($"/vacancies/{application.VacancyId}", cancellationToken);
        var candidateName = Html(application.CandidateName);
        var title = Html(application.Vacancy.Title);
        var company = Html(application.Vacancy.Company.Name);
        var subject = $"Update op je sollicitatie: {application.Vacancy.Title}";
        var body = $"""
            <p>Hoi {candidateName},</p>
            <p>De werkgever heeft gereageerd ({statusLabel}) op je sollicitatie voor
            <strong>{title}</strong> bij {company}.</p>
            <p><a href="{Html(deepLink)}">Open vacature</a></p>
            """;

        await _email.SendAsync(new EmailMessage(
            application.CandidateEmail,
            subject,
            body,
            "EmployerReaction"), cancellationToken);

        await _push.SendAsync(new PushMessage(
            application.CandidateEmail,
            "Reactie op je sollicitatie",
            $"{application.Vacancy.Company.Name}: {statusLabel} — {application.Vacancy.Title}",
            deepLink,
            "EmployerReaction"), cancellationToken);

        return Ok(MapEmployerDto(application));
    }

    [HttpPost("{id:guid}/contact")]
    [Authorize(Roles = JobsyRoles.ApplicationReactRoles)]
    public async Task<ActionResult<EmployerApplicationDto>> MarkEmployerContact(Guid id, CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null && !accessible.Contains(application.Vacancy.CompanyId))
        {
            return Forbid();
        }

        if (application.Status != ApplicationStatus.Accepted)
        {
            return BadRequest(new { message = "Contact kan alleen na acceptatie." });
        }

        application.Status = ApplicationStatus.EmployerContacting;
        application.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var deepLink = await BuildDeepLinkAsync($"/vacancies/{application.VacancyId}", cancellationToken);
        await _email.SendAsync(new EmailMessage(
            application.CandidateEmail,
            $"Werkgever neemt contact op: {application.Vacancy.Title}",
            $"""
             <p>Goed nieuws! De werkgever van <strong>{Html(application.Vacancy.Title)}</strong> neemt contact met je op.</p>
             <p><a href="{Html(deepLink)}">Open vacature</a></p>
             """,
            "EmployerContacting"), cancellationToken);
        await _push.SendAsync(new PushMessage(
            application.CandidateEmail,
            "Werkgever neemt contact op",
            $"{application.Vacancy.Company.Name} neemt contact op over {application.Vacancy.Title}",
            deepLink,
            "EmployerContacting"), cancellationToken);

        return Ok(MapEmployerDto(application));
    }

    [HttpPost("vacancies/{vacancyId:guid}/fulfill/{applicationId:guid}")]
    [Authorize(Roles = JobsyRoles.ApplicationReactRoles)]
    public async Task<ActionResult> FulfillVacancy(Guid vacancyId, Guid applicationId, CancellationToken cancellationToken)
    {
        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == vacancyId, cancellationToken);
        if (vacancy is null)
        {
            return NotFound();
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        if (accessible is not null && !accessible.Contains(vacancy.CompanyId))
        {
            return Forbid();
        }

        var chosen = await _db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId && a.VacancyId == vacancyId, cancellationToken);
        if (chosen is null)
        {
            return NotFound(new { message = "Geselecteerde sollicitatie niet gevonden." });
        }
        if (chosen.EmailVerifiedAt is null)
        {
            return BadRequest(new { message = "Alleen geverifieerde sollicitaties kunnen op vervuld worden gezet." });
        }

        vacancy.Status = VacancyStatus.Fulfilled;
        vacancy.ClosedAtUtc ??= DateTime.UtcNow;
        vacancy.FulfilledByApplicationId = chosen.Id;
        chosen.Status = ApplicationStatus.Hired;
        chosen.RespondedAt = DateTime.UtcNow;

        var others = await _db.Applications
            .Where(a => a.VacancyId == vacancyId && a.Id != chosen.Id && a.EmailVerifiedAt != null)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.Status = ApplicationStatus.FilledElsewhere;
            other.RespondedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _email.SendAsync(new EmailMessage(
            chosen.CandidateEmail,
            $"Gefeliciteerd! Je bent aangenomen voor {vacancy.Title}",
            $"<p>Gefeliciteerd! Je bent geselecteerd voor <strong>{Html(vacancy.Title)}</strong> bij {Html(vacancy.Company.Name)}.</p>",
            "ApplicationHired"), cancellationToken);

        foreach (var other in others)
        {
            await _email.SendAsync(new EmailMessage(
                other.CandidateEmail,
                $"Update sollicitatie: {vacancy.Title}",
                $"<p>Bedankt voor je sollicitatie op <strong>{Html(vacancy.Title)}</strong>.</p><p>De keuze is helaas op een andere kandidaat gevallen.</p>",
                "ApplicationFilledElsewhere"), cancellationToken);
        }

        return Ok();
    }

    private static EmployerApplicationDto MapEmployerDto(Core.Entities.Application a)
    {
        var revealed = a.Status is ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting or ApplicationStatus.Hired;
        return new EmployerApplicationDto(
            a.Id,
            a.VacancyId,
            a.Vacancy.Title,
            a.Vacancy.Company.Name,
            a.PreferredTransport,
            a.EstimatedTravelMinutes,
            a.CreatedAt,
            a.Status.ToString(),
            a.RespondedAt,
            revealed ? a.CandidateCity : null,
            revealed ? a.DistanceKm : null,
            ApplicationPreferenceRedaction.RedactForEmployer(a.PreferencesSummary, revealed),
            revealed ? a.CandidateName : null,
            revealed ? a.CandidateEmail : null,
            revealed ? a.CandidateAddress : null,
            revealed,
            revealed && a.WorkPermitConfirmed,
            revealed ? a.SnapshotAvailabilityJson : null,
            revealed ? a.SnapshotDrivingLicenses : null,
            revealed ? a.SnapshotEducations : null,
            revealed ? a.SnapshotAboutMe : null,
            revealed ? a.CandidateEmployerCount : 0,
            a.MatchPercent,
            a.MatchBreakdownJson,
            a.ViaSafetyNet,
            a.Motivation);
    }

    private async Task SendVerificationCodeAsync(
        Core.Entities.User candidate,
        Core.Entities.Vacancy vacancy,
        string code,
        CancellationToken cancellationToken)
    {
        await _email.SendAsync(new EmailMessage(
            candidate.Email,
            $"Verificatiecode voor sollicitatie: {vacancy.Title}",
            $"""
             <p>Hoi {Html(candidate.FullName)},</p>
             <p>Gebruik deze 6-cijferige code om je sollicitatie af te ronden:</p>
             <p style="font-size:1.6rem"><strong>{Html(code)}</strong></p>
             <p>De code is 10 minuten geldig.</p>
             """,
            "ApplicationVerificationCode"), cancellationToken);
    }

    private async Task NotifyEmployersOfNewApplicationAsync(
        Core.Entities.Vacancy vacancy,
        Core.Entities.Application application,
        CancellationToken cancellationToken)
    {
        var deepLink = await BuildDeepLinkAsync("/branch/applicants", cancellationToken);
        var contacts = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.IsActive
                && u.Role != UserRole.Candidate
                && (u.CompanyId == vacancy.CompanyId || u.CompanyMemberships.Any(m => m.CompanyId == vacancy.CompanyId)))
            .Select(u => u.Email)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var email in contacts)
        {
            await _email.SendAsync(new EmailMessage(
                email,
                $"Nieuwe sollicitatie: {vacancy.Title}",
                $"""
                 <p>Er is een nieuwe sollicitatie ontvangen voor <strong>{Html(vacancy.Title)}</strong>.</p>
                 <p><a href="{Html(deepLink)}">Open sollicitantenoverzicht</a></p>
                 """,
                "EmployerNewApplication"), cancellationToken);

            await _push.SendAsync(new PushMessage(
                email,
                "Nieuwe sollicitatie",
                $"{vacancy.Company.Name}: nieuwe kandidaat voor {vacancy.Title}",
                deepLink,
                "EmployerNewApplication"), cancellationToken);
        }
    }

    private static string BuildCompactPreferencesSummary(CandidatePreferencesDto preferences)
    {
        return JsonSerializer.Serialize(new
        {
            roles = preferences.Roles ?? [],
            maxTravelMinutes = preferences.MaxTravelMinutes,
            preferredTransport = preferences.PreferredTransport
        });
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static string? ExtractCityHint(Core.Entities.User candidate)
    {
        // Preferences JSON may include city; otherwise leave null until profile enrichment.
        if (string.IsNullOrWhiteSpace(candidate.PreferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(candidate.PreferencesJson);
            if (doc.RootElement.TryGetProperty("city", out var cityProp))
            {
                return cityProp.GetString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // ignore malformed prefs
        }

        return null;
    }

    private async Task<string> BuildDeepLinkAsync(string relativePath, CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = features.PublicWebBaseUrl.TrimEnd('/');
        if (!relativePath.StartsWith('/'))
        {
            relativePath = "/" + relativePath;
        }

        return baseUrl + relativePath;
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
