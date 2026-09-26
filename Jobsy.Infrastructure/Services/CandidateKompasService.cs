using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateKompasService : ICandidateKompasService
{
    private static readonly JsonSerializerOptions PrefsJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ICandidateCompetencyService _competencies;
    private readonly ICandidateCareerInterestService _career;
    private readonly ICandidateCulturePersonalityService _culture;
    private readonly ICandidateValuesService _values;
    private readonly IDeepAnalysisService _deep;
    private readonly ICandidateMatchSnapshotService _matches;
    private readonly IPlatformFeatureService _features;

    public CandidateKompasService(
        JobsyDbContext db,
        ICandidateCompetencyService competencies,
        ICandidateCareerInterestService career,
        ICandidateCulturePersonalityService culture,
        ICandidateValuesService values,
        IDeepAnalysisService deep,
        ICandidateMatchSnapshotService matches,
        IPlatformFeatureService features)
    {
        _db = db;
        _competencies = competencies;
        _career = career;
        _culture = culture;
        _values = values;
        _deep = deep;
        _matches = matches;
        _features = features;
    }

    public async Task<CandidateKompasDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Gebruiker niet gevonden.");

        var features = await _features.GetAsync(cancellationToken);

        // Direct _db queries and scoped services share one DbContext — keep sequential.
        var uploadedCv = await LoadCvAsync(userId, cancellationToken);
        var references = await _db.CandidateReferences.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAtUtc)
            .Select(r => new CandidateReferenceSummaryDto(r.Id, r.EmployerName, r.ContactName, r.Email, r.Phone))
            .ToListAsync(cancellationToken);
        var whoAmIRow = await _db.CandidateWhoAmIProfiles.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new
            {
                w.StoryText,
                w.KeywordsJson,
                w.StoryGeneratedAtUtc,
                w.InputFingerprint,
                w.FromOpenAi
            })
            .FirstOrDefaultAsync(cancellationToken);

        var competencies = await _competencies.GetAsync(userId, cancellationToken);
        var careerState = await _career.GetAsync(userId, cancellationToken, includeMatches: false);
        var culture = await _culture.GetAsync(userId, cancellationToken);
        var values = await _values.GetAsync(userId, cancellationToken);
        var (matches, matchStatus) = await _matches.GetAsync(userId, cancellationToken);
        var competenceDeep = await _deep.GetStateAsync(userId, AssessmentKind.Competence, cancellationToken);
        var careerDeep = await _deep.GetStateAsync(userId, AssessmentKind.Career, cancellationToken);
        var cultureDeep = await _deep.GetStateAsync(userId, AssessmentKind.Culture, cancellationToken);
        var valuesDeep = await _deep.GetStateAsync(userId, AssessmentKind.Values, cancellationToken);

        var prefs = TryPrefs(user.PreferencesJson);
        var hasUploadedCv = uploadedCv is not null;
        var hasReferences = references.Count > 0;
        var profile = new MeProfileSummaryDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.DateOfBirth.HasValue,
            user.OpenForWork,
            user.WhatsAppContactAllowed,
            features.AuthenticatorEnabled,
            user.HomeLocation?.Latitude,
            user.HomeLocation?.Longitude,
            prefs,
            uploadedCv,
            references);

        var career = careerState with { TopVacancies = matches };
        var insights = InsightsStatuses.IsUpdating(matchStatus)
                       || InsightsStatuses.IsUpdating(career.InsightsStatus)
            ? InsightsStatuses.Updating
            : InsightsStatuses.Ready;

        var competencyDone = CandidateCompetencyStatuses.IsCompleted(competencies.Status);
        var careerDone = CandidateCompetencyStatuses.IsCompleted(career.Status);
        var cultureDone = CandidateCompetencyStatuses.IsCompleted(culture.Status);
        var valuesDone = CandidateCompetencyStatuses.IsCompleted(values.Status);
        var competencyProvisional = !competencyDone && competencies.AnsweredCount > 0;
        var careerProvisional = !careerDone && career.AnsweredCount > 0;
        var cultureProvisional = !cultureDone && culture.Answers.Count > 0;
        var valuesProvisional = !valuesDone && values.Answers.Count > 0;
        var profileFilled = WhoAmICompleteness.IsProfileFilled(
            user.FullName,
            user.FirstName,
            user.LastName,
            user.PreferencesJson,
            hasUploadedCv,
            hasReferences);
        var hasBackground = WhoAmICompleteness.IsProfileFilled(
                                 user.FullName,
                                 user.FirstName,
                                 user.LastName,
                                 user.PreferencesJson,
                                 hasUploadedCv,
                                 hasReferences)
                             || MatchProfileCompleteness.HasEducationLevel(prefs);

        var completeness = KompasProfileCompleteness.Percent(
            profileFilled,
            competencyDone,
            careerDone,
            cultureDone,
            valuesDone,
            hasBackground,
            competencyProvisional,
            careerProvisional,
            cultureProvisional,
            valuesProvisional);

        var whoAmI = BuildWhoAmIStory(
            whoAmIRow?.StoryText,
            whoAmIRow?.KeywordsJson,
            whoAmIRow?.StoryGeneratedAtUtc,
            whoAmIRow?.InputFingerprint,
            whoAmIRow?.FromOpenAi ?? false,
            profileFilled,
            competencyDone,
            careerDone,
            cultureDone,
            competencies.Scores,
            career.Scores,
            culture.Scores,
            values.Scores);

        return new CandidateKompasDto(
            profile,
            competencies,
            career,
            culture,
            values,
            competenceDeep,
            careerDeep,
            cultureDeep,
            valuesDeep,
            matches,
            insights,
            whoAmI,
            completeness);
    }

    /// <summary>
    /// Pure read of stored story — never calls AI or enqueues generation.
    /// </summary>
    internal static WhoAmIStorySummaryDto BuildWhoAmIStory(
        string? storyText,
        string? keywordsJson,
        DateTime? generatedAtUtc,
        string? storedFingerprint,
        bool fromOpenAi,
        bool profileFilled,
        bool competencyDone,
        bool careerDone,
        bool cultureDone,
        CompetencyScores? competency,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values)
    {
        var keywords = ParseKeywords(keywordsJson);
        var story = string.IsNullOrWhiteSpace(storyText) ? null : storyText.Trim();
        var unlocked = WhoAmICompleteness.IsUnlocked(profileFilled, competencyDone, careerDone, cultureDone);

        if (story is null)
        {
            return new WhoAmIStorySummaryDto(null, keywords, generatedAtUtc, WhoAmIStoryStatuses.Empty);
        }

        if (!unlocked
            || competency is not { IsComplete: true }
            || career is not { IsComplete: true }
            || culture is not { IsComplete: true })
        {
            return new WhoAmIStorySummaryDto(story, keywords, generatedAtUtc, WhoAmIStoryStatuses.Ready);
        }

        var highlights = WhoAmIProfileHighlights.Empty;
        var expected = WhoAmICompleteness.Fingerprint(competency, career, culture, highlights, values);
        var stale = !string.Equals(storedFingerprint, expected, StringComparison.Ordinal);
        var retryFallback = CandidateInsightsFingerprint.ShouldRetryFallback(
            fromOpenAi,
            generatedAtUtc,
            DateTime.UtcNow);
        var status = stale || retryFallback ? WhoAmIStoryStatuses.Updating : WhoAmIStoryStatuses.Ready;
        return new WhoAmIStorySummaryDto(story, keywords, generatedAtUtc, status);
    }

    private static IReadOnlyList<string> ParseKeywords(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, PrefsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<CandidateUploadedCvSummaryDto?> LoadCvAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cvRow = await _db.CandidateUploadedCvs.AsNoTracking()
            .Where(c => c.UserId == userId)
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
        if (cvRow is null)
        {
            return null;
        }

        IReadOnlyList<string>? filled = null;
        if (!string.IsNullOrWhiteSpace(cvRow.FilledFieldsJson))
        {
            try
            {
                filled = JsonSerializer.Deserialize<List<string>>(cvRow.FilledFieldsJson, PrefsJson);
            }
            catch (JsonException)
            {
                filled = null;
            }
        }

        return new CandidateUploadedCvSummaryDto(
            cvRow.FileName,
            cvRow.ContentType,
            cvRow.SizeBytes,
            cvRow.UploadedAtUtc,
            cvRow.ExtractedAtUtc,
            filled);
    }

    private static CandidatePreferencesDto? TryPrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, PrefsJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
