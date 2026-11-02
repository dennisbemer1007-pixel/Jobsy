using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for read-only teacher portal pages (no Blazor).</summary>
public static class TeacherPortalReadOnlySsrHtml
{
    public static string BuildGroupBody(
        Guid classId,
        TeacherGroupInsightsDto group,
        CultureState culture)
    {
        var sb = new StringBuilder();
        AppendClassBreadcrumb(sb, classId, culture, culture["Leraar.Group.Title"]);
        sb.Append("<h1>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        var qsKey = group.QuestionSet == PupilQuestionSet.Groep78
            ? "School.QuestionSet.Groep78"
            : "School.QuestionSet.Vo";
        sb.Append(WebUtility.HtmlEncode(culture[qsKey]));
        sb.Append("</p></header>");

        if (!group.Visible)
        {
            sb.Append("<p class=\"sch-empty-state panel-section\">");
            sb.Append(WebUtility.HtmlEncode(string.Format(CultureInfo.CurrentCulture, culture["Leraar.Group.Hidden"], group.CompletedCount)));
            sb.Append("</p>");
            return sb.ToString();
        }

        sb.Append("<div class=\"sch-group-grid\">");
        AppendRiasecSection(sb, group, culture);
        AppendValuesSection(sb, group, culture);
        AppendCulturesSection(sb, group, culture);
        AppendCompetenceSection(sb, group, culture);
        AppendDiscussionSection(sb, group, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    public static string BuildDreamJobsBody(
        Guid classId,
        TeacherDreamJobsDto dto,
        CultureState culture)
    {
        var sb = new StringBuilder();
        AppendClassBreadcrumb(sb, classId, culture, culture["Leraar.DreamJobs.Title"]);
        sb.Append("<h1>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.DreamJobs.Title"]));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.DreamJobs.Lead"]));
        sb.Append("</p></header>");

        if (!dto.Visible)
        {
            sb.Append("<p class=\"sch-empty-state panel-section\">");
            sb.Append(WebUtility.HtmlEncode(string.Format(CultureInfo.CurrentCulture, culture["Leraar.Group.Hidden"], dto.CompletedCount)));
            sb.Append("</p>");
            return sb.ToString();
        }

        sb.Append("<section class=\"sch-card\"><div class=\"sch-chip-list\">");
        foreach (var job in dto.Jobs)
        {
            sb.Append("<span class=\"sch-chip\">");
            sb.Append(WebUtility.HtmlEncode(job.Key));
            sb.Append(" · ");
            sb.Append(job.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span>");
        }

        sb.Append("</div><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.DreamJobs.Undecided"]));
        sb.Append(": ");
        sb.Append(dto.UndecidedCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("</p>");
        if (dto.NotFilledCount > 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leraar.DreamJobs.NotFilled"]));
            sb.Append(": ");
            sb.Append(dto.NotFilledCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("</p>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    public static string BuildMaterialsBody(Guid classId, CultureState culture)
    {
        var sb = new StringBuilder();
        AppendClassBreadcrumb(sb, classId, culture, culture["Leraar.Material.Title"]);
        sb.Append("<h1>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Material.Title"]));
        sb.Append("</h1></header>");

        var pdfHref = $"/leraar/klas/{classId:D}/codelist.pdf";
        var csvHref = $"/leraar/klas/{classId:D}/codelist.csv";

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["School.CodeList.Pdf"]));
        sb.Append("</h2><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(pdfHref));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["School.CodeList.Pdf"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"");
        sb.Append(WebUtility.HtmlEncode(csvHref));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["School.CodeList.Csv"]));
        sb.Append("</a></div></section>");

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["School.Material.Lesbrief"]));
        sb.Append("</h2><p>");
        sb.Append(WebUtility.HtmlEncode(culture["School.Material.LesbriefBody"]));
        sb.Append("</p><a class=\"btn-compact\" href=\"/docs/scholen/lesbrief-lobsy.html\" target=\"_blank\" rel=\"noopener\">");
        sb.Append(WebUtility.HtmlEncode(culture["School.Material.OpenLesbrief"]));
        sb.Append("</a></section>");

        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Material.HowItWorks"]));
        sb.Append("</h2><ol class=\"sch-steps\"><li>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Material.Step1"]));
        sb.Append("</li><li>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Material.Step2"]));
        sb.Append("</li><li>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Material.Step3"]));
        sb.Append("</li></ol></section>");
        return sb.ToString();
    }

    public static string BuildMboHboBody(TeacherMboHboClassProgressDto model, CultureState culture)
    {
        var sb = new StringBuilder();
        var classId = model.ClassId;
        sb.Append("<header class=\"sch-page-header\"><p class=\"sch-breadcrumb\"><a href=\"");
        sb.Append("/leraar/klas/");
        sb.Append(classId.ToString("D", CultureInfo.InvariantCulture));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Nav.Overview"]));
        sb.Append("</a> / MBO/HBO</p><h1>");
        sb.Append(WebUtility.HtmlEncode(model.ClassLabel));
        sb.Append("</h1><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Codes.NamesNote"]));
        sb.Append("</p></header>");

        sb.Append("""
            <div class="mhbo-root" style="min-height:auto;background:transparent">
            <div class="mhbo-grid" style="grid-template-columns:1.55fr 1fr;margin-top:18px">
            <div class="mhbo-card" style="padding:14px 16px">
            <div style="display:flex;gap:16px;font-size:12.5px;color:#5a6576;margin-bottom:6px">
            <span><span class="mhbo-dot d0"></span> Niet gestart</span>
            <span><span class="mhbo-dot d1"></span> Bezig</span>
            <span><span class="mhbo-dot d2"></span> Afgerond</span>
            </div>
            <table style="width:100%;border-collapse:collapse;font-size:14px">
            <thead><tr>
            <th>Code</th><th>Vakken</th><th>Interesse</th><th>Compet.</th><th>Beroepen</th><th>Werkw.</th><th>Af</th><th>Paspoort</th>
            </tr></thead><tbody>
            """);

        foreach (var row in model.Codes)
        {
            sb.Append("<tr><td><b>");
            sb.Append(WebUtility.HtmlEncode(row.CodeDisplay));
            sb.Append("</b>");
            if (row.PatternSignal)
            {
                sb.Append("""<span class="mhbo-chip" title="Antwoordpatroon opvallend" style="margin-left:6px">⚠</span>""");
            }

            sb.Append("</td>");
            foreach (var st in row.BlockStatuses)
            {
                sb.Append("<td><span class=\"mhbo-dot ");
                sb.Append(DotClass(st));
                sb.Append("\"></span></td>");
            }

            sb.Append("<td>");
            sb.Append(row.CompletedCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("/5</td><td>");
            sb.Append(row.PassportReady ? "Klaar" : "—");
            sb.Append("</td></tr>");
        }

        sb.Append("</tbody></table></div><div class=\"mhbo-grid\">");
        sb.Append("<div class=\"mhbo-card\"><h2 style=\"font-size:18px;color:#0f2747\">Voortgang per test</h2>");
        foreach (var pr in model.Charts.BlockCompletion)
        {
            sb.Append("<div class=\"mhbo-hb\"><span style=\"width:110px\">");
            sb.Append(WebUtility.HtmlEncode(pr.BlockKey));
            sb.Append("</span><div class=\"mhbo-bar\" style=\"display:flex\"><i style=\"width:");
            sb.Append(pr.CompletedPercent.ToString(CultureInfo.InvariantCulture));
            sb.Append("%;background:#2e8b57\"></i><i style=\"width:");
            sb.Append(pr.InProgressPercent.ToString(CultureInfo.InvariantCulture));
            sb.Append("%;background:#e2b33c\"></i></div><b>");
            sb.Append(pr.CompletedPercent.ToString(CultureInfo.InvariantCulture));
            sb.Append("%</b></div>");
        }

        sb.Append("</div><div class=\"mhbo-card\"><h2 style=\"font-size:18px;color:#0f2747\">Interesses in de groep (gemiddeld)</h2>");
        foreach (var bar in model.Charts.InteresseAverage)
        {
            sb.Append("<div class=\"mhbo-hb\"><span style=\"width:110px\">");
            sb.Append(WebUtility.HtmlEncode(bar.Label));
            sb.Append("</span><div class=\"mhbo-bar\"><i style=\"width:");
            sb.Append(bar.Percent.ToString(CultureInfo.InvariantCulture));
            sb.Append("%;background:#0f2747\"></i></div><b>");
            sb.Append(bar.Percent.ToString(CultureInfo.InvariantCulture));
            sb.Append("</b></div>");
        }

        sb.Append("</div></div></div></div>");
        return sb.ToString();
    }

    private static void AppendClassBreadcrumb(StringBuilder sb, Guid classId, CultureState culture, string currentLabel)
    {
        sb.Append("<header class=\"sch-page-header\"><p class=\"sch-breadcrumb\"><a href=\"");
        sb.Append("/leraar/klas/");
        sb.Append(classId.ToString("D", CultureInfo.InvariantCulture));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Nav.Overview"]));
        sb.Append("</a> / ");
        sb.Append(WebUtility.HtmlEncode(currentLabel));
        sb.Append("</p>");
    }

    private static void AppendRiasecSection(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Interests"]));
        sb.Append("</h2><ul class=\"sch-bar-list\">");
        var max = Math.Max(1, group.RiasecBars.Count > 0 ? group.RiasecBars.Max(b => b.Count) : 1);
        foreach (var bar in group.RiasecBars)
        {
            sb.Append("<li><span>");
            sb.Append(WebUtility.HtmlEncode(culture[bar.KidLabelKey]));
            sb.Append(" <span class=\"muted\">(");
            sb.Append(WebUtility.HtmlEncode(bar.Letter));
            sb.Append(")</span></span><progress class=\"sch-progress-meter\" max=\"");
            sb.Append(max.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" value=\"");
            sb.Append(bar.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("\"></progress><span class=\"tabular\">");
            sb.Append(bar.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span></li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendValuesSection(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Drivers"]));
        sb.Append("</h2><ol class=\"sch-rank-list\">");
        foreach (var v in group.TopValues)
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(PupilDimensionText.Value(culture, v.Key)));
            sb.Append(" <span class=\"muted\">· ");
            sb.Append(v.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span></li>");
        }

        sb.Append("</ol></section>");
    }

    private static void AppendCulturesSection(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Cultures"]));
        sb.Append("</h2><ol class=\"sch-rank-list\">");
        foreach (var c in group.TopCultures)
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(PupilDimensionText.Culture(culture, c.Key)));
            sb.Append(" <span class=\"muted\">· ");
            sb.Append(c.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span></li>");
        }

        sb.Append("</ol></section>");
    }

    private static void AppendCompetenceSection(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Competence"]));
        sb.Append("</h2><ul class=\"sch-chip-list\">");
        foreach (var b in group.CompetenceBands)
        {
            sb.Append("<li class=\"sch-chip\">");
            sb.Append(WebUtility.HtmlEncode(b.Key));
            sb.Append(" · ");
            sb.Append(b.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendDiscussionSection(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Leraar.Group.Discussion"]));
        sb.Append("</h2><ul class=\"sch-prompt-list\">");
        foreach (var line in group.DiscussionPromptKeys)
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(line));
            sb.Append("</li>");
        }

        sb.Append("</ul></section>");
    }

    private static string DotClass(PupilMboHboTileStatus st)
        => st switch
        {
            PupilMboHboTileStatus.Completed => "d2",
            PupilMboHboTileStatus.InProgress => "d1",
            _ => "d0"
        };
}
