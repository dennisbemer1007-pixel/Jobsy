using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for teacher class overview (no Blazor).</summary>
public static class TeacherKlasOverviewSsrHtml
{
    public static string BuildBody(
        Guid classId,
        TeacherClassOverviewDto overview,
        int pendingTotal,
        IReadOnlyList<TeacherCodeRowDto> pendingPreview,
        TeacherMboHboGroupChartsDto? mboCharts,
        CultureState culture)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(overview.RetentionBanner))
        {
            sb.Append("<p class=\"sch-banner-info\">");
            sb.Append(E(overview.RetentionBanner));
            sb.Append("</p>");
        }

        sb.Append("<header class=\"sch-page-header\"><h1>Klas ");
        sb.Append(E(overview.ClassName));
        sb.Append("</h1><p class=\"muted\">");
        if (SchoolLevelRules.IsPrimary(overview.Level))
        {
            sb.Append(E(SchoolLevelRules.YearLabel(overview.Level, overview.Year)));
        }
        else
        {
            sb.Append(E(culture[SchoolLevelRules.LabelKey(overview.Level)]));
            sb.Append(" · ");
            sb.Append(E(SchoolLevelRules.YearLabel(overview.Level, overview.Year)));
            sb.Append(" · <span class=\"sch-pill ");
            sb.Append(overview.QuestionSet == PupilQuestionSet.Groep78 ? "sch-pill--ok" : "sch-pill--muted");
            sb.Append("\">");
            sb.Append(E(SchoolLevelRules.QuestionSetPillLabel(overview.QuestionSet)));
            sb.Append("</span>");
        }

        sb.Append(" · ");
        sb.Append(E(overview.SchoolYearLabel));
        sb.Append(" · ");
        sb.Append(overview.CodeCount.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(E(culture["Leraar.Kpi.Codes"]));
        sb.Append("</p>");
        if (overview.TestWindow == TestWindowState.Open && overview.TestWindowClosesOn is DateOnly until)
        {
            sb.Append("<p class=\"sch-pill sch-pill--ok\">");
            sb.Append(E(culture["School.TestWindow.StatusOpen"]));
            sb.Append(" · ");
            sb.Append(E(culture["School.TestWindow.Until"]));
            sb.Append(' ');
            sb.Append(until.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"sch-admin-toolbar sch-toolbar-wrap\">");
        if (overview.QuestionSet == PupilQuestionSet.MboHbo)
        {
            sb.Append("<a class=\"btn-compact\" href=\"");
            sb.Append($"/leraar/klas/{classId:D}/mbo-hbo");
            sb.Append("\">MBO/HBO voortgang</a>");
        }

        sb.Append("<a class=\"btn-compact\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/testvenster");
        sb.Append("\">");
        sb.Append(E(culture["School.Col.Window"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/codelist.pdf");
        sb.Append("\">");
        sb.Append(E(culture["School.CodeList.Pdf"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/codelist.csv");
        sb.Append("\">");
        sb.Append(E(culture["School.CodeList.Csv"]));
        sb.Append("</a></div></header>");

        AppendKpis(sb, overview, culture);
        AppendCharts(sb, overview, mboCharts, culture);

        if (pendingTotal > 0)
        {
            AppendPendingBanner(sb, classId, pendingTotal, pendingPreview, culture);
        }

        AppendCodesTable(sb, classId, overview, culture);
        AppendGroupInsights(sb, overview, culture);
        return sb.ToString();
    }

    private static void AppendKpis(StringBuilder sb, TeacherClassOverviewDto overview, CultureState culture)
    {
        sb.Append("<div class=\"sch-kpi-row sch-kpi-row--overview\" data-testid=\"leraar-kpi-row\">");
        AppendKpi(sb, culture["Leraar.Kpi.Pupils"], overview.CodeCount.ToString(CultureInfo.InvariantCulture), null);
        AppendKpi(
            sb,
            culture["Leraar.Kpi.Completed"],
            $"{overview.CompletedCount}/{overview.CodeCount}",
            $"{overview.CompletedPercent:0}%",
            "positive");
        AppendKpi(sb, culture["Leraar.Kpi.InProgress"], overview.InProgressCount.ToString(CultureInfo.InvariantCulture), null);
        var avg = overview.AverageMinutes is int m ? $"{m} min" : "n.v.t.";
        AppendKpi(sb, culture["Leraar.Kpi.AvgTime"], avg, null);
        sb.Append("</div>");
    }

    private static void AppendKpi(StringBuilder sb, string label, string value, string? delta, string deltaTone = "neutral")
    {
        sb.Append("<div class=\"ent-kpi-card\"><p class=\"ent-kpi-card__label\">");
        sb.Append(E(label));
        sb.Append("</p><p class=\"ent-kpi-card__value\">");
        sb.Append(E(value));
        sb.Append("</p>");
        if (!string.IsNullOrWhiteSpace(delta))
        {
            sb.Append("<p class=\"ent-kpi-card__delta ent-kpi-card__delta--");
            sb.Append(E(deltaTone));
            sb.Append("\">");
            sb.Append(E(delta));
            sb.Append("</p>");
        }

        sb.Append("</div>");
    }

    private static void AppendCharts(
        StringBuilder sb,
        TeacherClassOverviewDto overview,
        TeacherMboHboGroupChartsDto? mboCharts,
        CultureState culture)
    {
        var donutSegments = new List<TeacherOverviewChartSvg.Segment>
        {
            new(culture["Leraar.Overview.ProgressLegend.Completed"], overview.CompletedCount, "#33505f"),
            new(culture["Leraar.Overview.ProgressLegend.InProgress"], overview.InProgressCount, "#c4552a"),
            new(culture["Leraar.Overview.ProgressLegend.NotStarted"], overview.NotStartedCount, "#ddd8cf")
        };

        var barTitle = overview.QuestionSet == PupilQuestionSet.MboHbo
            ? culture["Leraar.Overview.Chart.Tests"]
            : culture["Leraar.Overview.Chart.Interests"];
        var barEmpty = overview.QuestionSet == PupilQuestionSet.MboHbo
            ? culture["Leraar.Overview.Chart.Empty"]
            : string.Format(CultureInfo.CurrentCulture, culture["Leraar.Group.Hidden"], overview.GroupInsights.CompletedCount);

        var barItems = BuildBarItems(overview, mboCharts, culture);

        sb.Append("<div class=\"sch-overview-charts\" data-testid=\"leraar-overview-charts\">");
        sb.Append("<section class=\"sch-card sch-chart-card sch-chart-donut\"><h2>");
        sb.Append(E(culture["Leraar.Overview.Chart.Status"]));
        sb.Append("</h2><div class=\"sch-chart-donut__wrap\">");
        sb.Append(TeacherOverviewChartSvg.Donut(donutSegments, culture["Leraar.Overview.Chart.Empty"]));
        sb.Append("<ul class=\"sch-chart-donut__legend\">");
        foreach (var seg in donutSegments)
        {
            sb.Append("<li><span class=\"sch-chart-donut__swatch\" style=\"background:");
            sb.Append(seg.Color);
            sb.Append("\"></span><span>");
            sb.Append(E(seg.Label));
            sb.Append("</span><strong class=\"tabular\">");
            sb.Append(seg.Value.ToString(CultureInfo.InvariantCulture));
            sb.Append("</strong></li>");
        }

        sb.Append("</ul></div></section>");
        sb.Append("<section class=\"sch-card sch-chart-card sch-chart-bars\"><h2>");
        sb.Append(E(barTitle));
        sb.Append("</h2>");
        if (barItems.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(barEmpty));
            sb.Append("</p>");
        }
        else
        {
            sb.Append(TeacherOverviewChartSvg.HorizontalBars(barItems, barEmpty));
        }

        sb.Append("</section></div>");
    }

    private static List<TeacherOverviewChartSvg.BarItem> BuildBarItems(
        TeacherClassOverviewDto overview,
        TeacherMboHboGroupChartsDto? mboCharts,
        CultureState culture)
    {
        if (overview.QuestionSet == PupilQuestionSet.MboHbo)
        {
            return mboCharts?.BlockCompletion
                       .Select(b => new TeacherOverviewChartSvg.BarItem(b.BlockKey, b.CompletedPercent, 100))
                       .ToList()
                   ?? [];
        }

        if (!overview.GroupInsights.Visible || overview.GroupInsights.RiasecBars.Count == 0)
        {
            return [];
        }

        var max = Math.Max(1, overview.GroupInsights.RiasecBars.Max(b => b.Count));
        return overview.GroupInsights.RiasecBars
            .Select(b => new TeacherOverviewChartSvg.BarItem(culture[b.KidLabelKey], b.Count, max))
            .ToList();
    }

    private static void AppendPendingBanner(
        StringBuilder sb,
        Guid classId,
        int pendingTotal,
        IReadOnlyList<TeacherCodeRowDto> pendingPreview,
        CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-pending-banner\" data-testid=\"leraar-pending-codes\">");
        sb.Append("<div class=\"sch-card__head\"><h2>");
        sb.Append(E(culture["Leraar.Pending.Title"]));
        sb.Append("</h2><a href=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Codes.All"]));
        sb.Append("</a></div><p class=\"muted\">");
        sb.Append(E(pendingTotal <= pendingPreview.Count
            ? culture.Format("Leraar.Pending.Subtitle", pendingTotal)
            : culture.Format("Leraar.Pending.Partial", pendingPreview.Count, pendingTotal)));
        sb.Append("</p><ul class=\"sch-pending-list\">");
        foreach (var row in pendingPreview)
        {
            sb.Append("<li><a class=\"sch-pending-list__link\" href=\"");
            sb.Append($"/leraar/klas/{classId:D}/code/{row.Id:D}");
            sb.Append("\"><code>");
            sb.Append(E(row.DisplayCode));
            sb.Append("</code><small>");
            sb.Append(E(PendingDetail(culture, row)));
            sb.Append("</small></a>");
            if (row.Status == PupilCodeStatus.NotStarted)
            {
                sb.Append("<span class=\"sch-pill sch-pill--muted\">");
                sb.Append(E(culture["Leraar.Kpi.NotStarted"]));
                sb.Append("</span>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendCodesTable(
        StringBuilder sb,
        Guid classId,
        TeacherClassOverviewDto overview,
        CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-codes-table\" data-testid=\"leraar-class-progress\">");
        sb.Append("<div class=\"sch-card__head\"><h2>");
        sb.Append(E(culture["Leraar.Codes.Title"]));
        sb.Append("</h2><a href=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Codes.All"]));
        sb.Append("</a></div><p class=\"muted\">");
        sb.Append(E(culture["Leraar.Codes.NamesNote"]));
        sb.Append("</p><div class=\"ent-data-table-wrap\">");
        if (overview.CodesPreview.Count == 0)
        {
            sb.Append("<p class=\"ent-data-table__state\">");
            sb.Append(E(culture["School.Codes.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("""
                <table class="ent-data-table data-table"><thead><tr>
                <th scope="col">#</th>
                """);
            sb.Append("<th scope=\"col\">");
            sb.Append(E(culture["School.Col.Code"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Status"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Progress"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.LastActive"]));
            sb.Append("</th><th scope=\"col\"></th></tr></thead><tbody>");
            foreach (var row in overview.CodesPreview)
            {
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
                sb.Append("\" aria-label=\"");
                sb.Append(E($"{row.ProgressCurrent}/{row.ProgressTotal}"));
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
                sb.Append("</td><td data-label=\"\">");
                if (row.Status == PupilCodeStatus.Completed)
                {
                    sb.Append("<a class=\"btn-compact\" href=\"");
                    sb.Append($"/leraar/klas/{classId:D}/code/{row.Id:D}");
                    sb.Append("\">");
                    sb.Append(E(culture["Leraar.Codes.View"]));
                    sb.Append("</a>");
                }

                sb.Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("</div></section>");
    }

    private static void AppendGroupInsights(StringBuilder sb, TeacherClassOverviewDto overview, CultureState culture)
    {
        TeacherGroupInsightsSsrHtml.AppendOverviewInsights(sb, overview.GroupInsights, overview.QuestionSet, culture);
    }

    private static string PendingDetail(CultureState culture, TeacherCodeRowDto row)
        => row.Status == PupilCodeStatus.NotStarted
            ? culture["Leraar.Pending.NotLoggedIn"]
            : culture.Format("Leraar.Pending.InProgress", row.ProgressCurrent, row.ProgressTotal);

    private static string StatusPill(PupilCodeStatus s) => s switch
    {
        PupilCodeStatus.Completed => "sch-pill--ok",
        PupilCodeStatus.InProgress => "sch-pill--info",
        _ => "sch-pill--muted"
    };

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
