using Jobsy.Core.Careers;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Match;

internal static class MatchOccupationDayResolve
{
    internal readonly record struct DayLink(string Href, string LinkText);

    internal static DayLink? TryBuild(CultureState culture, bool featureEnabled, string? escoId, string? jobTitle)
    {
        if (!featureEnabled)
        {
            return null;
        }

        var resolvedId = ResolveId(escoId, jobTitle);
        if (resolvedId is null)
        {
            return null;
        }

        var title = OccupationCatalog.Shared.Get(resolvedId)?.Nl;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.IsNullOrWhiteSpace(jobTitle) ? "dit beroep" : jobTitle.Trim();
        }

        if (!Guid.TryParse(resolvedId, out _))
        {
            return null;
        }

        var href = $"/candidate/beroep/{resolvedId}/dag?from=fit";
        var linkText = culture.Format("Day.Link", title);
        return new DayLink(href, linkText);
    }

    private static string? ResolveId(string? escoId, string? title)
    {
        if (!string.IsNullOrWhiteSpace(escoId) && OccupationCatalog.Shared.Get(escoId) is { } known)
        {
            return known.Id;
        }

        return OccupationCatalog.Shared.Resolve(title)?.Id;
    }
}
