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
    private readonly IWhoAmIGenerationService _generate;

    public WhoAmIService(JobsyDbContext db, IWhoAmIGenerationService generate)
    {
        _db = db;
        _generate = generate;
    }

    public Task<WhoAmIStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => LoadAsync(userId, persistStory: true, cancellationToken);

    public async Task<WhoAmIStateDto> SetIncludeOnCvAsync(
        Guid userId,
        bool includeOnCv,
        CancellationToken cancellationToken = default)
    {
        var state = await LoadAsync(userId, persistStory: true, cancellationToken);
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
        var state = await LoadAsync(userId, persistStory: true, cancellationToken);
        if (!state.IsUnlocked || !state.IncludeOnCv || string.IsNullOrWhiteSpace(state.Story)
            || state.CompetencyScores is not { IsComplete: true } competency
            || state.DiscScores is not { IsComplete: true } disc)
        {
            return null;
        }

        return ToAttachment(state.Story, state.Keywords, competency, disc);
    }

    private async Task<WhoAmIStateDto> LoadAsync(Guid userId, bool persistStory, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var prefs = MatchingProfileMapper.DeserializePrefs(user?.PreferencesJson);
        var profileFilled = WhoAmICompleteness.IsProfileFilled(user?.FullName, prefs);

        var competencyRow = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var careerRow = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var discRow = await _db.CandidateDiscProfiles.AsNoTracking()
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
        var disc = DiscTestCatalog.CompletedScoresOrNull(
            discRow?.Status,
            discRow?.DominantPercent,
            discRow?.InvloedPercent,
            discRow?.StabielPercent,
            discRow?.NauwkeurigPercent);

        var competencyDone = competency is { IsComplete: true };
        var careerDone = career is { IsComplete: true };
        var discDone = disc is { IsComplete: true };
        var unlocked = WhoAmICompleteness.IsUnlocked(profileFilled, competencyDone, careerDone, discDone);
        var encouragement = Encouragement(profileFilled, competencyDone, careerDone, discDone);

        string? story = null;
        IReadOnlyList<string> keywords = [];
        var fromOpenAi = false;
        DateTime? generatedAt = null;
        var includeOnCv = false;

        var stored = await _db.CandidateWhoAmIProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (stored is not null)
        {
            includeOnCv = stored.IncludeOnCv;
        }

        if (unlocked && competency is { IsComplete: true } cScores
            && career is { IsComplete: true } rScores
            && disc is { IsComplete: true } dScores)
        {
            var fingerprint = WhoAmICompleteness.Fingerprint(cScores, rScores, dScores);
            if (stored is not null
                && string.Equals(stored.InputFingerprint, fingerprint, StringComparison.Ordinal)
                && WhoAmIStoryBuilder.Sanitize(stored.StoryText) is { } cachedStory)
            {
                story = cachedStory;
                keywords = LikertAnswerJson.ParseTags(stored.KeywordsJson);
                fromOpenAi = stored.FromOpenAi;
                generatedAt = stored.StoryGeneratedAtUtc;
            }
            else
            {
                var generated = await _generate.GenerateAsync(cScores, rScores, dScores, cancellationToken);
                story = generated.Story;
                keywords = generated.Keywords;
                fromOpenAi = generated.FromOpenAi;
                generatedAt = DateTime.UtcNow;
                if (persistStory)
                {
                    var track = stored;
                    if (track is null)
                    {
                        track = NewRow(userId);
                        _db.CandidateWhoAmIProfiles.Add(track);
                    }

                    track.StoryText = story ?? "";
                    track.KeywordsJson = JsonSerializer.Serialize(keywords, Json);
                    track.InputFingerprint = fingerprint;
                    track.FromOpenAi = fromOpenAi;
                    track.StoryGeneratedAtUtc = generatedAt;
                    track.UpdatedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    includeOnCv = track.IncludeOnCv;
                }
            }
        }

        return new WhoAmIStateDto(
            profileFilled,
            competencyDone,
            careerDone,
            discDone,
            unlocked,
            encouragement,
            story,
            fromOpenAi,
            keywords,
            competency,
            disc,
            includeOnCv,
            generatedAt);
    }

    internal static LobsyCvWhoAmI ToAttachment(
        string story,
        IReadOnlyList<string> keywords,
        CompetencyScores competency,
        DiscScores disc)
        => new(
            story,
            keywords,
            CompetencyTestCatalog.CategoryCodes
                .Select(code => new LobsyCvScoreBar(WhoAmIKeywords.EverydayCompetency(code), competency.Get(code)))
                .ToList(),
            DiscTestCatalog.CategoryCodes
                .Select(code => new LobsyCvScoreBar(DiscTestCatalog.EverydayLabel(code), disc.Get(code)))
                .ToList());

    private static CandidateWhoAmIProfile NewRow(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static string Encouragement(
        bool profile,
        bool competency,
        bool career,
        bool disc)
    {
        if (WhoAmICompleteness.IsUnlocked(profile, competency, career, disc))
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

        return "Rond de gedragsanalyse af (gratis Quick-Scan of de uitgebreide versie). Daarna is je rapport klaar.";
    }
}
