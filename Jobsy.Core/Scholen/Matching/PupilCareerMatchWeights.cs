namespace Jobsy.Core.Scholen.Matching;

/// <summary>Option A weights (Dennis 2026-11-01): RIASEC + vakken + overige tests.</summary>
public static class PupilCareerMatchWeights
{
    public const double Riasec = 0.45;
    public const double Vakken = 0.25;
    public const double Overige = 0.30;

    public const double MboBeroepentest = 0.15;
    public const double MboCompetentie = 0.10;
    public const double MboWerkwaarden = 0.05;

    public const double DefaultMinMatch = 0.70;
    public const int MaxSuggestions = 5;
    public const int MaxPerGroup = 2;

    public const double NeutralSubjectFit = 0.5;
}
