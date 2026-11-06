using Jobsy.Core.Careers;

namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-only hint above the RIASEC list. It does not change stored match
/// percents and is never sent to employers or partners.
/// </summary>
public static class CareerGoalFit
{
    public sealed record Hit(string Title, bool EducationAbove);

    /// <summary>
    /// Role family for a logistics lead. A shared word like "teamleider" is not enough:
    /// "Teamleider winkel of horeca" stays out of a logistiek goal.
    /// </summary>
    private static readonly string[][] RoleFamilies =
    [
        ["teamleider logistiek", "planner", "planningsmedewerker", "voorman", "logistiek supervisor"]
    ];

    /// <summary>
    /// Dream title plus catalog titles in the same role family, so the goal row
    /// stays visible after an uitgebreid compass replaces the free occupation list.
    /// </summary>
    public static IEnumerable<string> DreamTitles(string? dream)
    {
        var dreamFold = CareerOccupationKeys.Fold(dream ?? "");
        if (dreamFold.Length == 0 || string.IsNullOrWhiteSpace(dream))
        {
            yield break;
        }

        yield return dream.Trim();
        foreach (var entry in CareerDreamCatalog.All)
        {
            var fold = CareerOccupationKeys.Fold(entry.Title);
            if (RankAgainstDream(fold, dreamFold) >= 0)
            {
                yield return entry.Title;
            }
        }
    }

    public static IReadOnlyList<Hit> Pick(
        IEnumerable<string> titles,
        string? dream,
        string? evidence,
        string? education,
        int take = 4)
    {
        var dreamFold = CareerOccupationKeys.Fold(dream ?? "");
        if (dreamFold.Length == 0)
        {
            return [];
        }

        var currentJobs = CurrentJobFolds(evidence);
        var ranked = new List<(string Title, int Rank, bool Above)>();
        foreach (var title in titles)
        {
            if (string.IsNullOrWhiteSpace(title)
                || ranked.Any(h => string.Equals(h.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var fold = CareerOccupationKeys.Fold(title);
            if (IsCurrentJob(fold, currentJobs))
            {
                continue;
            }

            var rank = RankAgainstDream(fold, dreamFold);
            if (rank < 0)
            {
                continue;
            }

            ranked.Add((title.Trim(), rank, RequiresHigherEducation(title, education)));
        }

        return ranked
            .OrderBy(h => h.Rank)
            .ThenBy(h => h.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, take))
            .Select(h => new Hit(h.Title, h.Above))
            .ToList();
    }

    /// <summary>
    /// Shown percent for the candidate. A title that needs a clearly higher diploma
    /// cannot stay in the 100% Super-match tier. Stored scores are unchanged.
    /// </summary>
    public static int DisplayPercent(string title, int percent, string? education)
        => percent >= CareerCompassBuilder.SuperMatchMin && RequiresHigherEducation(title, education)
            ? CareerCompassBuilder.SuperMatchMin - 1
            : percent;

    public static bool BelongsInSuper(string title, int percent, string? education)
        => DisplayPercent(title, percent, education) >= CareerCompassBuilder.SuperMatchMin;

    public static IReadOnlyList<string> AboveEducation(IEnumerable<string> titles, string? education)
    {
        var rank = EducationRank(education);
        if (rank is 0 or >= 5)
        {
            return [];
        }

        return titles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Where(t => RequiresHigherEducation(t, education))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int EducationRank(string? education)
    {
        var tokens = Tokens(education);
        if (tokens.Contains("wo") || tokens.Any(t => t.StartsWith("universit", StringComparison.Ordinal)))
        {
            return 6;
        }

        if (tokens.Contains("hbo"))
        {
            return 5;
        }

        if (tokens.Any(t => t.StartsWith("mbo", StringComparison.Ordinal)))
        {
            var text = string.Join(' ', tokens);
            if (text.Contains("mbo 4", StringComparison.Ordinal) || text.Contains("mbo4", StringComparison.Ordinal))
            {
                return 4;
            }

            if (text.Contains("mbo 3", StringComparison.Ordinal) || text.Contains("mbo3", StringComparison.Ordinal))
            {
                return 3;
            }

            return 2;
        }

        return 0;
    }

    /// <summary>
    /// A catalogue job is clearly higher when its ISCO skill level is 4.
    /// A free-text title that is not an ESCO occupation keeps the old dream-catalog rank.
    /// </summary>
    public static bool IsClearlyHigherEducation(string title)
    {
        var occupation = OccupationCatalog.Shared.Resolve(title);
        if (occupation is not null)
        {
            return occupation.IscoLevel >= 4;
        }

        return RequiredRank(title) >= 4;
    }

    public static bool RequiresHigherEducation(string title, string? education)
    {
        var occupation = OccupationCatalog.Shared.Resolve(title);
        if (occupation is not null)
        {
            var gate = CareerEducationGate.MaxIscoLevel(education);
            if (gate.MaxLevel is not int max || occupation.IscoLevel is not int level)
            {
                return false;
            }

            return level > max;
        }

        var have = EducationRank(education);
        if (have is 0 or >= 5)
        {
            return false;
        }

        var need = RequiredRank(title);
        return need >= 4 && need > have;
    }

    private static int RankAgainstDream(string titleFold, string dreamFold)
    {
        if (titleFold == dreamFold
            || titleFold.Contains(dreamFold, StringComparison.Ordinal)
            || dreamFold.Contains(titleFold, StringComparison.Ordinal) && titleFold.Length >= 8)
        {
            return 0;
        }

        return SameFamily(dreamFold, titleFold) ? 1 : -1;
    }

    private static bool SameFamily(string dreamFold, string titleFold)
    {
        foreach (var family in RoleFamilies)
        {
            var dreamIn = family.Any(member => dreamFold.Contains(member, StringComparison.Ordinal));
            var titleIn = family.Any(member => titleFold.Contains(member, StringComparison.Ordinal));
            if (dreamIn && titleIn)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCurrentJob(string titleFold, IReadOnlyList<string> currentJobs)
        => currentJobs.Any(job =>
            titleFold.Contains(job, StringComparison.Ordinal)
            || job.Contains(titleFold, StringComparison.Ordinal));

    private static List<string> CurrentJobFolds(string? evidence)
    {
        var fold = CareerOccupationKeys.Fold(evidence ?? "");
        if (fold.Length == 0)
        {
            return [];
        }

        return CareerDreamCatalog.All
            .Select(entry => CareerOccupationKeys.Fold(entry.Title))
            .Where(title => title.Length >= 5 && CareerOccupationKeys.Hits(fold, title))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static int RequiredRank(string title)
    {
        var entry = FindOccupationEntry(title);
        return entry is null ? 0 : LevelRank(entry.Level);
    }

    private static CareerDreamCatalog.Entry? FindOccupationEntry(string title)
    {
        var direct = CareerDreamCatalog.FindByTitleOrAlias(title);
        if (direct is not null)
        {
            return direct;
        }

        var fold = CareerOccupationKeys.Fold(title);
        CareerDreamCatalog.Entry? best = null;
        var bestLen = 0;
        foreach (var entry in CareerDreamCatalog.All)
        {
            var entryFold = CareerOccupationKeys.Fold(entry.Title);
            if (entryFold.Length >= 5
                && fold.Contains(entryFold, StringComparison.Ordinal)
                && entryFold.Length > bestLen)
            {
                best = entry;
                bestLen = entryFold.Length;
            }
        }

        return best;
    }

    private static int LevelRank(string level) => level switch
    {
        "Entry" => 1,
        "Mbo2" => 2,
        "Mbo3" => 3,
        "Mbo4" => 4,
        "Hbo" => 5,
        "Wo" => 6,
        _ => 0
    };

    private static List<string> Tokens(string? text)
    {
        var folded = CareerOccupationKeys.Fold(text ?? "");
        return folded.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
