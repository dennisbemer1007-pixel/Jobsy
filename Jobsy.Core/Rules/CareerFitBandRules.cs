namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate career fit bands (D4). Delegates thresholds to <see cref="RoleFitBandRules"/>
/// so passport "Past deze baan?" and career share one source.
/// </summary>
public static class CareerFitBandRules
{
    public enum CareerFitBand
    {
        Unknown = 0,
        Good = 1,
        Fair = 2,
        NotYet = 3
    }

    public static CareerFitBand From(int? percent)
    {
        if (percent is null)
        {
            return CareerFitBand.Unknown;
        }

        return RoleFitBandRules.FromPercent(percent.Value) switch
        {
            RoleFitBandRules.Band.Good => CareerFitBand.Good,
            RoleFitBandRules.Band.Fair => CareerFitBand.Fair,
            _ => CareerFitBand.NotYet
        };
    }

    public static string ToApi(CareerFitBand band) => band switch
    {
        CareerFitBand.Good => "Good",
        CareerFitBand.Fair => "Fair",
        CareerFitBand.NotYet => "NotYet",
        _ => "Unknown"
    };

    public static string? LabelKey(CareerFitBand band) => band switch
    {
        CareerFitBand.Good => "CareerFit.Good",
        CareerFitBand.Fair => "CareerFit.Fair",
        CareerFitBand.NotYet => "CareerFit.NotYet",
        _ => null
    };
}
