using System.Net;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

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

        var vacancy = await _db.Vacancies
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == request.VacancyId, cancellationToken);

        if (vacancy is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var applicationCount = await _db.Applications.CountAsync(a => a.VacancyId == vacancy.Id, cancellationToken);
        if (!VacancyVisibilityRules.CanAcceptApplications(vacancy, today, applicationCount))
        {
            return BadRequest(new { message = "Deze vacature neemt geen sollicitaties meer aan." });
        }

        var candidate = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (candidate is null || !candidate.IsActive)
        {
            return Unauthorized(new { message = "Inloggegevens incompleet; solliciteren niet mogelijk." });
        }

        var alreadyApplied = await _db.Applications.AnyAsync(
            a => a.VacancyId == vacancy.Id
                 && (a.CandidateUserId == candidate.Id
                     || a.CandidateEmail.ToLower() == candidate.Email.ToLower()),
            cancellationToken);
        if (alreadyApplied)
        {
            return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
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

        var application = new Core.Entities.Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancy.Id,
            CandidateUserId = candidate.Id,
            CandidateName = candidate.FullName,
            CandidateEmail = candidate.Email,
            CandidateCity = ExtractCityHint(candidate),
            PreferredTransport = request.PreferredTransport,
            EstimatedTravelMinutes = request.EstimatedTravelMinutes,
            DistanceKm = distanceKm,
            PreferencesSummary = candidate.PreferencesJson,
            Status = ApplicationStatus.Pending,
            ConsentAcceptedAt = DateTime.UtcNow,
            ConsentVersion = string.IsNullOrWhiteSpace(request.ConsentVersion)
                ? PrivacyConstants.CurrentConsentVersion
                : request.ConsentVersion.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Je hebt al gereageerd op deze vacature." });
        }

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

        var dto = new ApplicationDto(
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

        return Ok(new ApplyResultDto(dto, ConfirmationEmailQueued: true, authenticatorStubUsed));
    }

    [HttpPost("{id:guid}/react")]
    [Authorize(Policy = JobsyPolicies.RequireEmployer)]
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

    private static EmployerApplicationDto MapEmployerDto(Core.Entities.Application a)
    {
        var revealed = a.Status == ApplicationStatus.Accepted;
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
            revealed);
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
