using System.Net;
using Jobsy.Api.Models;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
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
    private readonly ILobsyCvPdfService _lobsyCvPdf;
    private readonly IUserNotificationService _notifications;
    private readonly ICandidateActionTokenService _actionTokens;
    private readonly IVacancyDiscoveryIndex _discoveryIndex;

    public ApplicationsController(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        IEmailService email,
        IPushNotificationService push,
        IPlatformFeatureService features,
        ILobsyCvPdfService lobsyCvPdf,
        IUserNotificationService notifications,
        ICandidateActionTokenService actionTokens,
        IVacancyDiscoveryIndex discoveryIndex)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
        _email = email;
        _push = push;
        _features = features;
        _lobsyCvPdf = lobsyCvPdf;
        _notifications = notifications;
        _actionTokens = actionTokens;
        _discoveryIndex = discoveryIndex;
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
            query = query.Where(a =>
                accessible.Contains(a.Vacancy.CompanyId)
                || (a.Vacancy.IntermediaryCompanyId != null
                    && accessible.Contains(a.Vacancy.IntermediaryCompanyId.Value)));
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
                a.Motivation,
                a.StudentNumber,
                a.SchoolEmail,
                a.StudyProgram,
                a.StudyYear,
                a.ExclusivityValidationStatus,
                a.SnapshotPhoneNumber,
                a.SnapshotWhatsAppAllowed,
                a.CandidateAgeYears,
                a.HasUploadedCv,
                a.CandidateReferenceCount
            })
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(a =>
        {
            var revealed = ApplicationRules.IsPiiRevealed(a.Status);
            var contact = ApplicationRules.IsDirectContactRevealed(a.Status);
            var availability = LobsyCvModelFactory.ParseAvailabilityPayload(a.SnapshotAvailabilityJson);
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
                // Distance without address/city is allowed pre-accept for screening.
                a.DistanceKm,
                ApplicationPreferenceRedaction.RedactForEmployer(a.PreferencesSummary, revealed),
                revealed ? a.CandidateName : null,
                contact ? a.CandidateEmail : null,
                revealed ? a.CandidateAddress : null,
                revealed,
                revealed && a.WorkPermitConfirmed,
                revealed ? a.SnapshotAvailabilityJson : null,
                revealed ? ApplicationPreferenceRedaction.ToHumanReadable(a.SnapshotDrivingLicenses) : null,
                revealed ? ApplicationPreferenceRedaction.ToHumanReadable(a.SnapshotEducations) : null,
                revealed ? a.SnapshotAboutMe : null,
                revealed ? a.CandidateEmployerCount : 0,
                a.MatchPercent,
                MatchBreakdownJson: null,
                a.ViaSafetyNet,
                // Motivation is candidate-authored for the employer — show after verified apply (pre-accept OK).
                a.Motivation,
                LegalEligible: true,
                revealed ? a.StudentNumber : null,
                contact ? a.SchoolEmail : null,
                revealed ? a.StudyProgram : null,
                revealed ? a.StudyYear : null,
                revealed ? a.ExclusivityValidationStatus : null,
                CvPdfAvailable: revealed,
                CandidatePhone: contact ? a.SnapshotPhoneNumber : null,
                WhatsAppContactAllowed: contact && a.SnapshotWhatsAppAllowed,
                CandidateAgeYears: a.CandidateAgeYears,
                AvailabilitySummary: LobsyCvModelFactory.FormatAvailability(
                    availability.Slots,
                    availability.FlexibleTimes),
                UploadedCvAvailable: revealed && a.HasUploadedCv,
                CandidateReferenceCount: revealed ? a.CandidateReferenceCount : 0);
        }));
    }

    /// <summary>
    /// Lobsy-CV PDF for an application.
    /// Candidate (owner): allowed for verified applications (snapshot).
    /// Employer: only when PiiRevealed (Accepted / EmployerContacting / Hired).
    /// </summary>
    [HttpGet("{id:guid}/lobsy-cv.pdf")]
    [Authorize]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadLobsyCv(Guid id, CancellationToken cancellationToken)
    {
        var caller = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (caller is null || !caller.IsActive)
        {
            return Unauthorized();
        }

        var application = await _db.Applications
            .AsNoTracking()
            .Include(a => a.Vacancy)
                .ThenInclude(v => v.Company)
            .Include(a => a.Vacancy)
                .ThenInclude(v => v.IntermediaryCompany)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var isOwner = LobsyCvAccessRules.CanCandidateDownloadOwnApplication(
            caller.Id,
            application.CandidateUserId,
            caller.Email,
            application.CandidateEmail);

        if (isOwner)
        {
            if (application.EmailVerifiedAt is null)
            {
                return BadRequest(new
                {
                    code = "cv_not_verified",
                    message = "Bevestig eerst je sollicitatie met de verificatiecode voordat je het Lobsy-CV kunt downloaden."
                });
            }

            var model = LobsyCvModelFactory.FromApplicationForDownload(
                application,
                includePii: true,
                includeDirectContact: true);
            var pdf = await _lobsyCvPdf.RenderAsync(model, cancellationToken);
            return File(pdf, "application/pdf", _lobsyCvPdf.BuildFileName(model));
        }

        if (!_companyAuth.IsEmployer(User) && !_companyAuth.IsAdmin(User))
        {
            return Forbid();
        }

        if (!await CanAccessApplicationEmployerAsync(application, cancellationToken))
        {
            return Forbid();
        }

        if (!LobsyCvAccessRules.CanEmployerDownloadCv(application.Status, application.EmailVerifiedAt))
        {
            return StatusCode((int)HttpStatusCode.Forbidden, new
            {
                code = "cv_not_released",
                message = "Het Lobsy-CV is pas beschikbaar nadat je de sollicitatie hebt geaccepteerd."
            });
        }

        var employerModel = LobsyCvModelFactory.FromApplicationForDownload(
            application,
            includePii: true,
            includeDirectContact: ApplicationRules.IsDirectContactRevealed(application.Status));
        var employerPdf = await _lobsyCvPdf.RenderAsync(employerModel, cancellationToken);
        return File(employerPdf, "application/pdf", _lobsyCvPdf.BuildFileName(employerModel));
    }

    [HttpGet("{id:guid}/uploaded-cv")]
    [Authorize]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadUploadedCv(Guid id, CancellationToken cancellationToken)
    {
        var caller = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (caller is null || !caller.IsActive)
        {
            return Unauthorized();
        }

        var application = await _db.Applications
            .AsNoTracking()
            .Include(a => a.Vacancy)
            .Include(a => a.UploadedCv)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var isOwner = LobsyCvAccessRules.CanCandidateDownloadOwnApplication(
            caller.Id,
            application.CandidateUserId,
            caller.Email,
            application.CandidateEmail);

        if (!isOwner)
        {
            if (!_companyAuth.IsEmployer(User) && !_companyAuth.IsAdmin(User))
            {
                return Forbid();
            }

            if (!await CanAccessApplicationEmployerAsync(application, cancellationToken))
            {
                return Forbid();
            }

            if (!LobsyCvAccessRules.CanEmployerDownloadCv(application.Status, application.EmailVerifiedAt))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, new
                {
                    code = "cv_not_released",
                    message = "Het geüploade CV is pas beschikbaar nadat je de sollicitatie hebt geaccepteerd."
                });
            }
        }
        else if (application.EmailVerifiedAt is null)
        {
            return BadRequest(new
            {
                code = "cv_not_verified",
                message = "Bevestig eerst je sollicitatie voordat je het geüploade CV kunt downloaden."
            });
        }

        if (application.UploadedCv is null || application.UploadedCv.Content.Length == 0)
        {
            return NotFound(new { message = "Er is geen geüpload CV bij deze sollicitatie." });
        }

        return File(application.UploadedCv.Content, application.UploadedCv.ContentType, application.UploadedCv.FileName);
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
            a => a.VacancyId == vacancy.Id
                 && a.EmailVerifiedAt != null
                 && a.Status != ApplicationStatus.Withdrawn,
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
            && ApplicationRules.IsSameCandidate(candidate.Id, candidate.Email, existing.CandidateUserId, existing.CandidateEmail)
            && ApplicationRules.BlocksDuplicateApplication(existing.Status, existing.EmailVerifiedAt))
        {
            return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
        }

        var preferences = MeController.ParsePreferences(candidate.PreferencesJson);
        var referenceRows = await _db.CandidateReferences.AsNoTracking()
            .Where(r => r.UserId == candidate.Id)
            .Select(r => new { r.EmployerName, r.ContactName, r.Email, r.Phone })
            .ToListAsync(cancellationToken);
        var completeReferences = CandidateReferenceRules.CountComplete(
            referenceRows.Select(r => (r.EmployerName, r.ContactName, r.Email, r.Phone)));
        var requirementError = ApplicationRequirementRules.ValidateHardRequirements(
            vacancy.RequiredDrivingLicense,
            vacancy.RequiredEducation,
            vacancy.MinimumEmployers,
            preferences.DrivingLicenses,
            preferences.Educations,
            preferences.Employers?.Count ?? 0,
            vacancy.MinimumReferences,
            completeReferences);
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
        var requireEmailVerification = vacancy.RequireEmailVerification;

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

        if (existing is not null && ApplicationRules.CanReuseWithdrawnApplication(existing.Status))
        {
            // Reopen withdrawn application instead of blocking re-apply.
            application.Status = ApplicationStatus.Pending;
            application.RespondedAt = null;
            application.CreatedAt = DateTime.UtcNow;
            application.CandidateUserId = candidate.Id;
            application.CandidateEmail = candidate.Email;
        }

        application.CandidateCity = LobsyCvModelFactory.ExtractCity(preferences.HomeAddress)
                                    ?? ExtractCityHint(candidate);
        application.CandidateAddress = Truncate(preferences.HomeAddress, 512);
        application.PreferredTransport = request.PreferredTransport;
        application.EstimatedTravelMinutes = request.EstimatedTravelMinutes;
        application.DistanceKm = distanceKm;
        application.CandidateAgeYears = ageYears;
        application.SnapshotDateOfBirth = candidate.DateOfBirth;
        // Compact summary only — full prefs JSON easily exceeds varchar(1024) and broke Apply.
        application.PreferencesSummary = BuildCompactPreferencesSummary(preferences);
        application.ConsentAcceptedAt = DateTime.UtcNow;
        // Server stamps consent version — client-supplied values are ignored (AVG integrity).
        application.ConsentVersion = PrivacyConstants.CurrentConsentVersion;
        application.WorkPermitConfirmed = request.WorkPermitConfirmed;
        application.SnapshotAvailabilityJson = Truncate(
            LobsyCvModelFactory.SerializeAvailabilitySnapshot(preferences),
            4000);
        application.SnapshotDrivingLicenses = Truncate(
            preferences.DrivingLicenses is null ? null : string.Join(", ", preferences.DrivingLicenses),
            512);
        application.SnapshotEducations = Truncate(
            preferences.Educations is null ? null : string.Join(", ", preferences.Educations),
            512);
        application.SnapshotAboutMe = Truncate(preferences.AboutMe, 1024);
        application.CandidateEmployerCount = preferences.Employers?.Count ?? 0;
        application.CandidateReferenceCount = completeReferences;
        await SnapshotUploadedCvAsync(application, candidate.Id, cancellationToken);
        application.SnapshotPhoneNumber = Truncate(candidate.PhoneNumber, 32);
        application.SnapshotWhatsAppAllowed = candidate.WhatsAppContactAllowed
                                              && !string.IsNullOrWhiteSpace(candidate.PhoneNumber);
        application.SnapshotHomeLatitude = candidate.HomeLocation?.Latitude;
        application.SnapshotHomeLongitude = candidate.HomeLocation?.Longitude;
        application.SnapshotCertificatesJson =
            LobsyCvModelFactory.SerializeCertificatesSnapshot(preferences.Certificates, maxLength: 4000);
        // Home address is never shown on Lobsy-CV; keep snapshot flag false for privacy consistency.
        application.SnapshotShowAddressOnCv = false;
        application.CandidateName = CandidateNameRules.ComposeFullName(
            candidate.FirstName, candidate.LastName, candidate.FullName);
        var motivationText = string.IsNullOrWhiteSpace(request.Motivation)
            ? preferences.DefaultMotivation
            : request.Motivation.Trim();
        application.Motivation = Truncate(
            string.IsNullOrWhiteSpace(motivationText) ? null : motivationText.Trim(),
            500);
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
            if (!requireEmailVerification)
            {
                // Verification disabled for this vacancy: finalize immediately (no OTP UI).
                application.EmailVerifiedAt = DateTime.UtcNow;
                application.EmailVerificationCode = null;
                application.EmailVerificationExpiresAt = null;
                application.EmailVerificationFailedAttempts = 0;
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

                await SendApplicationConfirmationAsync(
                    candidate, vacancy, application, authenticatorStubUsed, cancellationToken);
                await NotifyEmployersOfNewApplicationAsync(vacancy, application, cancellationToken);

                var immediateDto = new ApplicationDto(
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
                    application.RespondedAt);

                return Ok(new ApplyResultDto(
                    immediateDto,
                    ConfirmationEmailQueued: true,
                    authenticatorStubUsed,
                    RequiresVerification: false,
                    VerificationCodeSent: false,
                    DirectContact: ToDirectContactDto(vacancy),
                    RequiresSafetyNetConfirmation: false,
                    MatchPercent: application.MatchPercent ?? match.TotalPercent,
                    MatchBreakdownJson: application.MatchBreakdownJson ?? matchJson));
            }

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

        if (!requireEmailVerification)
        {
            return BadRequest(new { message = "E-mailverificatie is voor deze vacature niet vereist." });
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

        await SendApplicationConfirmationAsync(
            candidate, vacancy, existing, authenticatorStubUsed, cancellationToken);

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
                 && a.Status != ApplicationStatus.Withdrawn
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

        application.Status = ApplicationStatus.Withdrawn;
        application.RespondedAt = DateTime.UtcNow;
        await ApplicationPrivacyCleanup.RemoveUploadedCvSnapshotsAsync(_db, [application.Id], cancellationToken);
        ApplicationRules.ScrubPersonalDataOnWithdraw(application);
        await _db.SaveChangesAsync(cancellationToken);

        var vacancy = await _db.Vacancies.AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == application.VacancyId, cancellationToken);
        if (vacancy is not null)
        {
            await NotifyEmployersOfWithdrawalAsync(vacancy, application, cancellationToken);
        }

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
        if (!CanAccessApplicationCompany(application, accessible))
        {
            return Forbid();
        }

        if (!ApplicationRules.CanEmployerReact(application.Status))
        {
            return BadRequest(new { message = "Op deze sollicitatie is al gereageerd." });
        }

        var respondedAt = DateTime.UtcNow;
        if (_db.Database.IsRelational())
        {
            // Atomic Pending→react on Postgres (race-safe).
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

            // Keep the tracked graph in sync for e-mail / push below.
            application.Status = request.Status;
            application.RespondedAt = respondedAt;
            _db.Entry(application).Property(a => a.Status).IsModified = false;
            _db.Entry(application).Property(a => a.RespondedAt).IsModified = false;
        }
        else
        {
            // InMemory test provider has no ExecuteUpdate — tracked update is enough there.
            application.Status = request.Status;
            application.RespondedAt = respondedAt;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var appsLink = await BuildDeepLinkAsync("/candidate/applications", cancellationToken);
        var baseUrl = (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl;

        if (request.Status == ApplicationStatus.Rejected)
        {
            var rejectSubject = $"Update op je sollicitatie: {application.Vacancy.Title}";
            var rejectBody =
                $"{application.Vacancy.Company.Name}: helaas niet geselecteerd voor {application.Vacancy.Title}";
            var rejectMail = TransactionalEmails.EmployerReactionRejected(
                baseUrl, application.CandidateName, application.Vacancy.Title, application.Vacancy.Company.Name);
            await _email.SendAsync(new EmailMessage(
                application.CandidateEmail,
                rejectMail.Subject,
                rejectMail.Html,
                rejectMail.Category), cancellationToken);

            await _push.SendAsync(new PushMessage(
                application.CandidateEmail,
                "Update op je sollicitatie",
                rejectBody,
                appsLink,
                "EmployerReaction"), cancellationToken);

            await NotifyCandidateAsync(
                application,
                rejectSubject,
                rejectBody,
                "EmployerReaction",
                "/candidate/applications",
                cancellationToken);
        }
        else
        {
            var acceptSubject = $"Je sollicitatie is geaccepteerd: {application.Vacancy.Title}";
            var acceptBody =
                $"{application.Vacancy.Company.Name}: sollicitatie geaccepteerd — {application.Vacancy.Title}";
            var acceptMail = TransactionalEmails.EmployerReactionAccepted(
                baseUrl, application.CandidateName, application.Vacancy.Title, application.Vacancy.Company.Name);
            await _email.SendAsync(new EmailMessage(
                application.CandidateEmail,
                acceptMail.Subject,
                acceptMail.Html,
                acceptMail.Category), cancellationToken);

            await _push.SendAsync(new PushMessage(
                application.CandidateEmail,
                "Positief nieuws over je sollicitatie",
                acceptBody,
                appsLink,
                "EmployerReaction"), cancellationToken);

            await NotifyCandidateAsync(
                application,
                acceptSubject,
                acceptBody,
                "EmployerReaction",
                "/candidate/applications",
                cancellationToken);
        }

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
        if (!CanAccessApplicationCompany(application, accessible))
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

        var appsLink = await BuildDeepLinkAsync("/candidate/applications", cancellationToken);
        var contactSubject = $"Werkgever neemt contact op: {application.Vacancy.Title}";
        var contactBody = $"{application.Vacancy.Company.Name} neemt contact op over {application.Vacancy.Title}";
        var contactMail = TransactionalEmails.EmployerContacting(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            application.Vacancy.Title);
        await _email.SendAsync(new EmailMessage(
            application.CandidateEmail,
            contactMail.Subject,
            contactMail.Html,
            contactMail.Category), cancellationToken);
        await _push.SendAsync(new PushMessage(
            application.CandidateEmail,
            "Werkgever neemt contact op",
            contactBody,
            appsLink,
            "EmployerContacting"), cancellationToken);
        await NotifyCandidateAsync(
            application,
            contactSubject,
            contactBody,
            "EmployerContacting",
            "/candidate/applications",
            cancellationToken);

        return Ok(MapEmployerDto(application));
    }

    [HttpPost("vacancies/{vacancyId:guid}/fulfill/{applicationId:guid}")]
    [Authorize(Roles = JobsyRoles.ApplicationReactRoles)]
    public async Task<ActionResult> FulfillVacancy(
        Guid vacancyId,
        Guid applicationId,
        [FromBody] FulfillVacancyRequest? request,
        CancellationToken cancellationToken)
    {
        var rejectOthers = request?.RejectOtherApplications ?? true;
        var closeVacancy = request?.CloseVacancy ?? true;

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

        if (chosen.Status is ApplicationStatus.Hired
            or ApplicationStatus.Rejected
            or ApplicationStatus.FilledElsewhere)
        {
            return BadRequest(new { message = "Deze sollicitatie is al afgerond." });
        }

        if (chosen.Status is not (ApplicationStatus.Accepted or ApplicationStatus.EmployerContacting))
        {
            return BadRequest(new { message = "Matchen kan pas na acceptatie van de kandidaat." });
        }

        chosen.Status = ApplicationStatus.Hired;
        chosen.RespondedAt = DateTime.UtcNow;

        if (closeVacancy)
        {
            vacancy.Status = VacancyStatus.Fulfilled;
            vacancy.ClosedAtUtc ??= DateTime.UtcNow;
            vacancy.FulfilledByApplicationId = chosen.Id;
        }

        List<Core.Entities.Application> others = [];
        if (rejectOthers)
        {
            others = await _db.Applications
                .Where(a => a.VacancyId == vacancyId
                            && a.Id != chosen.Id
                            && a.EmailVerifiedAt != null
                            && a.Status != ApplicationStatus.Rejected
                            && a.Status != ApplicationStatus.FilledElsewhere
                            && a.Status != ApplicationStatus.Hired
                            && a.Status != ApplicationStatus.Withdrawn)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.Status = ApplicationStatus.FilledElsewhere;
                other.RespondedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (closeVacancy)
        {
            _discoveryIndex.Invalidate();
        }

        string? withdrawEmailPath = null;
        string? withdrawInAppPath = null;
        if (chosen.CandidateUserId is Guid hiredUserId)
        {
            var action = await _actionTokens.IssueAsync(
                hiredUserId,
                CandidateActionPurposes.WithdrawOtherApplications,
                chosen.Id,
                cancellationToken: cancellationToken);
            withdrawEmailPath = action.RelativeActionPath;
            withdrawInAppPath = CandidateActionPurposes.WithdrawOthersInAppPath(chosen.Id);
        }

        var withdrawAbsolute = withdrawEmailPath is null
            ? null
            : await BuildDeepLinkAsync(withdrawEmailPath, cancellationToken);
        var hiredSubject = $"Gefeliciteerd! Je bent aangenomen voor {vacancy.Title}";
        var hiredNotifyBody = $"Wat een feest — je bent aangenomen voor {vacancy.Title} bij {vacancy.Company.Name}.";
        var hiredMail = TransactionalEmails.ApplicationHired(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            chosen.CandidateName,
            vacancy.Title,
            vacancy.Company.Name,
            chosen.Id,
            withdrawAbsolute);
        await _email.SendAsync(new EmailMessage(
            chosen.CandidateEmail,
            hiredMail.Subject,
            hiredMail.Html,
            hiredMail.Category), cancellationToken);

        await NotifyCandidateAsync(
            chosen,
            hiredSubject,
            hiredNotifyBody,
            "ApplicationHired",
            "/candidate/applications",
            cancellationToken,
            actionLabel: withdrawInAppPath is null ? null : "Andere sollicitaties netjes intrekken",
            actionUrl: withdrawInAppPath);

        foreach (var other in others)
        {
            var otherSubject = $"Update sollicitatie: {vacancy.Title}";
            var otherBody = $"Helaas is de keuze op een andere kandidaat gevallen voor {vacancy.Title}.";
            var otherMail = TransactionalEmails.ApplicationFilledElsewhere(
                (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
                other.CandidateName,
                vacancy.Title,
                vacancy.Company.Name);
            await _email.SendAsync(new EmailMessage(
                other.CandidateEmail,
                otherMail.Subject,
                otherMail.Html,
                otherMail.Category), cancellationToken);

            await NotifyCandidateAsync(
                other,
                otherSubject,
                otherBody,
                "ApplicationFilledElsewhere",
                "/candidate/applications",
                cancellationToken);
        }

        return Ok(new
        {
            hiredApplicationId = chosen.Id,
            vacancyClosed = closeVacancy,
            rejectedOtherCount = others.Count
        });
    }

    private static EmployerApplicationDto MapEmployerDto(Core.Entities.Application a)
    {
        var revealed = ApplicationRules.IsPiiRevealed(a.Status);
        var contact = ApplicationRules.IsDirectContactRevealed(a.Status);
        var availability = LobsyCvModelFactory.ParseAvailabilityPayload(a.SnapshotAvailabilityJson);
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
            a.DistanceKm,
            ApplicationPreferenceRedaction.RedactForEmployer(a.PreferencesSummary, revealed),
            revealed ? a.CandidateName : null,
            contact ? a.CandidateEmail : null,
            revealed ? a.CandidateAddress : null,
            revealed,
            revealed && a.WorkPermitConfirmed,
            revealed ? a.SnapshotAvailabilityJson : null,
            revealed ? ApplicationPreferenceRedaction.ToHumanReadable(a.SnapshotDrivingLicenses) : null,
            revealed ? ApplicationPreferenceRedaction.ToHumanReadable(a.SnapshotEducations) : null,
            revealed ? a.SnapshotAboutMe : null,
            revealed ? a.CandidateEmployerCount : 0,
            a.MatchPercent,
            MatchBreakdownJson: null,
            a.ViaSafetyNet,
            a.Motivation,
            LegalEligible: true,
            revealed ? a.StudentNumber : null,
            contact ? a.SchoolEmail : null,
            revealed ? a.StudyProgram : null,
            revealed ? a.StudyYear : null,
            revealed ? a.ExclusivityValidationStatus : null,
            CvPdfAvailable: revealed,
            CandidatePhone: contact ? a.SnapshotPhoneNumber : null,
            WhatsAppContactAllowed: contact && a.SnapshotWhatsAppAllowed,
            CandidateAgeYears: a.CandidateAgeYears,
            AvailabilitySummary: LobsyCvModelFactory.FormatAvailability(
                availability.Slots,
                availability.FlexibleTimes),
            UploadedCvAvailable: revealed && a.HasUploadedCv,
            CandidateReferenceCount: revealed ? a.CandidateReferenceCount : 0);
    }

    private async Task SnapshotUploadedCvAsync(
        Core.Entities.Application application,
        Guid candidateUserId,
        CancellationToken cancellationToken)
    {
        var uploaded = await _db.CandidateUploadedCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == candidateUserId, cancellationToken);
        application.HasUploadedCv = uploaded is not null;
        if (uploaded is null)
        {
            if (application.UploadedCv is not null)
            {
                _db.ApplicationUploadedCvs.Remove(application.UploadedCv);
                application.UploadedCv = null;
            }

            return;
        }

        var snapshot = await _db.ApplicationUploadedCvs
            .FirstOrDefaultAsync(c => c.ApplicationId == application.Id, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new Core.Entities.ApplicationUploadedCv { ApplicationId = application.Id };
            _db.ApplicationUploadedCvs.Add(snapshot);
        }

        snapshot.FileName = uploaded.FileName;
        snapshot.ContentType = uploaded.ContentType;
        snapshot.Content = uploaded.Content;
        snapshot.SizeBytes = uploaded.SizeBytes;
        application.UploadedCv = snapshot;
    }

    private async Task SendApplicationConfirmationAsync(
        Core.Entities.User candidate,
        Core.Entities.Vacancy vacancy,
        Core.Entities.Application application,
        bool authenticatorStubUsed,
        CancellationToken cancellationToken)
    {
        var subject = $"Sollicitatie bevestigd: {vacancy.Title}";
        var body = $"Je sollicitatie op {vacancy.Title} bij {vacancy.Company.Name} is ontvangen.";
        var mail = TransactionalEmails.ApplicationConfirmation(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            candidate.FullName,
            vacancy.Title,
            vacancy.Company.Name,
            authenticatorStubUsed);
        await _email.SendAsync(new EmailMessage(
            candidate.Email,
            mail.Subject,
            mail.Html,
            mail.Category), cancellationToken);

        await NotifyCandidateAsync(
            application,
            subject,
            body,
            "ApplicationConfirmation",
            "/candidate/applications",
            cancellationToken);
    }

    private async Task SendVerificationCodeAsync(
        Core.Entities.User candidate,
        Core.Entities.Vacancy vacancy,
        string code,
        CancellationToken cancellationToken)
    {
        var subject = $"Verificatiecode voor sollicitatie: {vacancy.Title}";
        var body = "Gebruik de 6-cijferige code in je e-mail om je sollicitatie af te ronden. De code is 10 minuten geldig.";
        var mail = TransactionalEmails.ApplicationVerificationCode(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            candidate.FullName,
            vacancy.Title,
            vacancy.Id,
            code);
        await _email.SendAsync(new EmailMessage(
            candidate.Email,
            mail.Subject,
            mail.Html,
            mail.Category), cancellationToken);

        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                candidate.Id,
                subject,
                body,
                "ApplicationVerificationCode",
                $"/vacancies/{vacancy.Id}"),
            cancellationToken);
    }

    private async Task NotifyCandidateAsync(
        Core.Entities.Application application,
        string title,
        string body,
        string category,
        string? deepLink,
        CancellationToken cancellationToken,
        string? actionLabel = null,
        string? actionUrl = null)
    {
        if (application.CandidateUserId is not Guid userId)
        {
            await _notifications.CreateForEmailAsync(
                application.CandidateEmail,
                title,
                body,
                category,
                deepLink,
                actionLabel,
                actionUrl,
                "Application",
                application.Id,
                cancellationToken);
            return;
        }

        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                userId,
                title,
                body,
                category,
                deepLink,
                actionLabel,
                actionUrl,
                "Application",
                application.Id),
            cancellationToken);
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
            .Select(u => new { u.Id, u.Email })
            .Distinct()
            .ToListAsync(cancellationToken);

        var subject = $"Nieuwe sollicitatie: {vacancy.Title}";
        var body = $"{vacancy.Company.Name}: nieuwe kandidaat voor {vacancy.Title}";
        var mail = TransactionalEmails.EmployerNewApplication(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            vacancy.Title);
        foreach (var contact in contacts)
        {
            await _email.SendAsync(new EmailMessage(
                contact.Email,
                mail.Subject,
                mail.Html,
                mail.Category), cancellationToken);

            await _push.SendAsync(new PushMessage(
                contact.Email,
                "Nieuwe sollicitatie",
                body,
                deepLink,
                "EmployerNewApplication"), cancellationToken);

            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    contact.Id,
                    subject,
                    body,
                    "EmployerNewApplication",
                    "/branch/applicants",
                    RelatedEntityType: "Application",
                    RelatedEntityId: application.Id),
                cancellationToken);
        }
    }

    private async Task NotifyEmployersOfWithdrawalAsync(
        Core.Entities.Vacancy vacancy,
        Core.Entities.Application application,
        CancellationToken cancellationToken)
    {
        var contacts = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.IsActive
                && u.Role != UserRole.Candidate
                && (u.CompanyId == vacancy.CompanyId || u.CompanyMemberships.Any(m => m.CompanyId == vacancy.CompanyId)))
            .Select(u => new { u.Id, u.Email })
            .Distinct()
            .ToListAsync(cancellationToken);

        var subject = $"Sollicitatie ingetrokken: {vacancy.Title}";
        var body = "Een kandidaat heeft de sollicitatie ingetrokken.";
        var mail = TransactionalEmails.CandidateWithdrawn(
            (await _features.GetAsync(cancellationToken)).PublicWebBaseUrl,
            vacancy.Title);
        foreach (var contact in contacts)
        {
            await _email.SendAsync(new EmailMessage(
                contact.Email,
                mail.Subject,
                mail.Html,
                mail.Category), cancellationToken);

            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    contact.Id,
                    subject,
                    body,
                    "CandidateWithdrawn",
                    "/branch/applicants",
                    RelatedEntityType: "Application",
                    RelatedEntityId: application.Id),
                cancellationToken);
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
        return EmailLayout.Absolute(features.PublicWebBaseUrl, relativePath);
    }

    private static bool CanAccessApplicationCompany(
        Application application,
        IReadOnlyCollection<Guid>? accessible)
    {
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

    private async Task<bool> CanAccessApplicationEmployerAsync(
        Application application,
        CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        return CanAccessApplicationCompany(application, accessible);
    }

    private static string Html(string? value) => EmailLayout.Escape(value);
}
