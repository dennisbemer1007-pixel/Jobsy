namespace Jobsy.Core.Rules;

/// <summary>Word-only competency bands for candidate UI (no scores or percentages).</summary>
public static class CompetencySkillBands
{
    public const int StrongMin = 70;
    public const int GrowingMin = 45;

    public static string BandKey(int score) => score switch
    {
        >= StrongMin => "Training.CompetencyBand.Strong",
        >= GrowingMin => "Training.CompetencyBand.Growing",
        _ => "Training.CompetencyBand.GrowHere"
    };

    public static string CssModifier(int score) => score switch
    {
        >= StrongMin => "strong",
        >= GrowingMin => "growing",
        _ => "grow-here"
    };

    public static bool ShowsInlineOffers(int score) => score < StrongMin;
}
