using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Maps A/B progress to manager comparison labels (internal 1–5; never shown as cijfer).</summary>
internal static class WerkgeverApkEmployeeDimensionScores
{
    public static int? SelfScore(
        WerkgeverApkManagerQuestion question,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeAbCatalog catalog,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        _ = level;
        if (!string.IsNullOrWhiteSpace(question.EmployeeCompetenceDimension))
        {
            var bipolar = WerkgeverApkAbScoring.BipolarScore(progress, catalog, question.EmployeeCompetenceDimension);
            if (bipolar is not null)
            {
                return bipolar;
            }
        }

        var counts = WerkgeverApkAbScoring.CountDimensions(progress, catalog);
        if (!string.IsNullOrWhiteSpace(question.EmployeeCompetenceDimension))
        {
            return WerkgeverApkAbScoring.RelativeLikertScore(counts, question.EmployeeCompetenceDimension);
        }

        if (!string.IsNullOrWhiteSpace(question.EmployeeValueBarKey))
        {
            return WerkgeverApkAbScoring.RelativeLikertScore(counts, question.EmployeeValueBarKey);
        }

        return null;
    }
}