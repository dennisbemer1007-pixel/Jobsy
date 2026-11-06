using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Scholen;

/// <summary>Journey strip + scene context for G78 pause/stop when the pupil session is gone.</summary>
public sealed record PupilGroep78JourneySnapshot(
    string? NodeKey,
    string? MetaLine,
    bool SceneMetaIsHtml,
    bool ShowStrip,
    bool UseResultSceneBand)
{
    public static PupilGroep78JourneySnapshot FromProgress(
        PupilProgressStateDto? progress,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey)
    {
        if (progress?.QuestionSet != PupilQuestionSet.Groep78)
        {
            return Hidden;
        }

        var nodeKey = ResolveNodeKey(progress);
        if (progress.Completed)
        {
            return new PupilGroep78JourneySnapshot(
                "dit-ben-jij",
                culture["Leerling.G78.Result.JourneyMeta"],
                SceneMetaIsHtml: true,
                ShowStrip: true,
                UseResultSceneBand: true);
        }

        var meta = BuildPauseMeta(progress, culture, registry, themeKey, nodeKey);
        return new PupilGroep78JourneySnapshot(
            nodeKey,
            meta,
            SceneMetaIsHtml: false,
            ShowStrip: true,
            UseResultSceneBand: string.Equals(nodeKey, "dit-ben-jij", StringComparison.OrdinalIgnoreCase));
    }

    public static PupilGroep78JourneySnapshot FromPauseQuery(
        IQueryCollection query,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey)
    {
        if (!string.Equals(query["g78"], "1", StringComparison.Ordinal))
        {
            return Hidden;
        }

        var nodeKey = query["jn"].ToString();
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return Hidden;
        }

        if (string.Equals(query["jc"], "1", StringComparison.Ordinal))
        {
            return new PupilGroep78JourneySnapshot(
                "dit-ben-jij",
                culture["Leerling.G78.Result.JourneyMeta"],
                SceneMetaIsHtml: true,
                ShowStrip: true,
                UseResultSceneBand: true);
        }

        var answered = int.TryParse(query["ja"], out var a) ? a : 0;
        var total = int.TryParse(query["jt"], out var t) ? t : 60;
        var meta = string.Format(
            culture["Leerling.G78.Journey.PauseMeta"],
            StripTitle(culture, registry, themeKey, nodeKey),
            answered,
            total);
        return new PupilGroep78JourneySnapshot(
            nodeKey,
            meta,
            SceneMetaIsHtml: false,
            ShowStrip: true,
            UseResultSceneBand: false);
    }

    public static void AppendPauseQuery(StringBuilder qs, PupilProgressStateDto progress)
    {
        AppendPauseQueryValues(qs, ResolveNodeKey(progress), progress.AnsweredCount, progress.TotalItems, progress.Completed);
    }

    public static void AppendPauseQueryFromForm(StringBuilder qs, IFormCollection form)
    {
        var nodeKey = form["jn"].ToString();
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return;
        }

        _ = int.TryParse(form["ja"].ToString(), out var answered);
        _ = int.TryParse(form["jt"].ToString(), out var total);
        var completed = string.Equals(form["jc"], "1", StringComparison.Ordinal);
        AppendPauseQueryValues(qs, nodeKey, answered, total, completed);
    }

    private static void AppendPauseQueryValues(StringBuilder qs, string nodeKey, int answered, int total, bool completed)
    {
        qs.Append("&jn=").Append(Uri.EscapeDataString(nodeKey));
        qs.Append("&ja=").Append(answered.ToString(System.Globalization.CultureInfo.InvariantCulture));
        qs.Append("&jt=").Append(total.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (completed)
        {
            qs.Append("&jc=1");
        }
    }

    public static (string Stem, string Position) SceneBand(string? themeKey, PupilGroep78JourneySnapshot snapshot)
    {
        if (snapshot.UseResultSceneBand)
        {
            return PupilJourneyTheme.ResultSceneBand(themeKey);
        }

        var worldKey = BandWorldKey(snapshot.NodeKey);
        return (PupilJourneyTheme.WorldBandImageStem(themeKey, worldKey), "center 30%");
    }

    private static string BandWorldKey(string? nodeKey)
    {
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return "koraalrif";
        }

        return nodeKey switch
        {
            "schelpenrij" or "schatkaart" => "grot",
            "vuurtorenlampen" => "vuurtoren",
            "pauze-eiland" => "pauze-eiland",
            "dit-ben-jij" => "lagune",
            _ => nodeKey,
        };
    }

    /// <summary>Fields for G78 pause POST when API progress is unavailable after sign-out.</summary>
    public static (string NodeKey, int Answered, int Total, bool Completed)? PauseFormFields(PupilProgressStateDto? progress)
    {
        if (progress?.QuestionSet != PupilQuestionSet.Groep78)
        {
            return null;
        }

        return (ResolveNodeKey(progress), progress.AnsweredCount, progress.TotalItems, progress.Completed);
    }

    private static string ResolveNodeKey(PupilProgressStateDto progress)
    {
        if (progress.Completed)
        {
            return "dit-ben-jij";
        }

        return progress.NextStep switch
        {
            "puzzle" when !string.IsNullOrWhiteSpace(progress.NextPuzzleKey) => progress.NextPuzzleKey,
            "island" => "pauze-eiland",
            "vakken" => "lagune",
            "done" => "dit-ben-jij",
            _ => string.IsNullOrWhiteSpace(progress.CurrentWorldKey) ? "koraalrif" : progress.CurrentWorldKey,
        };
    }

    private static string BuildPauseMeta(
        PupilProgressStateDto progress,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey,
        string nodeKey)
        => string.Format(
            culture["Leerling.G78.Journey.PauseMeta"],
            StripTitle(culture, registry, themeKey, nodeKey),
            progress.AnsweredCount,
            progress.TotalItems);

    private static string StripTitle(
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string? themeKey,
        string nodeKey)
    {
        var def = registry.Get(PupilQuestionSet.Groep78);
        var nodes = PupilGroep78Journey.BuildNodes(def);
        var kind = PupilGroep78JourneyNodeKind.World;
        foreach (var node in nodes)
        {
            if (string.Equals(node.Key, nodeKey, StringComparison.OrdinalIgnoreCase))
            {
                kind = node.Kind;
                break;
            }
        }

        return culture[PupilJourneyTheme.StripTitleKey(themeKey, new PupilGroep78JourneyNode(nodeKey, kind))];
    }

    private static readonly PupilGroep78JourneySnapshot Hidden = new(null, null, false, false, false);
}
