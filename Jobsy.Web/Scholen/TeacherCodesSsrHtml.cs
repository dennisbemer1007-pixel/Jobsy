using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for teacher class codes list (no Blazor).</summary>
public static class TeacherCodesSsrHtml
{
    public static string BuildBody(
        Guid classId,
        IReadOnlyList<TeacherCodeRowDto> rows,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc,
        string? successMessage,
        string? errorMessage,
        string addFormAntiforgery,
        string replaceFormAntiforgeryPrefix,
        CultureState culture)
    {
        var filtered = FilterAndSort(rows, query, statusFilter, sort, sortDesc);
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\">");
        sb.Append("<p class=\"sch-breadcrumb\"><a href=\"");
        sb.Append($"/leraar/klas/{classId:D}");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Nav.Overview"]));
        sb.Append("</a> / ");
        sb.Append(E(culture["Leraar.Codes.Title"]));
        sb.Append("</p><h1>");
        sb.Append(E(culture["Leraar.Codes.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["Leraar.Codes.NamesNote"]));
        sb.Append("</p></header>");

        sb.Append("<div class=\"sch-page-actions\">");
        sb.Append("<form method=\"post\" action=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes/add");
        sb.Append(PreserveQuerySuffix(classId, query, statusFilter, sort, sortDesc));
        sb.Append("\">");
        sb.Append(addFormAntiforgery);
        sb.Append("<button type=\"submit\" class=\"btn-compact\">");
        sb.Append(E(culture["Leraar.Codes.Add"]));
        sb.Append("</button></form></div>");

        AppendFilterBar(sb, classId, query, statusFilter, sort, sortDesc, culture);

        if (!string.IsNullOrEmpty(successMessage))
        {
            sb.Append("<p class=\"state-message\" role=\"status\">");
            sb.Append(E(successMessage));
            sb.Append("</p>");
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(errorMessage));
            sb.Append("</p>");
        }

        sb.Append("<div data-testid=\"leraar-code-table\">");
        if (!filtered.Any())
        {
            sb.Append("<p class=\"ent-data-table__state\">");
            sb.Append(E(culture["School.Codes.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("""
                <div class="ent-data-table-wrap"><table class="ent-data-table data-table"><thead><tr>
                """);
            AppendSortHeader(sb, classId, query, statusFilter, sort, sortDesc, "num", "#");
            sb.Append("<th scope=\"col\">");
            sb.Append(E(culture["School.Col.Code"]));
            sb.Append("</th>");
            AppendSortHeader(sb, classId, query, statusFilter, sort, sortDesc, "status", culture["School.Col.Status"]);
            sb.Append("<th scope=\"col\">");
            sb.Append(E(culture["School.Col.Progress"]));
            sb.Append("</th>");
            AppendSortHeader(sb, classId, query, statusFilter, sort, sortDesc, "active", culture["School.Col.LastActive"]);
            sb.Append("<th scope=\"col\"></th></tr></thead><tbody>");
            foreach (var row in filtered)
            {
                AppendRow(sb, classId, row, query, statusFilter, sort, sortDesc, replaceFormAntiforgeryPrefix, culture);
            }

            sb.Append("</tbody></table></div>");
        }

        sb.Append("</div>");
        sb.Append("<p class=\"muted sch-hint sch-hint--below\">");
        sb.Append(E(culture["Leraar.Codes.DeleteHint"]));
        sb.Append("</p>");
        return sb.ToString();
    }

    public static string ListPath(
        Guid classId,
        string? query = null,
        PupilCodeStatus? status = null,
        string sort = "num",
        bool sortDesc = false,
        string? flash = null,
        string? flashCode = null,
        string? error = null)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q.Add($"q={Uri.EscapeDataString(query.Trim())}");
        }

        if (status is PupilCodeStatus s)
        {
            q.Add($"status={Uri.EscapeDataString(s.ToString())}");
        }

        if (!string.Equals(sort, "num", StringComparison.Ordinal))
        {
            q.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        if (sortDesc)
        {
            q.Add("desc=1");
        }

        if (!string.IsNullOrEmpty(flash))
        {
            q.Add($"flash={Uri.EscapeDataString(flash)}");
        }

        if (!string.IsNullOrEmpty(flashCode))
        {
            q.Add($"code={Uri.EscapeDataString(flashCode)}");
        }

        if (!string.IsNullOrEmpty(error))
        {
            q.Add($"error={Uri.EscapeDataString(error)}");
        }

        var qs = q.Count == 0 ? "" : "?" + string.Join("&", q);
        return $"/leraar/klas/{classId:D}/codes{qs}";
    }

    internal static IReadOnlyList<TeacherCodeRowDto> FilterAndSort(
        IReadOnlyList<TeacherCodeRowDto> codes,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc)
    {
        IEnumerable<TeacherCodeRowDto> q = codes;
        if (statusFilter is PupilCodeStatus s)
        {
            q = q.Where(c => c.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(c => c.DisplayCode.Contains(query, StringComparison.OrdinalIgnoreCase)
                             || c.Number.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.Ordinal));
        }

        q = sort switch
        {
            "status" => sortDesc ? q.OrderByDescending(c => c.Status) : q.OrderBy(c => c.Status),
            "active" => sortDesc
                ? q.OrderByDescending(c => c.LastSeenAtUtc ?? DateTime.MinValue)
                : q.OrderBy(c => c.LastSeenAtUtc ?? DateTime.MaxValue),
            _ => sortDesc ? q.OrderByDescending(c => c.Number) : q.OrderBy(c => c.Number)
        };
        return q.ToList();
    }

    private static void AppendFilterBar(
        StringBuilder sb,
        Guid classId,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc,
        CultureState culture)
    {
        sb.Append("<form method=\"get\" action=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes");
        sb.Append("\" class=\"ent-filter-bar filter-bar\">");
        sb.Append("<input type=\"hidden\" name=\"sort\" value=\"");
        sb.Append(E(sort));
        sb.Append("\" />");
        if (sortDesc)
        {
            sb.Append("<input type=\"hidden\" name=\"desc\" value=\"1\" />");
        }

        sb.Append("<label class=\"ent-filter-bar__search\"><span class=\"visually-hidden\">");
        sb.Append(E(culture["Leraar.Codes.Filter"]));
        sb.Append("</span><input type=\"search\" name=\"q\" value=\"");
        sb.Append(E(query));
        sb.Append("\" placeholder=\"");
        sb.Append(E(culture["Leraar.Codes.Filter"]));
        sb.Append("\" /></label><div class=\"ent-filter-bar__chips\">");
        AppendFilterChip(sb, classId, query, null, statusFilter, sort, sortDesc, culture["School.Filter.All"]);
        AppendFilterChip(sb, classId, query, PupilCodeStatus.Completed, statusFilter, sort, sortDesc, culture["School.Status.Completed"]);
        AppendFilterChip(sb, classId, query, PupilCodeStatus.InProgress, statusFilter, sort, sortDesc, culture["Leraar.Status.Busy"]);
        AppendFilterChip(sb, classId, query, PupilCodeStatus.NotStarted, statusFilter, sort, sortDesc, culture["School.Status.NotStarted"]);
        sb.Append("</div><button type=\"submit\" class=\"btn-compact visually-hidden\">");
        sb.Append(E(culture["School.Filter.All"]));
        sb.Append("</button></form>");
    }

    private static void AppendFilterChip(
        StringBuilder sb,
        Guid classId,
        string query,
        PupilCodeStatus? chipStatus,
        PupilCodeStatus? activeStatus,
        string sort,
        bool sortDesc,
        string label)
    {
        var active = activeStatus == chipStatus;
        sb.Append("<a class=\"btn-compact");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"");
        sb.Append(ListPath(classId, query, chipStatus, sort, sortDesc));
        sb.Append("\">");
        sb.Append(E(label));
        sb.Append("</a>");
    }

    private static void AppendSortHeader(
        StringBuilder sb,
        Guid classId,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc,
        string sortKey,
        string label)
    {
        var nextDesc = sort == sortKey && !sortDesc;
        sb.Append("<th scope=\"col\"><a class=\"sch-sort\" href=\"");
        sb.Append(ListPath(classId, query, statusFilter, sortKey, nextDesc));
        sb.Append("\">");
        sb.Append(E(label));
        sb.Append("</a></th>");
    }

    private static void AppendRow(
        StringBuilder sb,
        Guid classId,
        TeacherCodeRowDto row,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc,
        string replaceFormAntiforgeryPrefix,
        CultureState culture)
    {
        var canView = row.Status == PupilCodeStatus.Completed
                      || (row.ProgressTotal == PupilMboHboTeacherProgress.BlockCount
                          && row.ProgressCurrent == PupilMboHboTeacherProgress.BlockCount);
        sb.Append("<tr class=\"ent-data-table__row\"><td class=\"tabular\" data-label=\"#\">");
        sb.Append(row.Number.ToString(CultureInfo.InvariantCulture));
        sb.Append("</td><td data-label=\"");
        sb.Append(E(culture["School.Col.Code"]));
        sb.Append("\"><span class=\"sch-code\">");
        sb.Append(E(row.DisplayCode));
        sb.Append("</span></td><td data-label=\"");
        sb.Append(E(culture["School.Col.Status"]));
        sb.Append("\"><span class=\"sch-pill ");
        sb.Append(StatusPill(row.Status));
        sb.Append("\">");
        sb.Append(E(PupilCodeStatusText.Label(culture, row.Status, row.LastSeenAtUtc)));
        sb.Append("</span></td><td data-label=\"");
        sb.Append(E(culture["School.Col.Progress"]));
        sb.Append("\"><progress class=\"sch-progress-meter\" max=\"");
        sb.Append(row.ProgressTotal.ToString(CultureInfo.InvariantCulture));
        sb.Append("\" value=\"");
        sb.Append(row.ProgressCurrent.ToString(CultureInfo.InvariantCulture));
        sb.Append("\"></progress><span class=\"tabular muted\">");
        sb.Append(row.ProgressCurrent.ToString(CultureInfo.InvariantCulture));
        sb.Append('/');
        sb.Append(row.ProgressTotal.ToString(CultureInfo.InvariantCulture));
        sb.Append("</span></td><td data-label=\"");
        sb.Append(E(culture["School.Col.LastActive"]));
        sb.Append("\">");
        sb.Append(row.LastSeenAtUtc is DateTime t
            ? E(SchoolActivityTime.Format(t))
            : "—");
        sb.Append("</td><td class=\"sch-row-actions\" data-label=\"\">");
        if (canView)
        {
            sb.Append("<a class=\"btn-compact\" href=\"");
            sb.Append($"/leraar/klas/{classId:D}/code/{row.Id:D}");
            sb.Append("\">");
            sb.Append(E(culture["Leraar.Codes.View"]));
            sb.Append("</a>");
        }

        sb.Append("<form method=\"post\" action=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes/{row.Id:D}/replace");
        sb.Append(PreserveQuerySuffix(classId, query, statusFilter, sort, sortDesc));
        sb.Append("\" style=\"display:inline\" data-sch-confirm=\"");
        sb.Append(E(culture["School.Codes.ReplaceConfirm"]));
        sb.Append("\">");
        sb.Append(replaceFormAntiforgeryPrefix);
        sb.Append("<button type=\"submit\" class=\"btn-compact\">");
        sb.Append(E(culture["School.Codes.Replace"]));
        sb.Append("</button></form></td></tr>");
    }

    private static string PreserveQuerySuffix(
        Guid classId,
        string query,
        PupilCodeStatus? statusFilter,
        string sort,
        bool sortDesc)
    {
        var path = ListPath(classId, query, statusFilter, sort, sortDesc);
        var qIndex = path.IndexOf('?', StringComparison.Ordinal);
        return qIndex < 0 ? "" : path[qIndex..];
    }

    private static string StatusPill(PupilCodeStatus s) => s switch
    {
        PupilCodeStatus.Completed => "sch-pill--ok",
        PupilCodeStatus.InProgress => "sch-pill--info",
        _ => "sch-pill--muted"
    };

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
