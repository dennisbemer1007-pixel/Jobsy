using Jobsy.Core.Features;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

namespace Jobsy.Web.Saved;

internal static class SavedSsrData
{
    internal sealed record SharedGroup(
        Guid VacancyId,
        string VacancyTitle,
        int ShareCount,
        DateTime LastSharedAt);

    internal sealed record PageData(
        bool IsAuthenticatedCandidate,
        bool EmployersEnabled,
        bool ShowJobListTabs,
        bool AfterMatchEnabled,
        bool ShowActiviteitenTab,
        int UnreadMessagesTotal,
        string Filter,
        string Sort,
        string? ErrorMessage,
        bool ActionSaved,
        IReadOnlyList<CandidateEngagementItem> Items,
        IReadOnlyList<CandidateEngagementItem> Filtered,
        int CountOpen,
        int CountClosed,
        IReadOnlyList<SharedGroup> SharedGroups,
        bool ExternalVacanciesEnabled);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState _,
        bool isCandidate,
        string? filterRaw,
        string? sortRaw,
        string? errorMessage,
        bool actionSaved,
        CancellationToken cancellationToken = default)
    {
        var filter = SavedSsrQuery.NormalizeFilter(filterRaw);
        var sort = SavedSsrQuery.NormalizeSort(sortRaw);
        var flags = await featureFlags.GetAsync(cancellationToken);
        var employersOn = flags.EmployersEnabled;
        var showTabs = !RoleNavCatalog.ShowsSavedInNav(flags);
        var afterMatchOn = flags.AfterMatchEnabled;
        var showActiviteitenTab = flags.IsEnabled(PlatformFeature.ActivityLog);

        if (!isCandidate)
        {
            return new PageData(
                false,
                employersOn,
                showTabs,
                afterMatchOn,
                showActiviteitenTab,
                0,
                filter,
                sort,
                errorMessage,
                actionSaved,
                [],
                [],
                0,
                0,
                [],
                flags.CandidateExternalVacanciesEnabled);
        }

        if (!employersOn)
        {
            return new PageData(
                true,
                false,
                showTabs,
                afterMatchOn,
                showActiviteitenTab,
                0,
                filter,
                sort,
                errorMessage,
                actionSaved,
                [],
                [],
                0,
                0,
                [],
                false);
        }

        List<CandidateEngagementItem> items;
        try
        {
            items = (await api.GetMyLikesAsync(cancellationToken)).ToList();
        }
        catch
        {
            items = [];
        }

        IReadOnlyList<SharedGroup> shared;
        try
        {
            var shares = await api.GetMySharesAsync(cancellationToken);
            shared = GroupShares(shares);
        }
        catch
        {
            shared = [];
        }

        var countOpen = items.Count(i => !IsClosed(i));
        var countClosed = items.Count(IsClosed);
        var filtered = ApplyFilterSort(items, filter, sort);

        return new PageData(
            true,
            true,
            showTabs,
            afterMatchOn,
            showActiviteitenTab,
            0,
            filter,
            sort,
            errorMessage,
            actionSaved,
            items,
            filtered,
            countOpen,
            countClosed,
            shared,
            flags.CandidateExternalVacanciesEnabled);
    }

    internal static bool IsClosed(CandidateEngagementItem item)
        => string.Equals(item.SavedStateKind, nameof(KbSavedJobStateKind.Closed), StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.SavedStateKind, nameof(KbSavedJobStateKind.Fulfilled), StringComparison.OrdinalIgnoreCase);

    internal static bool MatchesFilter(CandidateEngagementItem item, string filter)
        => filter switch
        {
            SavedSsrQuery.FilterOpen => !IsClosed(item),
            SavedSsrQuery.FilterClosed => IsClosed(item),
            _ => true,
        };

    internal static IReadOnlyList<CandidateEngagementItem> SortedAll(PageData data)
        => ApplyFilterSort(data.Items, SavedSsrQuery.FilterAll, data.Sort);

    private static List<CandidateEngagementItem> ApplyFilterSort(
        IReadOnlyList<CandidateEngagementItem> items,
        string filter,
        string sort)
    {
        IEnumerable<CandidateEngagementItem> q = filter switch
        {
            SavedSsrQuery.FilterOpen => items.Where(i => !IsClosed(i)),
            SavedSsrQuery.FilterClosed => items.Where(i => IsClosed(i)),
            _ => items,
        };

        q = sort switch
        {
            SavedSsrQuery.SortFit => q.OrderByDescending(i => i.FitPercent ?? 0),
            SavedSsrQuery.SortClosing => q.OrderBy(i => i.DaysUntilEnd ?? int.MaxValue),
            _ => q.OrderByDescending(i => i.CreatedAt),
        };

        return q.ToList();
    }

    private static List<SharedGroup> GroupShares(IEnumerable<CandidateEngagementItem> items)
        => items
            .GroupBy(i => i.VacancyId)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.CreatedAt).First();
                return new SharedGroup(
                    g.Key,
                    first.VacancyTitle,
                    g.Count(),
                    g.Max(x => x.CreatedAt));
            })
            .OrderByDescending(g => g.LastSharedAt)
            .ToList();
}
