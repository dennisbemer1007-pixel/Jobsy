namespace Jobsy.Core.Rules;

/// <summary>
/// Keeps Super-match / Sterke keus / Handige verbreding as a strict descending hierarchy.
/// Top jobs are never left looking "forced low" (e.g. 88% when they are the kernfit).
/// </summary>
public static class CareerCompassHierarchy
{
    public const int TargetPerBand = 3;

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

        var super = TakeBand(ranked, CareerCompassBuilder.BandSuper);
        var strong = TakeBand(ranked, CareerCompassBuilder.BandStrong);
        var broaden = TakeBand(ranked, CareerCompassBuilder.BandBroaden);

        if (super.Count == 0 && ranked.Count > 0)
        {
            var take = Math.Min(CareerCompassSanitize.MaxPerBand, Math.Min(ranked.Count, Math.Max(TargetPerBand, 1)));
            super = AssignBand(
                ranked.Take(take),
                start: 100,
                min: CareerCompassBuilder.SuperMatchMin,
                CareerCompassBuilder.BandSuper);
            var rest = ranked.Skip(take).ToList();
            strong = TakeBand(rest, CareerCompassBuilder.BandStrong);
            broaden = TakeBand(rest, CareerCompassBuilder.BandBroaden);
        }

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

    private static List<CareerOccupationMatch> AssignBand(
        IEnumerable<CareerOccupationMatch> items,
        int start,
        int min,
        string band)
    {
        var list = new List<CareerOccupationMatch>();
        var index = 0;
        foreach (var item in items)
        {
            var percent = Math.Clamp(Math.Max(item.Percent, start - index), min, 100);
            list.Add(item with { Percent = percent, Band = band });
            index++;
        }

        return list;
    }
}
