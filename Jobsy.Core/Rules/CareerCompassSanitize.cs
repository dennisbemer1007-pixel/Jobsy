using System.Text.RegularExpressions;
using Jobsy.Core.Careers;

namespace Jobsy.Core.Rules;

public static class CareerCompassSanitize
{
    public const int MaxPerBand = 8;
    public const int MaxNotes = 6;
    public const int MinStrengths = 3;
    public const int MaxStrengths = 5;
    public const int MaxStrengthWords = 4;

    private static readonly Regex ComboSplit = new(
        @"\s+of\s+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex EnglishLeak = new(
        @"\b(hands-on|hands on|skills?|leadership|teamwork|problem-solving|career|your|you|the|and|with)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static CareerCompassSnapshot? FromDto(CareerCompassJson.CompassDto dto, bool fromOpenAi)
    {
        var strengths = CleanStrengths(dto.Strengths);
        var allJobs = CleanJobs(
            (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? []).ToList());
        var notes = CleanTexts(dto.PracticalNotes, MaxNotes);

        if (allJobs.Count == 0)
        {
            // Occupations that do not map onto the catalogue are not a usable compass.
            return null;
        }

        if (notes.Count == 0)
        {
            notes =
            [
                "Jij floreert waar de taken lijken op jouw top-beroepen: herkenbaar werk, in een sfeer die bij je past.",
                "Kijk welke taken bij je sterke kanten horen. De naam van het beroep mag nét anders zijn."
            ];
        }

        return CareerCompassHierarchy.FromOccupations(
            strengths,
            allJobs,
            notes,
            dto.FromDeepAnalysis || fromOpenAi,
            fromOpenAi || dto.FromOpenAi);
    }

    /// <summary>Catalogue title, or null when the provider invented a name we do not know.</summary>
    internal static string? CanonicalTitle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var dream = CareerDreamCatalog.FindByTitleOrAlias(raw.Trim());
        if (dream is not null)
        {
            return dream.Title;
        }

        foreach (var occ in CareerCompassBuilder.Occupations)
        {
            if (string.Equals(occ.Title, raw.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return occ.Title;
            }

            var head = occ.Title.Split('/')[0].Trim();
            if (head.Length > 0 && string.Equals(head, raw.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return occ.Title;
            }
        }

        return null;
    }

    internal static bool ContainsEnglishLeak(string? text)
        => !string.IsNullOrWhiteSpace(text) && EnglishLeak.IsMatch(text);

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
            foreach (var part in ExpandTitle(item.Title))
            {
                var title = CanonicalTitle(part);
                if (title is null || !seen.Add(title))
                {
                    continue;
                }

                var percent = Math.Clamp(item.Percent, 0, 100);
                var band = CareerCompassBuilder.Band(percent);
                if (string.IsNullOrEmpty(band))
                {
                    continue;
                }

                var why = CleanText(item.Why);
                if (why is null || ContainsEnglishLeak(why))
                {
                    why = $"Dit beroep sluit aan bij hoe jij scoort ({percent}%).";
                }

                var keys = CareerOccupationKeys.Merge(title, item.Keys);
                list.Add(new CareerOccupationMatch(title, percent, band, why, keys));
            }
        }

        return list;
    }

    private static IEnumerable<string> ExpandTitle(string? title)
    {
        var cleaned = CleanText(title);
        if (cleaned is null)
        {
            yield break;
        }

        var parts = ComboSplit.Split(cleaned);
        if (parts.Length <= 1)
        {
            yield return cleaned;
            yield break;
        }

        foreach (var part in parts)
        {
            var piece = part.Trim().Trim('*', '"', '\'', '«', '»');
            if (piece.Length > 0)
            {
                yield return piece;
            }
        }
    }

    private static List<string> CleanStrengths(IReadOnlyList<string>? items)
    {
        if (items is null)
        {
            return [];
        }

        var list = new List<string>();
        foreach (var item in items)
        {
            var text = CleanText(item);
            if (text is null || ContainsEnglishLeak(text))
            {
                continue;
            }

            if (text.Contains('.') || text.Contains('!') || text.Contains('?') || text.Contains(','))
            {
                continue;
            }

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length is < 1 or > MaxStrengthWords)
            {
                continue;
            }

            if (list.Any(existing => string.Equals(existing, text, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            list.Add(text);
            if (list.Count == MaxStrengths)
            {
                break;
            }
        }

        return list.Count >= MinStrengths ? list : [];
    }

    private static List<string> CleanTexts(IReadOnlyList<string>? items, int take)
    {
        if (items is null)
        {
            return [];
        }

        return items
            .Select(CleanText)
            .Where(t => !string.IsNullOrWhiteSpace(t) && !ContainsEnglishLeak(t))
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

        var trimmed = value.Trim().Replace("**", "", StringComparison.Ordinal).Replace("__", "", StringComparison.Ordinal).Trim();
        if (trimmed.Length == 0 || CareerCompassBuilder.ContainsForbiddenJargon(trimmed))
        {
            return null;
        }

        return trimmed.Length > 400 ? trimmed[..400].Trim() : trimmed;
    }
}
