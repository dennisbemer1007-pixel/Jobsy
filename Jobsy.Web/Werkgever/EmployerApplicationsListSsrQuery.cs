using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Jobsy.Web.Werkgever;

internal static class EmployerApplicationsListSsrQuery
{
    internal sealed record Parsed(
        bool OverdueOnly,
        Guid? VacancyId,
        Guid? CandidateDrawerId,
        int Page,
        string ListStatusFilter,
        string SortKey,
        bool SortAsc);

    internal static Parsed Parse(IQueryCollection query)
    {
        query.TryGetValue("filter", out var filterValues);
        var overdueOnly = string.Equals(
            filterValues.FirstOrDefault(),
            ApplicationPipelineRules.FilterOverdue,
            StringComparison.OrdinalIgnoreCase);

        Guid? vacancyId = null;
        var vacancyRaw = ApplicationPipelineRules.ResolveVacancyQuery(
            query.TryGetValue("vacancyId", out var vacancyIdValues) ? vacancyIdValues.ToString() : null,
            query.TryGetValue("vacature", out var vacatureValues) ? vacatureValues.ToString() : null,
            query.TryGetValue("vacancy", out var vacancyValues) ? vacancyValues.ToString() : null);
        if (Guid.TryParse(vacancyRaw, out var parsedVacancy))
        {
            vacancyId = parsedVacancy;
        }

        Guid? drawerId = null;
        var applicationRaw = query.TryGetValue("application", out var applicationValues)
            ? applicationValues.ToString()
            : query.TryGetValue("kandidaat", out var kandidaatValues)
                ? kandidaatValues.ToString()
                : null;
        if (Guid.TryParse(applicationRaw, out var parsedApp))
        {
            drawerId = parsedApp;
        }

        query.TryGetValue("page", out var pageValues);
        _ = int.TryParse(pageValues.FirstOrDefault(), out var page);
        page = Math.Max(1, page);

        query.TryGetValue("status", out var statusValues);
        var status = string.IsNullOrWhiteSpace(statusValues.FirstOrDefault())
            ? "all"
            : statusValues.FirstOrDefault()!.Trim().ToLowerInvariant();

        query.TryGetValue("sort", out var sortValues);
        var sort = string.IsNullOrWhiteSpace(sortValues.FirstOrDefault())
            ? "created"
            : sortValues.FirstOrDefault()!.Trim().ToLowerInvariant();

        query.TryGetValue("sortDir", out var sortDirValues);
        var sortAsc = string.Equals(sortDirValues.FirstOrDefault(), "asc", StringComparison.OrdinalIgnoreCase);

        return new Parsed(overdueOnly, vacancyId, drawerId, page, status, sort, sortAsc);
    }

    internal static bool IsListView(IQueryCollection query)
    {
        query.TryGetValue("view", out var viewValues);
        return string.Equals(
            viewValues.FirstOrDefault(),
            ApplicationPipelineRules.ViewList,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildPipelineRedirectPath(IQueryCollection query)
    {
        var parts = new List<string>();
        if (query.TryGetValue("filter", out var filterValues)
            && string.Equals(filterValues.FirstOrDefault(), ApplicationPipelineRules.FilterOverdue, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("filter=overdue");
        }

        var vacancyRaw = ApplicationPipelineRules.ResolveVacancyQuery(
            query.TryGetValue("vacancyId", out var vacancyIdValues) ? vacancyIdValues.ToString() : null,
            query.TryGetValue("vacature", out var vacatureValues) ? vacatureValues.ToString() : null,
            query.TryGetValue("vacancy", out var vacancyValues) ? vacancyValues.ToString() : null);
        if (Guid.TryParse(vacancyRaw, out var vacancyId))
        {
            parts.Add($"vacancyId={vacancyId:D}");
        }

        if (query.TryGetValue("kandidaat", out var kandidaatValues) && Guid.TryParse(kandidaatValues.FirstOrDefault(), out var kid))
        {
            parts.Add($"kandidaat={kid:D}");
        }
        else if (query.TryGetValue("application", out var applicationValues) && Guid.TryParse(applicationValues.FirstOrDefault(), out var appId))
        {
            parts.Add($"application={appId:D}");
        }

        return parts.Count == 0
            ? "/werkgever/sollicitaties/pipeline"
            : "/werkgever/sollicitaties/pipeline?" + string.Join('&', parts);
    }

    internal static string BuildListPath(Parsed query)
    {
        var parts = new List<string> { $"view={ApplicationPipelineRules.ViewList}" };
        if (query.OverdueOnly)
        {
            parts.Add("filter=overdue");
        }

        if (query.VacancyId is Guid vacancyId)
        {
            parts.Add($"vacancyId={vacancyId:D}");
        }

        if (query.CandidateDrawerId is Guid drawerId)
        {
            parts.Add($"kandidaat={drawerId:D}");
        }

        if (query.Page > 1)
        {
            parts.Add($"page={query.Page}");
        }

        if (!string.Equals(query.ListStatusFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"status={Uri.EscapeDataString(query.ListStatusFilter)}");
        }

        if (!string.Equals(query.SortKey, "created", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"sort={Uri.EscapeDataString(query.SortKey)}");
        }

        if (query.SortAsc)
        {
            parts.Add("sortDir=asc");
        }

        return "/werkgever/sollicitaties?" + string.Join('&', parts);
    }
}
