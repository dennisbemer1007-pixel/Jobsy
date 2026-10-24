using Jobsy.Web.KandidaatBanen;

namespace Jobsy.Web.Applications;

internal static class ApplicationsSsrQuery
{
    internal const string TabSollicitaties = "sollicitaties";
    internal const string TabBerichten = "berichten";
    internal const string TabActiviteiten = "activiteiten";

    internal const string FilterAll = "all";
    internal const string FilterVacancy = "vacancy";
    internal const string FilterInitiative = "initiative";
    internal const string FilterExternal = "external";
    internal const string FilterDone = "done";

    internal static string NormalizeTab(string? raw)
    {
        if (string.Equals(raw, TabActiviteiten, StringComparison.OrdinalIgnoreCase))
        {
            return TabActiviteiten;
        }

        if (string.Equals(raw, TabBerichten, StringComparison.OrdinalIgnoreCase))
        {
            return TabBerichten;
        }

        return TabSollicitaties;
    }

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

    internal static string BuildUrl(string tab, string filter, Guid? selectedId, Guid? threadApplicationId = null)
    {
        var q = new List<string>();
        if (!string.Equals(tab, TabSollicitaties, StringComparison.Ordinal))
        {
            q.Add($"tab={Uri.EscapeDataString(tab)}");
        }

        if (string.Equals(tab, TabSollicitaties, StringComparison.Ordinal)
            && !string.Equals(filter, FilterAll, StringComparison.Ordinal))
        {
            q.Add($"filter={Uri.EscapeDataString(filter)}");
        }

        if (string.Equals(tab, TabBerichten, StringComparison.Ordinal)
            && threadApplicationId is Guid threadId
            && threadId != Guid.Empty)
        {
            q.Add($"thread={threadId:D}");
        }

        if (string.Equals(tab, TabSollicitaties, StringComparison.Ordinal)
            && selectedId is Guid id
            && id != Guid.Empty)
        {
            q.Add($"sel={id:D}");
        }

        return q.Count == 0
            ? KbRoutes.Applications
            : $"{KbRoutes.Applications}?{string.Join("&", q)}";
    }

    internal static bool TryGetSafeApplicationsReturn(string? raw, out string href)
    {
        href = "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!raw.StartsWith(KbRoutes.Applications, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (raw.Contains("//", StringComparison.Ordinal) || raw.Contains('\\'))
        {
            return false;
        }

        href = raw;
        return true;
    }
}
