namespace Jobsy.Core.Rules;

public static class CareerCompassSanitize
{
    public const int MaxPerBand = 8;
    public const int MaxNotes = 6;

    public static CareerCompassSnapshot? FromJson(string? json, bool fromOpenAi)
    {
        var parsed = CareerCompassJson.TryDeserialize(json);
        if (parsed is null)
        {
            return null;
        }

        return parsed with { FromOpenAi = fromOpenAi || parsed.FromOpenAi };
    }

    internal static CareerCompassSnapshot? FromDto(CareerCompassJson.CompassDto dto, bool fromOpenAi)
    {
        var strengths = CleanTexts(dto.Strengths, 5);
        var allJobs = CleanJobs(
            (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? []).ToList());
        var notes = CleanTexts(dto.PracticalNotes, MaxNotes);

        if (allJobs.Count == 0 && notes.Count == 0 && strengths.Count == 0)
        {
            return null;
        }

        if (notes.Count == 0)
        {
            notes =
            [
                "Jij floreert waar de taken lijken op jouw top-beroepen: herkenbaar werk, in een sfeer die bij je past.",
                "Open de banenkaart. Vacatures die lijken op jouw top-beroepen scoren hoger — ook als de functienaam nét anders is."
            ];
        }

        return CareerCompassHierarchy.FromOccupations(
            strengths,
            allJobs,
            notes,
            dto.FromDeepAnalysis || fromOpenAi,
            fromOpenAi || dto.FromOpenAi);
    }

    private static List<CareerOccupationMatch> CleanJobs(IReadOnlyList<CareerCompassJson.OccupationDto> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var list = new List<CareerOccupationMatch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var title = CleanText(item.Title);
            if (string.IsNullOrWhiteSpace(title) || !seen.Add(title))
            {
                continue;
            }

            var percent = Math.Clamp(item.Percent, 0, 100);
            var band = CareerCompassBuilder.Band(percent);
            if (string.IsNullOrEmpty(band))
            {
                continue;
            }

            var why = CleanText(item.Why)
                      ?? $"Dit beroep sluit aan bij hoe jij scoort ({percent}%).";
            var keys = CareerOccupationKeys.Merge(title, item.Keys);
            list.Add(new CareerOccupationMatch(title, percent, band, why, keys));
        }

        return list;
    }

    private static List<string> CleanTexts(IReadOnlyList<string>? items, int take)
    {
        if (items is null)
        {
            return [];
        }

        return items
            .Select(CleanText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (CareerCompassBuilder.ContainsForbiddenJargon(trimmed))
        {
            return null;
        }

        return trimmed.Length > 400 ? trimmed[..400].Trim() : trimmed;
    }
}
