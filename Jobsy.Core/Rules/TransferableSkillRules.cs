namespace Jobsy.Core.Rules;

/// <summary>
/// Maps related work domains so matching looks past an exact functietitel
/// (e.g. kassa → klantcontact → retail) via transferable skills.
/// </summary>
public static class TransferableSkillRules
{
    private static readonly (string Domain, string[] Keys)[] Domains =
    [
        ("zorg", ["zorg", "verpleeg", "verzorg", "welzijn", "helpende", "thuiszorg", "ouderzorg", "ggz"]),
        ("klant", ["winkel", "retail", "kassa", "verkoop", "horeca", "balie", "receptie", "klant", "gastvrij"]),
        ("logistiek", ["logistiek", "magazijn", "heftruck", "orderpick", "chauffeur", "distributie", "productie"]),
        ("techniek", ["techniek", "monteur", "install", "elektro", "bouw", "loodgieter", "metaal", "lasse"]),
        ("admin", ["admin", "boekhoud", "controller", "finance", "planning", "secretaria", "kantoor", "erp"]),
        ("onderwijs", ["onderwijs", "docent", "leraar", "juf", "meester", "pedagog", "assistent", "kinderopvang"]),
        ("it", ["software", "it", "ict", "developer", "programmeur", "systeem", "data", "netwerk"])
    ];

    /// <summary>
    /// 0–1 score: exact work-type / label overlap stays highest; related domains score partially.
    /// </summary>
    public static double Score01(
        IReadOnlyList<string>? candidateRoles,
        IReadOnlyList<string>? vacancyWorkTypes,
        string? vacancyTitle,
        string? vacancyDescription)
    {
        var vacancyDomains = DetectDomains(vacancyWorkTypes, vacancyTitle, vacancyDescription);
        if (vacancyDomains.Count == 0)
        {
            return (candidateRoles?.Count ?? 0) > 0 ? 0.55 : 0.4;
        }

        var candidateDomains = DetectDomains(candidateRoles, null, null);
        if (candidateDomains.Count == 0)
        {
            return 0.35;
        }

        var labelExact = LabelOverlap01(candidateRoles, vacancyWorkTypes, vacancyTitle);
        if (labelExact >= 0.99)
        {
            return 1.0;
        }

        if (labelExact > 0)
        {
            return Math.Clamp(0.85 + 0.1 * labelExact, 0.85, 0.95);
        }

        var domainHits = vacancyDomains.Count(v => candidateDomains.Contains(v));
        if (domainHits > 0)
        {
            // Same domain (e.g. horeca → retail) without exact label = transferable.
            return Math.Clamp(0.55 + 0.2 * (domainHits / (double)vacancyDomains.Count), 0.55, 0.75);
        }

        return 0.35;
    }

    private static double LabelOverlap01(
        IReadOnlyList<string>? candidateRoles,
        IReadOnlyList<string>? vacancyWorkTypes,
        string? vacancyTitle)
    {
        var vac = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in vacancyWorkTypes ?? [])
        {
            var folded = CareerOccupationKeys.Fold(label);
            if (folded.Length > 0)
            {
                vac.Add(folded);
            }
        }

        var titleFolded = CareerOccupationKeys.Fold(vacancyTitle ?? "");
        if (titleFolded.Length >= 3)
        {
            vac.Add(titleFolded);
        }

        if (vac.Count == 0)
        {
            return 0;
        }

        var hits = 0;
        foreach (var role in candidateRoles ?? [])
        {
            var folded = CareerOccupationKeys.Fold(role);
            if (folded.Length == 0)
            {
                continue;
            }

            if (vac.Contains(folded) || vac.Any(v => v.Contains(folded, StringComparison.Ordinal) || folded.Contains(v, StringComparison.Ordinal)))
            {
                hits++;
            }
        }

        return hits == 0 ? 0 : Math.Clamp(hits / (double)Math.Max(1, (candidateRoles?.Count ?? 1)), 0, 1);
    }

    public static IReadOnlyList<string> SharedDomainLabels(
        IReadOnlyList<string>? candidateRoles,
        IReadOnlyList<string>? vacancyWorkTypes,
        string? vacancyTitle,
        string? vacancyDescription)
    {
        var vacancy = DetectDomains(vacancyWorkTypes, vacancyTitle, vacancyDescription);
        var candidate = DetectDomains(candidateRoles, null, null);
        return vacancy
            .Where(candidate.Contains)
            .Select(DomainLabel)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> DetectDomains(
        IReadOnlyList<string>? labels,
        string? title,
        string? description)
    {
        var blob = CareerOccupationKeys.Fold(
            string.Join(' ', (labels ?? []).Append(title ?? "").Append(description ?? "")));
        if (blob.Length == 0)
        {
            return [];
        }

        var hits = new List<string>();
        foreach (var (domain, keys) in Domains)
        {
            if (keys.Any(k => CareerOccupationKeys.Hits(blob, k) || blob.Contains(k, StringComparison.Ordinal)))
            {
                hits.Add(domain);
            }
        }

        return hits;
    }

    public static bool TitleLooksExact(
        string? vacancyTitle,
        IReadOnlyList<string>? candidateRoles,
        IReadOnlyList<CareerOccupationMatch>? occupations)
    {
        var foldedTitle = CareerOccupationKeys.Fold(vacancyTitle ?? "");
        if (foldedTitle.Length < 3)
        {
            return false;
        }

        foreach (var role in candidateRoles ?? [])
        {
            var foldedRole = CareerOccupationKeys.Fold(role);
            if (foldedRole.Length >= 3
                && (foldedTitle.Contains(foldedRole, StringComparison.Ordinal)
                    || foldedRole.Contains(foldedTitle, StringComparison.Ordinal)
                    || CareerOccupationKeys.Hits(foldedTitle, foldedRole)))
            {
                return true;
            }
        }

        foreach (var occupation in occupations ?? [])
        {
            var phrase = CareerOccupationKeys.Fold(occupation.Title);
            if (phrase.Length >= 3 && CareerOccupationKeys.Hits(foldedTitle, phrase))
            {
                return true;
            }

            if (occupation.SearchKeys.Count(k => CareerOccupationKeys.Hits(foldedTitle, k)) >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private static string DomainLabel(string domain) => domain switch
    {
        "zorg" => "zorg & welzijn",
        "klant" => "klantcontact & retail",
        "logistiek" => "logistiek & productie",
        "techniek" => "techniek & installatie",
        "admin" => "administratie & cijfers",
        "onderwijs" => "onderwijs & begeleiding",
        "it" => "IT & data",
        _ => domain
    };
}
