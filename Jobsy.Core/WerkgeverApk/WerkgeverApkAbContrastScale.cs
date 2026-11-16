namespace Jobsy.Core.WerkgeverApk;

/// <summary>Five-point bipolar scale (mockup 13d): Zeker A … Zeker B.</summary>
public static class WerkgeverApkAbContrastScale
{
    public const int FullA = 1;
    public const int LeanA = 2;
    public const int Both = 3;
    public const int LeanB = 4;
    public const int FullB = 5;

    public const int FullWeight = 2;
    public const int LeanWeight = 1;

    public static readonly IReadOnlyList<(int Value, string LabelNl)> Labels =
    [
        (1, "Zeker A"),
        (2, "Liever A"),
        (3, "Allebei"),
        (4, "Liever B"),
        (5, "Zeker B"),
    ];

    public static int WeightTowardA(int answerValue) => answerValue switch
    {
        FullA => FullWeight,
        LeanA => LeanWeight,
        _ => 0,
    };

    public static int WeightTowardB(int answerValue) => answerValue switch
    {
        FullB => FullWeight,
        LeanB => LeanWeight,
        _ => 0,
    };

    /// <summary>1 = zeker B-pool, 5 = zeker A-pool (voor één-dimensie bipolaire competentie-items).</summary>
    public static int BipolarLikertFromAnswer(int answerValue) =>
        Math.Clamp(6 - answerValue, 1, 5);
}