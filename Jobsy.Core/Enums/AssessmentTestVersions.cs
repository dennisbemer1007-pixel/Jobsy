namespace Jobsy.Core.Enums;

/// <summary>
/// Quick-scan questionnaire version stored on research exports (v1 production; v2 via TestsV2Enabled on PR #647).
/// </summary>
public static class AssessmentTestVersions
{
    public const int V1 = 1;
    public const int V2 = 2;

    public static int Normalize(int version)
        => version == V2 ? V2 : V1;
}
