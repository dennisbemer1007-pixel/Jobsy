using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for school admin portal pages (no Blazor).</summary>
public static class SchoolPortalSsrHtml
{
    private static string E(string? s) => SchoolPortalSsrHelpers.E(s);

    public static string Dashboard(SchoolDashboardDto dash, CultureState culture)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(dash.RetentionBanner))
        {
            sb.Append("<p class=\"sch-banner-info\">");
            sb.Append(E(dash.RetentionBanner));
            sb.Append("</p>");
        }

        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.DashboardTitle"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.DashboardIntro"]));
        sb.Append("</p><p class=\"sch-page-meta\">");
        sb.Append(E(dash.SchoolName));
        sb.Append(" · ");
        sb.Append(E(dash.SchoolYearLabel));
        sb.Append(" · ");
        sb.Append(dash.ClassCount.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(E(culture["School.Kpi.Classes"]));
        sb.Append("</p><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"/school/klassen/nieuw\">");
        sb.Append(E(culture["School.Class.New"]));
        sb.Append("</a></div></header>");

        sb.Append("<div class=\"sch-kpi-row\">");
        SchoolPortalSsrHelpers.AppendKpi(sb, culture["School.Kpi.Classes"], dash.ClassCount.ToString(CultureInfo.InvariantCulture), null);
        SchoolPortalSsrHelpers.AppendKpi(sb, culture["School.Kpi.Codes"], dash.CodeCount.ToString(CultureInfo.InvariantCulture), culture["School.Kpi.CodesNote"]);
        SchoolPortalSsrHelpers.AppendKpi(sb, culture["School.Kpi.Completed"], dash.CompletedCount.ToString(CultureInfo.InvariantCulture),
            $"{dash.CompletedPercent:0}%", "positive");
        var mfaDelta = dash.TeachersWithoutMfa > 0
            ? string.Format(culture["School.Kpi.TeachersNoMfa"], dash.TeachersWithoutMfa)
            : null;
        SchoolPortalSsrHelpers.AppendKpi(sb, culture["School.Kpi.Teachers"], dash.TeacherCount.ToString(CultureInfo.InvariantCulture),
            mfaDelta, dash.TeachersWithoutMfa > 0 ? "warn" : "neutral");
        sb.Append("</div>");

        sb.Append("<div class=\"sch-dash-grid\"><section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Dash.TestsPerClass"]));
        sb.Append("</h2>");
        if (dash.Classes.Count == 0)
        {
            SchoolPortalSsrHelpers.AppendEmptyTable(sb, culture["School.Class.Empty"]);
        }
        else
        {
            SchoolPortalSsrHelpers.AppendDataTableOpen(sb);
            sb.Append("<thead><tr><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Class"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Teacher"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Codes"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Started"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Completed"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Progress"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Col.Window"]));
            sb.Append("</th></tr></thead><tbody>");
            foreach (var row in dash.Classes)
            {
                sb.Append("<tr class=\"ent-data-table__row\" data-sch-row-href=\"/school/klassen/");
                sb.Append(row.Id.ToString("D"));
                sb.Append("\" style=\"cursor:pointer\"><td data-label=\"");
                sb.Append(E(culture["School.Col.Class"]));
                sb.Append("\">");
                sb.Append(E(row.ClassName));
                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Col.Teacher"]));
                sb.Append("\">");
                sb.Append(E(row.TeacherNames ?? "—"));
                sb.Append("</td><td class=\"tabular\" data-label=\"");
                sb.Append(E(culture["School.Col.Codes"]));
                sb.Append("\">");
                sb.Append(row.CodeCount.ToString(CultureInfo.InvariantCulture));
                sb.Append("</td><td class=\"tabular\" data-label=\"");
                sb.Append(E(culture["School.Col.Started"]));
                sb.Append("\">");
                sb.Append(row.StartedCount.ToString(CultureInfo.InvariantCulture));
                sb.Append("</td><td class=\"tabular\" data-label=\"");
                sb.Append(E(culture["School.Col.Completed"]));
                sb.Append("\">");
                sb.Append(row.CompletedCount.ToString(CultureInfo.InvariantCulture));
                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Col.Progress"]));
                sb.Append("\"><span class=\"tabular\">");
                sb.Append(row.CompletionPercent.ToString("0", CultureInfo.InvariantCulture));
                sb.Append("%</span></td><td data-label=\"");
                sb.Append(E(culture["School.Col.Window"]));
                sb.Append("\"><span class=\"sch-pill ");
                sb.Append(SchoolPortalSsrHelpers.WindowPill(row.TestWindow));
                sb.Append("\">");
                sb.Append(E(SchoolPortalSsrHelpers.WindowLabel(culture, row.TestWindow, row.TestWindowClosesOn)));
                sb.Append("</span></td></tr>");
            }

            sb.Append("</tbody>");
            SchoolPortalSsrHelpers.AppendDataTableClose(sb);
        }

        sb.Append("</section><aside class=\"sch-dash-side\"><section class=\"sch-card\"><div class=\"sch-card__head\"><h2>");
        sb.Append(E(culture["School.Dash.Todo"]));
        sb.Append("</h2><a href=\"/school/te-doen\">");
        sb.Append(E(culture["School.Dash.TodoAll"]));
        sb.Append("</a></div>");
        if (dash.Todos.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["School.Todo.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"sch-todo-list\">");
            foreach (var t in dash.Todos)
            {
                sb.Append("<li>");
                if (!string.IsNullOrEmpty(t.Href))
                {
                    sb.Append("<a href=\"");
                    sb.Append(E(t.Href));
                    sb.Append("\">");
                    sb.Append(E(t.Title));
                    sb.Append("</a>");
                }
                else
                {
                    sb.Append(E(t.Title));
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("</section><section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Dash.Interests"]));
        sb.Append("</h2>");
        if (dash.SchoolRiasecTop3 is null || dash.SchoolRiasecTop3.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["School.Dash.InterestsEmpty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"sch-riasec\">");
            foreach (var n in dash.SchoolRiasecTop3)
            {
                sb.Append("<li><span>");
                sb.Append(E(n.Key));
                sb.Append("</span> <strong class=\"tabular\">");
                sb.Append(n.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append("</strong></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("</section></aside></div>");
        return sb.ToString();
    }

    public static string Todos(IReadOnlyList<SchoolTodoItemDto> items, CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Todo.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Todo.Lead"]));
        sb.Append("</p></header>");
        if (items.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["School.Todo.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"sch-todo-list sch-todo-list--full\">");
            foreach (var t in items)
            {
                sb.Append("<li>");
                if (!string.IsNullOrEmpty(t.Href))
                {
                    sb.Append("<a href=\"");
                    sb.Append(E(t.Href));
                    sb.Append("\">");
                    sb.Append(E(t.Title));
                    sb.Append("</a>");
                }
                else
                {
                    sb.Append(E(t.Title));
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        return sb.ToString();
    }

    public static string ClassList(
        IReadOnlyList<SchoolPortalClassListItemDto> rows,
        CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Class.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Class.Lead"]));
        sb.Append("</p><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"/school/klassen/nieuw\">");
        sb.Append(E(culture["School.Class.New"]));
        sb.Append("</a></div></header>");
        if (rows.Count == 0)
        {
            SchoolPortalSsrHelpers.AppendEmptyTable(sb, culture["School.Class.Empty"]);
            return sb.ToString();
        }

        SchoolPortalSsrHelpers.AppendDataTableOpen(sb);
        sb.Append("<thead><tr>");
        foreach (var col in new[]
                 {
                     "School.Col.Class", "School.Col.Level", "School.Col.Year", "School.Col.QuestionSet",
                     "School.Col.Teacher", "School.Col.Codes", "School.Col.Completed", "School.Col.Parents", "School.Col.Window"
                 })
        {
            sb.Append("<th scope=\"col\">");
            sb.Append(E(culture[col]));
            sb.Append("</th>");
        }

        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr class=\"ent-data-table__row\" data-sch-row-href=\"/school/klassen/");
            sb.Append(row.Id.ToString("D"));
            sb.Append("\" style=\"cursor:pointer\"><td data-label=\"");
            sb.Append(E(culture["School.Col.Class"]));
            sb.Append("\">");
            sb.Append(E(row.ClassName));
            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.Level"]));
            sb.Append("\">");
            sb.Append(E(culture[SchoolLevelRules.LabelKey(row.Level)]));
            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.Year"]));
            sb.Append("\">");
            sb.Append(E(SchoolLevelRules.YearLabel(row.Level, row.Year)));
            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.QuestionSet"]));
            sb.Append("\"><span class=\"sch-pill ");
            sb.Append(row.QuestionSet == PupilQuestionSet.Groep78 ? "sch-pill--ok" : "sch-pill--muted");
            sb.Append("\">");
            sb.Append(E(SchoolLevelRules.QuestionSetPillLabel(row.QuestionSet)));
            sb.Append("</span></td><td data-label=\"");
            sb.Append(E(culture["School.Col.Teacher"]));
            sb.Append("\">");
            sb.Append(E(row.TeacherNames.Count == 0 ? "—" : string.Join(", ", row.TeacherNames)));
            sb.Append("</td><td class=\"tabular\" data-label=\"");
            sb.Append(E(culture["School.Col.Codes"]));
            sb.Append("\">");
            sb.Append(row.CodeCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("</td><td class=\"tabular\" data-label=\"");
            sb.Append(E(culture["School.Col.Completed"]));
            sb.Append("\">");
            sb.Append(row.CompletedCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.Parents"]));
            sb.Append("\">");
            if (row.ParentalInfoConfirmed)
            {
                sb.Append("<span class=\"sch-pill sch-pill--ok\">");
                sb.Append(E(culture["School.Parents.Confirmed"]));
                sb.Append("</span>");
            }
            else
            {
                sb.Append("<span class=\"sch-pill sch-pill--warn\">");
                sb.Append(E(culture["School.Parents.Pending"]));
                sb.Append("</span>");
            }

            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.Window"]));
            sb.Append("\"><span class=\"sch-pill ");
            sb.Append(SchoolPortalSsrHelpers.WindowPill(row.TestWindow));
            sb.Append("\">");
            sb.Append(E(SchoolPortalSsrHelpers.WindowLabel(culture, row.TestWindow, row.TestWindowClosesOn)));
            sb.Append("</span></td></tr>");
        }

        sb.Append("</tbody>");
        SchoolPortalSsrHelpers.AppendDataTableClose(sb);
        return sb.ToString();
    }

    public static string ClassCreatePage(
        SchoolClassFormSsrHtml.FormModel model,
        IReadOnlyList<SchoolClassFormSsrHtml.TeacherOption> teachers,
        string antiforgery,
        CultureState culture,
        string? error = null)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Class.New"]));
        sb.Append("</h1></header><section class=\"sch-card\">");
        sb.Append(SchoolClassFormSsrHtml.Build(
            model,
            teachers,
            "/school/klassen",
            "/school/klassen",
            antiforgery,
            culture,
            error));
        sb.Append("</section>");
        return sb.ToString();
    }

    public static string ClassDetail(
        Guid classId,
        SchoolPortalClassDetailDto detail,
        bool editMode,
        SchoolClassFormSsrHtml.FormModel? editModel,
        IReadOnlyList<SchoolClassFormSsrHtml.TeacherOption>? editTeachers,
        string antiforgery,
        CultureState culture,
        string? actionError,
        string? windowError)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><p class=\"sch-breadcrumb\"><a href=\"/school/klassen\">");
        sb.Append(E(culture["School.Nav.Classes"]));
        sb.Append("</a> / ");
        sb.Append(E(detail.ClassName));
        sb.Append("</p><h1>");
        sb.Append(E(detail.ClassName));
        sb.Append("</h1><p class=\"muted\">");
        if (SchoolLevelRules.IsPrimary(detail.Level))
        {
            sb.Append(E(SchoolLevelRules.YearLabel(detail.Level, detail.Year)));
        }
        else
        {
            sb.Append(E(culture[SchoolLevelRules.LabelKey(detail.Level)]));
            sb.Append(" · ");
            sb.Append(E(SchoolLevelRules.YearLabel(detail.Level, detail.Year)));
            sb.Append(" · <span class=\"sch-pill ");
            sb.Append(detail.QuestionSet == PupilQuestionSet.Groep78 ? "sch-pill--ok" : "sch-pill--muted");
            sb.Append("\">");
            sb.Append(E(SchoolLevelRules.QuestionSetPillLabel(detail.QuestionSet)));
            sb.Append("</span>");
        }

        sb.Append(" · ");
        sb.Append(E(detail.SchoolYearLabel));
        sb.Append("</p><div class=\"sch-admin-toolbar sch-toolbar-wrap\">");
        sb.Append("<a class=\"btn-compact\" href=\"/school/klassen/");
        sb.Append(classId.ToString("D"));
        sb.Append("?edit=1\">");
        sb.Append(E(culture["School.Class.Edit"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"/school/klassen/");
        sb.Append(classId.ToString("D"));
        sb.Append("/codelist.pdf\">");
        sb.Append(E(culture["School.CodeList.Pdf"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"/school/klassen/");
        sb.Append(classId.ToString("D"));
        sb.Append("/codelist.csv\">");
        sb.Append(E(culture["School.CodeList.Csv"]));
        sb.Append("</a><form method=\"post\" action=\"/school/klassen/");
        sb.Append(classId.ToString("D"));
        sb.Append("/codes/add\" class=\"sch-inline-form\">");
        sb.Append(antiforgery);
        sb.Append("<button type=\"submit\" class=\"btn-compact\">");
        sb.Append(E(culture["School.CodeList.Add"]));
        sb.Append("</button></form></div>");
        if (!string.IsNullOrEmpty(actionError))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(actionError));
            sb.Append("</p>");
        }

        sb.Append("</header>");

        if (editMode && editModel is not null)
        {
            sb.Append("<section class=\"sch-card\"><h2>");
            sb.Append(E(string.Format("Klas {0} bewerken", detail.ClassName)));
            sb.Append("</h2>");
            sb.Append(SchoolClassFormSsrHtml.Build(
                editModel,
                editTeachers ?? [],
                $"/school/klassen/{classId:D}",
                $"/school/klassen/{classId:D}",
                antiforgery,
                culture,
                actionError));
            sb.Append("</section>");
        }

        AppendParentsSection(sb, classId, detail, antiforgery, culture);
        AppendTestWindowSection(sb, classId, detail, antiforgery, culture, windowError);
        AppendCodesSection(sb, classId, detail, antiforgery, culture);
        AppendDeleteSection(sb, classId, antiforgery, culture);
        return sb.ToString();
    }

    private static void AppendParentsSection(
        StringBuilder sb,
        Guid classId,
        SchoolPortalClassDetailDto detail,
        string antiforgery,
        CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-card--parents\"><h2>");
        sb.Append(E(culture["School.Parents.Title"]));
        sb.Append("</h2><p>");
        sb.Append(E(culture["School.Parents.Body"]));
        sb.Append("</p><p><a href=\"/school/privacy#ouderbrief\">");
        sb.Append(E(culture["School.Parents.LetterLink"]));
        sb.Append("</a></p>");
        if (detail.ParentalInfoConfirmed)
        {
            sb.Append("<p class=\"sch-pill sch-pill--ok\">");
            sb.Append(E(culture["School.Parents.ConfirmedOn"]));
            if (detail.ParentalInfoConfirmedAtUtc is DateTime confirmed)
            {
                sb.Append(' ');
                sb.Append(E(SchoolPortalSsrHelpers.FormatAmsterdamDate(confirmed)));
            }

            if (!string.IsNullOrEmpty(detail.ParentalInfoConfirmedByName))
            {
                sb.Append(' ');
                sb.Append(E(culture["School.Parents.By"]));
                sb.Append(' ');
                sb.Append(E(detail.ParentalInfoConfirmedByName));
            }

            sb.Append("</p>");
            if (detail.TestWindow == TestWindowState.NotOpen)
            {
                sb.Append("<form method=\"post\" action=\"/school/klassen/");
                sb.Append(classId.ToString("D"));
                sb.Append("/parental\"><input type=\"hidden\" name=\"confirmed\" value=\"false\" />");
                sb.Append(antiforgery);
                sb.Append("<button type=\"submit\" class=\"btn-compact\">");
                sb.Append(E(culture["School.Parents.Withdraw"]));
                sb.Append("</button></form>");
            }
        }
        else
        {
            sb.Append("<form method=\"post\" action=\"/school/klassen/");
            sb.Append(classId.ToString("D"));
            sb.Append("/parental\"><input type=\"hidden\" name=\"confirmed\" value=\"true\" />");
            sb.Append(antiforgery);
            sb.Append("<label class=\"filter-check\"><input type=\"checkbox\" name=\"parentsCheck\" value=\"1\" required /> ");
            sb.Append(E(string.Format(culture["School.Parents.ConfirmCheckbox"], detail.ClassName)));
            sb.Append("</label><button type=\"submit\" class=\"btn-primary\">");
            sb.Append(E(culture["School.Parents.Confirm"]));
            sb.Append("</button></form>");
        }

        sb.Append("</section>");
    }

    private static void AppendTestWindowSection(
        StringBuilder sb,
        Guid classId,
        SchoolPortalClassDetailDto detail,
        string antiforgery,
        CultureState culture,
        string? windowError)
    {
        sb.Append("<section class=\"sch-card\" id=\"testvenster\"><h2>");
        sb.Append(E(culture["School.Col.Window"]));
        sb.Append("</h2><p><span class=\"sch-pill ");
        sb.Append(SchoolPortalSsrHelpers.WindowPill(detail.TestWindow));
        sb.Append("\">");
        sb.Append(E(SchoolPortalSsrHelpers.WindowLabel(culture, detail.TestWindow, detail.TestWindowClosesOn)));
        sb.Append("</span>");
        if (detail.TestWindowClosesOn is DateOnly d)
        {
            sb.Append("<span class=\"muted\"> · ");
            sb.Append(E(culture["School.TestWindow.Until"]));
            sb.Append(' ');
            sb.Append(d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            sb.Append("</span>");
        }

        sb.Append("</p>");
        if (!detail.ParentalInfoConfirmed)
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(culture["Leraar.TestWindow.ParentsMissing"]));
            sb.Append("</p>");
        }

        if (!string.IsNullOrEmpty(windowError))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(windowError));
            sb.Append("</p>");
        }

        var actionBase = $"/school/klassen/{classId:D}/test-window";
        if (detail.TestWindow == TestWindowState.Open)
        {
            sb.Append("<form method=\"post\" action=\"");
            sb.Append(actionBase);
            sb.Append("\">");
            sb.Append(antiforgery);
            sb.Append("<input type=\"hidden\" name=\"action\" value=\"close\" /><button type=\"submit\" class=\"btn-primary\">");
            sb.Append(E(culture["School.TestWindow.Close"]));
            sb.Append("</button></form>");
        }
        else
        {
            var (minDate, _) = JobsyDateInputRules.Bounds(JobsyDateConstraint.NotPast);
            var dateValue = detail.TestWindowClosesOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
            sb.Append("<form method=\"post\" action=\"");
            sb.Append(actionBase);
            sb.Append("\" class=\"sch-test-window-form\">");
            sb.Append(antiforgery);
            sb.Append("<input type=\"hidden\" name=\"action\" value=\"");
            sb.Append(detail.TestWindow == TestWindowState.Closed ? "reopen" : "open");
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
            sb.Append(E(detail.TestWindow == TestWindowState.Closed
                ? culture["School.TestWindow.Reopen"]
                : culture["School.TestWindow.Open"]));
            sb.Append("</button></form>");
        }

        sb.Append("</section>");
    }

    private static void AppendCodesSection(
        StringBuilder sb,
        Guid classId,
        SchoolPortalClassDetailDto detail,
        string antiforgery,
        CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Codes.Title"]));
        sb.Append("</h2>");
        if (detail.Codes.Count == 0)
        {
            SchoolPortalSsrHelpers.AppendEmptyTable(sb, culture["School.Codes.Empty"]);
            sb.Append("</section>");
            return;
        }

        SchoolPortalSsrHelpers.AppendDataTableOpen(sb);
        sb.Append("<thead><tr><th scope=\"col\">#</th><th scope=\"col\">");
        sb.Append(E(culture["School.Col.Code"]));
        sb.Append("</th><th scope=\"col\">");
        sb.Append(E(culture["School.Col.Status"]));
        sb.Append("</th><th scope=\"col\">");
        sb.Append(E(culture["School.Col.LastActive"]));
        sb.Append("</th><th scope=\"col\"></th></tr></thead><tbody>");
        var replaceConfirm = E(culture["School.Codes.ReplaceConfirm"]);
        var deleteConfirm = E(culture["School.Codes.DeleteConfirm"]);
        foreach (var codeRow in detail.Codes)
        {
            sb.Append("<tr class=\"ent-data-table__row\"><td class=\"tabular\" data-label=\"#\">");
            sb.Append(codeRow.Number.ToString(CultureInfo.InvariantCulture));
            sb.Append("</td><td data-label=\"");
            sb.Append(E(culture["School.Col.Code"]));
            sb.Append("\"><span class=\"sch-code\">");
            sb.Append(E(codeRow.DisplayCode));
            sb.Append("</span></td><td data-label=\"");
            sb.Append(E(culture["School.Col.Status"]));
            sb.Append("\"><span class=\"sch-pill ");
            sb.Append(SchoolPortalSsrHelpers.StatusPill(codeRow.Status));
            sb.Append("\">");
            sb.Append(E(SchoolPortalSsrHelpers.StatusLabel(culture, codeRow)));
            sb.Append("</span></td><td data-label=\"");
            sb.Append(E(culture["School.Col.LastActive"]));
            sb.Append("\">");
            sb.Append(codeRow.LastSeenAtUtc is DateTime t
                ? E(SchoolActivityTime.Format(t))
                : "—");
            sb.Append("</td><td class=\"sch-row-actions\" data-label=\"\">");
            sb.Append("<form method=\"post\" action=\"/school/klassen/");
            sb.Append(classId.ToString("D"));
            sb.Append("/codes/");
            sb.Append(codeRow.Id.ToString("D"));
            sb.Append("/replace\" class=\"sch-inline-form\" data-sch-confirm=\"");
            sb.Append(E(replaceConfirm));
            sb.Append("\">");
            sb.Append(antiforgery);
            sb.Append("<button type=\"submit\" class=\"btn-compact\">");
            sb.Append(E(culture["School.Codes.Replace"]));
            sb.Append("</button></form><form method=\"post\" action=\"/school/klassen/");
            sb.Append(classId.ToString("D"));
            sb.Append("/codes/");
            sb.Append(codeRow.Id.ToString("D"));
            sb.Append("/delete\" class=\"sch-inline-form\" data-sch-confirm=\"");
            sb.Append(E(deleteConfirm));
            sb.Append("\">");
            sb.Append(antiforgery);
            sb.Append("<button type=\"submit\" class=\"btn-outline-danger\">");
            sb.Append(E(culture["School.Codes.Delete"]));
            sb.Append("</button></form></td></tr>");
        }

        sb.Append("</tbody>");
        SchoolPortalSsrHelpers.AppendDataTableClose(sb);
        sb.Append("</section>");
    }

    private static void AppendDeleteSection(
        StringBuilder sb,
        Guid classId,
        string antiforgery,
        CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Class.Danger"]));
        sb.Append("</h2><form method=\"post\" action=\"/school/klassen/");
        sb.Append(classId.ToString("D"));
        sb.Append("/delete\">");
        sb.Append(antiforgery);
        sb.Append("<label>");
        sb.Append(E(culture["School.Class.DeleteConfirm"]));
        sb.Append(" <input class=\"sch-field\" type=\"text\" name=\"confirmName\" /></label>");
        sb.Append("<button type=\"submit\" class=\"btn-outline-danger\">");
        sb.Append(E(culture["School.Class.Delete"]));
        sb.Append("</button></form></section>");
    }

    public static string TeachersList(
        IReadOnlyList<SchoolPortalTeacherListItemDto> teachers,
        IReadOnlyList<SchoolPortalClassListItemDto> classes,
        Guid? filterClassId,
        bool inviteMode,
        bool assignMode,
        SchoolPortalTeacherListItemDto? assignTarget,
        HashSet<Guid> inviteClasses,
        HashSet<Guid> assignClasses,
        string inviteName,
        string inviteEmail,
        string? domainHint,
        string antiforgery,
        CultureState culture,
        string? drawerError)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Teachers.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Teachers.Lead"]));
        sb.Append("</p><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"/school/leraren?invite=1\">");
        sb.Append(E(culture["School.Teachers.Invite"]));
        sb.Append("</a></div></header>");

        sb.Append("<form method=\"get\" action=\"/school/leraren\" class=\"sch-filter-row\"><label>");
        sb.Append(E(culture["School.Col.Class"]));
        sb.Append(" <select class=\"sch-field\" name=\"classId\" data-sch-auto-submit><option value=\"\">");
        sb.Append(E(culture["School.Filter.All"]));
        sb.Append("</option>");
        foreach (var c in classes)
        {
            sb.Append("<option value=\"");
            sb.Append(c.Id.ToString("D"));
            sb.Append('"');
            if (filterClassId == c.Id)
            {
                sb.Append(" selected");
            }

            sb.Append('>');
            sb.Append(E(c.ClassName));
            sb.Append("</option>");
        }

        sb.Append("</select></label></form>");

        var filtered = filterClassId is Guid fid
            ? teachers.Where(t => t.Classes.Any(c => c.ClassId == fid)).ToList()
            : teachers.ToList();

        if (filtered.Count == 0)
        {
            SchoolPortalSsrHelpers.AppendEmptyTable(sb, culture["School.Teachers.Empty"]);
        }
        else
        {
            SchoolPortalSsrHelpers.AppendDataTableOpen(sb);
            sb.Append("<thead><tr><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.Name"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.Email"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.Classes"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.Mfa"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.Status"]));
            sb.Append("</th><th scope=\"col\">");
            sb.Append(E(culture["School.Teachers.Col.LastLogin"]));
            sb.Append("</th><th scope=\"col\"></th></tr></thead><tbody>");
            foreach (var t in filtered)
            {
                sb.Append("<tr class=\"ent-data-table__row\"><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.Name"]));
                sb.Append("\">");
                sb.Append(E(t.DisplayName));
                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.Email"]));
                sb.Append("\">");
                sb.Append(E(t.Email));
                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.Classes"]));
                sb.Append("\">");
                foreach (var c in t.Classes)
                {
                    sb.Append("<span class=\"sch-chip\">");
                    sb.Append(E(c.ClassName));
                    sb.Append("</span> ");
                }

                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.Mfa"]));
                sb.Append("\">");
                if (t.AuthenticatorEnabled)
                {
                    sb.Append("<span class=\"sch-pill sch-pill--ok\">");
                    sb.Append(E(culture["School.Teachers.MfaOn"]));
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append("<span class=\"sch-pill sch-pill--warn\">");
                    sb.Append(E(culture["School.Teachers.MfaOff"]));
                    sb.Append("</span>");
                }

                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.Status"]));
                sb.Append("\">");
                sb.Append(E(t.InvitePending
                    ? culture["School.Teachers.StatusInvited"]
                    : culture["School.Teachers.StatusActive"]));
                sb.Append("</td><td data-label=\"");
                sb.Append(E(culture["School.Teachers.Col.LastLogin"]));
                sb.Append("\">");
                sb.Append(t.LastLoginAtUtc is DateTime seen
                    ? E(SchoolPortalSsrHelpers.FormatAmsterdamShort(seen))
                    : "—");
                sb.Append("</td><td class=\"sch-row-actions\" data-label=\"\">");
                sb.Append("<a class=\"btn-compact\" href=\"/school/leraren?assign=");
                sb.Append(t.UserId.ToString("D"));
                if (filterClassId is Guid fc)
                {
                    sb.Append("&classId=");
                    sb.Append(fc.ToString("D"));
                }

                sb.Append("\">");
                sb.Append(E(culture["School.Teachers.Assign"]));
                sb.Append("</a><form method=\"post\" action=\"/school/leraren/");
                sb.Append(t.UserId.ToString("D"));
                sb.Append("/resend\" class=\"sch-inline-form\">");
                sb.Append(antiforgery);
                sb.Append("<button type=\"submit\" class=\"btn-compact\">");
                sb.Append(E(culture["School.Teachers.Resend"]));
                sb.Append("</button></form><form method=\"post\" action=\"/school/leraren/");
                sb.Append(t.UserId.ToString("D"));
                sb.Append("/remove\" class=\"sch-inline-form\">");
                sb.Append(antiforgery);
                sb.Append("<button type=\"submit\" class=\"btn-outline-danger\">");
                sb.Append(E(culture["School.Teachers.Remove"]));
                sb.Append("</button></form></td></tr>");
            }

            sb.Append("</tbody>");
            SchoolPortalSsrHelpers.AppendDataTableClose(sb);
        }

        if (inviteMode)
        {
            sb.Append("<section class=\"sch-card\"><h2>");
            sb.Append(E(culture["School.Teachers.Invite"]));
            sb.Append("</h2><p class=\"muted\">");
            sb.Append(E(culture["School.Teachers.InviteLead"]));
            sb.Append("</p>");
            if (!string.IsNullOrEmpty(drawerError))
            {
                sb.Append("<p class=\"state-message error\">");
                sb.Append(E(drawerError));
                sb.Append("</p>");
            }

            sb.Append("<form method=\"post\" action=\"/school/leraren/invite\">");
            sb.Append(antiforgery);
            sb.Append("<label>");
            sb.Append(E(culture["School.Teachers.Field.Name"]));
            sb.Append(" <input class=\"sch-field\" type=\"text\" name=\"name\" value=\"");
            sb.Append(E(inviteName));
            sb.Append("\" /></label><label>");
            sb.Append(E(culture["School.Teachers.Field.Email"]));
            sb.Append(" <input class=\"sch-field\" type=\"email\" name=\"email\" value=\"");
            sb.Append(E(inviteEmail));
            sb.Append("\" /></label>");
            if (!string.IsNullOrEmpty(domainHint))
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(E(string.Format(culture["School.Teachers.DomainHint"], domainHint)));
                sb.Append("</p>");
            }

            sb.Append("<div class=\"sch-info-box\">");
            sb.Append(E(culture["School.Teachers.ScopeNote"]));
            sb.Append("</div><fieldset class=\"sch-check-list\"><legend>");
            sb.Append(E(culture["School.Teachers.Field.Classes"]));
            sb.Append("</legend>");
            foreach (var c in classes)
            {
                sb.Append("<label class=\"filter-check\"><input type=\"checkbox\" name=\"classIds\" value=\"");
                sb.Append(c.Id.ToString("D"));
                sb.Append('"');
                if (inviteClasses.Contains(c.Id))
                {
                    sb.Append(" checked");
                }

                sb.Append(" /> ");
                sb.Append(E(c.ClassName));
                sb.Append("</label>");
            }

            sb.Append("</fieldset><div class=\"sch-info-box sch-info-box--locked\"><strong>");
            sb.Append(E(culture["School.Teachers.MfaLocked"]));
            sb.Append("</strong><p>");
            sb.Append(E(culture["School.Teachers.MfaLockedHelp"]));
            sb.Append("</p></div><div class=\"sch-drawer-actions\"><a class=\"btn-compact\" href=\"/school/leraren\">");
            sb.Append(E(culture["School.Cancel"]));
            sb.Append("</a><button type=\"submit\" class=\"btn-primary\">");
            sb.Append(E(culture["School.Teachers.SendInvite"]));
            sb.Append("</button></div></form></section>");
        }

        if (assignMode && assignTarget is not null)
        {
            sb.Append("<section class=\"sch-card\"><h2>");
            sb.Append(E(culture["School.Teachers.Assign"]));
            sb.Append("</h2><p>");
            sb.Append(E(assignTarget.DisplayName));
            sb.Append("</p>");
            if (!string.IsNullOrEmpty(drawerError))
            {
                sb.Append("<p class=\"state-message error\">");
                sb.Append(E(drawerError));
                sb.Append("</p>");
            }

            sb.Append("<form method=\"post\" action=\"/school/leraren/");
            sb.Append(assignTarget.UserId.ToString("D"));
            sb.Append("/assign\">");
            sb.Append(antiforgery);
            sb.Append("<fieldset class=\"sch-check-list\">");
            foreach (var c in classes)
            {
                sb.Append("<label class=\"filter-check\"><input type=\"checkbox\" name=\"classIds\" value=\"");
                sb.Append(c.Id.ToString("D"));
                sb.Append('"');
                if (assignClasses.Contains(c.Id))
                {
                    sb.Append(" checked");
                }

                sb.Append(" /> ");
                sb.Append(E(c.ClassName));
                sb.Append("</label>");
            }

            sb.Append("</fieldset><div class=\"sch-drawer-actions\"><a class=\"btn-compact\" href=\"/school/leraren\">");
            sb.Append(E(culture["School.Cancel"]));
            sb.Append("</a><button type=\"submit\" class=\"btn-primary\">");
            sb.Append(E(culture["School.Save"]));
            sb.Append("</button></div></form></section>");
        }

        return sb.ToString();
    }

    public static string Results(
        IReadOnlyList<SchoolPortalClassListItemDto> classes,
        PupilQuestionSet? filterSet,
        bool hasBothSets,
        Guid? selectedClassId,
        SchoolPortalResultsDto? results,
        string? classError,
        CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Results.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Results.Lead"]));
        sb.Append("</p></header>");

        if (hasBothSets && filterSet is PupilQuestionSet set)
        {
            sb.Append("<div class=\"sch-tabs\" role=\"tablist\" aria-label=\"");
            sb.Append(E(culture["School.Results.TestSwitch"]));
            sb.Append("\">");
            AppendSetTab(sb, PupilQuestionSet.Groep78, set, culture);
            AppendSetTab(sb, PupilQuestionSet.Vo, set, culture);
            sb.Append("</div>");
        }
        else if (filterSet is PupilQuestionSet onlySet)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture[onlySet == PupilQuestionSet.Groep78 ? "School.QuestionSet.Groep78" : "School.QuestionSet.Vo"]));
            sb.Append("</p>");
        }

        var visible = filterSet is null
            ? classes
            : classes.Where(c => c.QuestionSet == filterSet).ToList();

        sb.Append("<div class=\"sch-tabs\">");
        foreach (var c in visible)
        {
            sb.Append("<a class=\"sch-tabs__item");
            if (selectedClassId == c.Id)
            {
                sb.Append(" is-active");
            }

            sb.Append("\" href=\"/school/resultaten?classId=");
            sb.Append(c.Id.ToString("D"));
            if (filterSet is PupilQuestionSet fs)
            {
                sb.Append("&set=");
                sb.Append(fs.ToString());
            }

            sb.Append("\">");
            sb.Append(E(c.ClassName));
            sb.Append("</a>");
        }

        sb.Append("</div>");

        if (!string.IsNullOrEmpty(classError))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(classError));
            sb.Append("</p>");
        }
        else if (results is null)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["School.Results.PickClass"]));
            sb.Append("</p>");
        }
        else
        {
            AppendResultsBody(sb, results, culture);
        }

        return sb.ToString();
    }

    private static void AppendSetTab(StringBuilder sb, PupilQuestionSet set, PupilQuestionSet active, CultureState culture)
    {
        sb.Append("<a class=\"sch-tabs__item");
        if (active == set)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"/school/resultaten?set=");
        sb.Append(set.ToString());
        sb.Append("\">");
        sb.Append(E(culture[set == PupilQuestionSet.Groep78 ? "School.QuestionSet.Groep78" : "School.QuestionSet.Vo"]));
        sb.Append("</a>");
    }

    private static void AppendResultsBody(StringBuilder sb, SchoolPortalResultsDto results, CultureState culture)
    {
        sb.Append("<div class=\"sch-results-layout\"><div><section class=\"sch-card\"><p class=\"muted\">");
        sb.Append(E(culture[results.QuestionSet == PupilQuestionSet.Groep78 ? "School.QuestionSet.Groep78" : "School.QuestionSet.Vo"]));
        sb.Append("</p><h2>");
        sb.Append(E(culture["School.Results.Totals"]));
        sb.Append("</h2>");
        if (!results.Totals.TotalsVisible)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["School.Results.TotalsHidden"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<p>");
            sb.Append(E(culture["School.Col.Completed"]));
            sb.Append(": <strong class=\"tabular\">");
            sb.Append(results.Totals.CompletedCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(" / ");
            sb.Append(results.Totals.TotalCodes.ToString(CultureInfo.InvariantCulture));
            sb.Append("</strong> (");
            sb.Append(results.Totals.CompletionPercent.ToString("0", CultureInfo.InvariantCulture));
            sb.Append("%)</p><h3>");
            sb.Append(E(culture["School.Results.Riasec"]));
            sb.Append("</h3><ul class=\"sch-riasec\">");
            foreach (var n in results.Totals.RiasecTop3)
            {
                sb.Append("<li><span>");
                sb.Append(E(n.Key));
                sb.Append("</span> <strong class=\"tabular\">");
                sb.Append(n.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append("</strong></li>");
            }

            sb.Append("</ul><h3>");
            sb.Append(E(culture["School.Results.Values"]));
            sb.Append("</h3><ul class=\"sch-riasec\">");
            foreach (var n in results.Totals.TopValues)
            {
                sb.Append("<li><span>");
                sb.Append(E(PupilDimensionText.Value(culture, n.Key)));
                sb.Append("</span> <strong class=\"tabular\">");
                sb.Append(n.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append("</strong></li>");
            }

            sb.Append("</ul><h3>");
            sb.Append(E(culture["School.Results.DreamJobs"]));
            sb.Append("</h3><ul class=\"sch-riasec\">");
            foreach (var n in results.Totals.DreamJobs)
            {
                sb.Append("<li><span>");
                sb.Append(E(PupilDreamJobText.Label(culture, n.Key)));
                sb.Append("</span> <strong class=\"tabular\">");
                sb.Append(n.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append("</strong></li>");
            }

            sb.Append("</ul><p class=\"muted\">");
            sb.Append(E(culture["Leraar.DreamJobs.Undecided"]));
            sb.Append(": ");
            sb.Append(results.Totals.UndecidedDreamJobCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("</p>");
            if (results.Totals.NotFilledDreamJobCount > 0)
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(E(culture["Leraar.DreamJobs.NotFilled"]));
                sb.Append(": ");
                sb.Append(results.Totals.NotFilledDreamJobCount.ToString(CultureInfo.InvariantCulture));
                sb.Append("</p>");
            }
        }

        sb.Append("</section>");
        if (results.PerCodeEnabled && results.PerCode is not null)
        {
            sb.Append("<section class=\"sch-card\"><h2>");
            sb.Append(E(culture["School.Results.PerCode"]));
            sb.Append("</h2>");
            if (results.PerCode.Count == 0)
            {
                SchoolPortalSsrHelpers.AppendEmptyTable(sb, "—");
            }
            else
            {
                SchoolPortalSsrHelpers.AppendDataTableOpen(sb);
                sb.Append("<thead><tr><th scope=\"col\">");
                sb.Append(E(culture["School.Col.Code"]));
                sb.Append("</th><th scope=\"col\">");
                sb.Append(E(culture["School.Col.Status"]));
                sb.Append("</th><th scope=\"col\">");
                sb.Append(E(culture["School.Results.Interest"]));
                sb.Append("</th><th scope=\"col\">");
                sb.Append(E(culture["School.Results.TopValue"]));
                sb.Append("</th><th scope=\"col\">");
                sb.Append(E(culture["School.Results.DreamJob"]));
                sb.Append("</th></tr></thead><tbody>");
                foreach (var row in results.PerCode)
                {
                    sb.Append("<tr class=\"ent-data-table__row\"><td data-label=\"");
                    sb.Append(E(culture["School.Col.Code"]));
                    sb.Append("\"><span class=\"sch-code\">");
                    sb.Append(E(row.DisplayCode));
                    sb.Append("</span></td><td data-label=\"");
                    sb.Append(E(culture["School.Col.Status"]));
                    sb.Append("\">");
                    sb.Append(E(SchoolPortalSsrHelpers.ResultsStatusLabel(culture, row.Status)));
                    sb.Append("</td><td data-label=\"");
                    sb.Append(E(culture["School.Results.Interest"]));
                    sb.Append("\">");
                    sb.Append(E(row.InterestCode ?? "—"));
                    sb.Append("</td><td data-label=\"");
                    sb.Append(E(culture["School.Results.TopValue"]));
                    sb.Append("\">");
                    sb.Append(E(PupilDimensionText.Value(culture, row.TopValue)));
                    sb.Append("</td><td data-label=\"");
                    sb.Append(E(culture["School.Results.DreamJob"]));
                    sb.Append("\">");
                    sb.Append(E(PupilDreamJobText.Label(culture, row.DreamJobKey)));
                    sb.Append("</td></tr>");
                }

                sb.Append("</tbody>");
                SchoolPortalSsrHelpers.AppendDataTableClose(sb);
            }

            sb.Append("</section>");
        }

        sb.Append("</div><aside class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Results.WhatSchoolSees"]));
        sb.Append("</h2>");
        if (results.PerCodeEnabled)
        {
            sb.Append("<span class=\"sch-pill sch-pill--ok\">");
            sb.Append(E(culture["School.Results.PerCodeOn"]));
            sb.Append("</span><p class=\"muted\">");
            sb.Append(E(culture["School.Results.PerCodeOnNote"]));
            sb.Append("</p><p class=\"muted\">");
            sb.Append(E(culture["School.Results.TeacherOnlyNote"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<span class=\"sch-pill sch-pill--muted\">");
            sb.Append(E(culture["School.Results.PerCodeOff"]));
            sb.Append("</span><p class=\"muted\">");
            sb.Append(E(culture["School.Results.PerCodeOffNote"]));
            sb.Append("</p>");
        }

        sb.Append("</aside></div>");
    }

    public static string Details(SchoolProfileDto profile, CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Details.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Details.Lead"]));
        sb.Append("</p></header><section class=\"sch-card\"><dl class=\"sch-dl\">");
        sb.Append("<div><dt>");
        sb.Append(E(culture["School.Details.Name"]));
        sb.Append("</dt><dd>");
        sb.Append(E(profile.Name));
        sb.Append("</dd></div><div><dt>");
        sb.Append(E(culture["School.Details.City"]));
        sb.Append("</dt><dd>");
        sb.Append(E(profile.City));
        sb.Append("</dd></div><div><dt>");
        sb.Append(E(culture["School.Details.Brin"]));
        sb.Append("</dt><dd>");
        sb.Append(E(profile.BrinCode ?? "—"));
        sb.Append("</dd></div><div><dt>");
        sb.Append(E(culture["School.Details.Domains"]));
        sb.Append("</dt><dd>");
        sb.Append(E(string.Join(", ", profile.AllowedEmailDomains)));
        sb.Append("</dd></div></dl><p class=\"muted\">");
        sb.Append(E(culture["School.Details.ContactLobsy"]));
        sb.Append("</p></section>");
        return sb.ToString();
    }

    public static string Materials(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Material.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Material.Lead"]));
        sb.Append("</p></header>");
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Material.Lesbrief"]));
        sb.Append("</h2><p>");
        sb.Append(E(culture["School.Material.LesbriefBody"]));
        sb.Append("</p><a class=\"btn-primary\" href=\"/docs/scholen/lesbrief-lobsy.html\" target=\"_blank\" rel=\"noopener\">");
        sb.Append(E(culture["School.Material.OpenLesbrief"]));
        sb.Append("</a></section><section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Material.LoginSteps"]));
        sb.Append("</h2><ol class=\"sch-steps\">");
        for (var i = 1; i <= 4; i++)
        {
            sb.Append("<li>");
            sb.Append(E(culture[$"School.Material.Step{i}"]));
            sb.Append("</li>");
        }

        sb.Append("</ol></section>");
        return sb.ToString();
    }

    public static string Privacy(
        SchoolPrivacyDto privacy,
        string antiforgery,
        CultureState culture,
        string? deleteError,
        string? deleteDone,
        string deleteConfirmValue)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"sch-page-header\"><h1>");
        sb.Append(E(culture["School.Privacy.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(E(culture["School.Privacy.Lead"]));
        sb.Append("</p></header>");

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Privacy.Agreement"]));
        sb.Append("</h2>");
        if (privacy.ProcessorAgreementSignedOn is DateOnly d)
        {
            sb.Append("<p><span class=\"sch-pill sch-pill--ok\">");
            sb.Append(d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            sb.Append("</span> · ");
            sb.Append(E(privacy.ProcessorAgreementVersion));
            sb.Append(" · <a href=\"/privacy#verwerkers\">");
            sb.Append(E(culture["School.Privacy.AgreementLink"]));
            sb.Append("</a></p>");
        }
        else
        {
            sb.Append("<p><span class=\"sch-pill sch-pill--warn\">");
            sb.Append(E(culture["School.Privacy.AgreementMissing"]));
            sb.Append("</span></p>");
        }

        sb.Append("<p>");
        sb.Append(E(culture["School.Privacy.Roles"]));
        sb.Append("</p><p>");
        sb.Append(E(string.Format(
            culture["School.Privacy.Retention"],
            privacy.RetentionCutoff.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture),
            privacy.RetentionYearLabel)));
        sb.Append("</p></section>");

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Privacy.Confirmations"]));
        sb.Append("</h2><ul class=\"sch-todo-list\">");
        foreach (var c in privacy.ClassConfirmations)
        {
            sb.Append("<li><a href=\"/school/klassen/");
            sb.Append(c.ClassId.ToString("D"));
            sb.Append("\">");
            sb.Append(E(c.ClassName));
            sb.Append("</a> — ");
            if (c.Confirmed)
            {
                sb.Append("<span class=\"sch-pill sch-pill--ok\">");
                sb.Append(E(culture["School.Parents.Confirmed"]));
                sb.Append("</span>");
            }
            else
            {
                sb.Append("<span class=\"sch-pill sch-pill--warn\">");
                sb.Append(E(culture["School.Parents.Pending"]));
                sb.Append("</span>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ul></section>");

        sb.Append("<section class=\"sch-card\" id=\"ouderbrief\"><h2>");
        sb.Append(E(culture["School.Privacy.Ouderbrief"]));
        sb.Append("</h2>");
        foreach (var letter in privacy.Ouderbrieven)
        {
            sb.Append("<h3>");
            sb.Append(E(culture[letter.QuestionSet == PupilQuestionSet.Groep78 ? "School.QuestionSet.Groep78" : "School.QuestionSet.Vo"]));
            sb.Append("</h3><pre class=\"sch-ouderbrief\">");
            sb.Append(E(letter.Text));
            sb.Append("</pre>");
        }

        sb.Append("</section>");

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["School.Privacy.DeleteYear"]));
        sb.Append("</h2><p class=\"muted\">");
        sb.Append(E(culture["School.Privacy.DeleteYearHelp"]));
        sb.Append("</p>");
        if (!string.IsNullOrEmpty(deleteError))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(deleteError));
            sb.Append("</p>");
        }

        if (!string.IsNullOrEmpty(deleteDone))
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(E(deleteDone));
            sb.Append("</p>");
        }

        sb.Append("<form method=\"post\" action=\"/school/privacy/delete-year\">");
        sb.Append(antiforgery);
        sb.Append("<label>");
        sb.Append(E(culture["School.Privacy.DeleteYearConfirm"]));
        sb.Append(" <input class=\"sch-field\" type=\"text\" name=\"confirmPhrase\" value=\"");
        sb.Append(E(deleteConfirmValue));
        sb.Append("\" /></label><button type=\"submit\" class=\"btn-compact btn-outline-danger sch-privacy-delete\">");
        sb.Append(E(culture["School.Privacy.DeleteYear"]));
        sb.Append("</button></form></section>");
        return sb.ToString();
    }
}
