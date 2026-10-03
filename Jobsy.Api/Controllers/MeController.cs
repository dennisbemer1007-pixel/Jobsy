using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Media;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public partial class MeController : ControllerBase
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
    private readonly ICandidateInsightsQueue _insightsQueue;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly ITransactionalMailer _mailer;
    private const string VacancySourceLanguage = "nl";

    public MeController(
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        JobsyDbContext db,
        IPlatformFeatureService features,
        ITranslationService translation,
        ILobsyCvPdfService lobsyCvPdf,
        ICvTextExtractor cvText,
        ICvExtractionService cvExtraction,
        ICandidateInsightsQueue insightsQueue,
        ICandidateMatchSnapshotService matchSnapshots,
        ITransactionalMailer mailer)
    {
        _companyAuth = companyAuth;
        _users = users;
        _db = db;
        _features = features;
        _translation = translation;
        _lobsyCvPdf = lobsyCvPdf;
        _cvText = cvText;
        _cvExtraction = cvExtraction;
        _insightsQueue = insightsQueue;
        _matchSnapshots = matchSnapshots;
        _mailer = mailer;
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
        user.PreferencesJson = SerializePreferences(existing with { Language = language });

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
            if (request.DateOfBirth > today.AddYears(-CandidateConsentRules.MinimumCandidateAge)
                || request.DateOfBirth < today.AddYears(-100))
            {
                return BadRequest(new { message = "Je geboortedatum moet passen bij een leeftijd van 13 tot 100 jaar." });
            }

            user.DateOfBirth = request.DateOfBirth;
        }
        else if (user.DateOfBirth is null)
        {
            return BadRequest(new { message = "Vul eerst je geboortedatum in. Die hebben we nodig voor veilige leeftijdschecks." });
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

            ContactVerification.ApplyPhone(user, phone);
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

            var merged = new CandidatePreferencesDto(
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
                request.Preferences.ShowAddressOnCv,
                request.Preferences.NoWorkExperience ?? existing.NoWorkExperience,
                request.Preferences.EducationDirection ?? existing.EducationDirection,
                request.Preferences.AvailabilityPresets ?? existing.AvailabilityPresets,
                request.Preferences.AvailabilityPresetsOverridden ?? existing.AvailabilityPresetsOverridden,
                request.Preferences.SpokenLanguages ?? existing.SpokenLanguages,
                request.Preferences.DutchLevel ?? existing.DutchLevel,
                request.Preferences.EmployerPreferences ?? existing.EmployerPreferences,
                request.Preferences.LearningGoals ?? existing.LearningGoals,
                request.Preferences.Hobbies ?? existing.Hobbies,
                ShareablePreferenceNormalizer.NormalizeWork(request.Preferences.WorkPreferences, existing.WorkPreferences),
                request.Preferences.ShareEmployerPreferences ?? existing.ShareEmployerPreferences,
                ShareablePreferenceNormalizer.NormalizeRegion(request.Preferences.WorkRegion, existing.WorkRegion),
                request.Preferences.HasOwnCar ?? existing.HasOwnCar,
                ShareablePreferenceNormalizer.NormalizeContracts(request.Preferences.ContractPreferences, existing.ContractPreferences));

            user.PreferencesJson = SerializePreferences(merged);
        }

        if (request.AvailableFromDate.HasValue
            || request.ClearAvailableFromDate)
        {
            if (request.ClearAvailableFromDate)
            {
                user.AvailableFromDate = null;
            }
            else if (request.AvailableFromDate is DateOnly availableFrom)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (availableFrom < today.AddYears(-1) || availableFrom > today.AddYears(5))
                {
                    return BadRequest(new { message = "Ongeldige beschikbaar-vanaf datum." });
                }

                // Today or past → treat as Direct (null).
                user.AvailableFromDate = availableFrom <= today ? null : availableFrom;
            }
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
        await _matchSnapshots.MarkInputsStaleAsync(user.Id, cancellationToken);
        _insightsQueue.TryEnqueue(user.Id);
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
                CompanyLogoUrl = a.Vacancy.Company.LogoUrl,
                IntermediaryName = a.Vacancy.IntermediaryCompany != null ? a.Vacancy.IntermediaryCompany.Name : null,
                IntermediaryAddress = a.Vacancy.IntermediaryCompany != null ? a.Vacancy.IntermediaryCompany.Address : null,
                IntermediaryLogoUrl = a.Vacancy.IntermediaryCompany != null ? a.Vacancy.IntermediaryCompany.LogoUrl : null,
                a.Vacancy.ShowClientAddressOnMap,
                HasIntermediary = a.Vacancy.IntermediaryCompanyId != null,
                ImageUrl = a.Vacancy.ImageUrl,
                WorkTypes = a.Vacancy.WorkTypes,
                WorkTypeLabels = a.Vacancy.WorkTypeLabels,
                CategoryId = a.Vacancy.CategoryId,
                a.CandidateName,
                a.CandidateEmail,
                a.PreferredTransport,
                a.EstimatedTravelMinutes,
                a.CreatedAt,
                Status = a.Status,
                a.RespondedAt
            })
            .ToListAsync(cancellationToken);

        var appIds = rows.Select(r => r.Id).ToList();
        var historyByApp = await _db.ApplicationStatusHistories.AsNoTracking()
            .Where(h => appIds.Contains(h.ApplicationId))
            .OrderBy(h => h.OccurredAtUtc)
            .ToListAsync(cancellationToken);
        var historyLookup = historyByApp.ToLookup(h => h.ApplicationId);

        var items = rows.Select(row =>
        {
            var (companyName, location) = CandidateApplicationLocation.ForPublicCard(
                row.HasIntermediary,
                row.ShowClientAddressOnMap,
                row.CompanyName,
                row.CompanyAddress,
                row.IntermediaryName,
                row.IntermediaryAddress);
            // Same company whose name is shown → same logo (intermediary when masked).
            var logo = row.HasIntermediary && !row.ShowClientAddressOnMap
                ? row.IntermediaryLogoUrl
                : row.CompanyLogoUrl;
            var workType = WorkTypeLabels.ResolveLabels(row.WorkTypes, row.WorkTypeLabels).FirstOrDefault();
            var pictureUrl = VacancyImageUrls.ForCard(row.ImageUrl, logo, row.VacancyId, workType);
            var pictureKind = VacancyImageUrls.ForCardKind(pictureUrl, logo);
            var history = historyLookup[row.Id].ToList();
            var timeline = ApplicationTimelineBuilder.Build(
                row.CreatedAt,
                row.Status,
                row.RespondedAt,
                history);
            var timelineDto = timeline.Steps.Select(s => new ApplicationTimelineStepDto(
                s.Key.ToString(),
                s.State.ToString(),
                s.OccurredAtUtc,
                s.LabelKey)).ToList();
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
                row.Status.ToString(),
                row.RespondedAt,
                location,
                pictureUrl,
                pictureKind,
                timelineDto,
                ApplicationTimelineBuilder.NextStepKey(row.Status),
                timeline.LegacyNoHistory);
        }).ToList();

        var lang = await ResolveTargetLanguageAsync(user, cancellationToken);
        if (!JobsyLanguages.AreSame(VacancySourceLanguage, lang) && items.Count > 0)
        {
            var batch = await _translation.TranslateVacanciesBatchAsync(
                items.Select(i => new VacancyTranslationRequest(i.VacancyId, i.VacancyTitle, string.Empty)).ToList(),
                VacancySourceLanguage,
                lang,
                cancellationToken);
            for (var i = 0; i < items.Count; i++)
            {
                items[i] = items[i] with { VacancyTitle = batch[i].Title };
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

    /// <summary>
    /// Per-stone done state for <c>/candidate/hoe-werkt-lobsy</c> (05 §2). Read-only:
    /// every query is <c>AsNoTracking</c> and nothing is saved.
    /// </summary>
    [HttpGet("journey-summary")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<CandidateJourneySummaryDto>> GetJourneySummary(
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var userId = lookup.Id;

        var discoveryDone = await _db.CandidateOnboardings.AsNoTracking()
            .AnyAsync(o => o.UserId == userId && (o.CompletedAtUtc != null || o.FinishReached), cancellationToken);

        var preferencesJson = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);

        var passportDone = HasAnyProof(preferencesJson)
            || await _db.CandidateReferences.AsNoTracking()
                .AnyAsync(r => r.UserId == userId, cancellationToken)
            || await _db.CandidateUploadedCvs.AsNoTracking()
                .AnyAsync(c => c.UserId == userId, cancellationToken);

        var careerDone = await _db.CandidateCareerStepProgress.AsNoTracking()
            .AnyAsync(
                p => p.UserId == userId
                     && p.CompletedAtUtc != null
                     && p.Plan.Status == Jobsy.Core.Entities.CareerPlanStatuses.Active,
                cancellationToken);

        var jobMapDone = await _db.VacancyLikes.AsNoTracking()
                .AnyAsync(l => l.UserId == userId, cancellationToken)
            || await _db.VacancyShares.AsNoTracking()
                .AnyAsync(s => s.UserId == userId, cancellationToken);

        var applicationsDone = await _db.Applications.AsNoTracking()
            .AnyAsync(a => a.CandidateUserId == userId, cancellationToken);

        return Ok(new CandidateJourneySummaryDto(
            discoveryDone,
            passportDone,
            careerDone,
            jobMapDone,
            applicationsDone));
    }

    /// <summary>At least one paspoort proof: employer, education, certificate or own CV.</summary>
    private static bool HasAnyProof(string? preferencesJson)
    {
        var preferences = ParsePreferences(preferencesJson);
        return (preferences.Employers?.Any(e => !string.IsNullOrWhiteSpace(e.EmployerName)) ?? false)
               || (preferences.Certificates?.Any(c => !string.IsNullOrWhiteSpace(c.Name)) ?? false)
               || (preferences.Educations?.Any(e =>
                   !string.IsNullOrWhiteSpace(e)
                   && !string.Equals(e, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase)) ?? false);
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

        var likeRows = await _db.VacancyLikes.AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.VacancyId,
                Title = l.Vacancy.Title,
                CompanyName = l.Vacancy.Company.Name,
                CompanyAddress = l.Vacancy.Company.Address,
                l.CreatedAt,
                ImageUrl = l.Vacancy.ImageUrl,
                LogoUrl = l.Vacancy.Company.LogoUrl,
                Status = l.Vacancy.Status,
                EndDate = l.Vacancy.EndDate,
                ClosedAtUtc = l.Vacancy.ClosedAtUtc,
                MinHours = l.Vacancy.MinHoursPerWeek,
                MaxHours = l.Vacancy.MaxHoursPerWeek,
                CategoryId = l.Vacancy.CategoryId,
                WorkTypes = l.Vacancy.WorkTypes,
                WorkTypeLabels = l.Vacancy.WorkTypeLabels,
                IntermediaryName = l.Vacancy.IntermediaryCompany != null ? l.Vacancy.IntermediaryCompany.Name : null,
                IntermediaryAddress = l.Vacancy.IntermediaryCompany != null ? l.Vacancy.IntermediaryCompany.Address : null,
                ShowClient = l.Vacancy.ShowClientAddressOnMap,
                HasIntermediary = l.Vacancy.IntermediaryCompanyId != null
            })
            .ToListAsync(cancellationToken);

        var likedVacancyIds = likeRows.Select(l => l.VacancyId).ToList();
        var applied = await _db.Applications.AsNoTracking()
            .Where(a => a.CandidateUserId == user.Id
                        && a.EmailVerifiedAt != null
                        && likedVacancyIds.Contains(a.VacancyId)
                        && a.Status != ApplicationStatus.Withdrawn)
            .Select(a => new { a.VacancyId, a.Id })
            .ToListAsync(cancellationToken);
        var appliedByVacancy = applied
            .GroupBy(a => a.VacancyId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var now = DateTime.UtcNow;
        var items = likeRows.Select(l =>
        {
            var (companyName, location) = CandidateApplicationLocation.ForPublicCard(
                l.HasIntermediary,
                l.ShowClient,
                l.CompanyName,
                l.CompanyAddress,
                l.IntermediaryName,
                l.IntermediaryAddress);
            var state = KbSavedJobStateResolver.Resolve(l.Status, l.EndDate, l.ClosedAtUtc, now);
            var workType = WorkTypeLabels.ResolveLabels(l.WorkTypes, l.WorkTypeLabels).FirstOrDefault();
            appliedByVacancy.TryGetValue(l.VacancyId, out var appId);
            var hasApplied = appliedByVacancy.ContainsKey(l.VacancyId);
            return new CandidateVacancyEngagementDto(
                l.Id,
                l.VacancyId,
                l.Title,
                companyName,
                l.CreatedAt,
                null,
                l.ImageUrl,
                l.LogoUrl,
                l.Status.ToString(),
                l.EndDate,
                l.ClosedAtUtc,
                location,
                l.MinHours is null ? null : (int?)decimal.ToInt32(l.MinHours.Value),
                l.MaxHours is null ? null : (int?)decimal.ToInt32(l.MaxHours.Value),
                l.CategoryId,
                workType,
                HasApplied: hasApplied,
                ApplicationId: hasApplied ? appId : null,
                FitPercent: null,
                FitBand: null,
                WhyLineKey: null,
                FitGateClosed: true,
                SavedStateKind: state.Kind.ToString(),
                DaysUntilEnd: state.DaysUntilEnd,
                SavedStateLabelKey: state.LabelKey);
        }).ToList();

        return Ok(SlimEngagementMedia(await TranslateEngagementTitlesAsync(items, user, cancellationToken)));
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
            .Select(s => new
            {
                s.Id,
                s.VacancyId,
                Title = s.Vacancy.Title,
                CompanyName = s.Vacancy.Company.Name,
                s.CreatedAt,
                Channel = s.Channel.ToString(),
                ImageUrl = s.Vacancy.ImageUrl,
                LogoUrl = s.Vacancy.Company.LogoUrl
            })
            .ToListAsync(cancellationToken);

        var mapped = items.Select(s => new CandidateVacancyEngagementDto(
            s.Id,
            s.VacancyId,
            s.Title,
            s.CompanyName,
            s.CreatedAt,
            s.Channel,
            s.ImageUrl,
            s.LogoUrl)).ToList();

        return Ok(SlimEngagementMedia(await TranslateEngagementTitlesAsync(mapped, user, cancellationToken)));
    }

    private static List<CandidateVacancyEngagementDto> SlimEngagementMedia(
        List<CandidateVacancyEngagementDto> items)
        => items.Select(i => i with
        {
            ImageUrl = VacancyImageUrls.ForPublicList(i.ImageUrl, i.VacancyId),
            CompanyLogoUrl = VacancyImageUrls.Normalize(i.CompanyLogoUrl)
        }).ToList();

    private async Task<List<CandidateVacancyEngagementDto>> TranslateEngagementTitlesAsync(
        List<CandidateVacancyEngagementDto> items,
        Core.Entities.User user,
        CancellationToken cancellationToken)
    {
        var lang = await ResolveTargetLanguageAsync(user, cancellationToken);
        if (JobsyLanguages.AreSame(VacancySourceLanguage, lang) || items.Count == 0)
        {
            return items;
        }

        var batch = await _translation.TranslateVacanciesBatchAsync(
            items.Select(i => new VacancyTranslationRequest(i.VacancyId, i.VacancyTitle, string.Empty)).ToList(),
            VacancySourceLanguage,
            lang,
            cancellationToken);
        for (var i = 0; i < items.Count; i++)
        {
            items[i] = items[i] with { VacancyTitle = batch[i].Title };
        }

        return items;
    }

    private Task<string> ResolveTargetLanguageAsync(Core.Entities.User user, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
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
        var diplomaEvaluations = await LoadDiplomaEvaluationSharedFactsAsync(user.Id, cancellationToken);
        // AI "Wie ben ik" is not attached to the Lobsy-CV (decision 22). The profile stays in the app.
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
            hasUploadedOwnCv: hasUploadedCv,
            diplomaEvaluations: diplomaEvaluations);

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
        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { message = CandidateConsentRules.ParentalConsentRequiredMessage });
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
                ContactVerification.ApplyPhone(user, merged.PhoneNumber);
            }

            user.PreferencesJson = SerializePreferences(merged.Preferences with
            {
                NoWorkExperience = merged.Preferences.NoWorkExperience ?? prefs.NoWorkExperience,
                EducationDirection = merged.Preferences.EducationDirection ?? prefs.EducationDirection,
                AvailabilityPresets = merged.Preferences.AvailabilityPresets ?? prefs.AvailabilityPresets,
                AvailabilityPresetsOverridden =
                    merged.Preferences.AvailabilityPresetsOverridden ?? prefs.AvailabilityPresetsOverridden,
                SpokenLanguages = merged.Preferences.SpokenLanguages ?? prefs.SpokenLanguages,
                DutchLevel = merged.Preferences.DutchLevel ?? prefs.DutchLevel,
                EmployerPreferences = merged.Preferences.EmployerPreferences ?? prefs.EmployerPreferences,
                LearningGoals = merged.Preferences.LearningGoals ?? prefs.LearningGoals,
                Hobbies = merged.Preferences.Hobbies ?? prefs.Hobbies
            });
            existing.ExtractedAtUtc = DateTime.UtcNow;
            existing.FilledFieldsJson = JsonSerializer.Serialize(merged.FilledFields, JsonOptions);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _insightsQueue.TryEnqueue(user.Id);
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

    [HttpPost("test-ai-consent")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MeProfileDto>> AcceptTestAiConsent(CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { message = CandidateConsentRules.ParentalConsentRequiredMessage });
        }

        user.TestAiConsentAt = DateTime.UtcNow;
        user.TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpDelete("test-ai-consent")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MeProfileDto>> WithdrawTestAiConsent(
        [FromQuery] bool deleteResults,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        user.TestAiConsentAt = null;
        user.TestAiConsentVersion = null;
        if (deleteResults)
        {
            await DeleteTestResultsAsync(user.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpPost("talent-pool-consent")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MeProfileDto>> AcceptTalentPoolConsent(CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (CandidateConsentRules.AgeYears(user.DateOfBirth) is not int age
            || age < CandidateConsentRules.TalentPoolMinimumAge)
        {
            return BadRequest(new { message = CandidateConsentRules.TalentPoolAdultOnlyMessage });
        }

        user.TalentPoolConsentAt = DateTime.UtcNow;
        user.TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpDelete("talent-pool-consent")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MeProfileDto>> WithdrawTalentPoolConsent(CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        user.TalentPoolConsentAt = null;
        user.TalentPoolConsentVersion = null;
        await _db.SaveChangesAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        return Ok(await BuildProfileDtoAsync(user, features.AuthenticatorEnabled, cancellationToken));
    }

    [HttpPost("parental-consent-request")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> RequestParentalConsent(
        [FromBody] RequestParentalConsentRequest request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!CandidateConsentRules.RequiresParentalConsent(user))
        {
            return BadRequest(new { message = "Toestemming van een ouder of voogd is voor jou niet nodig." });
        }

        var email = request.ParentEmail?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            return BadRequest(new { message = "Vul een geldig e-mailadres van je ouder of voogd in." });
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        user.ParentalConsentEmail = email;
        user.ParentalConsentTokenHash = VerificationCodes.Hash(token);
        user.ParentalConsentTokenExpiresAt = expiresAt;
        user.ParentalConsentAt = null;
        await _db.SaveChangesAsync(cancellationToken);

        var features = await _features.GetAsync(cancellationToken);
        var confirmUrl = EmailLayout.Absolute(
            features.PublicWebBaseUrl,
            $"/toestemming?t={Uri.EscapeDataString(token)}");
        var mail = TransactionalEmails.ParentalConsent(
            features.PublicWebBaseUrl,
            user.FirstName,
            confirmUrl,
            expiresAt);
        await _mailer.SendAsync(mail, email, cancellationToken: cancellationToken);

        return Ok(new { message = "We hebben je ouder of voogd een e-mail met een bevestigingslink gestuurd." });
    }

    [HttpPost("phone-verification/start")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult> StartPhoneVerification(
        [FromServices] IPhoneVerificationService phones,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound();
        }

        var result = await phones.StartAsync(lookup.Id, cancellationToken);
        if (!result.Ok)
        {
            return result.Error switch
            {
                "phone_verification_disabled" => NotFound(new { error = "feature_disabled" }),
                "invalid_phone" => BadRequest(new { message = "Ongeldig telefoonnummer." }),
                _ => NotFound()
            };
        }

        return Ok(new { challengeId = result.ChallengeId });
    }

    [HttpPost("phone-verification/verify")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("otp-verify")]
    public async Task<ActionResult> VerifyPhone(
        [FromBody] PhoneVerificationRequest request,
        [FromServices] IPhoneVerificationService phones,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound();
        }

        var result = await phones.VerifyAsync(lookup.Id, request.ChallengeId, request.Code ?? "", cancellationToken);
        if (!result.Ok)
        {
            return result.Error switch
            {
                "phone_verification_disabled" => NotFound(new { error = "feature_disabled" }),
                "code_expired" => StatusCode(StatusCodes.Status410Gone, new { message = "code_expired" }),
                _ => Unauthorized(new { message = "invalid_code" })
            };
        }

        var user = await _db.Users.FirstAsync(u => u.Id == lookup.Id, cancellationToken);
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

        var diplomaEvaluations = await LoadDiplomaEvaluationFactsAsync(user.Id, cancellationToken);

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
            references,
            user.AvailableFromDate,
            user.TalentPoolConsentAt,
            user.TalentPoolConsentVersion,
            user.TestAiConsentAt,
            user.TestAiConsentVersion,
            user.ParentalConsentAt,
            user.ParentalConsentEmail,
            user.EmailVerifiedAtUtc is not null,
            user.PhoneVerifiedAtUtc is not null,
            diplomaEvaluations);
    }

    private async Task<Core.Entities.User?> ResolveActiveCandidateAsync(CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return null;
        }

        return await _db.Users.FirstOrDefaultAsync(
            u => u.Id == lookup.Id && u.IsActive && u.Role == Core.Enums.UserRole.Candidate,
            cancellationToken);
    }

    private async Task DeleteTestResultsAsync(Guid userId, CancellationToken cancellationToken)
    {
        _db.CandidateCompetencies.RemoveRange(
            await _db.CandidateCompetencies.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateCareerInterests.RemoveRange(
            await _db.CandidateCareerInterests.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateCulturePersonalityProfiles.RemoveRange(
            await _db.CandidateCulturePersonalityProfiles.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateValuesProfiles.RemoveRange(
            await _db.CandidateValuesProfiles.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateDeepAnalyses.RemoveRange(
            await _db.CandidateDeepAnalyses.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateWhoAmIProfiles.RemoveRange(
            await _db.CandidateWhoAmIProfiles.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateRoleFitChecks.RemoveRange(
            await _db.CandidateRoleFitChecks.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateCareerPlans.RemoveRange(
            await _db.CandidateCareerPlans.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateCareerStepProgress.RemoveRange(
            await _db.CandidateCareerStepProgress.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateMatchSnapshots.RemoveRange(
            await _db.CandidateMatchSnapshots.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
        _db.CandidateVacancyCultureFits.RemoveRange(
            await _db.CandidateVacancyCultureFits.Where(x => x.UserId == userId).ToListAsync(cancellationToken));
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

            bool? noWorkExperience = null;
            if (root.TryGetProperty("noWorkExperience", out var noWorkEl)
                && (noWorkEl.ValueKind is JsonValueKind.True or JsonValueKind.False))
            {
                noWorkExperience = noWorkEl.GetBoolean();
            }

            string? educationDirection = null;
            if (root.TryGetProperty("educationDirection", out var eduDirEl)
                && eduDirEl.ValueKind == JsonValueKind.String)
            {
                educationDirection = eduDirEl.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(educationDirection))
                {
                    educationDirection = null;
                }
                else if (educationDirection.Length > 80)
                {
                    educationDirection = educationDirection[..80];
                }
            }

            List<string>? availabilityPresets = null;
            if (root.TryGetProperty("availabilityPresets", out var presetsEl)
                && presetsEl.ValueKind == JsonValueKind.Array)
            {
                availabilityPresets = [];
                foreach (var item in presetsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var code = item.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(code)
                        && AvailabilityPresetRules.IsKnown(code)
                        && !availabilityPresets.Contains(code, StringComparer.OrdinalIgnoreCase))
                    {
                        availabilityPresets.Add(AvailabilityPresetRules.TryGet(code)!.Code);
                    }
                }
            }

            bool? availabilityPresetsOverridden = null;
            if (root.TryGetProperty("availabilityPresetsOverridden", out var overriddenEl)
                && (overriddenEl.ValueKind is JsonValueKind.True or JsonValueKind.False))
            {
                availabilityPresetsOverridden = overriddenEl.GetBoolean();
            }

            List<CandidateLanguageDto>? spokenLanguages = null;
            if (root.TryGetProperty("spokenLanguages", out var spokenEl)
                && spokenEl.ValueKind == JsonValueKind.Array)
            {
                spokenLanguages = [];
                foreach (var item in spokenEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    string? code = null;
                    if (item.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
                    {
                        code = codeEl.GetString();
                    }

                    string? level = null;
                    if (item.TryGetProperty("level", out var levelEl) && levelEl.ValueKind == JsonValueKind.String)
                    {
                        level = levelEl.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        spokenLanguages.Add(new CandidateLanguageDto(code, level));
                    }
                }
            }

            string? dutchLevel = null;
            if (root.TryGetProperty("dutchLevel", out var dutchEl) && dutchEl.ValueKind == JsonValueKind.String)
            {
                dutchLevel = dutchEl.GetString();
            }

            List<string>? employerPreferences = null;
            if (root.TryGetProperty("employerPreferences", out var empPrefEl)
                && empPrefEl.ValueKind == JsonValueKind.Array)
            {
                employerPreferences = ReadStringArray(empPrefEl);
            }

            List<string>? learningGoals = null;
            if (root.TryGetProperty("learningGoals", out var goalsEl)
                && goalsEl.ValueKind == JsonValueKind.Array)
            {
                learningGoals = ReadStringArray(goalsEl);
            }

            List<string>? hobbies = null;
            if (root.TryGetProperty("hobbies", out var hobbiesEl)
                && hobbiesEl.ValueKind == JsonValueKind.Array)
            {
                hobbies = ReadStringArray(hobbiesEl);
            }

            SharedWorkPreferences? workPreferences = null;
            if (root.TryGetProperty("workPreferences", out var workEl) && workEl.ValueKind == JsonValueKind.Object)
            {
                workPreferences = new SharedWorkPreferences(
                    ReadOptionalString(workEl, "indoor"),
                    ReadOptionalString(workEl, "outdoor"),
                    ReadOptionalString(workEl, "physicalWork"),
                    ReadOptionalString(workEl, "pace"));
            }

            var shareEmployerPreferences = ReadOptionalBool(root, "shareEmployerPreferences");
            var workRegion = ReadOptionalString(root, "workRegion");
            var hasOwnCar = ReadOptionalBool(root, "hasOwnCar");
            List<string>? contractPreferences = null;
            if (root.TryGetProperty("contractPreferences", out var contractEl)
                && contractEl.ValueKind == JsonValueKind.Array)
            {
                contractPreferences = ReadStringArray(contractEl);
            }

            var parsed = new CandidatePreferencesDto(
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
                showAddressOnCv,
                noWorkExperience,
                educationDirection,
                availabilityPresets,
                availabilityPresetsOverridden,
                spokenLanguages,
                dutchLevel,
                employerPreferences,
                learningGoals,
                hobbies,
                ShareablePreferenceNormalizer.NormalizeWork(workPreferences, null),
                shareEmployerPreferences,
                ShareablePreferenceNormalizer.NormalizeRegion(workRegion, null),
                hasOwnCar,
                ShareablePreferenceNormalizer.NormalizeContracts(contractPreferences, null));

            return CandidatePreferencesValidator.Sanitize(parsed);
        }
        catch (Exception)
        {
            return EmptyPreferences();
        }
    }

    private static string? ReadOptionalString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = el.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool? ReadOptionalBool(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el)
            || el.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return null;
        }

        return el.GetBoolean();
    }

    private static List<string> ReadStringArray(JsonElement arrayEl)
    {
        var list = new List<string>();
        foreach (var item in arrayEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = item.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                list.Add(value);
            }
        }

        return list;
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
        null,
        null,
        null);

    public static string SerializePreferences(CandidatePreferencesDto prefs)
    {
        var sanitized = CandidatePreferencesValidator.Sanitize(prefs);
        var trimmedHome = string.IsNullOrWhiteSpace(sanitized.HomeAddress) ? null : sanitized.HomeAddress.Trim();
        if (trimmedHome is { Length: > 256 })
        {
            trimmedHome = trimmedHome[..256];
        }

        var trimmedMotivation = string.IsNullOrWhiteSpace(sanitized.DefaultMotivation)
            ? null
            : sanitized.DefaultMotivation.Trim();
        if (trimmedMotivation is { Length: > 500 })
        {
            trimmedMotivation = trimmedMotivation[..500];
        }

        return JsonSerializer.Serialize(new
        {
            roles = sanitized.Roles ?? [],
            maxTravelMinutes = sanitized.MaxTravelMinutes,
            preferredTransport = sanitized.PreferredTransport,
            language = string.IsNullOrWhiteSpace(sanitized.Language)
                ? null
                : JobsyLanguages.Normalize(sanitized.Language),
            ageYears = sanitized.AgeYears,
            aboutMe = string.IsNullOrWhiteSpace(sanitized.AboutMe) ? null : sanitized.AboutMe.Trim(),
            defaultMotivation = trimmedMotivation,
            drivingLicenses = sanitized.DrivingLicenses?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            availability = sanitized.Availability,
            employers = sanitized.Employers?
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
            educations = sanitized.Educations?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            homeAddress = trimmedHome,
            minHoursPerWeek = sanitized.MinHoursPerWeek,
            maxHoursPerWeek = sanitized.MaxHoursPerWeek,
            flexibleTimes = sanitized.FlexibleTimes,
            certificates = sanitized.Certificates?
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
            showAddressOnCv = sanitized.ShowAddressOnCv,
            noWorkExperience = sanitized.NoWorkExperience,
            educationDirection = string.IsNullOrWhiteSpace(sanitized.EducationDirection)
                ? null
                : (sanitized.EducationDirection.Trim().Length > 80
                    ? sanitized.EducationDirection.Trim()[..80]
                    : sanitized.EducationDirection.Trim()),
            availabilityPresets = sanitized.AvailabilityPresets?
                .Where(x => !string.IsNullOrWhiteSpace(x) && AvailabilityPresetRules.IsKnown(x))
                .Select(x => AvailabilityPresetRules.TryGet(x)!.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            availabilityPresetsOverridden = sanitized.AvailabilityPresetsOverridden,
            spokenLanguages = sanitized.SpokenLanguages?
                .Select(l => new { code = l.Code, level = l.Level })
                .ToArray(),
            dutchLevel = sanitized.DutchLevel,
            employerPreferences = sanitized.EmployerPreferences,
            learningGoals = sanitized.LearningGoals,
            hobbies = sanitized.Hobbies,
            workPreferences = sanitized.WorkPreferences is null
                ? null
                : new
                {
                    indoor = sanitized.WorkPreferences.Indoor,
                    outdoor = sanitized.WorkPreferences.Outdoor,
                    physicalWork = sanitized.WorkPreferences.PhysicalWork,
                    pace = sanitized.WorkPreferences.Pace
                },
            shareEmployerPreferences = sanitized.ShareEmployerPreferences,
            workRegion = sanitized.WorkRegion,
            hasOwnCar = sanitized.HasOwnCar,
            contractPreferences = sanitized.ContractPreferences
        }, JsonOptions);
    }

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
        bool? showAddressOnCv = null,
        bool? noWorkExperience = null,
        string? educationDirection = null,
        IEnumerable<string>? availabilityPresets = null,
        bool? availabilityPresetsOverridden = null,
        IEnumerable<CandidateLanguageDto>? spokenLanguages = null,
        string? dutchLevel = null,
        IEnumerable<string>? employerPreferences = null,
        IEnumerable<string>? learningGoals = null,
        IEnumerable<string>? hobbies = null)
        => SerializePreferences(new CandidatePreferencesDto(
            roles.ToList(),
            maxTravelMinutes,
            preferredTransport,
            language,
            ageYears,
            aboutMe,
            defaultMotivation,
            drivingLicenses?.ToList(),
            availability,
            employers?.ToList(),
            educations?.ToList(),
            homeAddress,
            minHoursPerWeek,
            maxHoursPerWeek,
            flexibleTimes,
            certificates?.ToList(),
            showAddressOnCv,
            noWorkExperience,
            educationDirection,
            availabilityPresets?.ToList(),
            availabilityPresetsOverridden,
            spokenLanguages?.ToList(),
            dutchLevel,
            employerPreferences?.ToList(),
            learningGoals?.ToList(),
            hobbies?.ToList()));
}
