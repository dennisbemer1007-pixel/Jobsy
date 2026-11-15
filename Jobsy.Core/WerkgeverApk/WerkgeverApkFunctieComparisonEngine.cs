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

            var question = ResolveEvidenceQuestion(point, catalog, level);
            if (question is null || !progress.Answers.ContainsKey(question.Id))
            {
                continue;
            }

            if (score >= StrongThreshold)
            {
                var sentence = StrongSentence(question);
                if (sentence is not null)
                {
                    strengths.Add(new WerkgeverApkFunctieStrength(point.Label, sentence));
                }
            }
            else
            {
                var tip = GrowthTooltip(point, profile.DisplayTitle, question);
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

    internal static int? ScoreForPoint(
        WerkgeverApkFunctieProfilePoint point,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        var question = ResolveEvidenceQuestion(point, catalog, level);
        if (question is null)
        {
            return null;
        }

        return progress.Answers.TryGetValue(question.Id, out var v) ? v : null;
    }

    private static PupilMboHboQuestion? ResolveEvidenceQuestion(
        WerkgeverApkFunctieProfilePoint point,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        if (string.IsNullOrWhiteSpace(point.EvidenceQuestionId))
        {
            return null;
        }

        return catalog.Find(PupilMboHboBlock.Competentie, level, point.EvidenceQuestionId);
    }

    private static string? StrongSentence(PupilMboHboQuestion question)
        => $"Jij gaf aan dat {LowerFirst(question.Question)}";

    private static string? GrowthTooltip(
        WerkgeverApkFunctieProfilePoint point,
        string occupationTitle,
        PupilMboHboQuestion question)
    {
        var occ = string.IsNullOrWhiteSpace(occupationTitle) ? "jouw functie" : occupationTitle.ToLowerInvariant();
        return
            $"In je antwoorden gaf je aan dat {LowerFirst(question.Question)} " +
            $"In het profiel van {occ} hoort ook {point.Label.ToLowerInvariant()}. " +
            "Dit is geen oordeel, alleen een punt waarop jij nog kunt groeien.";
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