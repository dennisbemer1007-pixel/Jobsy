namespace Jobsy.Core.Rules;

/// <summary>
/// Band labels for passport "Past deze baan?" (no hero %). Thresholds: ≥75 good, ≥50 fair, else not yet.
/// </summary>
public static class RoleFitBandRules
{
    public const int GoodMin = 75;
    public const int FairMin = 50;

    public enum Band
    {
        Good = 0,
        Fair = 1,
        NotYet = 2
    }

    public static Band FromPercent(int matchPercent)
    {
        if (matchPercent >= GoodMin)
        {
            return Band.Good;
        }

        if (matchPercent >= FairMin)
        {
            return Band.Fair;
        }

        return Band.NotYet;
    }

    /// <summary>UiStringsPassport key for the band label.</summary>
    public static string LabelKey(Band band) => band switch
    {
        Band.Good => "Passport.Fit.Band.Good",
        Band.Fair => "Passport.Fit.Band.Fair",
        _ => "Passport.Fit.Band.NotYet"
    };

    public static string LabelKey(int matchPercent) => LabelKey(FromPercent(matchPercent));

    /// <summary>Canonical Dutch labels (parity with UiStringsPassport nl).</summary>
    public static string DutchLabel(Band band) => band switch
    {
        Band.Good => "Past goed",
        Band.Fair => "Past redelijk",
        _ => "Past nog niet"
    };

    public static string DutchLabel(int matchPercent) => DutchLabel(FromPercent(matchPercent));

    /// <summary>CSS modifier for status-pill / match-score styling.</summary>
    public static string CssModifier(Band band) => band switch
    {
        Band.Good => "good",
        Band.Fair => "fair",
        _ => "not-yet"
    };

    public static string CssModifier(int matchPercent) => CssModifier(FromPercent(matchPercent));
}
