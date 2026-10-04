namespace Jobsy.Core.Rules;

/// <summary>
/// Keeps Super-match / Sterke keus / Handige verbreding as exclusive percent bands.
/// A job stays in the band its percent actually falls in.
/// </summary>
public static class CareerCompassHierarchy
{

    public static CareerCompassSnapshot FromOccupations(
        IReadOnlyList<string> strengths,
        IReadOnlyList<CareerOccupationMatch> jobs,
        IReadOnlyList<string> notes,
        bool fromDeepAnalysis,
        bool fromOpenAi)
    {
        var ranked = jobs
            .OrderByDescending(m => m.Percent)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Bands stay on the real percent. "Past heel goed" is only the >95 band
        // (95 and up). A provider must not push an 85–94 job into that label.
        var super = TakeBand(ranked, CareerCompassBuilder.BandSuper);
        var strong = TakeBand(ranked, CareerCompassBuilder.BandStrong);
        var broaden = TakeBand(ranked, CareerCompassBuilder.BandBroaden);

        return new CareerCompassSnapshot(
            strengths,
            super,
            strong,
            broaden,
            notes,
            fromDeepAnalysis,
            fromOpenAi);
    }

    private static List<CareerOccupationMatch> TakeBand(IReadOnlyList<CareerOccupationMatch> items, string band)
        => items
            .Where(m => m.Band == band)
            .Take(CareerCompassSanitize.MaxPerBand)
            .ToList();
}
