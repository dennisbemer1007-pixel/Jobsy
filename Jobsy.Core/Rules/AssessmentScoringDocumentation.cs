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

    public static string VersionNote(int version)
        => version == AssessmentTestVersions.V2
            ? "Quick-Scan v2: uitgebreidere itemsets per schaal; zie handleiding v2."
            : "Quick-Scan v1: originele 18–25 items; scores blijven geldig op v1-set.";
}
