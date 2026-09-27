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
        var deepStates = await _deep.GetStatesAsync(
            userId,
            [AssessmentKind.Competence, AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values],
            cancellationToken);
        var competenceDeep = deepStates[AssessmentKind.Competence];
        var careerDeep = deepStates[AssessmentKind.Career];
        var cultureDeep = deepStates[AssessmentKind.Culture];
        var valuesDeep = deepStates[AssessmentKind.Values];

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

        var career = careerState with { TopVacancies = [] };
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

    public async Task<CandidateDnaSummaryDto> GetDnaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Gebruiker niet gevonden.");

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

        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.SamenwerkenPercent,
                c.ResultaatgerichtheidPercent,
                c.StressbestendigheidPercent,
                c.InnovatiePercent,
                c.ExtraversiePercent
            })
            .FirstOrDefaultAsync(cancellationToken);

        var careerRow = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.RealisticPercent,
                c.InvestigativePercent,
                c.ArtisticPercent,
                c.SocialPercent,
                c.EnterprisingPercent,
                c.ConventionalPercent
            })
            .FirstOrDefaultAsync(cancellationToken);

        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.AutonomyPercent,
                c.InformalPercent,
                c.CollaborationPercent,
                c.FlexibilityPercent,
                c.InnovationPercent,
                c.PeopleFirstPercent,
                c.OpennessPercent,
                c.ConscientiousnessPercent,
                c.ExtraversionPercent,
                c.AgreeablenessPercent,
                c.EmotionalStabilityPercent
            })
            .FirstOrDefaultAsync(cancellationToken);

        var valuesRow = await _db.CandidateValuesProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Status,
                c.AnswersJson,
                c.AutonomyPercent,
                c.ConnectionPercent,
                c.AchievementPercent,
                c.StabilityPercent,
                c.ImpactPercent
            })
            .FirstOrDefaultAsync(cancellationToken);

        var deepRows = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new { d.Kind, d.Status, d.AnswersJson })
            .ToListAsync(cancellationToken);

        var hasUploadedCv = await _db.CandidateUploadedCvs.AsNoTracking()
            .AnyAsync(c => c.UserId == userId, cancellationToken);
        var hasReferences = await _db.CandidateReferences.AsNoTracking()
            .AnyAsync(r => r.UserId == userId, cancellationToken);

        var competencyResolved = competencyRow is null
            ? ProvisionalAssessmentScores.ResolveCompetency(null, null, null, null, null, null, null)
            : ProvisionalAssessmentScores.ResolveCompetency(
                competencyRow.Status,
                competencyRow.AnswersJson,
                competencyRow.SamenwerkenPercent,
                competencyRow.ResultaatgerichtheidPercent,
                competencyRow.StressbestendigheidPercent,
                competencyRow.InnovatiePercent,
                competencyRow.ExtraversiePercent);
        var careerResolved = careerRow is null
            ? ProvisionalAssessmentScores.ResolveCareer(null, null, null, null, null, null, null, null)
            : ProvisionalAssessmentScores.ResolveCareer(
                careerRow.Status,
                careerRow.AnswersJson,
                careerRow.RealisticPercent,
                careerRow.InvestigativePercent,
                careerRow.ArtisticPercent,
                careerRow.SocialPercent,
                careerRow.EnterprisingPercent,
                careerRow.ConventionalPercent);

        CulturePersonalityScores? storedCulture = null;
        if (cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status))
        {
            storedCulture = new CulturePersonalityScores(
                cultureRow.AutonomyPercent,
                cultureRow.InformalPercent,
                cultureRow.CollaborationPercent,
                cultureRow.FlexibilityPercent,
                cultureRow.InnovationPercent,
                cultureRow.PeopleFirstPercent,
                cultureRow.OpennessPercent,
                cultureRow.ConscientiousnessPercent,
                cultureRow.ExtraversionPercent,
                cultureRow.AgreeablenessPercent,
                cultureRow.EmotionalStabilityPercent);
        }

        var cultureResolved = cultureRow is null
            ? ProvisionalAssessmentScores.ResolveCulture(null, null, null)
            : ProvisionalAssessmentScores.ResolveCulture(
                cultureRow.Status,
                cultureRow.AnswersJson,
                storedCulture);

        var valuesResolved = valuesRow is null
            ? ProvisionalAssessmentScores.ResolveValues(null, null, null, null, null, null, null)
            : ProvisionalAssessmentScores.ResolveValues(
                valuesRow.Status,
                valuesRow.AnswersJson,
                valuesRow.AutonomyPercent,
                valuesRow.ConnectionPercent,
                valuesRow.AchievementPercent,
                valuesRow.StabilityPercent,
                valuesRow.ImpactPercent);

        var competencyDone = competencyRow is not null
                               && CandidateCompetencyStatuses.IsCompleted(competencyRow.Status);
        var careerDone = careerRow is not null && CandidateCompetencyStatuses.IsCompleted(careerRow.Status);
        var cultureDone = cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status);
        var valuesDone = valuesRow is not null && CandidateCompetencyStatuses.IsCompleted(valuesRow.Status);

        var competencyAnswers = CompetencyTestCatalog.ParseAnswersJson(competencyRow?.AnswersJson);
        var careerAnswers = CareerTestCatalog.ParseAnswersJson(careerRow?.AnswersJson);
        var cultureAnswers = CulturePersonalityCatalog.ParseAnswers(cultureRow?.AnswersJson);
        var valuesAnswers = SchwartzValuesCatalog.ParseAnswers(valuesRow?.AnswersJson);

        var competencyProvisional = competencyResolved.IsProvisional;
        var careerProvisional = careerResolved.IsProvisional;
        var cultureProvisional = cultureResolved.IsProvisional;
        var valuesProvisional = valuesResolved.IsProvisional;

        var prefs = TryPrefs(user.PreferencesJson);
        var profileFilled = WhoAmICompleteness.IsProfileFilled(
            user.FullName,
            user.FirstName,
            user.LastName,
            user.PreferencesJson,
            hasUploadedCv,
            hasReferences);
        var hasBackground = profileFilled || MatchProfileCompleteness.HasEducationLevel(prefs);

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
            competencyResolved.Scores,
            careerResolved.Scores,
            cultureResolved.Scores,
            valuesResolved.Scores);

        bool IsDeepCompleted(AssessmentKind kind)
        {
            var row = deepRows.FirstOrDefault(r => r.Kind == kind);
            if (row is null)
            {
                return false;
            }

            var answers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, kind);
            var expected = DeepAnalysisCatalog.QuestionCountFor(kind);
            return CandidateDeepAnalysisStatuses.IsCompleted(row.Status) && answers.Count >= expected;
        }

        CompetencyScores? competencyCompleted = competencyDone ? competencyResolved.Scores : null;
        CompetencyScores? competencyPreview = competencyProvisional ? competencyResolved.Scores : null;
        RiasecScores? careerCompleted = careerDone ? careerResolved.Scores : null;
        RiasecScores? careerPreview = careerProvisional ? careerResolved.Scores : null;
        CulturePersonalityScores? cultureCompleted = cultureDone ? cultureResolved.Scores : null;
        CulturePersonalityScores? culturePreview = cultureProvisional ? cultureResolved.Scores : null;
        SchwartzValuesScores? valuesCompleted = valuesDone ? valuesResolved.Scores : null;
        SchwartzValuesScores? valuesPreview = valuesProvisional ? valuesResolved.Scores : null;

        return new CandidateDnaSummaryDto(
            whoAmI,
            completeness,
            new CandidateDnaCompetencySummaryDto(
                competencyRow?.Status ?? CandidateCompetencyStatuses.Draft,
                competencyAnswers.Count,
                CompetencyTestCatalog.QuestionCount,
                competencyCompleted,
                competencyPreview,
                IsDeepCompleted(AssessmentKind.Competence)),
            new CandidateDnaCareerSummaryDto(
                careerRow?.Status ?? CandidateCompetencyStatuses.Draft,
                careerAnswers.Count,
                CareerTestCatalog.QuestionCount,
                careerCompleted,
                careerPreview,
                IsDeepCompleted(AssessmentKind.Career)),
            new CandidateDnaCultureSummaryDto(
                cultureRow?.Status ?? CandidateCompetencyStatuses.Draft,
                cultureAnswers.Count,
                CulturePersonalityCatalog.QuestionCount,
                cultureCompleted,
                culturePreview,
                IsDeepCompleted(AssessmentKind.Culture)),
            new CandidateDnaValuesSummaryDto(
                valuesRow?.Status ?? CandidateCompetencyStatuses.Draft,
                valuesAnswers.Count,
                SchwartzValuesCatalog.QuestionCount,
                valuesCompleted,
                valuesPreview,
                IsDeepCompleted(AssessmentKind.Values)));
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
