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
    internal static string BuildBody(CultureState culture, ApplicationsSsrData.PageData data, string? detailReturnHref = null)
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
        sb.Append("<div class=\"sollicitaties-page-wrap\">");
        AppendHeroStrip(sb, data);
        sb.Append("<div class=\"panel-page sollicitaties-page");
        if (data.DetailView)
        {
            sb.Append(" sollicitaties-page--detail");
        }

        sb.Append("\" data-testid=\"applications-page-ssr\"><div class=\"sollicitaties-shell\">");
        AppendPageIntro(sb, culture, data);
        sb.Append("<div class=\"sollicitaties-page__body\">");
        AppendTabsRow(sb, culture, data);
        AppendAllTabPanels(sb, culture, data, detailReturnHref);

        sb.Append("</div></div></div></div>");
        return sb.ToString();
    }

    private static void AppendHeroStrip(StringBuilder sb, ApplicationsSsrData.PageData data)
    {
        var (desk, mob) = data.Tab switch
        {
            ApplicationsSsrQuery.TabActiviteiten => (KreeftHeaderStripAssets.ActiviteitenDesk, KreeftHeaderStripAssets.ActiviteitenMob),
            ApplicationsSsrQuery.TabBerichten => (KreeftHeaderStripAssets.BerichtenDesk, KreeftHeaderStripAssets.BerichtenMob),
            _ => (KreeftHeaderStripAssets.SollicitatiesDesk, KreeftHeaderStripAssets.SollicitatiesMob),
        };
        KreeftHeaderStripHtml.Append(sb, desk, mob);
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
                culture["Apps.Ndm.Messages"],
                badge: data.UnreadMessagesTotal > 0 ? data.UnreadMessagesTotal : null);
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
        sb.Append("\" data-sollic-tab=\"");
        sb.Append(WebUtility.HtmlEncode(tab));
        sb.Append("\" data-enhance-nav=\"false\"");
        if (string.Equals(tab, ApplicationsSsrQuery.TabBerichten, StringComparison.Ordinal))
        {
            sb.Append(" data-sollic-unread-threads=\"");
            sb.Append(badge ?? 0);
            sb.Append('"');
        }
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
        sb.Append("\" role=\"status\"><span class=\"sollicitaties-init-banner__icon\" aria-hidden=\"true\">");
        sb.Append(SollicitatiesInlineIcons.People16);
        sb.Append("</span><span class=\"sollicitaties-init-banner__text\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Apps.InitiativeWeekBanner", remaining, max)));
        sb.Append("</span></p>");
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
            sb.Append("\" data-enhance-nav=\"false\"><span class=\"sollicitaties-toolbar__add-icon\" aria-hidden=\"true\">");
            sb.Append(SollicitatiesInlineIcons.Plus16);
            sb.Append("</span>");
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
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            filter,
            data.SelectedId)));
        sb.Append("\" data-sollic-filter=\"");
        sb.Append(WebUtility.HtmlEncode(filter));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append(" · ");
        sb.Append(count);
        sb.Append("</a>");
    }

    private static void AppendEmpty(StringBuilder sb, CultureState culture, ApplicationsSsrData.PageData data, bool hidden = false)
    {
        sb.Append("<div class=\"sollicitaties-empty\" data-sollic-empty");
        if (hidden)
        {
            sb.Append(" hidden");
        }

        sb.Append("><article class=\"sollicitaties-empty-card\"><h2 class=\"sollicitaties-empty-card__title\">");
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

    private static void AppendAllTabPanels(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        string? detailReturnHref)
    {
        sb.Append("<div class=\"sollicitaties-tab-panels\" data-sollic-active-tab=\"");
        sb.Append(WebUtility.HtmlEncode(data.Tab));
        sb.Append("\" data-sollic-active-filter=\"");
        sb.Append(WebUtility.HtmlEncode(data.Filter));
        sb.Append("\">");

        var sollicActive = string.Equals(data.Tab, ApplicationsSsrQuery.TabSollicitaties, StringComparison.Ordinal);
        sb.Append("<div class=\"sollicitaties-tab-panel\" data-sollic-tab-panel=\"");
        sb.Append(ApplicationsSsrQuery.TabSollicitaties);
        sb.Append('"');
        if (!sollicActive)
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        AppendToolbar(sb, culture, data);
        AppendEmpty(sb, culture, data, hidden: data.FilteredRows.Count > 0);
        AppendLayout(sb, culture, data, detailReturnHref);
        sb.Append("</div>");

        if (data.AfterMatchEnabled)
        {
            var berichtenActive = string.Equals(data.Tab, ApplicationsSsrQuery.TabBerichten, StringComparison.Ordinal);
            sb.Append("<div class=\"sollicitaties-tab-panel\" data-sollic-tab-panel=\"");
            sb.Append(ApplicationsSsrQuery.TabBerichten);
            sb.Append('"');
            if (!berichtenActive)
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
            SollicitatiesBerichtenSsr.AppendBerichtenTab(sb, culture, data);
            sb.Append("</div>");
        }

        var activiteitenActive = string.Equals(data.Tab, ApplicationsSsrQuery.TabActiviteiten, StringComparison.Ordinal);
        sb.Append("<div class=\"sollicitaties-tab-panel\" data-sollic-tab-panel=\"");
        sb.Append(ApplicationsSsrQuery.TabActiviteiten);
        sb.Append('"');
        if (!activiteitenActive)
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        AppendActiviteitenPlaceholder(sb, culture);
        sb.Append("</div></div>");
    }

    private static void AppendLayout(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        string? detailReturnHref)
    {
        SollicitationRow? selected = null;
        foreach (var row in data.AllRows)
        {
            if (!ApplicationsSsrQuery.MatchesFilter(row, data.Filter))
            {
                continue;
            }

            if (data.SelectedId is Guid sel && row.RowId == sel)
            {
                selected = row;
                break;
            }
        }

        if (selected is null)
        {
            selected = data.AllRows.FirstOrDefault(r => ApplicationsSsrQuery.MatchesFilter(r, data.Filter));
        }

        sb.Append("<div class=\"sollicitaties-layout\" data-sollic-layout=\"1\"");
        if (data.FilteredRows.Count == 0)
        {
            sb.Append(" hidden");
        }

        sb.Append("><div class=\"sollicitaties-list\" role=\"list\">");
        foreach (var row in data.AllRows)
        {
            var matches = ApplicationsSsrQuery.MatchesFilter(row, data.Filter);
            AppendListCard(sb, culture, data, row, selected?.RowId == row.RowId, hidden: !matches);
        }

        sb.Append("</div><aside class=\"sollicitaties-detail\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.MyApplications"]));
        sb.Append("\">");
        foreach (var row in data.AllRows)
        {
            var matches = ApplicationsSsrQuery.MatchesFilter(row, data.Filter);
            var isSel = selected?.RowId == row.RowId && matches;
            sb.Append("<div class=\"sollic-detail-panel\" data-sollic-row-id=\"");
            sb.Append(row.RowId.ToString("D"));
            sb.Append("\" data-sollic-filters=\"");
            sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.RowFilterKeys(row)));
            sb.Append('"');
            if (!isSel)
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
            AppendDetailPanel(sb, culture, data, row, detailReturnHref);
            sb.Append("</div>");
        }

        sb.Append("</aside></div>");
    }

    private static void AppendListCard(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        SollicitationRow row,
        bool selected,
        bool hidden = false)
    {
        var selectHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            data.Filter,
            row.RowId);
        sb.Append("<article class=\"sollic-card sollic-card--app");
        if (selected)
        {
            sb.Append(" is-selected");
        }

        if (row.IsDone)
        {
            sb.Append(" sollic-card--done");
        }

        sb.Append("\" role=\"listitem\" data-sollic-row-id=\"");
        sb.Append(row.RowId.ToString("D"));
        sb.Append("\" data-sollic-filters=\"");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrQuery.RowFilterKeys(row)));
        sb.Append('"');
        if (row.ApplicationId is Guid appIdForCard)
        {
            sb.Append(" data-sollic-app-id=\"");
            sb.Append(appIdForCard.ToString("D"));
            sb.Append('"');
        }

        if (hidden)
        {
            sb.Append(" hidden");
        }

        sb.Append("><a class=\"sollic-card__select\" href=\"");
        sb.Append(WebUtility.HtmlEncode(selectHref));
        sb.Append("\" data-enhance-nav=\"false\"><span class=\"sollic-card__avatar logo-sq\" aria-hidden=\"true\">");
        sb.Append(WebUtility.HtmlEncode(ApplicationsSsrData.CompanyInitials(row.CompanyName)));
        sb.Append("</span><span class=\"sollic-card__main\"><span class=\"sollic-card__source sollic-card__source--");
        sb.Append(row.IsInitiative ? "initiative" : row.IsExternal ? "external" : "vacancy");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture[row.SourceTagKey]));
        sb.Append("</span><span class=\"sollic-card__title\">");
        sb.Append(WebUtility.HtmlEncode(row.Title));
        sb.Append("</span><span class=\"sollic-card__meta c\">");
        sb.Append(WebUtility.HtmlEncode(CompanyPlace(row)));
        sb.Append("</span>");
        if (row.AfterMatch is null && !string.IsNullOrWhiteSpace(row.CardHint))
        {
            sb.Append("<span class=\"sollic-card__hint\">");
            sb.Append(WebUtility.HtmlEncode(row.CardHint));
            sb.Append("</span>");
        }

        sb.Append("</span></a><div class=\"sollic-card__status-row s\">");
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
            var cardReturn = ApplicationsSsrQuery.BuildUrl(data.Tab, data.Filter, row.RowId);
            var cardAction = AfterMatchSollicitatiesPresentation.BuildCardAction(culture, row.AfterMatch, cardReturn);
            if (cardAction is not null)
            {
                sb.Append("<a class=\"sollic-card__ndm-act act act--");
                sb.Append(cardAction.Modifier == "action-pick" ? "do" : "ok");
                sb.Append("\" href=\"");
                sb.Append(WebUtility.HtmlEncode(cardAction.Href));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(cardAction.Label));
                sb.Append("</a>");
            }
            else
            {
                sb.Append("<span class=\"sollic-card__pill sollic-card__pill--");
                sb.Append(WebUtility.HtmlEncode(row.StatusModifier));
                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(row.StatusLabel));
                sb.Append("</span>");
            }

            if (!string.IsNullOrWhiteSpace(row.CardHint))
            {
                sb.Append("<span class=\"sollic-card__step\">");
                sb.Append(WebUtility.HtmlEncode(row.CardHint));
                sb.Append("</span>");
            }
        }

        sb.Append("</div><div class=\"sollic-card__right right\">");
        if (row.AfterMatch is { UnreadMessageCount: > 0 } am)
        {
            sb.Append("<a class=\"sollic-card__msg msgc\" href=\"");
            sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Messages(am.ApplicationId)));
            sb.Append("\" data-enhance-nav=\"false\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(string.Format(culture["Apps.Ndm.UnreadAria"], am.UnreadMessageCount)));
            sb.Append("\"><span class=\"sollic-card__msg-icon\" aria-hidden=\"true\">✉</span><b class=\"sollic-card__msg-count\">");
            sb.Append(am.UnreadMessageCount);
            sb.Append("</b></a>");
        }

        sb.Append("<span class=\"sollic-card__chev chev\" aria-hidden=\"true\">›</span></div></article>");
    }

    private static void AppendDetailPanel(
        StringBuilder sb,
        CultureState culture,
        ApplicationsSsrData.PageData data,
        SollicitationRow row,
        string? detailReturnHref)
    {
        var listHref = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            data.Filter,
            null);
        sb.Append("<div class=\"sollic-detail card detail\">");
        if (!string.IsNullOrEmpty(detailReturnHref))
        {
            sb.Append("<a class=\"sollic-detail__back back sollic-detail__back--return\" href=\"");
            sb.Append(WebUtility.HtmlEncode(detailReturnHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Back"]));
            sb.Append("</a>");
        }

        sb.Append("<a class=\"sollic-detail__back back\" href=\"");
        sb.Append(WebUtility.HtmlEncode(listHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.BackAllApplications"]));
        sb.Append("</a><div class=\"sollic-detail__head\"><span class=\"sollic-card__source sollic-card__source--");
        sb.Append(row.IsInitiative ? "initiative" : row.IsExternal ? "external" : "vacancy");
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture[row.SourceTagKey]));
        sb.Append("</span><h2 class=\"sollic-detail__title\">");
        sb.Append(WebUtility.HtmlEncode(row.Title));
        sb.Append("</h2><p class=\"sollic-detail__meta muted small\">");
        sb.Append(WebUtility.HtmlEncode(CompanyPlace(row)));
        sb.Append("</p></div>");
        if (row.AfterMatch is not null)
        {
            if (!string.IsNullOrWhiteSpace(row.AfterMatch.LastCancelReason))
            {
                sb.Append("<p class=\"sollic-detail__status-note state-message\" role=\"status\">");
                sb.Append(WebUtility.HtmlEncode(
                    row.AfterMatch.LastCancelByEmployer == true
                        ? string.Format(
                            culture["AfterMatch.InterviewCancelled.CandidateNoteEmployerFmt"],
                            row.AfterMatch.LastCancelReason)
                        : string.Format(
                            culture["AfterMatch.InterviewCancelled.CandidateNoteSelfFmt"],
                            row.AfterMatch.LastCancelReason)));
                sb.Append("</p>");
            }

            AppendAfterMatchTimeline(sb, culture, row.AfterMatch);
            AppendDetailTip(sb, culture, row);
            var detailReturn = ApplicationsSsrQuery.BuildUrl(data.Tab, data.Filter, row.RowId);
            var actions = AfterMatchSollicitatiesPresentation.BuildDetailActions(culture, row.AfterMatch, detailReturn);
            if (actions is not null)
            {
                sb.Append("<div class=\"sollic-detail__acts acts\">");
                sb.Append("<a class=\"btn btn-primary sollic-detail__cta btn--primary\" href=\"");
                sb.Append(WebUtility.HtmlEncode(actions.PrimaryHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(actions.PrimaryLabel));
                sb.Append("</a><a class=\"btn sollic-detail__cta-secondary\" href=\"");
                sb.Append(WebUtility.HtmlEncode(actions.MessagesHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.Messages"]));
                if (actions.UnreadMessages > 0)
                {
                    sb.Append("<b class=\"sollic-detail__msg-badge\">");
                    sb.Append(actions.UnreadMessages);
                    sb.Append("</b>");
                }

                sb.Append("</a></div>");
                AppendDetailMoreLinks(sb, culture, row);
            }
        }
        else if (row.Timeline.Count > 0)
        {
            AppendMockup01VerticalTimeline(sb, culture, row.Timeline);
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

    private static void AppendAfterMatchTimeline(
        StringBuilder sb,
        CultureState culture,
        ApplicationAfterMatchSummary summary)
    {
        var steps = AfterMatchSollicitatiesPresentation.BuildAfterMatchTimeline(culture, summary);
        AppendMockup02HorizontalTimeline(sb, culture, steps);
    }

    private const string MockupTlIconCheck =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\"><path d=\"m5 12 4 4 10-10\"/></svg>";

    private const string MockupTlIconCal =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"><rect x=\"4\" y=\"5\" width=\"16\" height=\"15\" rx=\"2\"/><path d=\"M4 10h16M9 3v4M15 3v4\"/></svg>";

    private const string MockupTlIconHand =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m3 12 4-4 4 2 3-2 4 1 3 3-6 6-3-2-3 2z\"/></svg>";

    private const string MockupTlIconFlag =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\"><path d=\"M5 21V4M5 4h11l-2 4 2 4H5\"/></svg>";

    private static void AppendMockup01VerticalTimeline(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<SollicitationTimelineStep> steps)
    {
        sb.Append("<ol class=\"sollic-vsteps vsteps\" role=\"list\">");
        foreach (var step in steps)
        {
            sb.Append("<li class=\"vsteps__step vsteps__step--");
            sb.Append(WebUtility.HtmlEncode(step.State.ToLowerInvariant()));
            sb.Append("\" role=\"listitem\"><span class=\"vsteps__dot\" aria-hidden=\"true\">");
            if (string.Equals(step.State, "Done", StringComparison.OrdinalIgnoreCase)
                || string.Equals(step.State, "Skipped", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(MockupTlIconCheck);
            }

            sb.Append("</span><span class=\"vsteps__body\"><span class=\"vsteps__label\">");
            sb.Append(WebUtility.HtmlEncode(step.Label));
            sb.Append("</span>");
            if (step.OccurredAtUtc is DateTime at)
            {
                sb.Append("<span class=\"vsteps__sub\">");
                sb.Append(WebUtility.HtmlEncode(FormatTimelineDate(culture, at, step)));
                sb.Append("</span>");
            }
            else if (string.Equals(step.State, "Current", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<span class=\"vsteps__sub\">");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Now"]));
                sb.Append("</span>");
            }
            else if (string.Equals(step.Key, "Outcome", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<span class=\"vsteps__sub\">");
                sb.Append(WebUtility.HtmlEncode(culture["Kb.Timeline.AfterInterview"]));
                sb.Append("</span>");
            }

            sb.Append("</span></li>");
        }

        sb.Append("</ol>");
    }

    private static void AppendMockup02HorizontalTimeline(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<SollicitationTimelineStep> steps)
    {
        sb.Append("<div class=\"sollic-ndm-tl tl\" role=\"list\">");
        foreach (var step in steps)
        {
            sb.Append("<div class=\"s");
            sb.Append(MockupTimelineStateClass(step.State));
            sb.Append("\" role=\"listitem\"><i aria-hidden=\"true\">");
            sb.Append(MockupTimelineIcon(step.Key));
            sb.Append("</i>");
            sb.Append(WebUtility.HtmlEncode(step.Label));
            if (step.OccurredAtUtc is DateTime at)
            {
                sb.Append("<small>");
                sb.Append(WebUtility.HtmlEncode(FormatTimelineDate(culture, at, step)));
                sb.Append("</small>");
            }
            else if (string.Equals(step.State, "Current", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<small>");
                sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Now"]));
                sb.Append("</small>");
            }
            else if (string.Equals(step.Key, "Outcome", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(step.State, "Upcoming", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<small>");
                sb.Append(WebUtility.HtmlEncode(culture["Kb.Timeline.AfterInterview"]));
                sb.Append("</small>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
    }

    private static string MockupTimelineStateClass(string state)
        => state switch
        {
            "Done" or "Skipped" => " done",
            "Current" => " now",
            _ => string.Empty
        };

    private static string MockupTimelineIcon(string key)
        => key switch
        {
            "Yes" or "Sent" or "Created" => MockupTlIconCheck,
            "Plan" or "Seen" or "EmployerViewed" => MockupTlIconCal,
            "Interview" or "Gesprek" => MockupTlIconHand,
            "Outcome" or "Uitslag" => MockupTlIconFlag,
            _ => MockupTlIconCal
        };

    private static void AppendDetailMoreLinks(StringBuilder sb, CultureState culture, SollicitationRow row)
    {
        if (row.ApplicationId is not Guid appId || row.VacancyId is not Guid vacId)
        {
            return;
        }

        sb.Append("<div class=\"sollic-detail__more more\">");
        sb.Append("<a class=\"sollic-detail__more-link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.Vacancy(vacId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.MenuVacancy"]));
        sb.Append("</a><a class=\"sollic-detail__more-link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(AfterMatchSollicitatiesUrls.LobsyCvPdf(appId)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.LobsyCv"]));
        sb.Append("</a><button type=\"button\" class=\"sollic-detail__more-link sollic-detail__more-link--danger danger sollic-detail__withdraw\" data-application-id=\"");
        sb.Append(appId.ToString("D"));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.WithdrawShort"]));
        sb.Append("</button></div>");
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

        sb.Append("<div class=\"sollic-detail__tip note\"><p class=\"sollic-detail__tip-title\">");
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
            return CandidateVacancyUrls.BanenkaartDetail(vacId);
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
