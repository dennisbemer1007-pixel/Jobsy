namespace Jobsy.Core.Rules;

/// <summary>
/// Overall Functie-Fit % (Tier 1 + culture + formal) plus stepping-stone titles.
/// Live vacancy cards stay in the API layer so they stay fresh.
/// </summary>
public static class RoleFitFunnel
{
    public const int DirectMatchMin = 60;
    public const int MaxSimilar = 4;
    public const int MaxDirect = 5;

    public static int CombineOverall(
        int roleFitPercent,
        bool availabilityOk,
        int? culturePercent,
        VacancyBarrierCheck? formal)
    {
        var tier1 = availabilityOk ? 100 : 35;
        var culture = culturePercent ?? Math.Clamp(roleFitPercent, 0, 100);
        var papers = formal is null || formal.TotalCount == 0
            ? (availabilityOk ? 100 : 70)
            : (int)Math.Round(100d * formal.MetCount / formal.TotalCount, MidpointRounding.AwayFromZero);
        var blended = 0.25 * tier1
                      + 0.30 * Math.Clamp(culture, 0, 100)
                      + 0.30 * Math.Clamp(papers, 0, 100)
                      + 0.15 * Math.Clamp(roleFitPercent, 0, 100);
        return (int)Math.Clamp(Math.Round(blended, MidpointRounding.AwayFromZero), 0, 100);
    }

    public static IReadOnlyList<RoleFitSimilarRole> SuggestSimilar(string jobTitle, RiasecScores career)
    {
        var title = RoleFitCheckBuilder.NormalizeTitle(jobTitle) ?? "deze functie";
        var folded = CareerOccupationKeys.Fold(title);
        var licensed = LooksLicensed(folded);
        var extras = EducationSteppingStones(folded);
        var ranked = CareerCompassBuilder.Occupations
            .Select(job => (Job: job, Match: CareerCompassBuilder.Score(job, career)))
            .Where(x => x.Match.Percent >= CareerCompassBuilder.BroadenMin)
            .Where(x => !SameRole(folded, x.Job.Title))
            .OrderByDescending(x => licensed && IsSteppingStone(x.Job.Title) ? 1 : 0)
            .ThenByDescending(x => x.Match.Percent)
            .ThenBy(x => x.Job.Title, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSimilar)
            .Select(x => new RoleFitSimilarRole(
                x.Job.Title,
                licensed && IsSteppingStone(x.Job.Title)
                    ? $"{x.Job.Title} is een realistische opstap: dezelfde richting, minder papieren eisen."
                    : x.Match.Why,
                x.Match.Percent))
            .ToList();

        return MergeSimilar(extras, ranked);
    }

    public static IReadOnlyList<RoleFitSimilarRole> MergeSimilar(
        IEnumerable<RoleFitSimilarRole>? preferred,
        IEnumerable<RoleFitSimilarRole>? fallback)
    {
        var list = new List<RoleFitSimilarRole>();
        foreach (var item in (preferred ?? []).Concat(fallback ?? []))
        {
            var title = RoleFitCheckBuilder.NormalizeTitle(item.Title);
            if (title is null)
            {
                continue;
            }

            if (list.Any(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var why = string.IsNullOrWhiteSpace(item.Why) || CareerCompassBuilder.ContainsForbiddenJargon(item.Why)
                ? "Deze richting sluit beter aan bij hoe jij scoort."
                : item.Why.Trim();
            if (why.Contains('@', StringComparison.Ordinal))
            {
                continue;
            }

            list.Add(new RoleFitSimilarRole(title, why, Math.Clamp(item.FitPercent, 0, 100)));
            if (list.Count >= MaxSimilar)
            {
                break;
            }
        }

        return list;
    }

    public static bool CanStartImmediately(
        bool legalEligible,
        int matchPercent,
        bool? travelWithinPreference,
        VacancyBarrierCheck formal,
        bool availabilityOk)
        => legalEligible
           && matchPercent >= DirectMatchMin
           && travelWithinPreference != false
           && availabilityOk
           && formal.Items.All(i => i.Met);

    private static bool SameRole(string foldedTitle, string occupationTitle)
    {
        var foldedJob = CareerOccupationKeys.Fold(occupationTitle);
        if (foldedJob.Length == 0)
        {
            return true;
        }

        return foldedTitle.Contains(foldedJob, StringComparison.Ordinal)
               || foldedJob.Contains(foldedTitle, StringComparison.Ordinal)
               || CareerOccupationKeys.FromTitle(occupationTitle)
                   .Count(key => CareerOccupationKeys.Hits(foldedTitle, key)) >= 2;
    }

    private static bool LooksLicensed(string folded)
        => CareerOccupationKeys.Hits(folded, "verpleeg")
           || folded.Contains("verpleeg", StringComparison.Ordinal)
           || folded.Contains("piloot", StringComparison.Ordinal)
           || folded.Contains("vlieg", StringComparison.Ordinal)
           || folded.Contains("elektricien", StringComparison.Ordinal)
           || folded.Contains("monteur", StringComparison.Ordinal)
           || folded.Contains("software", StringComparison.Ordinal)
           || folded.Contains("boekhoud", StringComparison.Ordinal)
           || folded.Contains("docent", StringComparison.Ordinal)
           || folded.Contains("leraar", StringComparison.Ordinal)
           || folded.Contains("juf", StringComparison.Ordinal)
           || folded.Contains("meester", StringComparison.Ordinal)
           || folded.Contains("onderwij", StringComparison.Ordinal)
           || folded.Contains("pedagog", StringComparison.Ordinal);

    private static IReadOnlyList<RoleFitSimilarRole> EducationSteppingStones(string folded)
    {
        if (!(folded.Contains("juf", StringComparison.Ordinal)
              || folded.Contains("meester", StringComparison.Ordinal)
              || folded.Contains("docent", StringComparison.Ordinal)
              || folded.Contains("leraar", StringComparison.Ordinal)
              || folded.Contains("onderwij", StringComparison.Ordinal)))
        {
            return [];
        }

        return
        [
            new RoleFitSimilarRole("Onderwijsassistent", "Zelfde klas, minder papieren eisen dan juf of meester.", 86),
            new RoleFitSimilarRole("Pedagogisch medewerker", "Kinderen begeleiden in opvang of buitenschoolse opvang.", 84),
            new RoleFitSimilarRole("Praktijkopleider", "Vak overbrengen op de werkvloer — een logische opstap of verbreding.", 80)
        ];
    }

    private static bool IsSteppingStone(string occupationTitle)
    {
        var folded = CareerOccupationKeys.Fold(occupationTitle);
        return folded.Contains("helpende", StringComparison.Ordinal)
               || folded.Contains("assistent", StringComparison.Ordinal)
               || folded.Contains("productie", StringComparison.Ordinal)
               || folded.Contains("magazijn", StringComparison.Ordinal)
               || folded.Contains("schoonmaak", StringComparison.Ordinal)
               || folded.Contains("horeca", StringComparison.Ordinal)
               || folded.Contains("winkel", StringComparison.Ordinal)
               || folded.Contains("kassa", StringComparison.Ordinal)
               || folded.Contains("onderwijsassistent", StringComparison.Ordinal)
               || folded.Contains("pedagogisch", StringComparison.Ordinal)
               || folded.Contains("praktijkopleider", StringComparison.Ordinal);
    }
}
