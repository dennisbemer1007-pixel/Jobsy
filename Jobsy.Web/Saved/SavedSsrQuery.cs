namespace Jobsy.Web.Saved;

internal static class SavedSsrQuery
{
    internal const string FilterAll = "all";
    internal const string FilterOpen = "open";
    internal const string FilterClosed = "closed";

    internal const string SortNewest = "newest";
    internal const string SortFit = "fit";
    internal const string SortClosing = "closing";

    internal static string NormalizeFilter(string? raw)
        => raw switch
        {
            FilterOpen => FilterOpen,
            FilterClosed => FilterClosed,
            _ => FilterAll,
        };

    internal static string NormalizeSort(string? raw)
        => raw switch
        {
            SortFit => SortFit,
            SortClosing => SortClosing,
            _ => SortNewest,
        };

    internal static string BuildUrl(string filter, string sort, string? error = null, bool saved = false)
    {
        var q = new List<string>
        {
            $"filter={Uri.EscapeDataString(filter)}",
            $"sort={Uri.EscapeDataString(sort)}",
        };
        if (saved)
        {
            q.Add("saved=1");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            q.Add("error=" + Uri.EscapeDataString(error));
        }

        return "/candidate/liked?" + string.Join("&", q);
    }
}
