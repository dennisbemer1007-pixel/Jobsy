using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Maps employee MBO progress to the ten manager comparison labels (Likert 1–5).</summary>
internal static class WerkgeverApkEmployeeDimensionScores
{
    public static int? SelfScore(
        WerkgeverApkManagerQuestion question,
        PupilMboHboProgressState progress,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        var catalog = PupilMboHboCatalog.Default;
        if (!string.IsNullOrWhiteSpace(question.EmployeeCompetenceDimension))
        {
            return AverageForDimension(
                PupilMboHboBlock.Competentie,
                progress,
                level,
                catalog,
                question.EmployeeCompetenceDimension);
        }

        if (!string.IsNullOrWhiteSpace(question.EmployeeValueBarKey))
        {
            return AverageForValueBarKey(progress, level, catalog, question.EmployeeValueBarKey);
        }

        return null;
    }

    private static int? AverageForDimension(
        PupilMboHboBlock block,
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog catalog,
        string dimension)
    {
        var values = new List<int>();
        foreach (var q in catalog.QuestionsFor(block, level))
        {
            if (!string.Equals(q.Dimension, dimension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (progress.Answers.TryGetValue(q.Id, out var v))
            {
                values.Add(v);
            }
        }

        return AverageRounded(values);
    }

    private static int? AverageForValueBarKey(
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog catalog,
        string barKey)
    {
        var values = new List<int>();
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Werkwaarden, level))
        {
            var key = PupilMboHboDimensionLabels.BarGroupKey(q.Dimension);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!DimensionKeyMatches(key, barKey) && !DimensionKeyMatches(q.Dimension, barKey))
            {
                continue;
            }

            if (progress.Answers.TryGetValue(q.Id, out var v))
            {
                values.Add(v);
            }
        }

        return AverageRounded(values);
    }

    private static bool DimensionKeyMatches(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle))
        {
            return false;
        }

        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || needle.Contains(haystack, StringComparison.OrdinalIgnoreCase);
    }

    private static int? AverageRounded(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
    }
}
