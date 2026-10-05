namespace Jobsy.Core.Rules;

/// <summary>
/// Splits the one ranked list into the three tiers already stored on each job.
/// Equal percents share a tier. A tier is not cut short of that tied group.
/// </summary>
public static class CareerCompassHierarchy
{

    public static CareerCompassSnapshot FromOccupations(
        IReadOnlyList<string> strengths,
        IReadOnlyList<CareerOccupationMatch> jobs,
        IReadOnlyList<string> notes,
        bool fromDeepAnalysis,
        bool fromOpenAi,
        string? scoresFingerprint = null)
    {
        var ranked = jobs
            .OrderByDescending(m => m.Percent)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

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
            fromOpenAi,
            scoresFingerprint ?? "");
    }

    private static List<CareerOccupationMatch> TakeBand(IReadOnlyList<CareerOccupationMatch> items, string band)
        => items
            .Where(m => m.Band == band)
            .Take(CareerCompassSanitize.MaxCatalogueJobs)
            .ToList();
}
