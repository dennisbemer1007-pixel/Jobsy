using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public MeController(
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        JobsyDbContext db,
        IPlatformFeatureService features)
    {
        _companyAuth = companyAuth;
        _users = users;
        _db = db;
        _features = features;
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
            return Ok(ToProfileDto(user, features.AuthenticatorEnabled));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Profiel kon niet worden geladen.", detail = ex.Message });
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
            existing.DrivingLicenses,
            existing.Availability,
            existing.Employers,
            existing.Educations,
            existing.HomeAddress);

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(ToProfileDto(user, features.AuthenticatorEnabled));
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
                request.Preferences.DrivingLicenses,
                request.Preferences.Availability,
                request.Preferences.Employers,
                request.Preferences.Educations,
                request.Preferences.HomeAddress);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(ToProfileDto(user, features.AuthenticatorEnabled));
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

        var items = await _db.Applications.AsNoTracking()
            .Where(a => a.CandidateUserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationDto(
                a.Id,
                a.VacancyId,
                a.Vacancy.Title,
                a.Vacancy.Company.Name,
                a.CandidateName,
                a.CandidateEmail,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.CreatedAt,
                a.EmailVerifiedAt == null ? "PendingVerification" : a.Status.ToString(),
                a.RespondedAt))
            .ToListAsync(cancellationToken);

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
                l.Vacancy.ImageUrl))
            .ToListAsync(cancellationToken);

        return Ok(items);
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
                s.Vacancy.ImageUrl))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    private MeProfileDto ToProfileDto(Core.Entities.User user, bool authenticatorEnabled) => new(
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
        user.HomeLocation?.Longitude);

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

                    employers.Add(new CandidateEmployerHistoryDto(
                        name.Trim(),
                        string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
                        years,
                        description));
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

            return new CandidatePreferencesDto(
                roles,
                maxTravel,
                transport,
                language,
                ageYears,
                aboutMe,
                drivingLicenses.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                availability,
                employers,
                educations.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                homeAddress);
        }
        catch (Exception)
        {
            return EmptyPreferences();
        }
    }

    private static CandidatePreferencesDto EmptyPreferences() => new(
        [],
        null,
        null,
        null,
        null,
        null,
        [],
        new Dictionary<string, string[]>(),
        [],
        [],
        null);

    public static string SerializePreferences(
        IEnumerable<string> roles,
        int? maxTravelMinutes,
        string? preferredTransport,
        string? language,
        int? ageYears = null,
        string? aboutMe = null,
        IEnumerable<string>? drivingLicenses = null,
        IReadOnlyDictionary<string, string[]>? availability = null,
        IEnumerable<CandidateEmployerHistoryDto>? employers = null,
        IEnumerable<string>? educations = null,
        string? homeAddress = null)
    {
        var trimmedHome = string.IsNullOrWhiteSpace(homeAddress) ? null : homeAddress.Trim();
        if (trimmedHome is { Length: > 256 })
        {
            trimmedHome = trimmedHome[..256];
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
                        description
                    };
                })
                .ToArray(),
            educations = educations?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            homeAddress = trimmedHome
        }, JsonOptions);
    }
}
