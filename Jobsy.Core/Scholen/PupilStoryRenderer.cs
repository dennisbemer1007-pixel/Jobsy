using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Renders "Dit ben jij" + dream-job fit from fixed templates (no AI).</summary>
public sealed class PupilStoryRenderer : IPupilStoryRenderer
{
    public const int TemplateVersion = PupilStoryTemplates.Version;

    public PupilStoryViewDto Render(PupilResult result, PupilProgress? progress, string language = "nl")
    {
        ArgumentNullException.ThrowIfNull(result);
        var likes = ParseChips(progress?.LikesJson);
        var keys = PupilStoryTemplates.TryParse(result.StoryKeysJson)
                   ?? PupilStoryTemplates.SelectKeys(result, likes);

        var sentences = new List<string>
        {
            PupilVerhaalLocalized.Get(keys.BigFiveKey, language),
            PupilVerhaalLocalized.Get(keys.RiasecKey, language),
            PupilVerhaalLocalized.Get(keys.SchwartzKey, language),
            PupilVerhaalLocalized.Get(keys.CultureKey, language)
        };

        if (keys.LikeChipKeys.Count >= 2)
        {
            var a = ChipInSentence(keys.LikeChipKeys[0], language);
            var b = ChipInSentence(keys.LikeChipKeys[1], language);
            sentences.Add(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                PupilVerhaalLocalized.Get("LeerlingStory.Likes.Two", language), a, b));
        }
        else if (keys.LikeChipKeys.Count == 1)
        {
            sentences.Add(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                PupilVerhaalLocalized.Get("LeerlingStory.Likes.One", language),
                ChipInSentence(keys.LikeChipKeys[0], language)));
        }

        MergeDuplicateBedenkt(sentences, language);
        var body = string.Join(" ", sentences);
        var tiles = new List<PupilStoryTileDto>
        {
            new("competence",
                PupilVerhaalLocalized.Get("LeerlingStory.Tile.ZoBenJij", language),
                PupilVerhaalLocalized.Get(keys.TileCompetenceKey, language)),
            new("riasec",
                PupilVerhaalLocalized.Get("LeerlingStory.Tile.DitDoeJe", language),
                PupilVerhaalLocalized.Get(PupilStoryTemplates.NormalizeTileRiasecKey(keys.TileRiasecKey), language)),
            new("values",
                PupilVerhaalLocalized.Get("LeerlingStory.Tile.Belangrijk", language),
                PupilVerhaalLocalized.Get(keys.TileValueKey, language)),
            new("culture",
                PupilVerhaalLocalized.Get("LeerlingStory.Tile.Thuis", language),
                PupilVerhaalLocalized.Get(keys.TileCultureKey, language)),
        };

        var jobIdeas = keys.JobIdeaKeys
            .Select(PupilRiasecJobIdeas.TitleFor)
            .ToList();

        return new PupilStoryViewDto(
            Title: PupilVerhaalLocalized.Get("LeerlingStory.Title", language),
            Body: body,
            Tiles: tiles,
            JobIdeas: jobIdeas,
            LikeChipKeys: keys.LikeChipKeys.ToList());
    }

    public DreamJobRouteStubDto RenderDreamRoute(
        PupilResult result,
        PupilProgress? progress,
        PupilClassContext classContext,
        string language = "nl")
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
        var have = fit.HaveSentenceKeys.Select(k => ResolveCopy(k, classContext, now, language)).ToList();
        var learn = fit.NeedSentenceKeys.Select(k => ResolveNeedNext(k, classContext, now, language)).ToList();
        var steps = fit.RouteStepKeys.Select(k => ResolveRouteStep(k, classContext, now, language)).ToList();

        return new DreamJobRouteStubDto(
            JobKey: fit.JobKey,
            JobTitle: fit.JobTitle,
            HaveCount: fit.HaveCount,
            TotalCount: fit.TotalCount,
            HaveItems: have,
            LearnItems: learn,
            RouteSteps: steps,
            Encouragement: ResolveCopy(fit.EncouragementKey, classContext, now, language),
            AltRoute: fit.AltRouteKey is null ? null : ResolveCopy(fit.AltRouteKey, classContext, now, language));
    }

    public IReadOnlyList<string> ConversationStarterKeys(PupilResult result)
        => PupilStoryTemplates.ConversationStarterKeys(result)
            .Select(k => PupilVerhaalCopy.TryGet(k, out var t) ? t : k)
            .ToList();

    public IReadOnlyList<string> ClassDiscussionPromptKeys(string? topLetter1 = null, string? topLetter2 = null)
        => PupilStoryTemplates.ClassDiscussionPromptKeys(topLetter1 ?? "S", topLetter2 ?? "R")
            .Select(k => PupilVerhaalCopy.TryGet(k, out var t) ? t : k)
            .ToList();

    private static string ResolveNeedNext(string key, PupilClassContext ctx, string nowLabel, string language)
    {
        var nextKey = key.EndsWith(".Next", StringComparison.Ordinal) ? key : key + ".Next";
        var resolved = ResolveCopy(nextKey, ctx, nowLabel, language);
        return string.Equals(resolved, nextKey, StringComparison.Ordinal)
            ? ResolveCopy(key, ctx, nowLabel, language)
            : resolved;
    }

    private static string ResolveRouteStep(string key, PupilClassContext ctx, string nowLabel, string language)
    {
        var raw = ResolveCopy(key, ctx, nowLabel, language);
        var parts = raw.Split('|', 2);
        return parts.Length == 2 ? $"{parts[0]} · {parts[1]}" : raw;
    }

    private static string ResolveCopy(string key, PupilClassContext ctx, string nowLabel, string language)
        => PupilVerhaalLocalized.Get(key, language, ctx).Replace("{nu}", nowLabel, StringComparison.Ordinal);

    /// <summary>
    /// Innovatie + an artistic sentence both start with "bedenkt graag".
    /// One sentence keeps both ideas.
    /// </summary>
    private static void MergeDuplicateBedenkt(List<string> sentences, string language)
    {
        if (sentences.Count < 2)
        {
            return;
        }

        var first = sentences[0];
        var second = sentences[1];
        if (!first.Contains("bedenkt graag", StringComparison.Ordinal)
            && !first.Contains("like to come up", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!second.Contains("bedenkt graag", StringComparison.Ordinal)
            && !second.Contains("like to come up", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        sentences[0] = PupilVerhaalLocalized.IsEnglish(language)
            ? (second.Contains("find out", StringComparison.OrdinalIgnoreCase)
                ? "You like to come up with new ideas and find out how things work."
                : "You like to come up with new ideas and make something beautiful.")
            : second.Contains("zoekt uit", StringComparison.Ordinal)
                ? "Jij bedenkt graag nieuwe manieren en zoekt uit hoe iets werkt."
                : "Jij bedenkt graag nieuwe manieren en maakt graag iets moois.";
        sentences.RemoveAt(1);
    }

    private static string ChipLabel(string chipKey, string language)
        => PupilPortalChipLabels.Label(chipKey, language);

    private static string ChipInSentence(string chipKey, string language)
    {
        var label = ChipLabel(chipKey, language);
        var culture = PupilVerhaalLocalized.IsEnglish(language)
            ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            : System.Globalization.CultureInfo.GetCultureInfo("nl-NL");
        return label.ToLower(culture);
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
