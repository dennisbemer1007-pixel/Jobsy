namespace Jobsy.Core.Enums;

/// <summary>
/// Quick-Scan question-set version stored on candidate rows and research exports.
/// v1 results keep v1 scoring/items forever; v2 legacy extended; v3 tier (25 free) via <c>TestsV2Enabled</c>.
/// </summary>
public static class AssessmentTestVersions
{
    public const int V1 = 1;

    /// <summary>Legacy extended v2 banks (40/48/48/40).</summary>
    public const int V2 = 2;

    /// <summary>Owner-tier free Quick-Scan: 25 v2 items per instrument (see QuickScanFreeTierV3).</summary>
    public const int V3 = 3;

    public static int Normalize(int? version) => version switch
    {
        V3 => V3,
        V2 => V2,
        _ => V1
    };

    /// <summary>Uses v2 item keys/translations (v2 full bank or v3 subset).</summary>
    public static bool UsesV2ItemBank(int version) => Normalize(version) is V2 or V3;

    public static bool IsFreeTier25(int version) => Normalize(version) == V3;

    /// <summary>Extended v2 bank (pre-tier pilot rows).</summary>
    public static bool IsLegacyExtendedV2(int version) => Normalize(version) == V2;

    public static string Label(int version) => Normalize(version) switch
    {
        V3 => "v3",
        V2 => "v2",
        _ => "v1"
    };
}
