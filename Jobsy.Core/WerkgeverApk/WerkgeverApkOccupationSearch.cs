using Jobsy.Core.Careers;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Rules;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Manager functie-kiezen: deduped ESCO suggestions (names only, no codes).</summary>
public static class WerkgeverApkOccupationSearch
{
    private const int MaxHits = 6;

    private static readonly string[] LogisticsEscoIds =
    [
        "cd94def5-3442-4c2e-ae3d-0761a3008bcb", // orderpicker
        "040fcfa5-24be-43eb-afd5-828297175b16", // magazijnmedewerker kleding
        "45037d43-a8f5-4f46-b332-b2935bc305f4", // vrachtwagenchauffeur
        "c8b296be-54ae-48cf-be21-031fa8457937", // logistiek medewerker
    ];

    public static IReadOnlyList<WerkgeverApkOccupationSearchHitDto> ForManager(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var q = query.Trim();
        var hits = new List<WerkgeverApkOccupationSearchHitDto>();

        if (LooksLikeLogisticsPick(q))
        {
            foreach (var id in LogisticsEscoIds)
            {
                AddOcc(hits, id);
            }
        }

        var qFold = CareerOccupationKeys.Fold(q);
        foreach (var hit in OccupationCatalog.Shared.Search(q, 12))
        {
            if (!TitleMatchesQuery(hit.EscoId, qFold))
            {
                continue;
            }

            AddOcc(hits, hit.EscoId);
        }

        return DedupeAndCap(hits);
    }

    private static bool LooksLikeLogisticsPick(string q)
    {
        var fold = CareerOccupationKeys.Fold(q);
        return fold.StartsWith("order", StringComparison.Ordinal)
               || fold.StartsWith("magaz", StringComparison.Ordinal)
               || fold.Contains("logist", StringComparison.Ordinal)
               || fold.StartsWith("vracht", StringComparison.Ordinal)
               || fold.StartsWith("plan", StringComparison.Ordinal);
    }

    private static bool TitleMatchesQuery(string escoId, string queryFold)
    {
        var occ = OccupationCatalog.Shared.Get(escoId);
        if (occ is null)
        {
            return false;
        }

        if (LogisticsEscoIds.Contains(escoId, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var titleFold = CareerOccupationKeys.Fold(occ.Nl);
        return titleFold.Contains(queryFold, StringComparison.Ordinal)
               || queryFold.Length >= 3 && titleFold.StartsWith(queryFold, StringComparison.Ordinal);
    }

    private static void AddOcc(List<WerkgeverApkOccupationSearchHitDto> hits, string escoId)
    {
        var occ = OccupationCatalog.Shared.Get(escoId);
        if (occ is null)
        {
            return;
        }

        var name = Capitalize(occ.Nl);
        hits.Add(new WerkgeverApkOccupationSearchHitDto(escoId, name));
    }

    private static IReadOnlyList<WerkgeverApkOccupationSearchHitDto> DedupeAndCap(
        List<WerkgeverApkOccupationSearchHitDto> hits)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<WerkgeverApkOccupationSearchHitDto>();

        foreach (var hit in hits)
        {
            if (!seenIds.Add(hit.EscoOccupationId))
            {
                continue;
            }

            var nameKey = CareerOccupationKeys.Fold(hit.Name);
            if (!seenNames.Add(nameKey))
            {
                continue;
            }

            list.Add(hit);
            if (list.Count >= MaxHits)
            {
                break;
            }
        }

        return list;
    }

    private static string Capitalize(string nl)
        => nl.Length == 0 ? nl : char.ToUpper(nl[0]) + nl[1..];
}
