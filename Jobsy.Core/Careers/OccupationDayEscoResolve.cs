using Jobsy.Core.Scholen.Matching;

namespace Jobsy.Core.Careers;

/// <summary>Resolve ESCO occupation ids for day-in-life links (no API calls).</summary>
public static class OccupationDayEscoResolve
{
    public static string? GuidFromEscoUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return null;
        }

        var tail = uri.Trim().TrimEnd('/').Split('/').LastOrDefault();
        return Guid.TryParse(tail, out _) ? tail : null;
    }

    public static string? FromJobKey(string? jobKey)
        => GuidFromEscoUri(PupilDreamJobEscoCatalog.TryGetUri(jobKey));

    public static string? FromTitleOrEscoId(string? escoId, string? title)
    {
        if (!string.IsNullOrWhiteSpace(escoId) && OccupationCatalog.Shared.Get(escoId.Trim()) is { } known)
        {
            return known.Id;
        }

        return OccupationCatalog.Shared.Resolve(title)?.Id;
    }

    public static string? FromTitle(string? title) => FromTitleOrEscoId(null, title);
}
