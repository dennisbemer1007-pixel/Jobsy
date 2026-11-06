namespace Jobsy.Core.Rules;

/// <summary>
/// Display bands and ranking thresholds when <see cref="MatchCalibrationV1Calculator"/> is active.
/// </summary>
public static class MatchCalibrationBands
{
    /// <summary>Top-10 / widget cutoff (was 60 under legacy).</summary>
    public const int DisplayThreshold = 55;

    /// <summary>Strong / green band (was 70).</summary>
    public const int StrongMatchThreshold = 78;

    /// <summary>Good band lower bound.</summary>
    public const int GoodMatchThreshold = 62;

    /// <summary>Twijfel band lower bound / Gulden Middenweg (was 50).</summary>
    public const int GuldenMiddenwegThreshold = 45;

    /// <summary>Proactive matching minimum (unchanged intent).</summary>
    public const int ProactiveMinScore = 80;

    /// <summary>Below this confidence, UI avoids the green band.</summary>
    public const int LowConfidenceThreshold = 50;

    public static string ColorBand(int displayPercent, int confidencePercent)
    {
        var strong = displayPercent >= StrongMatchThreshold;
        if (strong && confidencePercent < LowConfidenceThreshold)
        {
            return "orange";
        }

        if (strong)
        {
            return "green";
        }

        return displayPercent >= GuldenMiddenwegThreshold ? "orange" : "red";
    }
}
