using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Candidate/employer-facing match copy — words, never raw match percentages.</summary>
public static class CandidateMatchDisplay
{
    public static string FitBandLabel(CultureState culture, int matchPercent)
        => KbLabels.FitBand(culture, CandidateFitDisplay.BandFor(matchPercent));

    public static string FitBandLabel(CultureState culture, KbFitBand band)
        => KbLabels.FitBand(culture, band);

    public static string OccupationFitLabel(int percent)
    {
        if (percent >= CareerCompassBuilder.SuperMatchMin)
        {
            return CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandSuper);
        }

        if (percent >= CareerCompassBuilder.StrongMatchMin)
        {
            return CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong);
        }

        if (percent >= CareerCompassBuilder.BroadenMin)
        {
            return CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandBroaden);
        }

        return "Richting om te bekijken";
    }

    public static string CompletenessLineKey(int percent) => percent switch
    {
        >= 100 => "Kompas.CompletenessDone",
        >= 88 => "Kompas.CompletenessAlmost",
        >= 70 => "Kompas.CompletenessGood",
        >= 40 => "Kompas.CompletenessStarted",
        _ => "Kompas.CompletenessBegin"
    };
}
