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
        var allJobs = AssignRankBands(CleanJobs(
            (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? []).ToList()));
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

    public const int MinCatalogueJobs = 8;
    public const int MaxCatalogueJobs = 12;

    /// <summary>
    /// Keeps provider jobs and, when fewer than 8 catalogue titles survived,
    /// fills from the local ranking for the same scores. AI "why" text stays.
    /// </summary>
    public static CareerCompassSnapshot EnsureDepth(CareerCompassSnapshot snapshot, RiasecScores? scores, string? education = null)
    {
        if (scores is not { IsComplete: true })
        {
            return snapshot;
        }

        var jobs = new List<CareerOccupationMatch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in snapshot.AllOccupations)
        {
            var title = CanonicalTitle(job.Title) ?? job.Title;
            if (!seen.Add(MatchKey(title)))
            {
                continue;
            }

            var percent = CareerCompassBuilder.CatalogueFit(title, scores, education);
            jobs.Add(new CareerOccupationMatch(title, percent, "", job.Why, job.SearchKeys));
        }

        var allowLead = CareerCompassBuilder.EnterprisingInTop3(scores);
        foreach (var local in CareerCompassBuilder.Ranked(scores))
        {
            if (jobs.Count >= MinCatalogueJobs && !IsLoneHigherEducation(jobs))
            {
                break;
            }

            var title = CanonicalTitle(local.Title) ?? local.Title;
            if (!allowLead && CareerCompassBuilder.IsLeadershipTitle(title))
            {
                continue;
            }

            if (!seen.Add(MatchKey(title)))
            {
                continue;
            }

            var fit = CareerCompassBuilder.CatalogueFit(title, scores, education);
            jobs.Add(new CareerOccupationMatch(title, fit, "", local.Why, local.SearchKeys));
        }

        if (IsLoneHigherEducation(jobs))
        {
            jobs.Clear();
        }

        jobs = OrderForEducation(jobs, education);
        if (jobs.Count > MaxCatalogueJobs)
        {
            jobs = jobs.Take(MaxCatalogueJobs).ToList();
        }

        jobs = AssignRankBands(jobs);
        return CareerCompassHierarchy.FromOccupations(
            snapshot.Strengths,
            jobs,
            snapshot.PracticalNotes,
            snapshot.FromDeepAnalysis,
            snapshot.FromOpenAi);
    }

    /// <summary>
    /// A job that needs a clearly higher diploma sorts after jobs the candidate can do now.
    /// </summary>
    internal static List<CareerOccupationMatch> OrderForEducation(
        List<CareerOccupationMatch> jobs,
        string? education)
    {
        bool Demote(CareerOccupationMatch job)
        {
            if (!string.IsNullOrWhiteSpace(education))
            {
                return CareerGoalFit.RequiresHigherEducation(job.Title, education);
            }

            if (!CareerGoalFit.IsClearlyHigherEducation(job.Title))
            {
                return false;
            }

            var code = CareerCompassBuilder.PrimaryCode(job.Title);
            return jobs.Any(other =>
                !ReferenceEquals(other, job)
                && !CareerGoalFit.IsClearlyHigherEducation(other.Title)
                && string.Equals(CareerCompassBuilder.PrimaryCode(other.Title), code, StringComparison.OrdinalIgnoreCase));
        }

        return jobs
            .OrderBy(job => Demote(job) ? 1 : 0)
            .ThenByDescending(job => job.Percent)
            .ThenBy(job => job.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static List<CareerOccupationMatch> AssignRankBands(List<CareerOccupationMatch> jobs)
    {
        var count = jobs.Count;
        if (count == 0)
        {
            return jobs;
        }

        var super = count >= 10 ? 4 : 3;
        var strong = count >= 9 ? 4 : 3;
        if (super + strong >= count)
        {
            super = Math.Max(1, count / 3);
            strong = Math.Max(1, (count - super) / 2);
        }

        var banded = new List<CareerOccupationMatch>(count);
        for (var i = 0; i < count; i++)
        {
            var band = i < super
                ? CareerCompassBuilder.BandSuper
                : i < super + strong
                    ? CareerCompassBuilder.BandStrong
                    : CareerCompassBuilder.BandBroaden;
            var job = jobs[i];
            banded.Add(new CareerOccupationMatch(job.Title, job.Percent, band, job.Why, job.SearchKeys));
        }

        return banded;
    }

    /// <summary>Catalogue title, or null when the provider invented a name we do not know.</summary>
    internal static string? CanonicalTitle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        var exact = ExactTitle(trimmed);
        if (exact is not null)
        {
            return exact;
        }

        foreach (var variant in TitleVariants(trimmed))
        {
            exact = ExactTitle(variant);
            if (exact is not null)
            {
                return exact;
            }
        }

        return FuzzyTitle(trimmed);
    }

    private static string? ExactTitle(string raw)
    {
        var dream = CareerDreamCatalog.FindByTitleOrAlias(raw);
        if (dream is not null)
        {
            return dream.Title;
        }

        foreach (var occ in CareerCompassBuilder.Occupations)
        {
            if (string.Equals(occ.Title, raw, StringComparison.OrdinalIgnoreCase))
            {
                return occ.Title;
            }

            var head = occ.Title.Split('/')[0].Trim();
            if (head.Length > 0 && string.Equals(head, raw, StringComparison.OrdinalIgnoreCase))
            {
                return occ.Title;
            }
        }

        return null;
    }

    private static IEnumerable<string> TitleVariants(string raw)
    {
        var fold = CareerOccupationKeys.Fold(raw);
        var tokens = fold.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stripped = tokens.Where(t => !IsRoleFiller(t)).ToArray();
        if (stripped.Length > 0 && stripped.Length < tokens.Length)
        {
            yield return string.Join(' ', stripped);
        }

        foreach (var suffix in new[] { "medewerkers", "medewerksters", "medewerker", "medewerkster" })
        {
            if (fold.EndsWith(suffix, StringComparison.Ordinal) && fold.Length > suffix.Length + 3)
            {
                yield return fold[..^suffix.Length].Trim();
            }
        }

        foreach (var part in raw.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!string.Equals(part, raw, StringComparison.OrdinalIgnoreCase))
            {
                yield return part;
            }
        }

        if (fold.EndsWith('s') && fold.Length > 5)
        {
            yield return fold[..^1];
        }

        if (fold.EndsWith("en", StringComparison.Ordinal) && fold.Length > 6)
        {
            yield return fold[..^2];
        }
    }

    private static bool IsRoleFiller(string token)
        => token is "medewerker" or "medewerkers" or "medewerkster" or "medewerksters" or "werknemer";

    private static string? FuzzyTitle(string raw)
    {
        var query = Compact(raw);
        if (query.Length < 6)
        {
            return null;
        }

        string? bestTitle = null;
        var bestLen = 0;
        foreach (var (phrase, canonical) in CataloguePhrases())
        {
            var compact = Compact(phrase);
            if (compact.Length < 8)
            {
                continue;
            }

            var hit = query.Contains(compact, StringComparison.Ordinal)
                      || compact.Contains(query, StringComparison.Ordinal);
            if (!hit || compact.Length <= bestLen)
            {
                continue;
            }

            bestTitle = canonical;
            bestLen = compact.Length;
        }

        return bestTitle;
    }

    private static string Compact(string value)
        => CareerOccupationKeys.Fold(value).Replace(" ", "", StringComparison.Ordinal);

    private static string MatchKey(string title)
        => Compact(CanonicalTitle(title) ?? title);

    private static bool IsLoneHigherEducation(IReadOnlyList<CareerOccupationMatch> jobs)
        => jobs.Count == 1 && CareerGoalFit.IsClearlyHigherEducation(jobs[0].Title);

    private static IEnumerable<(string Phrase, string Canonical)> CataloguePhrases()
    {
        foreach (var entry in CareerDreamCatalog.All)
        {
            var canonical = ExactTitle(entry.Title) ?? entry.Title;
            yield return (entry.Title, canonical);
            foreach (var alias in entry.Aliases)
            {
                yield return (alias, canonical);
            }
        }

        foreach (var occ in CareerCompassBuilder.Occupations)
        {
            var canonical = ExactTitle(occ.Title) ?? occ.Title;
            yield return (occ.Title, canonical);
            var head = occ.Title.Split('/')[0].Trim();
            if (head.Length > 0)
            {
                yield return (head, canonical);
            }
        }
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
                var band = item.Band ?? "";
                if (band is not (CareerCompassBuilder.BandSuper or CareerCompassBuilder.BandStrong or CareerCompassBuilder.BandBroaden))
                {
                    band = "";
                }

                var why = CleanText(item.Why);
                var safeWhy = $"Dit beroep sluit aan bij hoe jij scoort ({percent}%).";
                if (why is null || ContainsEnglishLeak(why))
                {
                    why = safeWhy;
                }
                else
                {
                    why = CandidateFactGuard.WithoutInventedHistory(
                        why,
                        CandidateFactSheet.ForCareerProse(),
                        safeWhy);
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

            if (CandidateFactGuard.RejectionReason(text, CandidateFactSheet.ForCareerProse()) is not null)
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

        var prose = CandidateFactSheet.ForCareerProse();
        return items
            .Select(CleanText)
            .Select(text => CandidateFactGuard.WithoutRejectedSentences(text, prose))
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
