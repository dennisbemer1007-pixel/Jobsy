using System.Net;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
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
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
    public async Task<ActionResult<IEnumerable<EmployerApplicationDto>>> GetForManagedCompanies(CancellationToken cancellationToken)
    {
        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
        var query = _db.Applications
            .AsNoTracking()
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(a => a.EmailVerifiedAt != null)
            .AsQueryable();

        if (accessible is not null)
        {
            query = query.Where(a => accessible.Contains(a.Vacancy.CompanyId));
        }

        var rows = await query
            .OrderBy(a => a.EstimatedTravelMinutes)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(MapEmployerDto));
    }

    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<ApplyResultDto>> Apply(
        [FromBody] ApplyRequest request,
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
            .FirstOrDefaultAsync(v => v.Id == request.VacancyId, cancellationToken);

        if (vacancy is null)
        {
            return NotFound();
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
        if (existing is not null && existing.EmailVerifiedAt is not null)
        {
            return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
        }

        var preferences = MeController.ParsePreferences(candidate.PreferencesJson);
        var requirementError = ValidateHardRequirements(vacancy, preferences);
        if (requirementError is not null)
        {
            return BadRequest(new { message = requirementError });
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
            distanceKm = HaversineKm(
                candidate.HomeLocation.Latitude,
                candidate.HomeLocation.Longitude,
                vacancy.Location.Latitude,
                vacancy.Location.Longitude);
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
        application.ConsentVersion = string.IsNullOrWhiteSpace(request.ConsentVersion)
            ? PrivacyConstants.CurrentConsentVersion
            : request.ConsentVersion.Trim();
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

        var isVerificationAttempt = !string.IsNullOrWhiteSpace(request.VerificationCode);
        if (!isVerificationAttempt)
        {
            var code = Random.Shared.Next(0, 1_000_000).ToString("D6");
            application.EmailVerificationCode = code;
            application.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
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

            await SendVerificationCodeAsync(candidate, vacancy, code, cancellationToken);
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
                "PendingVerification",
                null);
            return Ok(new ApplyResultDto(
                pendingDto,
                ConfirmationEmailQueued: false,
                authenticatorStubUsed,
                RequiresVerification: true,
                VerificationCodeSent: true));
        }

        if (existing is null)
        {
            return BadRequest(new { message = "Start eerst met Verzenden om een verificatiecode te ontvangen." });
        }

        if (existing.EmailVerifiedAt is not null)
        {
            return BadRequest(new { message = "Deze sollicitatie is al bevestigd." });
        }

        if (existing.EmailVerificationExpiresAt is null || existing.EmailVerificationExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Verificatiecode verlopen. Klik opnieuw op Verzenden." });
        }

        if (!string.Equals(existing.EmailVerificationCode, request.VerificationCode?.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Onjuiste verificatiecode." });
        }

        existing.EmailVerifiedAt = DateTime.UtcNow;
        existing.EmailVerificationCode = null;
        existing.EmailVerificationExpiresAt = null;
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
             <p><a href="{deepLink}">Bekijk je sollicitaties</a></p>
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

        return Ok(new ApplyResultDto(dto, ConfirmationEmailQueued: true, authenticatorStubUsed));
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
            <p><a href="{deepLink}">Open vacature</a></p>
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
             <p><a href="{deepLink}">Open vacature</a></p>
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
            a.CandidateCity,
            a.DistanceKm,
            ApplicationPreferenceRedaction.RedactForEmployer(a.PreferencesSummary, revealed),
            revealed ? a.CandidateName : null,
            revealed ? a.CandidateEmail : null,
            revealed ? a.CandidateAddress : null,
            revealed,
            a.WorkPermitConfirmed,
            a.SnapshotAvailabilityJson,
            a.SnapshotDrivingLicenses,
            a.SnapshotEducations,
            a.SnapshotAboutMe,
            a.CandidateEmployerCount);
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
                 <p><a href="{deepLink}">Open sollicitantenoverzicht</a></p>
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

    private static string? ValidateHardRequirements(Core.Entities.Vacancy vacancy, CandidatePreferencesDto prefs)
    {
        if (!string.IsNullOrWhiteSpace(vacancy.RequiredDrivingLicense))
        {
            var hasLicense = prefs.DrivingLicenses?.Any(x =>
                string.Equals(x, vacancy.RequiredDrivingLicense, StringComparison.OrdinalIgnoreCase)) == true;
            if (!hasLicense)
            {
                return $"Deze vacature vereist rijbewijs {vacancy.RequiredDrivingLicense}.";
            }
        }

        if (!string.IsNullOrWhiteSpace(vacancy.RequiredEducation))
        {
            var hasEducation = prefs.Educations?.Any(x =>
                string.Equals(x, vacancy.RequiredEducation, StringComparison.OrdinalIgnoreCase)) == true;
            if (!hasEducation)
            {
                return $"Deze vacature vereist opleiding/diploma: {vacancy.RequiredEducation}.";
            }
        }

        if (vacancy.MinimumEmployers is > 0)
        {
            var employerCount = prefs.Employers?.Count ?? 0;
            if (employerCount < vacancy.MinimumEmployers.Value)
            {
                return $"Deze vacature vereist minimaal {vacancy.MinimumEmployers.Value} eerdere werkgever(s).";
            }
        }

        return null;
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

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return Math.Round(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 1);
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
