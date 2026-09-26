using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class WhoAmIService : IWhoAmIService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JobsyDbContext _db;
    private readonly ICandidateInsightsQueue _queue;

    public WhoAmIService(JobsyDbContext db, ICandidateInsightsQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    public Task<WhoAmIStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => LoadAsync(userId, cancellationToken);

    public async Task<WhoAmIStateDto> SetIncludeOnCvAsync(
        Guid userId,
        bool includeOnCv,
        CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(userId, cancellationToken);
        if (!state.IsUnlocked)
        {
            throw new InvalidOperationException("Rond eerst alle vier stappen af voordat je het persoonsprofiel aan je Lobsy-CV kunt toevoegen.");
        }

        var row = await _db.CandidateWhoAmIProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = NewRow(userId);
            _db.CandidateWhoAmIProfiles.Add(row);
        }

        row.IncludeOnCv = includeOnCv;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return state with { IncludeOnCv = includeOnCv };
    }

    public async Task<LobsyCvWhoAmI?> GetCvAttachmentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Stored story only — never generate inside CV/PDF requests.
        var state = await LoadAsync(userId, cancellationToken);
        if (!state.IsUnlocked || !state.IncludeOnCv || string.IsNullOrWhiteSpace(state.Story)
            || state.CompetencyScores is not { IsComplete: true } competency
            || state.CultureScores is not { IsComplete: true } culture)
        {
            return null;
        }

        return ToAttachment(state.Story, state.Keywords, competency, culture);
    }

    private async Task<WhoAmIStateDto> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var hasUploadedCv = await _db.CandidateUploadedCvs.AsNoTracking()
            .AnyAsync(c => c.UserId == userId, cancellationToken);
        var hasReferences = await _db.CandidateReferences.AsNoTracking()
            .AnyAsync(c => c.UserId == userId, cancellationToken);
        var profileFilled = WhoAmICompleteness.IsProfileFilled(
            user?.FullName,
            user?.FirstName,
            user?.LastName,
            user?.PreferencesJson,
            hasUploadedCv,
            hasReferences);

        var prefs = TryReadPreferences(user?.PreferencesJson);
        var employers = (prefs?.Employers ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName) || !string.IsNullOrWhiteSpace(e.Role))
            .Select(e => new WhoAmIEmployerDto(
                string.IsNullOrWhiteSpace(e.EmployerName) ? (e.Role ?? "Werkervaring") : e.EmployerName.Trim(),
                string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim()))
            .Take(6)
            .ToList();
        var educations = (prefs?.Educations ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        var certificates = (prefs?.Certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c.Year is int y ? $"{c.Name.Trim()} ({y})" : c.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        var profileHighlights = WhoAmIProfileHighlights.FromPreferences(prefs);

        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var careerRow = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var valuesRow = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var competency = CompetencyTestCatalog.CompletedScoresOrNull(
            competencyRow?.Status,
            competencyRow?.SamenwerkenPercent,
            competencyRow?.ResultaatgerichtheidPercent,
            competencyRow?.StressbestendigheidPercent,
            competencyRow?.InnovatiePercent,
            competencyRow?.ExtraversiePercent);
        var career = CareerTestCatalog.CompletedScoresOrNull(
            careerRow?.Status,
            careerRow?.RealisticPercent,
            careerRow?.InvestigativePercent,
            careerRow?.ArtisticPercent,
            careerRow?.SocialPercent,
            careerRow?.EnterprisingPercent,
            careerRow?.ConventionalPercent);
        var culture = cultureRow is null
            || !CandidateCompetencyStatuses.IsCompleted(cultureRow.Status)
            ? null
            : new CulturePersonalityScores(
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
        if (culture is not { IsComplete: true })
        {
            culture = null;
        }

        SchwartzValuesScores? values = null;
        if (valuesRow is not null && CandidateCompetencyStatuses.IsCompleted(valuesRow.Status))
        {
            values = new SchwartzValuesScores(
                valuesRow.AutonomyPercent,
                valuesRow.ConnectionPercent,
                valuesRow.AchievementPercent,
                valuesRow.StabilityPercent,
                valuesRow.ImpactPercent);
            if (values is not { IsComplete: true })
            {
                values = null;
            }
        }

        var competencyDone = competency is { IsComplete: true };
        var careerDone = career is { IsComplete: true };
        var cultureDone = culture is { IsComplete: true };
        var unlocked = WhoAmICompleteness.IsUnlocked(profileFilled, competencyDone, careerDone, cultureDone);
        var encouragement = Encouragement(profileFilled, competencyDone, careerDone, cultureDone);

        string? story = null;
        IReadOnlyList<string> keywords = [];
        var fromOpenAi = false;
        DateTime? generatedAt = null;
        var includeOnCv = false;
        var insightsStatus = InsightsStatuses.Ready;

        var stored = await _db.CandidateWhoAmIProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (stored is not null)
        {
            includeOnCv = stored.IncludeOnCv;
            if (WhoAmIStoryBuilder.Sanitize(stored.StoryText) is { } cachedStory)
            {
                story = cachedStory;
                keywords = LikertAnswerJson.ParseTags(stored.KeywordsJson);
                fromOpenAi = stored.FromOpenAi;
                generatedAt = stored.StoryGeneratedAtUtc;
            }
        }

        if (unlocked && competency is { IsComplete: true } cScores
            && career is { IsComplete: true } rScores
            && culture is { IsComplete: true } cultureScores)
        {
            var fingerprint = WhoAmICompleteness.Fingerprint(cScores, rScores, cultureScores, profileHighlights, values);
            var fingerprintMatch = stored is not null
                                   && string.Equals(stored.InputFingerprint, fingerprint, StringComparison.Ordinal)
                                   && story is not null;
            var retryFallback = stored is not null
                                && CandidateInsightsFingerprint.ShouldRetryFallback(
                                    stored.FromOpenAi,
                                    stored.StoryGeneratedAtUtc,
                                    DateTime.UtcNow);
            if (!fingerprintMatch || retryFallback || story is null)
            {
                insightsStatus = InsightsStatuses.Updating;
                _queue.TryEnqueue(userId);
            }
        }

        return new WhoAmIStateDto(
            profileFilled,
            competencyDone,
            careerDone,
            cultureDone,
            unlocked,
            encouragement,
            story,
            fromOpenAi,
            keywords,
            competency,
            culture,
            career,
            includeOnCv,
            generatedAt,
            employers,
            educations,
            certificates,
            insightsStatus);
    }

    internal static LobsyCvWhoAmI ToAttachment(
        string story,
        IReadOnlyList<string> keywords,
        CompetencyScores competency,
        CulturePersonalityScores culture)
        => new(
            story,
            keywords,
            CompetencyTestCatalog.CategoryCodes
                .Select(code => new LobsyCvScoreBar(WhoAmIKeywords.EverydayCompetency(code), competency.Get(code)))
                .ToList(),
            CulturePersonalityCatalog.CategoryCodes
                .Select(code => new LobsyCvScoreBar(CulturePersonalityCatalog.EverydayLabel(code), culture.Get(code)))
                .ToList());

    private static CandidateWhoAmIProfile NewRow(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static CandidatePreferencesDto? TryReadPreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Encouragement(
        bool profile,
        bool competency,
        bool career,
        bool culture)
    {
        if (WhoAmICompleteness.IsUnlocked(profile, competency, career, culture))
        {
            return "";
        }

        if (!profile)
        {
            return "Vul eerst je naam, achtergrond en reistijd/vervoer in. Daarna ontgrendel je je verhaal.";
        }

        if (!competency)
        {
            return "Rond de competentietest af (ongeveer 3 minuten). Elk vinkje brengt je dichter bij je persoonsprofiel.";
        }

        if (!career)
        {
            return "Rond de beroepentest af. Samen met je competenties wordt je verhaal écht van jou.";
        }

        return "Rond de cultuurscan af (18 korte stellingen). Daarna is je rapport klaar.";
    }
}
