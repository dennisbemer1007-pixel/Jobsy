using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Werkgever;

internal static class EmployerApplicationsListSsrData
{
    internal sealed record PageData(
        EmployerApplicationsListSsrQuery.Parsed Query,
        EmployerRole? Role,
        bool CanWrite,
        bool ReadOnlyHint,
        IReadOnlyList<EmployerApplicationItem> PageItems,
        int TotalFiltered,
        int TotalPages,
        IReadOnlyList<VacancyListItem> Vacancies,
        string? ErrorMessage);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        EmployerScopeBootstrap scopeBootstrap,
        EmployerScopeState scope,
        AuthenticationStateProvider authState,
        EmployerApplicationsListSsrQuery.Parsed query,
        CancellationToken ct = default)
    {
        await scopeBootstrap.EnsureLoadedAsync(ct);
        var auth = await authState.GetAuthenticationStateAsync();
        var role = WerkgeverNav.ResolveRole(auth.User);

        try
        {
            int? overdue = query.OverdueOnly ? ApplicationPipelineRules.OverdueHours : null;
            var items = (await api.GetApplicationsAsync(
                vacancyId: query.VacancyId,
                overdueHours: overdue,
                ct: ct)).ToList();
            var companyIds = scope.CompanyIds.Count > 0 ? scope.CompanyIds : null;
            var vacancies = (await api.GetManagedVacanciesAsync(companyIds, ct)).ToList();

            var filtered = FilterList(items, query);
            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)ApplicationPipelineRules.ListPageSize));
            var page = Math.Clamp(query.Page, 1, totalPages);
            var pageItems = filtered
                .Skip((page - 1) * ApplicationPipelineRules.ListPageSize)
                .Take(ApplicationPipelineRules.ListPageSize)
                .ToList();

            if (page != query.Page)
            {
                query = query with { Page = page };
            }

            return new PageData(
                query,
                role,
                ApplicationPipelineRules.CanAct(role),
                role == EmployerRole.Regiomanager,
                pageItems,
                filtered.Count,
                totalPages,
                vacancies,
                null);
        }
        catch (Exception ex)
        {
            return new PageData(
                query,
                role,
                ApplicationPipelineRules.CanAct(role),
                role == EmployerRole.Regiomanager,
                [],
                0,
                1,
                [],
                ex.Message);
        }
    }

    private static IReadOnlyList<EmployerApplicationItem> FilterList(
        IReadOnlyList<EmployerApplicationItem> items,
        EmployerApplicationsListSsrQuery.Parsed query)
    {
        IEnumerable<EmployerApplicationItem> q = items;
        if (query.VacancyId is Guid vacancyId)
        {
            q = q.Where(a => a.VacancyId == vacancyId);
        }

        q = query.ListStatusFilter switch
        {
            "pending" => q.Where(a => ApplicationPipelineRules.StatusOf(a) == ApplicationStatus.Pending),
            "accepted" => q.Where(a => ApplicationPipelineRules.StatusOf(a) == ApplicationStatus.Accepted),
            "invited" => q.Where(a => ApplicationPipelineRules.StatusOf(a) == ApplicationStatus.EmployerContacting),
            "hired" => q.Where(a => ApplicationPipelineRules.StatusOf(a) == ApplicationStatus.Hired),
            "rejected" => q.Where(ApplicationPipelineRules.IsRejectedBucket),
            _ => q
        };

        q = query.SortKey switch
        {
            "candidate" => query.SortAsc
                ? q.OrderBy(a => DisplayNameKey(a), StringComparer.CurrentCultureIgnoreCase)
                : q.OrderByDescending(a => DisplayNameKey(a), StringComparer.CurrentCultureIgnoreCase),
            "vacancy" => query.SortAsc
                ? q.OrderBy(a => a.VacancyTitle, StringComparer.CurrentCultureIgnoreCase)
                : q.OrderByDescending(a => a.VacancyTitle, StringComparer.CurrentCultureIgnoreCase),
            "status" => query.SortAsc ? q.OrderBy(a => a.Status) : q.OrderByDescending(a => a.Status),
            "responded" => query.SortAsc
                ? q.OrderBy(a => a.RespondedAt ?? DateTime.MinValue)
                : q.OrderByDescending(a => a.RespondedAt ?? DateTime.MinValue),
            _ => query.SortAsc ? q.OrderBy(a => a.CreatedAt) : q.OrderByDescending(a => a.CreatedAt)
        };

        return q.ToList();
    }

    private static string DisplayNameKey(EmployerApplicationItem item)
        => ApplicationPipelineRules.DisplayName(item, "—");
}
