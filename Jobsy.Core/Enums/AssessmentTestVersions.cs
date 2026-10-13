namespace Jobsy.Core.Enums;

/// <summary>
/// Quick-Scan question-set version. Stored on candidate rows; v1 results keep v1 scoring/items forever.
/// </summary>
public static class AssessmentTestVersions
{
    public const int V1 = 1;
    public const int V2 = 2;

    public static int Normalize(int? version)
        => version is V2 ? V2 : V1;

    public static string Label(int version)
        => version == V2 ? "v2" : "v1";
}
