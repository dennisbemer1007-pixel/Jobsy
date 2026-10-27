using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Werkgever;

internal static class EmployerApplicationsListSsrShells
{
    internal static string BuildBody(CultureState culture, EmployerApplicationsListSsrData.PageData data)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"wg-app wg-app--dense\" data-testid=\"wg-applications-list-ssr\" data-wg-list-ssr=\"1\">");
        if (!string.IsNullOrWhiteSpace(data.ErrorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
            sb.Append("</p></div>");
            return sb.ToString();
        }

        sb.Append("<div class=\"wg-card\">");
        AppendFilterForm(sb, culture, data);
        if (data.ReadOnlyHint)
        {
            sb.Append("<p class=\"wg-readonly-hint\" role=\"status\">");
            sb.Append(WebUtility.HtmlEncode(culture["WgApp.RmHint"]));
            sb.Append("</p>");
        }

        AppendViewToggle(sb, culture, listActive: true);
        if (data.PageItems.Count == 0)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(
                data.TotalFiltered == 0 && data.Query.VacancyId is null && !data.Query.OverdueOnly
                    ? culture["WgApp.Empty"]
                    : culture["WgApp.EmptyFilter"]));
            sb.Append("</p>");
        }
        else
        {
            AppendDesktopTable(sb, culture, data);
            AppendMobileCards(sb, culture, data);
            AppendPager(sb, culture, data);
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void AppendFilterForm(StringBuilder sb, CultureState culture, EmployerApplicationsListSsrData.PageData data)
    {
        var action = EmployerApplicationsListSsrQuery.BuildListPath(data.Query with { Page = 1 });
        var actionBase = action.Split('?', 2)[0];
        sb.Append("<form method=\"get\" action=\"");
        sb.Append(WebUtility.HtmlEncode(actionBase));
        sb.Append("\" class=\"ent-filter-bar\" data-wg-list-filter-form=\"1\" data-enhance=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"view\" value=\"");
        sb.Append(ApplicationPipelineRules.ViewList);
        sb.Append("\" />");
        if (!string.Equals(data.Query.ListStatusFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("<input type=\"hidden\" name=\"status\" value=\"");
            sb.Append(WebUtility.HtmlEncode(data.Query.ListStatusFilter));
            sb.Append("\" />");
        }

        sb.Append("<label class=\"ent-filter-bar__select\"><span class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Filter.Vacancy"]));
        sb.Append("</span><select name=\"vacancyId\">");
        sb.Append("<option value=\"\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Filter.Vacancy"]));
        sb.Append("</option>");
        foreach (var v in VacancyOptions(data.Vacancies))
        {
            sb.Append("<option value=\"");
            sb.Append(v.Id.ToString("D"));
            sb.Append('"');
            if (data.Query.VacancyId == v.Id)
            {
                sb.Append(" selected");
            }

            sb.Append('>');
            sb.Append(WebUtility.HtmlEncode(v.Title));
            sb.Append(" · ");
            sb.Append(WebUtility.HtmlEncode(v.CompanyName));
            sb.Append("</option>");
        }

        sb.Append("</select></label><label class=\"wg-app__overdue\">");
        sb.Append("<input type=\"checkbox\" name=\"filter\" value=\"overdue\"");
        if (data.Query.OverdueOnly)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Filter.Overdue"]));
        sb.Append("</label></form>");
    }

    private static IEnumerable<(Guid Id, string Title, string CompanyName)> VacancyOptions(IReadOnlyList<VacancyListItem> vacancies)
        => vacancies
            .Where(v => ManagedVacancyMapFilter.IsPublishedOpen(v.Status, v.FulfilledByApplicationId))
            .GroupBy(v => $"{(v.CompanyName ?? "").Trim()}|{v.Title.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(v => v.Id).First())
            .Select(v => (v.Id, v.Title, v.CompanyName ?? ""))
            .OrderBy(v => v.Title, StringComparer.CurrentCultureIgnoreCase);

    private static void AppendViewToggle(StringBuilder sb, CultureState culture, bool listActive)
    {
        sb.Append("<div class=\"wg-app__views\" role=\"group\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["WgNav.Applications"]));
        sb.Append("\"><a class=\"");
        sb.Append(listActive ? "" : "is-active");
        sb.Append("\" href=\"/werkgever/sollicitaties/pipeline\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.View.Pipeline"]));
        sb.Append("</a><a class=\"");
        sb.Append(listActive ? "is-active" : "");
        sb.Append("\" href=\"/werkgever/sollicitaties?view=lijst\" aria-current=\"");
        sb.Append(listActive ? "page" : "false");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.View.List"]));
        sb.Append("</a></div>");
    }

    private static void AppendDesktopTable(StringBuilder sb, CultureState culture, EmployerApplicationsListSsrData.PageData data)
    {
        sb.Append("<div class=\"wg-app__list-desktop\"><table class=\"ent-data-table\"><thead><tr>");
        AppendSortHeader(sb, data, "candidate", culture["WgApp.List.Candidate"]);
        AppendSortHeader(sb, data, "vacancy", culture["WgApp.List.Vacancy"]);
        sb.Append("<th scope=\"col\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.List.Branch"]));
        sb.Append("</th>");
        AppendSortHeader(sb, data, "status", culture["WgApp.List.Status"]);
        AppendSortHeader(sb, data, "created", culture["WgApp.List.Applied"]);
        AppendSortHeader(sb, data, "responded", culture["WgApp.List.FirstResponse"]);
        sb.Append("<th scope=\"col\"><span class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Details"]));
        sb.Append("</span></th></tr></thead><tbody>");
        foreach (var a in data.PageItems)
        {
            var name = ApplicationPipelineRules.DisplayName(a, culture["WgApp.Anonymous"]);
            sb.Append("<tr><td><a class=\"wg-app-sort\" href=\"/werkgever/sollicitaties/");
            sb.Append(a.Id.ToString("D"));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(name));
            sb.Append("</a></td><td>");
            sb.Append(WebUtility.HtmlEncode(a.VacancyTitle));
            sb.Append("</td><td>");
            sb.Append(WebUtility.HtmlEncode(a.CompanyName));
            sb.Append("</td><td><span class=\"status-pill wg-app-status--");
            sb.Append(ApplicationPipelineRules.StatusCss(a.Status));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture[ApplicationPipelineRules.StatusLabelKey(a.Status)]));
            sb.Append("</span></td><td class=\"tabular\">");
            sb.Append(WebUtility.HtmlEncode(AmsterdamTime.ToLocal(a.CreatedAt).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)));
            sb.Append("</td><td class=\"tabular\">");
            sb.Append(a.RespondedAt is DateTime responded
                ? WebUtility.HtmlEncode(AmsterdamTime.ToLocal(responded).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture))
                : "—");
            sb.Append("</td><td><a class=\"btn-compact\" href=\"/werkgever/sollicitaties/");
            sb.Append(a.Id.ToString("D"));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Details"]));
            sb.Append("</a></td></tr>");
        }

        sb.Append("</tbody></table></div>");
    }

    private static void AppendMobileCards(StringBuilder sb, CultureState culture, EmployerApplicationsListSsrData.PageData data)
    {
        sb.Append("<div class=\"wg-app__list-mobile\"><div class=\"wg-app-mobile-groups\">");
        foreach (var a in data.PageItems)
        {
            var name = ApplicationPipelineRules.DisplayName(a, culture["WgApp.Anonymous"]);
            sb.Append("<a class=\"wg-app-card\" href=\"/werkgever/sollicitaties/");
            sb.Append(a.Id.ToString("D"));
            sb.Append("\"><div class=\"wg-app-card__top\"><span class=\"wg-app-card__name\">");
            sb.Append(WebUtility.HtmlEncode(name));
            sb.Append("</span></div><span class=\"status-pill wg-app-status--");
            sb.Append(ApplicationPipelineRules.StatusCss(a.Status));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture[ApplicationPipelineRules.StatusLabelKey(a.Status)]));
            sb.Append("</span><span class=\"wg-app__meta muted\">");
            sb.Append(WebUtility.HtmlEncode(a.VacancyTitle));
            sb.Append(" · ");
            sb.Append(WebUtility.HtmlEncode(a.CompanyName));
            sb.Append("</span></a>");
        }

        sb.Append("</div></div>");
    }

    private static void AppendPager(StringBuilder sb, CultureState culture, EmployerApplicationsListSsrData.PageData data)
    {
        sb.Append("<div class=\"wg-vac__footer\"><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(
            culture["WgApp.Pager"],
            data.PageItems.Count,
            CountPhrase.Applications(culture, data.TotalFiltered))));
        sb.Append("</span>");
        if (data.TotalPages > 1)
        {
            sb.Append("<nav class=\"ent-pager\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["EntUi.Pager"]));
            sb.Append("\">");
            if (data.Query.Page > 1)
            {
                sb.Append("<a class=\"btn-compact\" href=\"");
                sb.Append(WebUtility.HtmlEncode(EmployerApplicationsListSsrQuery.BuildListPath(data.Query with { Page = data.Query.Page - 1 })));
                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(culture["EntUi.PrevPage"]));
                sb.Append("</a>");
            }

            sb.Append("<span class=\"ent-pager__meta\">");
            sb.Append(WebUtility.HtmlEncode(string.Format(culture["EntUi.PageOf"], data.Query.Page, data.TotalPages)));
            sb.Append("</span>");
            if (data.Query.Page < data.TotalPages)
            {
                sb.Append("<a class=\"btn-compact\" href=\"");
                sb.Append(WebUtility.HtmlEncode(EmployerApplicationsListSsrQuery.BuildListPath(data.Query with { Page = data.Query.Page + 1 })));
                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(culture["EntUi.NextPage"]));
                sb.Append("</a>");
            }

            sb.Append("</nav>");
        }

        sb.Append("</div>");
    }

    private static void AppendSortHeader(
        StringBuilder sb,
        EmployerApplicationsListSsrData.PageData data,
        string sortKey,
        string label)
    {
        var active = string.Equals(data.Query.SortKey, sortKey, StringComparison.OrdinalIgnoreCase);
        var nextAsc = active && !data.Query.SortAsc;
        var href = EmployerApplicationsListSsrQuery.BuildListPath(
            data.Query with { SortKey = sortKey, SortAsc = nextAsc, Page = 1 });
        sb.Append("<th scope=\"col\"><a class=\"wg-app-sort");
        if (active)
        {
            sb.Append(data.Query.SortAsc ? " is-asc" : " is-desc");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</a></th>");
    }
}
