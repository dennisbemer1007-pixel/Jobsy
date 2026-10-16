using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.PupilPassport;

public sealed class PupilPassportPdfModelBuilder(IPupilStoryRenderer story)
{
    public PupilPassportPdfModel Build(
        PupilCode code,
        SchoolClass schoolClass,
        string schoolName,
        string displayCode,
        string? displayNameOnPdf,
        string language = "nl")
    {
        ArgumentNullException.ThrowIfNull(code.Result);
        var result = code.Result!;
        var progress = code.Progress;
        var ctx = PupilClassContext.From(schoolClass);
        var variant = ctx.Set == PupilQuestionSet.Groep78 ? PupilPassportVariant.Groep78 : PupilPassportVariant.Vo;
        var storyView = story.Render(result, progress);
        var dream = story.RenderDreamRoute(result, progress, ctx);
        var likes = ResolveChipLabels(ParseTagList(progress?.LikesJson));
        var dislikes = ResolveChipLabels(ParseTagList(progress?.DislikesJson));
        var likeKeys = ParseTagList(progress?.LikesJson);
        var keys = PupilStoryTemplates.TryParse(result.StoryKeysJson)
                   ?? PupilStoryTemplates.SelectKeys(result, likeKeys, ctx.Set);
        var holland = result.HollandCode ?? "";
        var riasecLetter = string.IsNullOrWhiteSpace(holland) ? 'S' : char.ToUpperInvariant(holland[0]);

        var worlds = BuildWorlds(storyView);
        var routeSteps = dream.RouteSteps
            .Select((step, i) =>
            {
                var parts = step.Split('·', 2, StringSplitOptions.TrimEntries);
                var title = parts[0];
                var detail = parts.Length > 1 ? parts[1] : "";
                var isGoal = i == dream.RouteSteps.Count - 1;
                return new PupilPassportRouteStep(title, detail, isGoal);
            })
            .ToList();

        var jobKeys = keys.JobIdeaKeys;
        var cards = PupilPassportJobCards.Build(jobKeys, likes, ctx.Set);
        var strengths = PupilPassportStrengths.For(result);
        var discovery = PupilPassportCopy.DiscoveryQuestions(dream.JobKey, holland);
        var tips = PupilPassportCopy.HomeClassTips(dream.JobKey, riasecLetter);

        var date = DateOnly.FromDateTime(result.CompletedAtUtc == default
            ? DateTime.UtcNow
            : result.CompletedAtUtc);

        var dnaTiles = storyView.Tiles
            .Select(t => (t.KidLabel, t.Explanation))
            .ToList();

        IReadOnlyList<PupilPassportSideJobType>? sideJobs = null;
        IReadOnlyList<PupilPassportSectorFit>? sectors = null;
        IReadOnlyList<string>? favSubjects = null;
        IReadOnlyList<string>? learningGoals = null;
        string? sideJobLine = null;
        string? travelLine = null;
        string? ownWords = null;
        string? profileLine = null;

        if (variant == PupilPassportVariant.Vo)
        {
            favSubjects = ParseTagList(progress?.VoFavoriteSubjectsJson).Select(SubjectLabel).ToList();
            if (favSubjects.Count == 0)
            {
                favSubjects = null;
            }

            profileLine = ProfileLabel(progress?.VoProfileKey);
            sideJobLine = SideJobAvailabilityLabel(progress?.VoSideJobAvailabilityJson);
            travelLine = TravelLabel(progress?.VoTravelModeKey);
            ownWords = string.IsNullOrWhiteSpace(progress?.VoOwnWords) ? null : progress!.VoOwnWords!.Trim();
            learningGoals = ParseTagList(progress?.VoLearningGoalsJson);
            if (learningGoals.Count == 0)
            {
                learningGoals = null;
            }

            var age = VoAssumedAge(ctx.Level);
            sideJobs = PupilVoSideJobCatalog.Pick(holland, likeKeys, age);
            sectors = PupilVoSectorFits.Build(holland, likeKeys);
        }

        return new PupilPassportPdfModel(
            Variant: variant,
            Language: language,
            SchoolName: schoolName,
            ClassLabel: BuildClassLabel(ctx, schoolClass.Name),
            DisplayCode: displayCode,
            Date: date,
            WorldsCompleted: 4,
            WorldsTotal: 4,
            DisplayNameOnPdf: displayNameOnPdf,
            StoryBody: storyView.Body,
            Worlds: worlds,
            DreamJobTitle: dream.JobTitle,
            DreamJobKey: dream.JobKey,
            DreamHaveCount: dream.HaveCount,
            DreamTotalCount: dream.TotalCount,
            DreamHaveItems: dream.HaveItems,
            DreamLearnItems: dream.LearnItems,
            RouteSteps: routeSteps,
            AltRouteNote: dream.AltRoute,
            Likes: likes,
            Dislikes: dislikes,
            LikeOwnWord: progress?.LikeOtherWord,
            DislikeOwnWord: progress?.DislikeOtherWord,
            Strengths: strengths,
            OccupationCards: cards,
            DiscoveryQuestions: discovery,
            HomeClassTips: tips,
            FooterPrivacy: variant == PupilPassportVariant.Groep78
                ? PupilPassportCopy.Get("LeerlingPassport.Footer.G78")
                : PupilPassportCopy.Get("LeerlingPassport.Footer.Vo"),
            ArchetypeTagline: variant == PupilPassportVariant.Vo
                ? PupilPassportCopy.ArchetypeTagline(holland)
                : null,
            LevelLine: variant == PupilPassportVariant.Vo ? LevelShort(ctx.Level, ctx.Year) : null,
            ProfileLine: profileLine,
            LocationLine: variant == PupilPassportVariant.Vo ? schoolName : null,
            FavoriteSubjects: favSubjects,
            SideJobAvailabilityLine: sideJobLine,
            TravelLine: travelLine,
            SideJobTypes: sideJobs,
            SectorFits: sectors,
            LearningGoals: learningGoals,
            OwnWords: ownWords,
            DnaTiles: dnaTiles);
    }

    private static IReadOnlyList<PupilPassportWorldStamp> BuildWorlds(PupilStoryViewDto story)
    {
        var tones = new[] { "w1", "w2", "w3", "w4" };
        var stamps = new List<PupilPassportWorldStamp>();
        var n = 0;
        foreach (var tile in story.Tiles)
        {
            var worldNo = n + 1;
            stamps.Add(new PupilPassportWorldStamp(
                WorldTitle(worldNo),
                tile.KidLabel,
                tile.Explanation,
                worldNo,
                tones[n % tones.Length]));
            n++;
        }

        return stamps;
    }

    private static string WorldTitle(int n) => n switch
    {
        1 => "Koraalrif",
        2 => "Schatgrot",
        3 => "Vuurtoren",
        4 => "Lagune",
        _ => $"Wereld {n}"
    };

    private static string BuildClassLabel(PupilClassContext ctx, string className)
        => ctx.Set == PupilQuestionSet.Groep78
            ? $"Groep {ctx.Year} · {className}"
            : $"{LevelShort(ctx.Level, ctx.Year)} · {className}";

    private static string LevelShort(SchoolLevel level, int year) => level switch
    {
        SchoolLevel.Havo => $"Havo {year}",
        SchoolLevel.Vwo => $"Vwo {year}",
        SchoolLevel.Mavo => $"Mavo {year}",
        SchoolLevel.VmboB => $"Vmbo basis {year}",
        SchoolLevel.VmboK => $"Vmbo kader {year}",
        SchoolLevel.VmboGt => $"Vmbo gl/t {year}",
        SchoolLevel.Groep78 => $"Groep {year}",
        _ => $"Klas {year}"
    };

    private static int VoAssumedAge(SchoolLevel level) => level switch
    {
        SchoolLevel.Groep78 => 13,
        SchoolLevel.VmboB or SchoolLevel.VmboK or SchoolLevel.VmboGt or SchoolLevel.Mavo => 16,
        SchoolLevel.Havo => 17,
        SchoolLevel.Vwo => 17,
        _ => 16
    };

    private static List<string> ParseTagList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<string> ResolveChipLabels(IReadOnlyList<string> keys)
        => keys.Select(PupilPortalChipLabels.Dutch).ToList();

    private static string? ProfileLabel(string? key) => key switch
    {
        "natuur-gezondheid" => "Profiel Natuur & Gezondheid",
        "economie" => "Profiel Economie",
        "cultuur-maatschappij" => "Profiel Cultuur & Maatschappij",
        "nt" => "Profiel Natuur & Techniek",
        _ => string.IsNullOrWhiteSpace(key) ? null : key
    };

    private static string? TravelLabel(string? key) => key switch
    {
        "fiets" => "Fiets",
        "ov" => "OV",
        "fiets-ov" => "Fiets + OV",
        _ => string.IsNullOrWhiteSpace(key) ? null : key
    };

    private static string? SideJobAvailabilityLabel(string? json)
    {
        var keys = ParseTagList(json);
        if (keys.Count == 0)
        {
            return null;
        }

        return string.Join(" · ", keys.Select(k => k switch
        {
            "za" => "Za",
            "zo" => "Zo",
            "wo-middag" => "wo-middag",
            "max-8" => "max. 8 uur per week",
            _ => k
        }));
    }

    private static string SubjectLabel(string key) => key switch
    {
        "biologie" => "Biologie",
        "tekenen" => "Tekenen",
        "engels" => "Engels",
        "natuurkunde" => "Natuurkunde",
        "economie" => "Economie",
        _ => key
    };
}

/// <summary>Shared chip labels (keeps builder free of Infrastructure).</summary>
public static class PupilPortalChipLabels
{
    public static string Dutch(string key) => key switch
    {
        "sport" => "Sport",
        "buiten" => "Buiten",
        "dieren" => "Dieren",
        "gamen" => "Gamen",
        "tekenen" => "Tekenen",
        "muziek" => "Muziek",
        "koken" => "Koken",
        "techniek" => "Techniek",
        "lezen" => "Lezen",
        "natuur" => "Natuur",
        "puzzels" => "Puzzels",
        "lang-stilzitten" => "Lang stilzitten",
        "druk-lawaai" => "Druk en lawaai",
        "voor-de-klas" => "Voor de klas staan",
        _ => key
    };
}
