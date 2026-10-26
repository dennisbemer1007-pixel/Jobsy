using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class ApplicationsActiviteitenSsrShells
{
    internal static void AppendTab(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data)
    {
        if (!data.ActivityLogEnabled)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
            sb.Append("</p>");
            return;
        }

        var overview = data.ActiviteitenOverview;
        if (overview is null)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Loading"]));
            sb.Append("</p>");
            return;
        }

        sb.Append("<div class=\"sollic-activiteiten\" data-testid=\"sollic-activiteiten-tab\"");
        AppendI18nAttr(sb, "data-msg-form-required", culture["Apps.Activiteiten.Msg.FormRequired"]);
        AppendI18nAttr(sb, "data-msg-save-failed", culture["Apps.Activiteiten.Msg.SaveFailed"]);
        AppendI18nAttr(sb, "data-msg-agreement-invalid", culture["Apps.Activiteiten.Msg.AgreementInvalid"]);
        AppendI18nAttr(sb, "data-msg-agreement-failed", culture["Apps.Activiteiten.Msg.AgreementFailed"]);
        sb.Append('>');
        sb.Append("<div class=\"sollic-activiteiten__main\">");
        AppendSummary(sb, culture, overview);
        AppendWeekList(sb, culture, overview);
        sb.Append("</div>");
        AppendAside(sb, culture, overview);
        sb.Append("</div>");
    }

    private static void AppendSummary(StringBuilder sb, CultureState culture, JobSearchActivityOverviewDto overview)
    {
        sb.Append("<section class=\"sollic-activiteiten-summary card\">");
        sb.Append("<p class=\"sollic-activiteiten-summary__period-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.PeriodPrefix"]));
        sb.Append(' ');
        sb.Append(WebUtility.HtmlEncode(overview.PeriodLabel));
        sb.Append("</p><h2 class=\"sollic-activiteiten-summary__title\">");
        sb.Append(WebUtility.HtmlEncode(
            overview.TargetMet
                ? culture["Apps.Activiteiten.TargetMet"]
                : culture["Apps.Activiteiten.TargetNotMet"]));
        sb.Append("</h2><div class=\"sollic-activiteiten-badges\">");
        AppendBadge(sb, culture.Format("Apps.Activiteiten.DoneBadge", overview.ActivitiesDoneInPeriod), "done");
        AppendBadge(sb, culture.Format("Apps.Activiteiten.AgreementBadge", overview.ActivitiesTarget), "goal");
        AppendBadge(sb, culture.Format("Apps.Activiteiten.DaysLeftBadge", overview.DaysRemainingInPeriod), "days");
        sb.Append("</div><p class=\"sollic-activiteiten-summary__adjust\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AdjustLead"]));
        sb.Append(" <button type=\"button\" class=\"link-btn\" data-sollic-activ-adjust-toggle=\"1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AdjustLink"]));
        sb.Append("</button></p>");
        sb.Append("<div class=\"sollic-activiteiten-agreement-edit\" data-sollic-activ-agreement-panel hidden>");
        sb.Append("<label class=\"sollic-activiteiten-field\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AgreementField"]));
        sb.Append("</span><input type=\"number\" min=\"1\" max=\"20\" step=\"1\" data-sollic-activ-agreement-input=\"1\" value=\"");
        sb.Append(overview.ActivitiesTarget);
        sb.Append("\" /></label><div class=\"sollic-activiteiten-agreement-edit__actions\">");
        sb.Append("<button type=\"button\" class=\"btn btn-primary\" data-sollic-activ-agreement-save=\"1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AgreementSave"]));
        sb.Append("</button><button type=\"button\" class=\"btn btn-secondary\" data-sollic-activ-agreement-cancel=\"1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AgreementCancel"]));
        sb.Append("</button></div><p class=\"state-message error sollic-activiteiten-agreement-error\" role=\"alert\" hidden></p></div>");
        sb.Append("<div class=\"sollic-activiteiten-uwv note\" role=\"note\"><p>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.UwvDisclaimer"]));
        sb.Append("</p></div><button type=\"button\" class=\"btn btn-primary sollic-activiteiten-pdf\" data-sollic-activ-pdf=\"1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.DownloadPdf"]));
        sb.Append("</button></section>");
    }

    private static void AppendBadge(StringBuilder sb, string text, string variant)
    {
        sb.Append("<span class=\"sollic-activiteiten-badge sollic-activiteiten-badge--");
        sb.Append(variant);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("</span>");
    }

    private static void AppendI18nAttr(StringBuilder sb, string attr, string value)
    {
        sb.Append(' ');
        sb.Append(attr);
        sb.Append("=\"");
        sb.Append(WebUtility.HtmlEncode(value));
        sb.Append('"');
    }

    private static void AppendWeekList(StringBuilder sb, CultureState culture, JobSearchActivityOverviewDto overview)
    {
        sb.Append("<section class=\"sollic-activiteiten-weeks card\">");
        sb.Append("<h2 class=\"sollic-activiteiten-weeks__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.PerWeek"]));
        sb.Append("</h2><div class=\"sollic-activiteiten-filters\" role=\"tablist\">");
        AppendFilterChip(sb, culture["Apps.Activiteiten.FilterAll"], "all", active: true);
        AppendFilterChip(sb, culture["Apps.Activiteiten.FilterApplications"], "applied", active: false);
        AppendFilterChip(sb, culture["Apps.Activiteiten.FilterInterviews"], "interview", active: false);
        AppendFilterChip(sb, culture["Apps.Activiteiten.FilterWorkshops"], "workshop", active: false);
        sb.Append("</div>");

        foreach (var week in overview.Weeks)
        {
            sb.Append("<div class=\"sollic-activiteiten-week\" data-sollic-activ-week=\"1\">");
            sb.Append("<div class=\"sollic-activiteiten-week__head\"><h3>");
            sb.Append(WebUtility.HtmlEncode(week.WeekLabel));
            sb.Append("</h3><span class=\"sollic-activiteiten-week__count sollic-activiteiten-week__count--pill\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Apps.Activiteiten.WeekCount", week.Count)));
            sb.Append("</span></div><ul class=\"sollic-activiteiten-list\">");
            foreach (var item in week.Items)
            {
                var filterKey = FilterKey(item.Kind);
                sb.Append("<li class=\"sollic-activiteiten-item\" data-sollic-activ-filter=\"");
                sb.Append(filterKey);
                sb.Append("\"><span class=\"sollic-activiteiten-item__icon\" aria-hidden=\"true\">");
                sb.Append(ApplicationsActiviteitenSsrIcons.ForKind(item.Kind, item.IsExternal));
                sb.Append("</span><div class=\"sollic-activiteiten-item__body\"><div class=\"sollic-activiteiten-item__title\">");
                sb.Append(WebUtility.HtmlEncode(KindTitle(culture, item.Kind)));
                sb.Append(" · ");
                sb.Append(WebUtility.HtmlEncode(item.FunctionTitle));
                sb.Append("</div><p class=\"sollic-activiteiten-item__meta\">");
                sb.Append(WebUtility.HtmlEncode(item.Employer));
                sb.Append(" · ");
                sb.Append(WebUtility.HtmlEncode(FormatActivityDate(culture, item.ActivityDate)));
                if (!string.IsNullOrWhiteSpace(item.ContactMethod))
                {
                    sb.Append(" · ");
                    sb.Append(WebUtility.HtmlEncode(item.ContactMethod));
                }

                sb.Append("</p>");
                if (!string.IsNullOrWhiteSpace(item.Result))
                {
                    sb.Append("<p class=\"sollic-activiteiten-item__result\">");
                    sb.Append(WebUtility.HtmlEncode(item.Result));
                    sb.Append("</p>");
                }

                if (item.HasAttachment)
                {
                    sb.Append("<a class=\"sollic-activiteiten-item__attachment link-btn\" href=\"/api/sollicitaties-client/activiteiten/");
                    sb.Append(item.Id.ToString("D"));
                    sb.Append("/attachment\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AttachmentLink"]));
                    sb.Append("</a>");
                }

                sb.Append("</div><div class=\"sollic-activiteiten-item__tags\">");
                if (item.IsExternal)
                {
                    AppendTag(sb, culture["Apps.SourceExternal"], "external");
                }

                if (item.Source == JobSearchActivitySource.Auto)
                {
                    AppendTag(sb, culture["Apps.Activiteiten.AutoTracked"], "auto");
                }

                if (!string.IsNullOrWhiteSpace(item.StatusLabelKey))
                {
                    var mod = string.Equals(item.StatusLabelKey, "Apps.Activiteiten.Sent", StringComparison.Ordinal)
                        ? "sent"
                        : "neutral";
                    AppendTag(sb, culture[item.StatusLabelKey!], mod);
                }

                sb.Append("</div></li>");
            }

            sb.Append("</ul></div>");
        }

        if (overview.Weeks.Count == 0)
        {
            sb.Append("<p class=\"state-message sollic-activiteiten-empty\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.Empty"]));
            sb.Append("</p>");
        }

        sb.Append("</section>");
    }

    private static void AppendAside(StringBuilder sb, CultureState culture, JobSearchActivityOverviewDto overview)
    {
        sb.Append("<aside class=\"sollic-activiteiten-aside card\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AddTitle"]));
        sb.Append("\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AddTitle"]));
        sb.Append("</h2><p class=\"sollic-activiteiten-aside__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.AddLead"]));
        sb.Append("</p><form class=\"sollic-activiteiten-form\" data-sollic-activ-form=\"1\" enctype=\"multipart/form-data\" method=\"post\">");
        sb.Append("<input type=\"hidden\" name=\"kind\" value=\"");
        sb.Append((int)JobSearchActivityKind.EmployerCalled);
        sb.Append("\" data-sollic-activ-kind-input=\"1\" />");
        sb.Append("<div class=\"sollic-activiteiten-kind-grid\" role=\"group\">");
        AppendKindButton(sb, culture, JobSearchActivityKind.EmployerCalled, selected: true);
        AppendKindButton(sb, culture, JobSearchActivityKind.Applied, selected: false);
        AppendKindButton(sb, culture, JobSearchActivityKind.VisitedInPerson, selected: false);
        AppendKindButton(sb, culture, JobSearchActivityKind.TempAgency, selected: false);
        AppendKindButton(sb, culture, JobSearchActivityKind.NetworkConversation, selected: false);
        AppendKindButton(sb, culture, JobSearchActivityKind.Workshop, selected: false);
        sb.Append("</div>");
        AppendField(sb, culture["Apps.Activiteiten.FieldDate"], "date", "activityDate", required: true);
        AppendField(sb, culture["Apps.Activiteiten.FieldEmployer"], "text", "employer", required: true);
        AppendField(sb, culture["Apps.Activiteiten.FieldFunction"], "text", "functionTitle", required: true);
        sb.Append("<label class=\"sollic-activiteiten-field\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.FieldResult"]));
        sb.Append("</span><textarea name=\"result\" rows=\"3\" maxlength=\"2000\"></textarea></label>");
        sb.Append("<label class=\"sollic-activiteiten-field\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.FieldAttachment"]));
        sb.Append("</span><input type=\"file\" name=\"attachment\" accept=\".pdf,image/jpeg,image/png,image/webp\" /></label>");
        sb.Append("<button type=\"submit\" class=\"btn btn-primary\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.Save"]));
        sb.Append("</button><p class=\"state-message error sollic-activiteiten-form-error\" role=\"alert\" hidden></p>");
        sb.Append("</form>");
        sb.Append("<input type=\"hidden\" data-sollic-activ-target=\"");
        sb.Append(overview.ActivitiesTarget);
        sb.Append("\" /></aside>");
    }

    private static void AppendFilterChip(StringBuilder sb, string label, string key, bool active)
    {
        sb.Append("<button type=\"button\" class=\"sollic-activiteiten-filter");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" data-sollic-activ-filter-chip=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</button>");
    }

    private static void AppendKindButton(StringBuilder sb, CultureState culture, JobSearchActivityKind kind, bool selected)
    {
        sb.Append("<button type=\"button\" class=\"sollic-activiteiten-kind");
        if (selected)
        {
            sb.Append(" is-selected");
        }

        sb.Append("\" data-sollic-activ-kind=\"");
        sb.Append((int)kind);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(KindTitle(culture, kind)));
        sb.Append("</button>");
    }

    private static void AppendField(StringBuilder sb, string label, string type, string name, bool required)
    {
        sb.Append("<label class=\"sollic-activiteiten-field\"><span>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span><input type=\"");
        sb.Append(type);
        sb.Append("\" name=\"");
        sb.Append(name);
        sb.Append('"');
        if (required)
        {
            sb.Append(" required");
        }

        sb.Append(" /></label>");
    }

    private static void AppendTag(StringBuilder sb, string text, string variant)
    {
        sb.Append("<span class=\"sollic-activiteiten-tag sollic-activiteiten-tag--");
        sb.Append(variant);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(text));
        sb.Append("</span>");
    }

    private static string FilterKey(JobSearchActivityKind kind)
        => kind switch
        {
            JobSearchActivityKind.Applied => "applied",
            JobSearchActivityKind.Interview => "interview",
            JobSearchActivityKind.Workshop => "workshop",
            _ => "other"
        };

    private static string KindTitle(CultureState culture, JobSearchActivityKind kind)
        => kind switch
        {
            JobSearchActivityKind.Applied => culture["Apps.Activiteiten.Kind.Applied"],
            JobSearchActivityKind.EmployerCalled => culture["Apps.Activiteiten.Kind.EmployerCalled"],
            JobSearchActivityKind.VisitedInPerson => culture["Apps.Activiteiten.Kind.Visited"],
            JobSearchActivityKind.TempAgency => culture["Apps.Activiteiten.Kind.TempAgency"],
            JobSearchActivityKind.NetworkConversation => culture["Apps.Activiteiten.Kind.Network"],
            JobSearchActivityKind.Workshop => culture["Apps.Activiteiten.Kind.Workshop"],
            JobSearchActivityKind.Interview => culture["Apps.Activiteiten.Kind.Interview"],
            _ => kind.ToString()
        };

    private static string FormatActivityDate(CultureState culture, DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var ci = CultureInfo.GetCultureInfo(culture.Language);
        return dt.ToString("dddd d MMMM", ci);
    }
}
