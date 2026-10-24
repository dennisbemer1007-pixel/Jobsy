using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class ApplicationsSsrShells
{
    internal static string BuildBody(CultureState culture, ApplicationsSsrData.PageData data)
    {
        if (!data.IsAuthenticatedCandidate)
        {
            return BuildGuestGate(culture);
        }

        if (!data.EmployersEnabled)
        {
            return BuildFeatureOff(culture);
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"panel-page sollicitaties-page\" data-testid=\"applications-page-ssr\">");
        AppendHeroStrip(sb);
        sb.Append("<div class=\"sollicitaties-shell\">");
        AppendPageIntro(sb, culture, data);
        sb.Append("<div class=\"sollicitaties-page__body\">");
        AppendTabsRow(sb, culture, data);
        if (string.Equals(data.Tab, ApplicationsSsrQuery.TabActiviteiten, StringComparison.Ordinal))
        {
            AppendActiviteitenPlaceholder(sb, culture);
        }
        else if (string.Equals(data.Tab, ApplicationsSsrQuery.TabBerichten, StringComparison.Ordinal))
        {
            AppendBerichtenInbox(sb, culture, data);
        }
        else
        {
            AppendToolbar(sb, culture, data);
            if (data.FilteredRows.Count == 0)
            {
                AppendEmpty(sb, culture, data);
            }
            else
            {
                AppendLayout(sb, culture, data);
            }
        }

        sb.Append("</div></div></div>");
        sb.Append("<script src=\"/js/sollicitaties-actions.js?v=20261024-sollicitaties3\" defer></script>");
        return sb.ToString();
    }

    private static void AppendHeroStrip(StringBuilder sb)
    {
        sb.Append("<div class=\"sollicitaties-hero-strip kb-banenkaart-header-strip\" aria-hidden=\"true\"><div class=\"kb-banenkaart-header-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append("\" srcset=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append(" 1x, ");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp2x));
        sb.Append(" 2x\" alt=\"\" width=\"1440\" height=\"150\" loading=\"eager\" decoding=\"async\" fetchpriority=\"high\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append("\" srcset=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append(" 1x, ");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp2x));
        sb.Append(" 2x\" alt=\"\" width=\"390\" height=\"90\" loading=\"eager\" decoding=\"async\" fetchpriority=\"high\" />");
        sb.Append("</div></div>");
    }

    private static void AppendPageIntro(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        var lead = data.ReadOnly
            ? culture["Apps.LeadReadOnly"]
            : culture["Apps.MockupLead"];

        sb.Append("<header class=\"sollicitaties-intro\"><span class=\"sollicitaties-intro__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</span><h1 class=\"sollicitaties-intro__title\" tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("</h1><p class=\"sollicitaties-intro__lead\">");
        sb.Append(WebUtility.HtmlEncode(lead));
        sb.Append("</p></header>");
    }

    private static void AppendTabsRow(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        sb.Append("<div class=\"sollicitaties-tabs-row\">");
        AppendSegmentTabs(sb, culture, data);
        AppendInitiativeBanner(sb, culture, data, inline: true);
        sb.Append("</div>");
        AppendInitiativeBanner(sb, culture, data, inline: false);
    }

    private static void AppendSegmentTabs(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        sb.Append("<nav class=\"sollicitaties-segment\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("\">");
        AppendSegmentTab(
            sb,
            data,
            ApplicationsSsrQuery.TabSollicitaties,
            culture["Nav.ApplicationsTab"],
            badge: null);
        if (data.AfterMatchEnabled)
        {
            AppendSegmentTab(
                sb,
                data,
                ApplicationsSsrQuery.TabBerichten,
                culture["Nav.MessagesTab"],
                badge: data.TotalUnreadMessages > 0 ? data.TotalUnreadMessages : null);
        }

        AppendSegmentTab(
            sb,
            data,
            ApplicationsSsrQuery.TabActiviteiten,
            culture["Nav.ActivitiesTab"],
            badge: null);
        sb.Append("<a class=\"sollicitaties-segment__tab");
        sb.Append("\" href=\"/candidate/liked\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.SavedTab"]));
        if (data.SavedCount > 0)
        {
            sb.Append("<span class=\"sollicitaties-segment__badge\" aria-hidden=\"true\">");
            sb.Append(data.SavedCount);
            sb.Append("</span>");
        }

        sb.Append("</a></nav>");
    }

    private static void AppendSegmentTab(
        StringBuilder sb,
        ApplicationsSsrData.PageData data,
        string tab,
        string label,
        int? badge)
    {
        var active = string.Equals(data.Tab, tab, StringComparison.Ordinal);
        sb.Append("<a class=\"sollicitaties-segment__tab");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.BuildUrl(tab, data.Filter, null)));
        sb.Append("\" data-enhance-nav=\"false\"");
        if (active)
        {
            sb.Append(" aria-current=\"page\"");
        }

        sb.Append('>');
        sb.Append(WebUtility.HtmlEncode(label));
        if (badge is > 0)
        {
            sb.Append("<span class=\"sollicitaties-segment__badge\" aria-hidden=\"true\">");
            sb.Append(badge.Value);
            sb.Append("</span>");
        }

        sb.Append("</a>");
    }

    private static void AppendInitiativeBanner(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data, bool inline)
    {
        if (data.InitiativeUsed is not int used || data.InitiativeMax is not int max || max <= 0)
        {
            return;
        }

        var remaining = Math.Max(0, max - used);
        if (remaining <= 0)
        {
            return;
        }

        sb.Append("<p class=\"sollicitaties-init-banner");
        sb.Append(inline ? " sollicitaties-init-banner--inline" : " sollicitaties-init-banner--stacked");
        sb.Append("\" role=\"status\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Apps.InitiativeWeekBanner", remaining, max)));
        sb.Append("</p>");
    }

    private static void AppendToolbar(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        sb.Append("<div class=\"sollicitaties-toolbar\"><div class=\"sollicitaties-toolbar__filters\" role=\"group\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("\">");
        AppendFilterChip(sb, data, ApplicationsSsrQuery.FilterAll, culture["Kb.Apps.FilterAll"], data.CountAll);
        AppendFilterChip(sb, data, ApplicationsSsrQuery.FilterVacancy, culture["Bk.Apps.FilterVacancy"], data.CountVacancy);
        AppendFilterChip(sb, data, ApplicationsSsrQuery.FilterInitiative, culture["Bk.Apps.FilterInitiative"], data.CountInitiative);
        if (data.ExternalVacanciesEnabled)
        {
            AppendFilterChip(sb, data, ApplicationsSsrQuery.FilterExternal, culture["Apps.SourceExternal"], data.CountExternal);
        }

        AppendFilterChip(sb, data, ApplicationsSsrQuery.FilterDone, culture["Kb.Apps.FilterDone"], data.CountDone);
        sb.Append("</div>");
        if (!data.ReadOnly)
        {
            sb.Append("<a class=\"btn btn-primary sollicitaties-toolbar__add\" href=\"");
            sb.Append(KbRoutes.ExternalVacancyAdd);
            sb.Append("\" data-enhance-nav=\"false\">+ ");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.AddApplication"]));
            sb.Append("</a>");
        }

        sb.Append("</div>");
    }

    private static void AppendFilterChip(
        StringBuilder sb,
        ApplicationsSsrData.PageData data,
        string filter,
        string label,
        int count)
    {
        var active = string.Equals(data.Filter, filter, StringComparison.Ordinal);
        sb.Append("<a class=\"sollicitaties-chip");
        if (active)
        {
            sb.Append(" is-selected");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.BuildUrl(data.Tab, filter, data.SelectedId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append(" · ");
        sb.Append(count);
        sb.Append("</a>");
    }

    private static void AppendEmpty(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        sb.Append("<div class=\"sollicitaties-empty\"><article class=\"sollicitaties-empty-card\"><h2 class=\"sollicitaties-empty-card__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("</h2><p class=\"sollicitaties-empty-card__lead\">");
        sb.Append(WebUtility.HtmlEncode(data.AllRows.Count == 0 ? culture["Apps.Empty"] : culture["Apps.EmptyTab"]));
        sb.Append("</p>");
        if (!data.ReadOnly && data.AllRows.Count == 0)
        {
            sb.Append("<a class=\"btn btn-primary sollicitaties-empty-card__cta\" href=\"");
            sb.Append(KbRoutes.Map);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateVacancies.BrowseMap"]));
            sb.Append("</a>");
        }

        sb.Append("</article></div>");
    }

    private static void AppendLayout(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        SollicitationRow? selected = null;
        foreach (var row in data.FilteredRows)
        {
            if (data.SelectedId is Guid sel && row.RowId == sel)
            {
                selected = row;
                break;
            }
        }

        selected ??= data.FilteredRows.Count > 0 ? data.FilteredRows[0] : null;
        sb.Append("<div class=\"sollicitaties-layout\"><div class=\"sollicitaties-list\" role=\"list\">");
        foreach (var row in data.FilteredRows)
        {
            AppendListCard(sb, culture, data, row, selected?.RowId == row.RowId);
        }

        sb.Append("</div>");
        if (selected is not null)
        {
            sb.Append("<aside class=\"sollicitaties-detail\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
            sb.Append("\">");
            AppendDetailPanel(sb, culture, selected);
            sb.Append("</aside>");
        }

        sb.Append("</div>");
    }

    private static void AppendListCard(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        SollicitationRow row,
        bool selected)
    {
        var selectHref = ApplicationsSsrQuery.BuildUrl(data.Tab, data.Filter, row.RowId);
        sb.Append("<article class=\"sollic-card");
        if (selected)
        {
            sb.Append(" is-selected");
        }

        if (row.IsDone)
        {
            sb.Append(" sollic-card--done");
        }

        sb.Append("\" role=\"listitem\"><a class=\"sollic-card__select\" href=\"");
        sb.Append(WebUtility.HtmlEncode(selectHref));
        sb.Append("\" data-enhance-nav=\"false\"><span class=\"sollic-card__avatar\" aria-hidden=\"true\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><span class=\"sollic-card__main\"><span class=\"sollic-card__source sollic-card__source--");
        sb.Append(row.IsInitiative ? "initiative" : row.IsExternal ? "external" : "vacancy");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture[row.SourceTagKey]));
        sb.Append("</span><span class=\"sollic-card__title\">");
        sb.Append(WebUtility.HtmlEncode(row.Title));
        sb.Append("</span><span class=\"sollic-card__meta\">");
        sb.Append(WebUtility.HtmlEncode(CompanyPlace(row)));
        sb.Append("</span><span class=\"sollic-card__status-row\">");
        if (row.AfterMatch is null)
        {
            sb.Append("<span class=\"sollic-card__pill sollic-card__pill--");
            sb.Append(WebUtility.HtmlEncode(row.StatusModifier));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(CardPillLabel(culture, row)));
            sb.Append("</span>");
            if (!string.IsNullOrWhiteSpace(CurrentStepLabel(row)))
            {
                sb.Append("<span class=\"sollic-card__step\">");
                sb.Append(WebUtility.HtmlEncode(CurrentStepLabel(row)!));
                sb.Append("</span>");
            }
        }
        else
        {
            var cardAction = AfterMatchSollicitatiesPresentation.BuildCardAction(culture, row.AfterMatch);
            if (cardAction is not null)
            {
                sb.Append("<a class=\"sollic-card__ndm-act sollic-card__ndm-act--");
                sb.Append(WebUtility.HtmlEncode(cardAction.Modifier));
                sb.Append("\" href=\"");
                sb.Append(WebUtility.HtmlEncode(cardAction.Href!));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(cardAction.Label));
                sb.Append("</a>");
            }
            else if (!string.IsNullOrWhiteSpace(row.CardHint))
            {
                sb.Append("<span class=\"sollic-card__step\">");
                sb.Append(WebUtility.HtmlEncode(row.CardHint));
                sb.Append("</span>");
            }
        }

        sb.Append("</span>");
        if (row.AfterMatch is null && !string.IsNullOrWhiteSpace(row.CardHint))
        {
            sb.Append("<span class=\"sollic-card__hint\">");
            sb.Append(WebUtility.HtmlEncode(row.CardHint));
            sb.Append("</span>");
        }

        sb.Append("</span></a><div class=\"sollic-card__aside\">");
        if (row.AfterMatch is { UnreadMessageCount: > 0 } am)
        {
            sb.Append("<a class=\"sollic-card__msg\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Messages(am.ApplicationId)));
            sb.Append("\" data-enhance-nav=\"false\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(string.Format(culture["Apps.Ndm.UnreadAria"], am.UnreadMessageCount)));
            sb.Append("\"><span class=\"sollic-card__msg-icon\" aria-hidden=\"true\">✉</span><span class=\"sollic-card__msg-count\">");
            sb.Append(am.UnreadMessageCount);
            sb.Append("</span></a>");
        }

        sb.Append("<span class=\"sollic-card__chev\" aria-hidden=\"true\">›</span></div></article>");
    }

    private static void AppendDetailPanel(StringBuilder sb, CultureState culture, SollicitationRow row)
    {
        sb.Append("<div class=\"sollic-detail\"><div class=\"sollic-detail__head\"><span class=\"sollic-card__source sollic-card__source--");
        sb.Append(row.IsInitiative ? "initiative" : row.IsExternal ? "external" : "vacancy");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture[row.SourceTagKey]));
        sb.Append("</span><h2 class=\"sollic-detail__title\">");
        sb.Append(WebUtility.HtmlEncode(row.Title));
        sb.Append("</h2><p class=\"sollic-detail__meta\">");
        sb.Append(WebUtility.HtmlEncode(CompanyPlace(row)));
        sb.Append("</p></div>");
        if (row.AfterMatch is not null)
        {
            AppendAfterMatchTimeline(sb, culture, row);
            AppendDetailTip(sb, culture, row);
            var actions = AfterMatchSollicitatiesPresentation.BuildDetailActions(culture, row.AfterMatch);
            if (actions is not null)
            {
                sb.Append("<div class=\"sollic-detail__acts\">");
                sb.Append("<a class=\"btn btn-primary sollic-detail__cta\" href=\"");
                sb.Append(WebUtility.HtmlEncode(actions.PrimaryHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(actions.PrimaryLabel));
                sb.Append("</a><a class=\"btn sollic-detail__cta-secondary\" href=\"");
                sb.Append(WebUtility.HtmlEncode(actions.MessagesHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.Messages"]));
                if (actions.UnreadMessages > 0)
                {
                    sb.Append("<span class=\"sollic-detail__msg-badge\">");
                    sb.Append(actions.UnreadMessages);
                    sb.Append("</span>");
                }

                sb.Append("</a></div>");
                AppendDetailMoreLinks(sb, culture, row);
            }
        }
        else if (row.Timeline.Count > 0)
        {
            sb.Append("<ol class=\"kb-timeline kb-timeline--vertical sollic-detail__timeline\">");
            foreach (var step in row.Timeline)
            {
                sb.Append("<li class=\"kb-timeline__step kb-timeline__step--");
                sb.Append(WebUtility.HtmlEncode(step.State.ToLowerInvariant()));
                sb.Append("\"><span class=\"kb-timeline__dot\" aria-hidden=\"true\"></span><span class=\"kb-timeline__label\">");
                sb.Append(WebUtility.HtmlEncode(step.Label));
                sb.Append("</span>");
                if (step.OccurredAtUtc is DateTime at)
                {
                    sb.Append("<time class=\"kb-timeline__date\" datetime=\"");
                    sb.Append(at.ToUniversalTime().ToString("yyyy-MM-dd"));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(FormatTimelineDate(culture, at, step)));
                    sb.Append("</time>");
                }
                else if (string.Equals(step.Key, "Outcome", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("<span class=\"kb-timeline__date\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Kb.Timeline.AfterInterview"]));
                    sb.Append("</span>");
                }
            }

            sb.Append("</ol>");
            AppendDetailTip(sb, culture, row);
            var primaryHref = RowHref(row);
            if (!string.IsNullOrEmpty(primaryHref))
            {
                sb.Append("<a class=\"btn btn-primary sollic-detail__cta\" href=\"");
                sb.Append(WebUtility.HtmlEncode(primaryHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(DetailCtaLabel(culture, row)));
                sb.Append("</a>");
            }
        }

        sb.Append("</div>");
    }

    private static void AppendAfterMatchTimeline(StringBuilder sb, CultureState culture, SollicitationRow row)
    {
        sb.Append("<div class=\"sollic-ndm-tl\" role=\"list\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\">");
        foreach (var step in row.Timeline)
        {
            var state = step.State.ToLowerInvariant();
            sb.Append("<div class=\"sollic-ndm-tl__step sollic-ndm-tl__step--");
            sb.Append(WebUtility.HtmlEncode(state));
            sb.Append("\" role=\"listitem\"><span class=\"sollic-ndm-tl__dot\" aria-hidden=\"true\"></span><span class=\"sollic-ndm-tl__label\">");
            sb.Append(WebUtility.HtmlEncode(step.Label));
            sb.Append("</span>");
            if (step.OccurredAtUtc is DateTime at)
            {
                sb.Append("<span class=\"sollic-ndm-tl__sub\">");
                sb.Append(WebUtility.HtmlEncode(FormatTimelineDate(culture, at, step)));
                sb.Append("</span>");
            }
            else if (string.Equals(state, "current", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<span class=\"sollic-ndm-tl__sub\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Now"]));
                sb.Append("</span>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
    }

    private static void AppendDetailMoreLinks(StringBuilder sb, CultureState culture, SollicitationRow row)
    {
        if (row.ApplicationId is not Guid appId || row.VacancyId is not Guid vacId)
        {
            return;
        }

        sb.Append("<div class=\"sollic-detail__more\">");
        sb.Append("<a class=\"sollic-detail__more-link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Vacancy(vacId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.CompanyPage"]));
        sb.Append("</a><a class=\"sollic-detail__more-link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.LobsyCvPdf(appId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.LobsyCv"]));
        sb.Append("</a><button type=\"button\" class=\"sollic-detail__more-link sollic-detail__more-link--danger sollic-detail__withdraw\" data-application-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.MenuWithdraw"]));
        sb.Append("</button></div>");
    }

    private static void AppendBerichtenInbox(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data)
    {
        sb.Append("<div class=\"sollic-berichten\">");
        if (data.FilteredRows.Count == 0)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.BerichtenEmpty"]));
            sb.Append("</p></div>");
            return;
        }

        sb.Append("<div class=\"sollic-berichten__list\" role=\"list\">");
        foreach (var row in data.FilteredRows)
        {
            if (row.ApplicationId is not Guid appId)
            {
                continue;
            }

            sb.Append("<a class=\"sollic-berichten__thread\" role=\"listitem\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Messages(appId)));
            sb.Append("\" data-enhance-nav=\"false\"><span class=\"sollic-card__avatar\" aria-hidden=\"true\">");
            sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
            sb.Append("</span><span class=\"sollic-berichten__body\"><b>");
            sb.Append(WebUtility.HtmlEncode(row.CompanyName));
            sb.Append("</b><span class=\"sollic-berichten__sub\">");
            sb.Append(WebUtility.HtmlEncode(row.Title));
            sb.Append("</span></span>");
            if (row.AfterMatch is { UnreadMessageCount: > 0 } am)
            {
                sb.Append("<span class=\"sollic-berichten__badge\">");
                sb.Append(am.UnreadMessageCount);
                sb.Append("</span>");
            }

            sb.Append("</a>");
        }

        sb.Append("</div><p class=\"sollic-berichten__safe\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.BerichtenSafe"]));
        sb.Append("</p></div>");
    }

    private static void AppendDetailTip(StringBuilder sb, CultureState culture, SollicitationRow row)
    {
        string? body = null;
        if (row.AfterMatch is { OpenSlotCount: > 0 })
        {
            body = culture["Apps.Ndm.DetailTipPick"];
        }

        var showInterviewTip = string.Equals(row.Status, nameof(ApplicationStatus.EmployerContacting), StringComparison.OrdinalIgnoreCase)
                               || row.Timeline.Any(t =>
                                   string.Equals(t.State, "Current", StringComparison.OrdinalIgnoreCase)
                                   && (string.Equals(t.Key, "Interview", StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(t.Key, "Gesprek", StringComparison.OrdinalIgnoreCase)));
        if (body is null && !showInterviewTip && string.IsNullOrWhiteSpace(row.NextStepKey))
        {
            return;
        }

        sb.Append("<div class=\"sollic-detail__tip\"><p class=\"sollic-detail__tip-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Detail.TipTitle"]));
        sb.Append("</p><p class=\"sollic-detail__tip-body\">");
        sb.Append(WebUtility.HtmlEncode(
            body
                ?? (showInterviewTip
                    ? culture["Apps.Detail.TipInterview"]
                    : culture[row.NextStepKey!])));
        sb.Append("</p></div>");
    }

    private static void AppendActiviteitenPlaceholder(StringBuilder sb, CultureState culture)
    {
        sb.Append("<p class=\"sollicitaties-activiteiten-placeholder state-message\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Activiteiten.Placeholder"]));
        sb.Append("</p>");
    }

    private static string BuildGuestGate(CultureState culture)
    {
        var returnUrl = Uri.EscapeDataString(KbRoutes.Applications);
        var sb = new StringBuilder();
        sb.Append("<div class=\"sollicitaties-gate\" data-testid=\"applications-gate\"><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Empty"]));
        sb.Append("</p><a class=\"btn btn-primary\" href=\"/login?returnUrl=");
        sb.Append(returnUrl);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Login"]));
        sb.Append("</a></div>");
        return sb.ToString();
    }

    private static string BuildFeatureOff(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<p class=\"state-message\">");
        sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
        sb.Append("</p>");
        return sb.ToString();
    }

    private static string CompanyPlace(SollicitationRow row)
        => string.IsNullOrWhiteSpace(row.Place)
            ? row.CompanyName
            : $"{row.CompanyName} · {row.Place}";

    private static string? RowHref(SollicitationRow row)
    {
        if (row.IsExternal && row.ExternalVacancyId is Guid extId)
        {
            return KbRoutes.ExternalVacancyDetail(extId);
        }

        if (row.VacancyId is Guid vacId)
        {
            return $"/vacancies/{vacId:D}";
        }

        return null;
    }

    private static string CardPillLabel(CultureState culture, SollicitationRow row)
    {
        var interviewCurrent = row.Timeline.FirstOrDefault(t =>
            string.Equals(t.State, "Current", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(t.Key, "Interview", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.Key, "Gesprek", StringComparison.OrdinalIgnoreCase)));
        if (interviewCurrent?.OccurredAtUtc is not null)
        {
            return culture["Apps.CardPill.InterviewPlanned"];
        }

        return row.StatusLabel;
    }

    private static string? CurrentStepLabel(SollicitationRow row)
    {
        var current = row.Timeline.FirstOrDefault(t =>
            string.Equals(t.State, "Current", StringComparison.OrdinalIgnoreCase));
        return current?.Label;
    }

    private static string DetailCtaLabel(CultureState culture, SollicitationRow row)
    {
        if (string.Equals(row.Status, nameof(ApplicationStatus.EmployerContacting), StringComparison.OrdinalIgnoreCase))
        {
            return culture["Kb.Next.Practice"];
        }

        if (row.IsExternal)
        {
            return culture["ExtVac.Detail.Apply"];
        }

        return culture["Apps.MenuVacancy"];
    }

    private static string FormatTimelineDate(CultureState culture, DateTime utc, SollicitationTimelineStep step)
    {
        if (string.Equals(step.Key, "Interview", StringComparison.OrdinalIgnoreCase)
            || string.Equals(step.Key, "Gesprek", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationsSsrData.FormatShortDateTime(culture, utc);
        }

        return ApplicationsSsrData.FormatShortDate(culture, utc);
    }
}
