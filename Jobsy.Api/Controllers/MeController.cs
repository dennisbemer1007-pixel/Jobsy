using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly ITranslationService _translation;
    private readonly ILobsyCvPdfService _lobsyCvPdf;
    private readonly ICvTextExtractor _cvText;
    private readonly ICvExtractionService _cvExtraction;
    private const string VacancySourceLanguage = "nl";

    public MeController(
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        JobsyDbContext db,
        IPlatformFeatureService features,
        ITranslationService translation,
        ILobsyCvPdfService lobsyCvPdf,
        ICvTextExtractor cvText,
        ICvExtractionService cvExtraction)
    {
        _companyAuth = companyAuth;
        _users = users;
        _db = db;
        _features = features;
        _translation = translation;
        _lobsyCvPdf = lobsyCvPdf;
        _cvText = cvText;
        _cvExtraction = cvExtraction;
    }

    [HttpGet("access")]
    public async Task<ActionResult<MeAccessDto>> GetAccess(CancellationToken cancellationToken)
    {
        var role = _companyAuth.GetPrimaryRole(User)?.ToString();
        var companies = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);

        return Ok(new MeAccessDto(
            role,
            _companyAuth.IsAdmin(User),
            _companyAuth.IsEmployer(User),
            _companyAuth.IsCandidate(User),
            companies,
            companies is null));
    }

        [HttpGet("profile")]
    public async Task<ActionResult<MeProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _users.FindByPrincipalAsync(User, cancellationToken);
            if (user is null)
            {
                return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
            }

            var features = await _features.GetAsync(cancellationToken);
            return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Profiel kon niet worden geladen." });
        }
    }

    [HttpPut("language")]
    public async Task<ActionResult<MeProfileDto>> UpdateLanguage(
        [FromBody] UpdateLanguageRequest request,
        CancellationToken cancellationToken)
    {
        if (!JobsyLanguages.IsSupported(request.Language))
        {
            return BadRequest(new
            {
                message = "Ongeldige taal. Ondersteund: nl, en, pl, ro, ar."
            });
        }

        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var language = JobsyLanguages.Normalize(request.Language);
        var existing = ParsePreferences(user.PreferencesJson);
        user.PreferencesJson = SerializePreferences(
            existing.Roles,
            existing.MaxTravelMinutes,
            existing.PreferredTransport,
            language,
            existing.AgeYears,
            existing.AboutMe,
            existing.DefaultMotivation,
            existing.DrivingLicenses,
            existing.Availability,
            existing.Employers,
            existing.Educations,
            existing.HomeAddress,
            existing.MinHoursPerWeek,
            existing.MaxHoursPerWeek,
            existing.FlexibleTimes,
            existing.Certificates,
            existing.ShowAddressOnCv);

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpPut("profile")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<MeProfileDto>> UpdateProfile(
        [FromBody] UpdateCandidateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (request.DateOfBirth is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.DateOfBirth > today || request.DateOfBirth < today.AddYears(-100))
            {
                return BadRequest(new { message = "Ongeldige geboortedatum." });
            }

            user.DateOfBirth = request.DateOfBirth;
        }

        if (request.OpenForWork is not null)
        {
            user.OpenForWork = request.OpenForWork.Value;
        }

        if (request.FirstName is not null || request.LastName is not null)
        {
            var first = request.FirstName is null
                ? user.FirstName
                : (string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim());
            var last = request.LastName is null
                ? user.LastName
                : (string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim());

            if (first is { Length: > 128 } || last is { Length: > 128 })
            {
                return BadRequest(new { message = "Voor- of achternaam is te lang." });
            }

            user.FirstName = first;
            user.LastName = last;
            var composed = CandidateNameRules.ComposeFullName(first, last, user.FullName);
            if (!string.IsNullOrWhiteSpace(composed))
            {
                user.FullName = composed.Length > 256 ? composed[..256] : composed;
            }
        }

        if (request.PhoneNumber is not null)
        {
            var phone = CandidatePhoneRules.Normalize(request.PhoneNumber);
            if (!CandidatePhoneRules.IsValid(phone))
            {
                return BadRequest(new { message = "Ongeldig telefoonnummer." });
            }

            user.PhoneNumber = phone;
            if (phone is null)
            {
                user.WhatsAppContactAllowed = false;
            }
        }

        if (request.WhatsAppContactAllowed is not null)
        {
            user.WhatsAppContactAllowed = request.WhatsAppContactAllowed.Value
                                          && !string.IsNullOrWhiteSpace(user.PhoneNumber);
        }

        if (request.ClearHomeLocation)
        {
            user.HomeLocation = null;
        }
        else if (request.HomeLatitude is not null || request.HomeLongitude is not null)
        {
            if (request.HomeLatitude is null || request.HomeLongitude is null)
            {
                return BadRequest(new { message = "HomeLatitude en HomeLongitude moeten samen worden gezet." });
            }

            if (request.HomeLatitude is < -90 or > 90 || request.HomeLongitude is < -180 or > 180)
            {
                return BadRequest(new { message = "Ongeldige thuislocatie-coördinaten." });
            }

            user.HomeLocation = new Core.ValueObjects.GeoPoint(request.HomeLatitude.Value, request.HomeLongitude.Value);
        }

        if (request.Preferences is not null)
        {
            if (request.Preferences.MaxTravelMinutes is < 1 or > 180)
            {
                return BadRequest(new { message = "Maximale reistijd moet tussen 1 en 180 minuten liggen." });
            }

            if (request.Preferences.Language is not null
                && !JobsyLanguages.IsSupported(request.Preferences.Language))
            {
                return BadRequest(new
                {
                    message = "Ongeldige taal. Ondersteund: nl, en, pl, ro, ar."
                });
            }

            if (request.Preferences.AgeYears is < 15 or > 67)
            {
                return BadRequest(new { message = "Leeftijd moet tussen 15 en 67 liggen." });
            }

            var roles = (request.Preferences.Roles ?? [])
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim().ToLowerInvariant())
                .Distinct()
                .Take(12)
                .ToArray();

            var existing = ParsePreferences(user.PreferencesJson);
            var language = request.Preferences.Language is null
                ? existing.Language
                : JobsyLanguages.Normalize(request.Preferences.Language);

            user.PreferencesJson = SerializePreferences(
                roles,
                request.Preferences.MaxTravelMinutes,
                string.IsNullOrWhiteSpace(request.Preferences.PreferredTransport)
                    ? null
                    : request.Preferences.PreferredTransport.Trim(),
                language,
                request.Preferences.AgeYears,
                request.Preferences.AboutMe,
                request.Preferences.DefaultMotivation,
                request.Preferences.DrivingLicenses,
                request.Preferences.Availability,
                request.Preferences.Employers,
                request.Preferences.Educations,
                request.Preferences.HomeAddress,
                request.Preferences.MinHoursPerWeek,
                request.Preferences.MaxHoursPerWeek,
                request.Preferences.FlexibleTimes,
                request.Preferences.Certificates,
                request.Preferences.ShowAddressOnCv);
        }

        if (request.References is not null)
        {
            var replaceError = await ReplaceReferencesAsync(user.Id, request.References, cancellationToken);
            if (replaceError is not null)
            {
                return BadRequest(new { message = replaceError });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpPut("date-of-birth")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<MeProfileDto>> UpdateDateOfBirth(
        [FromBody] UpdateDateOfBirthRequest request,
        CancellationToken cancellationToken)
    {
        return await UpdateProfile(
            new UpdateCandidateProfileRequest(
                OpenForWork: null,
                DateOfBirth: request.DateOfBirth,
                Preferences: null),
            cancellationToken);
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetMyApplications(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        // Only verified (actually submitted) applications — drafts awaiting a verification code stay out of Sollicitaties.
        var rows = await _db.Applications.AsNoTracking()
            .Where(a => a.CandidateUserId == user.Id && a.EmailVerifiedAt != null)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.VacancyId,
                Title = a.Vacancy.Title,
                CompanyName = a.Vacancy.Company.Name,
                CompanyAddress = a.Vacancy.Company.Address,
                IntermediaryName = a.Vacancy.IntermediaryCompany != null ? a.Vacancy.IntermediaryCompany.Name : null,
                IntermediaryAddress = a.Vacancy.IntermediaryCompany != null ? a.Vacancy.IntermediaryCompany.Address : null,
                a.Vacancy.ShowClientAddressOnMap,
                HasIntermediary = a.Vacancy.IntermediaryCompanyId != null,
                a.CandidateName,
                a.CandidateEmail,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.CreatedAt,
                Status = a.Status.ToString(),
                a.RespondedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var (companyName, location) = CandidateApplicationLocation.ForPublicCard(
                row.HasIntermediary,
                row.ShowClientAddressOnMap,
                row.CompanyName,
                row.CompanyAddress,
                row.IntermediaryName,
                row.IntermediaryAddress);
            return new ApplicationDto(
                row.Id,
                row.VacancyId,
                row.Title,
                companyName,
                row.CandidateName,
                row.CandidateEmail,
                row.PreferredTransport,
                row.EstimatedTravelMinutes,
                row.CreatedAt,
                row.Status,
                row.RespondedAt,
                location);
        }).ToList();

        var lang = await ResolveTargetLanguageAsync(user, cancellationToken);
        if (!JobsyLanguages.AreSame(VacancySourceLanguage, lang))
        {
            for (var i = 0; i < items.Count; i++)
            {
                var translated = await _translation.TranslateVacancyAsync(
                    items[i].VacancyTitle,
                    string.Empty,
                    VacancySourceLanguage,
                    lang,
                    cancellationToken);
                items[i] = items[i] with { VacancyTitle = translated.Title };
            }
        }

        return Ok(items);
    }

    [HttpPost("candidate-how-to-completed")]
    public async Task<IActionResult> CompleteCandidateHowTo(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (user.CandidateHowToCompletedAt is null)
        {
            user.CandidateHowToCompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("likes")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<IEnumerable<CandidateVacancyEngagementDto>>> GetMyLikes(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var items = await _db.VacancyLikes.AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new CandidateVacancyEngagementDto(
                l.Id,
                l.VacancyId,
                l.Vacancy.Title,
                l.Vacancy.Company.Name,
                l.CreatedAt,
                null,
                l.Vacancy.ImageUrl,
                l.Vacancy.Company.LogoUrl))
            .ToListAsync(cancellationToken);

        return Ok(await TranslateEngagementTitlesAsync(items, user, cancellationToken));
    }

    [HttpGet("shares")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<IEnumerable<CandidateVacancyEngagementDto>>> GetMyShares(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var items = await _db.VacancyShares.AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new CandidateVacancyEngagementDto(
                s.Id,
                s.VacancyId,
                s.Vacancy.Title,
                s.Vacancy.Company.Name,
                s.CreatedAt,
                s.Channel.ToString(),
                s.Vacancy.ImageUrl,
                s.Vacancy.Company.LogoUrl))
            .ToListAsync(cancellationToken);

        return Ok(await TranslateEngagementTitlesAsync(items, user, cancellationToken));
    }

    private async Task<List<CandidateVacancyEngagementDto>> TranslateEngagementTitlesAsync(
        List<CandidateVacancyEngagementDto> items,
        Core.Entities.User user,
        CancellationToken cancellationToken)
    {
        var lang = await ResolveTargetLanguageAsync(user, cancellationToken);
        if (JobsyLanguages.AreSame(VacancySourceLanguage, lang))
        {
            return items;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var translated = await _translation.TranslateVacancyAsync(
                items[i].VacancyTitle,
                string.Empty,
                VacancySourceLanguage,
                lang,
                cancellationToken);
            items[i] = items[i] with { VacancyTitle = translated.Title };
        }

        return items;
    }

    private Task<string> ResolveTargetLanguageAsync(Core.Entities.User user, CancellationToken cancellationToken)
    {
        if (Request.Query.TryGetValue("lang", out var langQuery) && JobsyLanguages.IsSupported(langQuery.ToString()))
        {
            return Task.FromResult(JobsyLanguages.Normalize(langQuery.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Jobsy-Language", out var langHeader)
            && JobsyLanguages.IsSupported(langHeader.ToString()))
        {
            return Task.FromResult(JobsyLanguages.Normalize(langHeader.ToString()));
        }

        var preferred = ParsePreferences(user.PreferencesJson).Language;
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return Task.FromResult(JobsyLanguages.Normalize(preferred));
        }

        return Task.FromResult(JobsyLanguages.Default);
    }

    /// <summary>
    /// Live Lobsy-CV PDF from the signed-in candidate profile (always allowed for the owner).
    /// Employers never use this endpoint — they use application snapshot PDF after Accept.
    /// </summary>
    [HttpGet("lobsy-cv.pdf")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadMyLobsyCv(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var hasUploadedCv = await _db.CandidateUploadedCvs.AsNoTracking()
            .AnyAsync(c => c.UserId == user.Id, cancellationToken);
        var preferences = ParsePreferences(user.PreferencesJson);
        var model = LobsyCvModelFactory.FromLiveProfile(
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.WhatsAppContactAllowed,
            preferences,
            user.HomeLocation?.Latitude,
            user.HomeLocation?.Longitude,
            DateTime.UtcNow,
            user.ConsentVersion ?? PrivacyConstants.CurrentConsentVersion,
            dateOfBirth: user.DateOfBirth,
            hasUploadedOwnCv: hasUploadedCv);

        var pdf = await _lobsyCvPdf.RenderAsync(model, cancellationToken);
        var fileName = _lobsyCvPdf.BuildFileName(model);
        return File(pdf, "application/pdf", fileName);
    }

    [HttpPost("cv")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("ai")]
    [RequestSizeLimit(CandidateCvFileRules.MaxBytes + 64_000)]
    public async Task<ActionResult<MeProfileDto>> UploadMyCv(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Kies een CV-bestand (PDF of Word)." });
        }

        if (!CandidateCvFileRules.TryNormalize(
                file.FileName,
                file.ContentType,
                checked((int)file.Length),
                out var safeName,
                out var contentType,
                out var fileError))
        {
            return BadRequest(new { message = fileError });
        }

        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var existing = await _db.CandidateUploadedCvs.FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        if (existing is null)
        {
            existing = new Core.Entities.CandidateUploadedCv
            {
                Id = Guid.NewGuid(),
                UserId = user.Id
            };
            _db.CandidateUploadedCvs.Add(existing);
        }

        existing.FileName = safeName;
        existing.ContentType = contentType;
        existing.Content = bytes;
        existing.SizeBytes = bytes.Length;
        existing.UploadedAtUtc = DateTime.UtcNow;
        existing.ExtractedAtUtc = null;
        existing.FilledFieldsJson = null;

        var text = _cvText.Extract(bytes, contentType, safeName);
        var extracted = await _cvExtraction.ExtractAsync(text, cancellationToken);
        var prefs = ParsePreferences(user.PreferencesJson);
        var merged = CvProfileMerge.Apply(user.FirstName, user.LastName, user.PhoneNumber, prefs, extracted);
        if (merged.FilledFields.Count > 0)
        {
            user.FirstName = merged.FirstName;
            user.LastName = merged.LastName;
            var composed = CandidateNameRules.ComposeFullName(user.FirstName, user.LastName, user.FullName);
            if (!string.IsNullOrWhiteSpace(composed))
            {
                user.FullName = composed.Length > 256 ? composed[..256] : composed;
            }

            if (!string.IsNullOrWhiteSpace(merged.PhoneNumber) && CandidatePhoneRules.IsValid(merged.PhoneNumber))
            {
                user.PhoneNumber = merged.PhoneNumber;
            }

            user.PreferencesJson = SerializePreferences(
                merged.Preferences.Roles,
                merged.Preferences.MaxTravelMinutes,
                merged.Preferences.PreferredTransport,
                merged.Preferences.Language,
                merged.Preferences.AgeYears,
                merged.Preferences.AboutMe,
                merged.Preferences.DefaultMotivation,
                merged.Preferences.DrivingLicenses,
                merged.Preferences.Availability,
                merged.Preferences.Employers,
                merged.Preferences.Educations,
                merged.Preferences.HomeAddress,
                merged.Preferences.MinHoursPerWeek,
                merged.Preferences.MaxHoursPerWeek,
                merged.Preferences.FlexibleTimes,
                merged.Preferences.Certificates,
                merged.Preferences.ShowAddressOnCv);
            existing.ExtractedAtUtc = DateTime.UtcNow;
            existing.FilledFieldsJson = JsonSerializer.Serialize(merged.FilledFields, JsonOptions);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpGet("cv")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadMyCv(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var cv = await _db.CandidateUploadedCvs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == lookup.Id, cancellationToken);
        if (cv is null)
        {
            return NotFound(new { message = "Er is nog geen eigen CV geüpload." });
        }

        return File(cv.Content, cv.ContentType, cv.FileName);
    }

    [HttpDelete("cv")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<MeProfileDto>> DeleteMyCv(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var existing = await _db.CandidateUploadedCvs.FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);
        if (existing is not null)
        {
            _db.CandidateUploadedCvs.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    /// <summary>
    /// Stamp the current privacy/terms consent version on the signed-in account.
    /// Client-supplied versions are ignored (AVG integrity).
    /// </summary>
    [HttpPost("accept-consent")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MeProfileDto>> AcceptConsent(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == lookup.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        user.TermsAcceptedAt = DateTime.UtcNow;
        user.ConsentVersion = PrivacyConstants.CurrentConsentVersion;
        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    private async Task<MeProfileDto> BuildProfileDtoAsync(
        Core.Entities.User user,
        bool authenticatorEnabled,
        CancellationToken cancellationToken)
    {
        var cvRow = await _db.CandidateUploadedCvs.AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .Select(c => new
            {
                c.FileName,
                c.ContentType,
                c.SizeBytes,
                c.UploadedAtUtc,
                c.ExtractedAtUtc,
                c.FilledFieldsJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        CandidateUploadedCvInfoDto? cv = null;
        if (cvRow is not null)
        {
            IReadOnlyList<string>? filled = null;
            if (!string.IsNullOrWhiteSpace(cvRow.FilledFieldsJson))
            {
                try
                {
                    filled = JsonSerializer.Deserialize<List<string>>(cvRow.FilledFieldsJson, JsonOptions);
                }
                catch (JsonException)
                {
                    filled = null;
                }
            }

            cv = new CandidateUploadedCvInfoDto(
                cvRow.FileName,
                cvRow.ContentType,
                cvRow.SizeBytes,
                cvRow.UploadedAtUtc,
                cvRow.ExtractedAtUtc,
                filled);
        }

        var references = await _db.CandidateReferences.AsNoTracking()
            .Where(r => r.UserId == user.Id)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAtUtc)
            .Select(r => new CandidateReferenceDto(r.Id, r.EmployerName, r.ContactName, r.Email, r.Phone))
            .ToListAsync(cancellationToken);

        return new MeProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.DateOfBirth,
            user.DateOfBirth.HasValue,
            user.OpenForWork,
            ParsePreferences(user.PreferencesJson),
            authenticatorEnabled,
            user.HomeLocation?.Latitude,
            user.HomeLocation?.Longitude,
            user.ConsentVersion,
            PrivacyConstants.RequiresAccountConsentReaccept(user.Role, user.ConsentVersion),
            PrivacyConstants.CurrentConsentVersion,
            CandidateNameRules.DisplayFirstName(user.FirstName, user.FullName),
            CandidateNameRules.DisplayLastName(user.LastName, user.FullName),
            user.PhoneNumber,
            user.WhatsAppContactAllowed,
            cv,
            references);
    }

    private async Task<string?> ReplaceReferencesAsync(
        Guid userId,
        IReadOnlyList<CandidateReferenceDto> incoming,
        CancellationToken cancellationToken)
    {
        var rows = incoming
            .Select(r => (
                Employer: CandidateReferenceRules.NormalizeName(r.EmployerName),
                Contact: CandidateReferenceRules.NormalizeName(r.ContactName),
                Email: CandidateReferenceRules.NormalizeEmail(r.Email),
                Phone: CandidatePhoneRules.Normalize(r.Phone)))
            .Where(r => !string.IsNullOrWhiteSpace(r.Employer)
                        || !string.IsNullOrWhiteSpace(r.Contact)
                        || !string.IsNullOrWhiteSpace(r.Email)
                        || !string.IsNullOrWhiteSpace(r.Phone))
            .ToList();

        if (rows.Count > CandidateReferenceRules.MaxPerCandidate)
        {
            return $"Je kunt maximaal {CandidateReferenceRules.MaxPerCandidate} recensies toevoegen.";
        }

        foreach (var row in rows)
        {
            var error = CandidateReferenceRules.ValidateEntry(row.Employer, row.Contact, row.Email, row.Phone);
            if (error is not null)
            {
                return error;
            }
        }

        var existing = await _db.CandidateReferences.Where(r => r.UserId == userId).ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.CandidateReferences.RemoveRange(existing);
        }

        var order = 0;
        foreach (var row in rows)
        {
            _db.CandidateReferences.Add(new Core.Entities.CandidateReference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmployerName = row.Employer!,
                ContactName = row.Contact!,
                Email = row.Email!,
                Phone = row.Phone!,
                SortOrder = order++,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return null;
    }

    public static CandidatePreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EmptyPreferences();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var roles = new List<string>();
            if (root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in rolesEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            roles.Add(value);
                        }
                    }
                }
            }

            int? maxTravel = null;
            if (root.TryGetProperty("maxTravelMinutes", out var travelEl)
                && travelEl.ValueKind == JsonValueKind.Number
                && travelEl.TryGetInt32(out var travel))
            {
                maxTravel = travel;
            }

            string? transport = null;
            if (root.TryGetProperty("preferredTransport", out var transportEl)
                && transportEl.ValueKind == JsonValueKind.String)
            {
                transport = transportEl.GetString();
            }

            string? language = null;
            if (root.TryGetProperty("language", out var languageEl)
                && languageEl.ValueKind == JsonValueKind.String)
            {
                var raw = languageEl.GetString();
                if (!string.IsNullOrWhiteSpace(raw) && JobsyLanguages.IsSupported(raw))
                {
                    language = JobsyLanguages.Normalize(raw);
                }
            }

            int? ageYears = null;
            if (root.TryGetProperty("ageYears", out var ageEl)
                && ageEl.ValueKind == JsonValueKind.Number
                && ageEl.TryGetInt32(out var age)
                && age is >= 15 and <= 67)
            {
                ageYears = age;
            }

            string? aboutMe = null;
            if (root.TryGetProperty("aboutMe", out var aboutEl) && aboutEl.ValueKind == JsonValueKind.String)
            {
                aboutMe = aboutEl.GetString();
            }

            string? defaultMotivation = null;
            if (root.TryGetProperty("defaultMotivation", out var motivEl) && motivEl.ValueKind == JsonValueKind.String)
            {
                defaultMotivation = motivEl.GetString();
                if (defaultMotivation is { Length: > 500 })
                {
                    defaultMotivation = defaultMotivation[..500];
                }
            }

            var drivingLicenses = new List<string>();
            if (root.TryGetProperty("drivingLicenses", out var drivingEl) && drivingEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in drivingEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            drivingLicenses.Add(value);
                        }
                    }
                }
            }

            var availability = new Dictionary<string, string[]>();
            if (root.TryGetProperty("availability", out var availabilityEl) && availabilityEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var day in availabilityEl.EnumerateObject())
                {
                    if (day.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var slots = day.Value.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (slots.Length > 0)
                    {
                        availability[day.Name] = slots;
                    }
                }
            }

            var employers = new List<CandidateEmployerHistoryDto>();
            if (root.TryGetProperty("employers", out var employersEl) && employersEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in employersEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var name = item.TryGetProperty("employerName", out var employerNameEl)
                               && employerNameEl.ValueKind == JsonValueKind.String
                        ? employerNameEl.GetString()
                        : null;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var role = item.TryGetProperty("role", out var roleEl)
                               && roleEl.ValueKind == JsonValueKind.String
                        ? roleEl.GetString()
                        : null;
                    int? years = null;
                    if (item.TryGetProperty("years", out var yearsEl)
                        && yearsEl.ValueKind == JsonValueKind.Number
                        && yearsEl.TryGetInt32(out var yearsVal)
                        && yearsVal is >= 0 and <= 80)
                    {
                        years = yearsVal;
                    }

                    string? description = null;
                    if (item.TryGetProperty("description", out var descriptionEl)
                        && descriptionEl.ValueKind == JsonValueKind.String)
                    {
                        description = descriptionEl.GetString()?.Trim();
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            description = null;
                        }
                        else if (description.Length > 1000)
                        {
                            description = description[..1000];
                        }
                    }

                    var startMonth = ReadEmployerMonth(item, "startMonth", "startDate");
                    var endMonth = ReadEmployerMonth(item, "endMonth", "endDate");
                    if (startMonth is not null
                        && endMonth is not null
                        && string.CompareOrdinal(endMonth, startMonth) < 0)
                    {
                        endMonth = null;
                    }

                    employers.Add(new CandidateEmployerHistoryDto(
                        name.Trim(),
                        string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
                        years,
                        description,
                        startMonth,
                        endMonth));
                }
            }

            var educations = new List<string>();
            if (root.TryGetProperty("educations", out var educationsEl) && educationsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in educationsEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            educations.Add(value);
                        }
                    }
                }
            }

            string? homeAddress = null;
            if (root.TryGetProperty("homeAddress", out var homeAddressEl)
                && homeAddressEl.ValueKind == JsonValueKind.String)
            {
                homeAddress = homeAddressEl.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(homeAddress))
                {
                    homeAddress = null;
                }
            }

            decimal? minHours = null;
            if (root.TryGetProperty("minHoursPerWeek", out var minHoursEl)
                && minHoursEl.ValueKind == JsonValueKind.Number
                && minHoursEl.TryGetDecimal(out var minHoursVal))
            {
                minHours = minHoursVal;
            }

            decimal? maxHours = null;
            if (root.TryGetProperty("maxHoursPerWeek", out var maxHoursEl)
                && maxHoursEl.ValueKind == JsonValueKind.Number
                && maxHoursEl.TryGetDecimal(out var maxHoursVal))
            {
                maxHours = maxHoursVal;
            }

            bool? flexibleTimes = null;
            if (root.TryGetProperty("flexibleTimes", out var flexibleEl)
                && (flexibleEl.ValueKind is JsonValueKind.True or JsonValueKind.False))
            {
                flexibleTimes = flexibleEl.GetBoolean();
            }

            var certificates = new List<CandidateCertificateDto>();
            if (root.TryGetProperty("certificates", out var certificatesEl) && certificatesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in certificatesEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var name = item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                        ? nameEl.GetString()?.Trim()
                        : null;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (name.Length > 200)
                    {
                        name = name[..200];
                    }

                    int? year = null;
                    if (item.TryGetProperty("year", out var yearEl)
                        && yearEl.ValueKind == JsonValueKind.Number
                        && yearEl.TryGetInt32(out var y)
                        && y is >= 1950 and <= 2100)
                    {
                        year = y;
                    }

                    certificates.Add(new CandidateCertificateDto(name, year));
                }
            }

            bool? showAddressOnCv = null;
            if (root.TryGetProperty("showAddressOnCv", out var showAddrEl)
                && (showAddrEl.ValueKind is JsonValueKind.True or JsonValueKind.False))
            {
                showAddressOnCv = showAddrEl.GetBoolean();
            }

            return new CandidatePreferencesDto(
                roles,
                maxTravel,
                transport,
                language,
                ageYears,
                aboutMe,
                defaultMotivation,
                drivingLicenses.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                availability,
                employers,
                educations.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                homeAddress,
                minHours,
                maxHours,
                flexibleTimes,
                certificates,
                showAddressOnCv);
        }
        catch (Exception)
        {
            return EmptyPreferences();
        }
    }

    private static string? ReadEmployerMonth(JsonElement item, string primaryName, string alternateName)
    {
        if (item.TryGetProperty(primaryName, out var primary) && primary.ValueKind == JsonValueKind.String)
        {
            var normalized = LobsyCvModelFactory.NormalizeMonth(primary.GetString());
            if (normalized is not null)
            {
                return normalized;
            }
        }

        if (item.TryGetProperty(alternateName, out var alternate) && alternate.ValueKind == JsonValueKind.String)
        {
            return LobsyCvModelFactory.NormalizeMonth(alternate.GetString());
        }

        return null;
    }

    private static string? NormalizeEmployerEndMonth(string? startMonth, string? endMonth)
    {
        var start = LobsyCvModelFactory.NormalizeMonth(startMonth);
        var end = LobsyCvModelFactory.NormalizeMonth(endMonth);
        if (end is null)
        {
            return null;
        }

        if (start is not null && string.CompareOrdinal(end, start) < 0)
        {
            return null;
        }

        return end;
    }

    private static CandidatePreferencesDto EmptyPreferences() => new(
        [],
        null,
        null,
        null,
        null,
        null,
        null,
        [],
        new Dictionary<string, string[]>(),
        [],
        [],
        null,
        null,
        null,
        null,
        [],
        null);

    public static string SerializePreferences(
        IEnumerable<string> roles,
        int? maxTravelMinutes,
        string? preferredTransport,
        string? language,
        int? ageYears = null,
        string? aboutMe = null,
        string? defaultMotivation = null,
        IEnumerable<string>? drivingLicenses = null,
        IReadOnlyDictionary<string, string[]>? availability = null,
        IEnumerable<CandidateEmployerHistoryDto>? employers = null,
        IEnumerable<string>? educations = null,
        string? homeAddress = null,
        decimal? minHoursPerWeek = null,
        decimal? maxHoursPerWeek = null,
        bool? flexibleTimes = null,
        IEnumerable<CandidateCertificateDto>? certificates = null,
        bool? showAddressOnCv = null)
    {
        var trimmedHome = string.IsNullOrWhiteSpace(homeAddress) ? null : homeAddress.Trim();
        if (trimmedHome is { Length: > 256 })
        {
            trimmedHome = trimmedHome[..256];
        }

        var trimmedMotivation = string.IsNullOrWhiteSpace(defaultMotivation) ? null : defaultMotivation.Trim();
        if (trimmedMotivation is { Length: > 500 })
        {
            trimmedMotivation = trimmedMotivation[..500];
        }

        return JsonSerializer.Serialize(new
        {
            roles,
            maxTravelMinutes,
            preferredTransport,
            language = string.IsNullOrWhiteSpace(language)
                ? null
                : JobsyLanguages.Normalize(language),
            ageYears,
            aboutMe = string.IsNullOrWhiteSpace(aboutMe) ? null : aboutMe.Trim(),
            defaultMotivation = trimmedMotivation,
            drivingLicenses = drivingLicenses?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            availability,
            employers = employers?
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
                .Select(e =>
                {
                    var description = string.IsNullOrWhiteSpace(e.Description) ? null : e.Description.Trim();
                    if (description is { Length: > 1000 })
                    {
                        description = description[..1000];
                    }

                    return new
                    {
                        employerName = e.EmployerName.Trim(),
                        role = string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim(),
                        years = e.Years is >= 0 and <= 80 ? e.Years : null,
                        description,
                        startMonth = LobsyCvModelFactory.NormalizeMonth(e.StartMonth),
                        endMonth = NormalizeEmployerEndMonth(e.StartMonth, e.EndMonth)
                    };
                })
                .ToArray(),
            educations = educations?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            homeAddress = trimmedHome,
            minHoursPerWeek,
            maxHoursPerWeek,
            flexibleTimes,
            certificates = certificates?
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .Select(c =>
                {
                    var name = c.Name.Trim();
                    if (name.Length > 200)
                    {
                        name = name[..200];
                    }

                    return new
                    {
                        name,
                        year = c.Year is >= 1950 and <= 2100 ? c.Year : null
                    };
                })
                .Take(30)
                .ToArray(),
            showAddressOnCv
        }, JsonOptions);
    }
}
