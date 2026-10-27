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
        var lang = NormalizeLanguage(language);
        var storyView = story.Render(result, progress, lang);
        var dream = story.RenderDreamRoute(result, progress, ctx, lang);
        var likes = ResolveChipLabels(ParseTagList(progress?.LikesJson), lang);
        var dislikes = ResolveChipLabels(ParseTagList(progress?.DislikesJson), lang);
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
        var cards = PupilPassportJobCards.Build(jobKeys, likes, ctx.Set, lang);
        var strengths = PupilPassportStrengths.For(result, lang);
        var discovery = PupilPassportLocalizedCopy.DiscoveryQuestions(dream.JobKey, holland, lang);
        var tips = PupilPassportLocalizedCopy.HomeClassTips(dream.JobKey, riasecLetter, lang);

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
        string? sideSub = null;
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
            (sideJobLine, sideSub) = SideJobAvailabilityParts(progress?.VoSideJobAvailabilityJson);
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

        string? levelSub = null;
        string? favSub = null;
        string? travelSub = null;
        if (variant == PupilPassportVariant.Vo)
        {
            var examYear = date.Year + Math.Max(1, 6 - ctx.Year);
            levelSub = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                PupilPassportCopy.Get("LeerlingPassport.Vo.Info.ExamAfterHbo"),
                examYear);
            if (favSubjects is { Count: > 2 })
            {
                favSub = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    PupilPassportCopy.Get("LeerlingPassport.Vo.Info.SubjectsAlso"),
                    favSubjects[2]);
            }

            if (!string.IsNullOrWhiteSpace(travelLine))
            {
                travelSub = PupilPassportCopy.Get("LeerlingPassport.Vo.Info.TravelMax");
            }
        }

        return new PupilPassportPdfModel(
            Variant: variant,
            Language: language,
            SchoolName: schoolName,
            ClassLabel: BuildClassLabel(ctx, schoolClass.Name, lang),
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
                ? PupilPassportLocalizedCopy.Get("LeerlingPassport.Footer.G78", lang)
                : PupilPassportLocalizedCopy.Get("LeerlingPassport.Footer.Vo", lang),
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
            DnaTiles: dnaTiles,
            LevelInfoSubline: levelSub,
            FavoriteSubjectsSubline: favSub,
            SideJobInfoSubline: sideSub,
            TravelInfoSubline: travelSub);
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

    private static string BuildClassLabel(PupilClassContext ctx, string className, string language)
        => ctx.Set == PupilQuestionSet.Groep78
            ? PupilVerhaalLocalized.IsEnglish(language)
                ? $"Group {ctx.Year} · {className}"
                : $"Groep {ctx.Year} · {className}"
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

    private static List<string> ResolveChipLabels(IReadOnlyList<string> keys, string language)
        => keys.Select(k => PupilPortalChipLabels.Label(k, language)).ToList();

    private static string NormalizeLanguage(string language)
        => string.IsNullOrWhiteSpace(language) ? "nl" : language.Trim();

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

    private static (string? main, string? sub) SideJobAvailabilityParts(string? json)
    {
        var keys = ParseTagList(json);
        if (keys.Count == 0)
        {
            return (null, null);
        }

        static string DayLabel(string k) => k switch
        {
            "za" => "Za",
            "zo" => "Zo",
            "wo-middag" => "wo-middag",
            _ => k
        };

        var days = keys.Where(k => !string.Equals(k, "max-8", StringComparison.Ordinal)).Select(DayLabel).ToList();
        var main = days.Count == 0 ? null : string.Join(" + ", days);
        var sub = keys.Any(k => string.Equals(k, "max-8", StringComparison.Ordinal))
            ? PupilPassportCopy.Get("LeerlingPassport.Vo.Info.SideJobHours")
            : null;
        return (main, sub);
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
    public static string Label(string key, string language)
        => PupilVerhaalLocalized.IsEnglish(language) ? English(key) : Dutch(key);

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

    public static string English(string key) => key switch
    {
        "sport" => "Sport",
        "buiten" => "Being outdoors",
        "dieren" => "Animals",
        "gamen" => "Gaming",
        "tekenen" => "Drawing",
        "muziek" => "Music",
        "koken" => "Cooking",
        "techniek" => "Technology",
        "lezen" => "Reading",
        "natuur" => "Nature",
        "puzzels" => "Puzzles",
        "lang-stilzitten" => "Sitting still for long",
        "druk-lawaai" => "Noise and bustle",
        "voor-de-klas" => "Speaking in front of class",
        _ => Dutch(key)
    };
}
