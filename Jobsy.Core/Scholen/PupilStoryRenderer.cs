using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Renders "Dit ben jij" + dream-job fit from fixed templates (no AI).</summary>
public sealed class PupilStoryRenderer : IPupilStoryRenderer
{
    public const int TemplateVersion = PupilStoryTemplates.Version;

    public PupilStoryViewDto Render(PupilResult result, PupilProgress? progress)
    {
        ArgumentNullException.ThrowIfNull(result);
        var likes = ParseChips(progress?.LikesJson);
        var keys = PupilStoryTemplates.TryParse(result.StoryKeysJson)
                   ?? PupilStoryTemplates.SelectKeys(result, likes);

        var sentences = new List<string>
        {
            PupilVerhaalCopy.Get(keys.BigFiveKey),
            PupilVerhaalCopy.Get(keys.RiasecKey),
            PupilVerhaalCopy.Get(keys.SchwartzKey),
            PupilVerhaalCopy.Get(keys.CultureKey)
        };

        if (keys.LikeChipKeys.Count >= 2)
        {
            var a = ChipLabel(keys.LikeChipKeys[0]);
            var b = ChipLabel(keys.LikeChipKeys[1]);
            sentences.Add(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                PupilVerhaalCopy.Get("LeerlingStory.Likes.Two"), a, b));
        }
        else if (keys.LikeChipKeys.Count == 1)
        {
            sentences.Add(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                PupilVerhaalCopy.Get("LeerlingStory.Likes.One"),
                ChipLabel(keys.LikeChipKeys[0])));
        }

        var body = string.Join(" ", sentences);
        var tiles = new List<PupilStoryTileDto>
        {
            new("competence",
                PupilVerhaalCopy.Get("LeerlingStory.Tile.ZoBenJij"),
                PupilVerhaalCopy.Get(keys.TileCompetenceKey)),
            new("riasec",
                PupilVerhaalCopy.Get("LeerlingStory.Tile.DitDoeJe"),
                PupilVerhaalCopy.Get(keys.TileRiasecKey)),
            new("values",
                PupilVerhaalCopy.Get("LeerlingStory.Tile.Belangrijk"),
                PupilVerhaalCopy.Get(keys.TileValueKey)),
            new("culture",
                PupilVerhaalCopy.Get("LeerlingStory.Tile.Thuis"),
                PupilVerhaalCopy.Get(keys.TileCultureKey)),
        };

        var jobIdeas = keys.JobIdeaKeys
            .Select(PupilRiasecJobIdeas.TitleFor)
            .ToList();

        return new PupilStoryViewDto(
            Title: PupilVerhaalCopy.Get("LeerlingStory.Title"),
            Body: body,
            Tiles: tiles,
            JobIdeas: jobIdeas,
            LikeChipKeys: keys.LikeChipKeys.ToList());
    }

    public DreamJobRouteStubDto RenderDreamRoute(PupilResult result, PupilProgress? progress, PupilClassContext classContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(classContext);
        var key = result.DreamJobKey ?? progress?.DreamJobKey;
        if (string.IsNullOrWhiteSpace(key)
            || string.Equals(key, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            return new DreamJobRouteStubDto(
                JobKey: key,
                JobTitle: null,
                HaveCount: 0,
                TotalCount: 5,
                HaveItems: [],
                LearnItems: [],
                RouteSteps: [],
                Encouragement: null,
                AltRoute: null);
        }

        var fit = PupilDreamJobFit.Evaluate(result, progress, key);
        if (fit is null)
        {
            var title = DreamJobCatalog.All.FirstOrDefault(j =>
                string.Equals(j.Key, key, StringComparison.OrdinalIgnoreCase))?.TitleNl;
            return new DreamJobRouteStubDto(key, title, 0, 5, [], [], [], null, null);
        }

        var now = PupilDreamJobRoutes.NowLabel(classContext);
        var have = fit.HaveSentenceKeys.Select(k => ResolveCopy(k, classContext, now)).ToList();
        var learn = fit.NeedSentenceKeys.Select(k => ResolveNeedNext(k, classContext, now)).ToList();
        var steps = fit.RouteStepKeys.Select(k => ResolveRouteStep(k, classContext, now)).ToList();

        return new DreamJobRouteStubDto(
            JobKey: fit.JobKey,
            JobTitle: fit.JobTitle,
            HaveCount: fit.HaveCount,
            TotalCount: fit.TotalCount,
            HaveItems: have,
            LearnItems: learn,
            RouteSteps: steps,
            Encouragement: ResolveCopy(fit.EncouragementKey, classContext, now),
            AltRoute: fit.AltRouteKey is null ? null : ResolveCopy(fit.AltRouteKey, classContext, now));
    }

    public IReadOnlyList<string> ConversationStarterKeys(PupilResult result)
        => PupilStoryTemplates.ConversationStarterKeys(result)
            .Select(k => PupilVerhaalCopy.TryGet(k, out var t) ? t : k)
            .ToList();

    public IReadOnlyList<string> ClassDiscussionPromptKeys(string? topLetter1 = null, string? topLetter2 = null)
        => PupilStoryTemplates.ClassDiscussionPromptKeys(topLetter1 ?? "S", topLetter2 ?? "R")
            .Select(k => PupilVerhaalCopy.TryGet(k, out var t) ? t : k)
            .ToList();

    private static string ResolveNeedNext(string key, PupilClassContext ctx, string nowLabel)
    {
        var nextKey = key.EndsWith(".Next", StringComparison.Ordinal) ? key : key + ".Next";
        var resolved = ResolveCopy(nextKey, ctx, nowLabel);
        return string.Equals(resolved, nextKey, StringComparison.Ordinal)
            ? ResolveCopy(key, ctx, nowLabel)
            : resolved;
    }

    private static string ResolveRouteStep(string key, PupilClassContext ctx, string nowLabel)
    {
        var raw = ResolveCopy(key, ctx, nowLabel);
        var parts = raw.Split('|', 2);
        return parts.Length == 2 ? $"{parts[0]} — {parts[1]}" : raw;
    }

    private static string ResolveCopy(string key, PupilClassContext ctx, string nowLabel)
        => PupilVerhaalCopy.Get(key, ctx).Replace("{nu}", nowLabel, StringComparison.Ordinal);

    private static string ChipLabel(string chipKey)
    {
        var chip = PupilInterestChipCatalog.LikeChips
            .FirstOrDefault(c => string.Equals(c.Key, chipKey, StringComparison.OrdinalIgnoreCase));
        if (chip is null)
        {
            return chipKey;
        }

        // Chip labels live in UiStringsScholen; use a short fallback from key.
        return chipKey switch
        {
            "sport" => "Sport",
            "buiten" => "Buiten zijn",
            "dieren" => "Dieren",
            "gamen" => "Gamen",
            "tekenen" => "Tekenen",
            "muziek" => "Muziek",
            "koken" => "Koken of bakken",
            "fietsen-repareren" => "Fietsen repareren",
            "bouwen" => "Bouwen & knutselen",
            "techniek" => "Techniek",
            "lezen" => "Lezen",
            "dansen" => "Dansen",
            "theater" => "Theater",
            "filmpjes" => "Filmpjes maken",
            "mode" => "Mode",
            "kleine-kinderen" => "Kleine kinderen",
            "natuur" => "Natuur",
            "autos" => "Auto's & motoren",
            "computers" => "Computers",
            "puzzels" => "Puzzels",
            "rekenen" => "Rekenen",
            "talen" => "Talen",
            "reizen" => "Reizen",
            "programmeren" => "Programmeren",
            _ => chipKey
        };
    }

    private static List<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
