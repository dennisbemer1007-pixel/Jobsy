using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public sealed record WerkgeverApkFunctieStrength(string Label, string Sentence);

public sealed record WerkgeverApkFunctieGrowth(string Label, string Tooltip);

public sealed record WerkgeverApkFunctieComparisonDto(
    string OccupationTitle,
    IReadOnlyList<WerkgeverApkFunctieStrength> Strengths,
    IReadOnlyList<WerkgeverApkFunctieGrowth> GrowthAreas);

public static class WerkgeverApkFunctieComparisonEngine
{
    private const int StrongThreshold = 4;
    private const int MaxGrowth = 5;
    private const int MaxStrength = 5;

    public static WerkgeverApkFunctieComparisonDto? Build(
        string? escoOccupationId,
        PupilMboHboProgressState progress,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        var profile = WerkgeverApkFunctieProfileCatalog.Resolve(escoOccupationId);
        if (profile.Points.Count == 0)
        {
            return null;
        }

        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var strengths = new List<WerkgeverApkFunctieStrength>();
        var growth = new List<(int Score, WerkgeverApkFunctieGrowth Item)>();

        foreach (var point in profile.Points)
        {
            var score = ScoreForPoint(point, progress, catalog, level);
            if (score is null)
            {
                continue;
            }

            if (score >= StrongThreshold)
            {
                var sentence = StrongSentence(point, progress, catalog, level);
                if (sentence is not null)
                {
                    strengths.Add(new WerkgeverApkFunctieStrength(point.Label, sentence));
                }
            }
            else
            {
                var tip = GrowthTooltip(point, profile.DisplayTitle, progress, catalog, level);
                if (tip is not null)
                {
                    growth.Add((score.Value, new WerkgeverApkFunctieGrowth(point.Label, tip)));
                }
            }
        }

        var orderedStrengths = strengths.Take(MaxStrength).ToList();
        var orderedGrowth = growth
            .OrderBy(g => g.Score)
            .ThenBy(g => g.Item.Label, StringComparer.OrdinalIgnoreCase)
            .Take(MaxGrowth)
            .Select(g => g.Item)
            .ToList();

        return new WerkgeverApkFunctieComparisonDto(profile.DisplayTitle, orderedStrengths, orderedGrowth);
    }

    private static int? ScoreForPoint(
        WerkgeverApkFunctieProfilePoint point,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        if (string.IsNullOrWhiteSpace(point.CompetenceDimension))
        {
            return null;
        }

        var values = new List<int>();
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, level))
        {
            if (!string.Equals(q.Dimension, point.CompetenceDimension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (progress.Answers.TryGetValue(q.Id, out var v))
            {
                values.Add(v);
            }
        }

        if (values.Count == 0)
        {
            return null;
        }

        return (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
    }

    private static string? StrongSentence(
        WerkgeverApkFunctieProfilePoint point,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        var best = BestQuestion(point, progress, catalog, level, highest: true);
        if (best is null)
        {
            return null;
        }

        return $"Jij gaf aan dat {LowerFirst(best.Question)}";
    }

    private static string? GrowthTooltip(
        WerkgeverApkFunctieProfilePoint point,
        string occupationTitle,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        var lowest = BestQuestion(point, progress, catalog, level, highest: false);
        if (lowest is null)
        {
            return null;
        }

        var occ = string.IsNullOrWhiteSpace(occupationTitle) ? "jouw functie" : occupationTitle.ToLowerInvariant();
        return
            $"In je antwoorden gaf je aan dat {LowerFirst(lowest.Question)} " +
            $"In het profiel van {occ} hoort ook {point.Label.ToLowerInvariant()}. " +
            "Dit is geen oordeel, alleen een punt waarop jij nog kunt groeien.";
    }

    private static PupilMboHboQuestion? BestQuestion(
        WerkgeverApkFunctieProfilePoint point,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level,
        bool highest)
    {
        PupilMboHboQuestion? pick = null;
        var pickScore = highest ? -1 : 6;
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, level))
        {
            if (!string.Equals(q.Dimension, point.CompetenceDimension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!progress.Answers.TryGetValue(q.Id, out var v))
            {
                continue;
            }

            if (highest && v > pickScore)
            {
                pickScore = v;
                pick = q;
            }
            else if (!highest && v < pickScore)
            {
                pickScore = v;
                pick = q;
            }
        }

        return pick;
    }

    private static string LowerFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var t = text.TrimEnd('.');
        return char.ToLowerInvariant(t[0]) + t[1..] + ".";
    }
}