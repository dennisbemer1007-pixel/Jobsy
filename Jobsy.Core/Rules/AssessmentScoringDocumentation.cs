using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Human-readable scoring reference (also copied into docs/tests handleiding).
/// </summary>
public static class AssessmentScoringDocumentation
{
    public const string LikertRange = "1 = helemaal oneens, 5 = helemaal eens";
    public const string ReverseFormula = "gescoorde waarde = 6 − ruwe waarde (alleen reverse items)";
    public const string PercentFormula =
        "schalenpercent = round((gemiddelde(gescoorde items) − 1) / 4 × 100), clamp 0–100";

    public static string VersionNote(int version) => AssessmentTestVersions.Normalize(version) switch
    {
        AssessmentTestVersions.V3 =>
            "Quick-Scan v3 (tier): 25 free items per instrument (v2 itembank-subset); diepte 150–200 betaald.",
        AssessmentTestVersions.V2 =>
            "Quick-Scan v2 (legacy extended): 40/48/48/40 items; scores blijven geldig op opgeslagen set.",
        _ => "Quick-Scan v1: originele 18–25 items; scores blijven geldig op v1-set."
    };
}
