using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for teacher test window page (no Blazor).</summary>
public static class TeacherTestWindowSsrHtml
{
    public static string BuildBody(
        Guid classId,
        TeacherClassOverviewDto overview,
        string? windowError,
        string? pauseError,
        string windowFormAntiforgery,
        string pauseClearAntiforgery,
        CultureState culture)
    {
        var state = overview.TestWindow;
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\">");
        sb.Append("<p class=\"sch-breadcrumb\"><a href=\"");
        sb.Append($"/leraar/klas/{classId:D}");
        sb.Append("\">Klas ");
        sb.Append(E(overview.ClassName));
        sb.Append("</a> / ");
        sb.Append(E(culture["School.TestWindow.Title"]));
        sb.Append("</p><h1>");
        sb.Append(E(culture["School.TestWindow.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.TestWindow.Lead"]));
        sb.Append("</p></header>");

        sb.Append("<section class=\"sch-card\">");
        sb.Append("<p><span class=\"sch-pill ");
        sb.Append(PillClass(state));
        sb.Append("\">");
        sb.Append(E(StatusLabel(culture, state)));
        sb.Append("</span>");
        if (overview.TestWindowClosesOn is DateOnly d)
        {
            sb.Append("<span class=\"muted\"> · ");
            sb.Append(E(culture["School.TestWindow.Until"]));
            sb.Append(' ');
            sb.Append(d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            sb.Append("</span>");
        }

        sb.Append("</p>");

        if (!overview.ParentalInfoConfirmed)
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(culture["Leraar.TestWindow.ParentsMissing"]));
            sb.Append("</p>");
        }

        if (overview.LoginPausedUntilUtc is DateTime paused && paused > DateTime.UtcNow)
        {
            sb.Append("<div class=\"sch-card sch-card--nested\"><h2>");
            sb.Append(E(culture["Leraar.LoginPause.Title"]));
            sb.Append("</h2><p>");
            sb.Append(E(culture["Leraar.LoginPause.Body"]));
            sb.Append("</p><form method=\"post\" action=\"");
            sb.Append($"/leraar/klas/{classId:D}/testvenster/clear-pause");
            sb.Append("\">");
            sb.Append(pauseClearAntiforgery);
            sb.Append("<button type=\"submit\" class=\"btn-secondary\">");
            sb.Append(E(culture["Leraar.LoginPause.Clear"]));
            sb.Append("</button></form>");
            if (!string.IsNullOrEmpty(pauseError))
            {
                sb.Append("<p class=\"state-message error\">");
                sb.Append(E(pauseError));
                sb.Append("</p>");
            }

            sb.Append("</div>");
        }

        AppendWindowForms(sb, classId, state, overview.TestWindowClosesOn, windowFormAntiforgery, windowError, culture);
        sb.Append("</section>");
        return sb.ToString();
    }

    public static string PagePath(Guid classId, string? error = null, string? pauseError = null)
    {
        if (string.IsNullOrEmpty(error) && string.IsNullOrEmpty(pauseError))
        {
            return $"/leraar/klas/{classId:D}/testvenster";
        }

        var q = new List<string>();
        if (!string.IsNullOrEmpty(error))
        {
            q.Add($"error={Uri.EscapeDataString(error)}");
        }

        if (!string.IsNullOrEmpty(pauseError))
        {
            q.Add($"pauseError={Uri.EscapeDataString(pauseError)}");
        }

        return $"/leraar/klas/{classId:D}/testvenster?{string.Join("&", q)}";
    }

    private static void AppendWindowForms(
        StringBuilder sb,
        Guid classId,
        TestWindowState state,
        DateOnly? closesOn,
        string antiforgery,
        string? error,
        CultureState culture)
    {
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(error));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"sch-drawer-actions\">");
        if (state == TestWindowState.Open)
        {
            sb.Append("<form method=\"post\" action=\"");
            sb.Append($"/leraar/klas/{classId:D}/testvenster");
            sb.Append("\">");
            sb.Append(antiforgery);
            sb.Append("<input type=\"hidden\" name=\"action\" value=\"close\" />");
            sb.Append("<button type=\"submit\" class=\"btn-primary\">");
            sb.Append(E(culture["School.TestWindow.Close"]));
            sb.Append("</button></form>");
        }
        else
        {
            var (minDate, _) = JobsyDateInputRules.Bounds(JobsyDateConstraint.NotPast);
            var dateValue = closesOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            if (state is TestWindowState.NotOpen or TestWindowState.Closed)
            {
                sb.Append("<form method=\"post\" action=\"");
                sb.Append($"/leraar/klas/{classId:D}/testvenster");
                sb.Append("\" class=\"sch-test-window-form\">");
                sb.Append(antiforgery);
                sb.Append("<input type=\"hidden\" name=\"action\" value=\"");
                sb.Append(state == TestWindowState.Closed ? "reopen" : "open");
                sb.Append("\" /><label>");
                sb.Append(E(culture["School.TestWindow.ClosesOn"]));
                sb.Append(" <input class=\"input jobsy-date-field__input jobsy-date-field__input--native\" type=\"date\" name=\"closesOn\"");
                if (!string.IsNullOrEmpty(minDate))
                {
                    sb.Append(" min=\"");
                    sb.Append(E(minDate));
                    sb.Append('"');
                }

                if (!string.IsNullOrEmpty(dateValue))
                {
                    sb.Append(" value=\"");
                    sb.Append(E(dateValue));
                    sb.Append('"');
                }

                sb.Append(" data-testid=\"school-test-window-closes\" /></label><button type=\"submit\" class=\"btn-primary\">");
                sb.Append(E(state == TestWindowState.Closed
                    ? culture["School.TestWindow.Reopen"]
                    : culture["School.TestWindow.Open"]));
                sb.Append("</button></form>");
            }
        }

        sb.Append("</div>");
    }

    private static string StatusLabel(CultureState culture, TestWindowState state) => state switch
    {
        TestWindowState.Open => culture["School.TestWindow.StatusOpen"],
        TestWindowState.Closed => culture["School.TestWindow.StatusClosed"],
        _ => culture["School.TestWindow.StatusNotOpen"]
    };

    private static string PillClass(TestWindowState state) => state switch
    {
        TestWindowState.Open => "sch-pill--ok",
        TestWindowState.Closed => "sch-pill--muted",
        _ => "sch-pill--warn"
    };

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
