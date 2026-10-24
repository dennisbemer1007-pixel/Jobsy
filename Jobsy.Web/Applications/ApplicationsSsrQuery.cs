using Jobsy.Web.KandidaatBanen;

namespace Jobsy.Web.Applications;

internal static class ApplicationsSsrQuery
{
    internal const string TabSollicitaties = "sollicitaties";
    internal const string TabActiviteiten = "activiteiten";

    internal const string FilterAll = "all";
    internal const string FilterVacancy = "vacancy";
    internal const string FilterInitiative = "initiative";
    internal const string FilterExternal = "external";
    internal const string FilterDone = "done";

    internal static string NormalizeTab(string? raw)
        => string.Equals(raw, TabActiviteiten, StringComparison.OrdinalIgnoreCase)
            ? TabActiviteiten
            : TabSollicitaties;

    internal static string NormalizeFilter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return FilterAll;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            FilterVacancy or "vacature" => FilterVacancy,
            FilterInitiative or "initiatief" or "initiatives" => FilterInitiative,
            FilterExternal or "buiten" => FilterExternal,
            FilterDone or "afgerond" or "done" => FilterDone,
            _ => FilterAll
        };
    }

    internal static bool MatchesFilter(SollicitationRow row, string filter)
    {
        if (filter == FilterAll)
        {
            return true;
        }

        if (filter == FilterVacancy)
        {
            return !row.IsExternal && !row.IsInitiative;
        }

        if (filter == FilterInitiative)
        {
            return row.IsInitiative && !row.IsExternal;
        }

        if (filter == FilterExternal)
        {
            return row.IsExternal;
        }

        if (filter == FilterDone)
        {
            return row.IsDone;
        }

        return true;
    }

    internal static string BuildUrl(string tab, string filter, Guid? selectedId)
    {
        var q = new List<string>();
        if (!string.Equals(tab, TabSollicitaties, StringComparison.Ordinal))
        {
            q.Add($"tab={Uri.EscapeDataString(tab)}");
        }

        if (!string.Equals(filter, FilterAll, StringComparison.Ordinal))
        {
            q.Add($"filter={Uri.EscapeDataString(filter)}");
        }

        if (selectedId is Guid id && id != Guid.Empty)
        {
            q.Add($"sel={id:D}");
        }

        return q.Count == 0
            ? KbRoutes.Applications
            : $"{KbRoutes.Applications}?{string.Join("&", q)}";
    }
}
