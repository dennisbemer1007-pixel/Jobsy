using System.Net;
using System.Text;
using Jobsy.Core.Uwv;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.UwvMockups;

namespace Jobsy.Web.Career;

internal static class CareerSsrShells
{
    internal static string BuildBody(CultureState culture, CareerSsrData.PageData data, string antiforgeryToken)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"career-page-wrap\">");
        AppendHeroStrip(sb, data);
        sb.Append("<div class=\"journey-page career-page career-page--illustrated panel-page--cand-ill\" data-testid=\"career-page-ssr\">");
        AppendHero(sb, culture, data);
        sb.Append("<div class=\"carriere-segmented career-uwv-sections opl-wrap\" data-testid=\"uwv-carriere-sections\">");
        if (data.EmptyProfile)
        {
            AppendEmptyProfile(sb, culture);
        }

        AppendTabNav(sb, culture, data.ActiveTab);
        if (!string.IsNullOrWhiteSpace(data.ErrorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(data.ErrorMessage));
            sb.Append("</p>");
        }
        else if (data.Saved)
        {
            sb.Append("<p class=\"state-message\" role=\"status\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Saved"]));
            sb.Append("</p>");
        }

        switch (data.ActiveTab)
        {
            case CareerSsrTabs.Omscholen:
                AppendOmscholenPanel(sb, culture, data);
                break;
            case CareerSsrTabs.Kansberoepen:
                AppendKansberoepenPanel(sb, culture, data);
                break;
            default:
                AppendVoorJouPanel(sb, culture, data, antiforgeryToken);
                break;
        }

        sb.Append("<p class=\"carriere-segmented__footnote\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.Footnote(culture.Language)));
        sb.Append("</p></div></div></div>");
        return sb.ToString();
    }

    private static void AppendHeroStrip(StringBuilder sb, CareerSsrData.PageData data)
    {
        var omscholen = string.Equals(data.ActiveTab, CareerSsrTabs.Omscholen, StringComparison.Ordinal);
        if (omscholen)
        {
            KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.OmscholenDesk, KreeftHeaderStripAssets.OmscholenMob, "omscholen");
            return;
        }

        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.CarriereDesk, KreeftHeaderStripAssets.CarriereMob, "carriere");
    }

    private static void AppendHero(StringBuilder sb, CultureState culture, CareerSsrData.PageData data)
    {
        sb.Append("<header class=\"carriere-ssr-intro uwv-carriere-showcase-hero\" data-testid=\"career-hero\">");
        sb.Append("<span class=\"carriere-ssr-intro__eyebrow cand-hero__eyebrow cand-hero__eyebrow--coral\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Career.Eyebrow(culture.Language)));
        sb.Append("</span><h1 tabindex=\"-1\" class=\"carriere-ssr-intro__title\">");
        sb.Append(WebUtility.HtmlEncode(data.HeroTitle));
        sb.Append("</h1><p class=\"carriere-ssr-intro__lead cand-hero__lead\">");
        sb.Append(WebUtility.HtmlEncode(data.HeroLead));
        sb.Append("</p></header>");
    }

    private static void AppendEmptyProfile(StringBuilder sb, CultureState culture)
    {
        var lang = culture.Language;
        sb.Append("<div class=\"carriere-segmented__empty-profile career-note career-note--soft\" data-testid=\"carriere-segmented-empty-profile\" role=\"status\">");
        sb.Append("<p class=\"carriere-segmented__empty-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.EmptyProfileTitle(lang)));
        sb.Append("</p><p class=\"carriere-segmented__empty-lead\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.EmptyProfileLead(lang)));
        sb.Append("</p><a class=\"career-btn career-btn--primary carriere-segmented__empty-cta\" href=\"/candidate/paspoort?tab=data\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.EmptyProfileCta(lang)));
        sb.Append("</a></div>");
    }

    private static void AppendTabNav(StringBuilder sb, CultureState culture, string activeTab)
    {
        var lang = culture.Language;
        sb.Append("<div class=\"carriere-segmented__nav-wrap\"><div class=\"carriere-segmented__nav\" role=\"tablist\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Career.Eyebrow(lang)));
        sb.Append("\">");
        AppendTabLink(sb, CareerSsrTabs.VoorJou, UwvMockupCopy.CarriereSegmented.TabVoorJou(lang), activeTab);
        AppendTabLink(sb, CareerSsrTabs.Omscholen, UwvMockupCopy.CarriereSegmented.TabOmscholen(lang), activeTab);
        AppendTabLink(sb, CareerSsrTabs.Kansberoepen, UwvMockupCopy.CarriereSegmented.TabKansberoepen(lang), activeTab);
        sb.Append("</div></div>");
    }

    private static void AppendTabLink(StringBuilder sb, string tab, string label, string activeTab)
    {
        var selected = string.Equals(tab, activeTab, StringComparison.OrdinalIgnoreCase);
        sb.Append("<a class=\"carriere-segmented__tab");
        if (selected)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" role=\"tab\" href=\"");
        sb.Append(WebUtility.HtmlEncode(CareerClientDreamSaver.BuildCareerUrl(tab)));
        sb.Append("\" data-enhance-nav=\"false\" aria-selected=\"");
        sb.Append(selected ? "true" : "false");
        sb.Append("\" id=\"carriere-tab-");
        sb.Append(tab);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</a>");
    }

    private static void AppendVoorJouPanel(
        StringBuilder sb,
        CultureState culture,
        CareerSsrData.PageData data,
        string antiforgeryToken)
    {
        var lang = culture.Language;
        sb.Append("<div class=\"carriere-segmented__panel\" role=\"tabpanel\" id=\"carriere-panel-voor-jou\" data-testid=\"career-tab-voor-jou\">");
        sb.Append("<form class=\"career-ssr-dream-form\" method=\"post\" action=\"/api/career-client/save-dream\" data-enhance=\"false\" data-enhance-nav=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"returnTab\" value=\"");
        sb.Append(CareerSsrTabs.VoorJou);
        sb.Append("\" /><label><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Step6.Title"]));
        sb.Append("</span><input name=\"dreamTitle\" type=\"text\" value=\"");
        sb.Append(WebUtility.HtmlEncode(data.DreamTitle ?? ""));
        sb.Append("\" data-testid=\"career-dream-title\" data-career-static-field=\"1\" autocomplete=\"off\" /></label>");
        sb.Append("<button type=\"submit\" class=\"btn btn-primary\" data-testid=\"career-dream-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></form>");

        AppendSimilarJobs(sb, lang, data);
        AppendStrongMatches(sb, lang, data);
        sb.Append("</div>");
    }

    private static void AppendSimilarJobs(StringBuilder sb, string lang, CareerSsrData.PageData data)
    {
        var jobs = UwvMockupData.SimilarJobs;
        var take = VisibleCount("similar", jobs.Count, data.ExpandedGroups);
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-similar\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-similar\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(data.EmptyProfile ? UwvMockupCopy.Similar.EmptyTitle(lang) : UwvMockupCopy.Similar.Title(lang)));
        sb.Append("</h2>");
        AppendMeerLink(sb, lang, CareerSsrTabs.VoorJou, "similar", jobs.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\">");
        foreach (var job in jobs.Take(take))
        {
            sb.Append("<li class=\"carriere-segmented__card\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.WhyEn, job.WhyNl)));
            sb.Append("</p><p class=\"carriere-segmented__route muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.RouteEn, job.RouteNl)));
            sb.Append("</p></li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendStrongMatches(StringBuilder sb, string lang, CareerSsrData.PageData data)
    {
        var jobs = UwvMockupData.StrongMatchJobs;
        var take = VisibleCount("strong", jobs.Count, data.ExpandedGroups);
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-strong\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-strong\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.StrongMatches.Title(lang)));
        sb.Append("</h2>");
        AppendMeerLink(sb, lang, CareerSsrTabs.VoorJou, "strong", jobs.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\">");
        foreach (var job in jobs.Take(take))
        {
            sb.Append("<li class=\"carriere-segmented__card\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.WhyEn, job.WhyNl)));
            sb.Append("</p><p class=\"carriere-segmented__route muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.RouteEn, job.RouteNl)));
            sb.Append("</p>");
            if (job.MapLink)
            {
                sb.Append("<a class=\"uwv-inline-link\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.MapLink(lang)));
                sb.Append("</a>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendOmscholenPanel(StringBuilder sb, CultureState culture, CareerSsrData.PageData data)
    {
        var lang = culture.Language;
        sb.Append("<div class=\"carriere-segmented__panel\" role=\"tabpanel\" id=\"carriere-panel-omscholen\" data-testid=\"career-tab-omscholen\">");
        var sectors = UwvMockupData.OmscholenSectors;
        var take = VisibleCount("omscholen", sectors.Count, data.ExpandedGroups);
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-omscholen\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-omscholen\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.Title(lang)));
        sb.Append("</h2>");
        AppendMeerLink(sb, lang, CareerSsrTabs.Omscholen, "omscholen", sectors.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\">");
        foreach (var (sector, _) in sectors.Take(take))
        {
            sb.Append("<li class=\"carriere-segmented__card carriere-segmented__card--rich\"><div class=\"carriere-segmented__card-body\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, sector.NameEn, sector.NameNl)));
            sb.Append(" · ");
            sb.Append(WebUtility.HtmlEncode(En(lang, sector.DetailEn, sector.DetailNl)));
            sb.Append("</h3><a class=\"uwv-inline-link\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.MapLink(lang)));
            sb.Append("</a></div></li>");
        }

        sb.Append("</ul></section>");
        sb.Append("<aside class=\"uwv-omscholen__aside carriere-segmented__aside\"><p>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.Werkcentrum(lang)));
        sb.Append(" <a href=\"https://www.werkcentrum.nl/\" rel=\"noopener noreferrer\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.WerkcentrumLink(lang)));
        sb.Append("</a></p></aside></div>");
    }

    private static void AppendKansberoepenPanel(StringBuilder sb, CultureState culture, CareerSsrData.PageData data)
    {
        var lang = culture.Language;
        var entries = data.KansDataset?.Entries ?? [];
        sb.Append("<div class=\"carriere-segmented__panel\" role=\"tabpanel\" id=\"carriere-panel-kansberoepen\" data-testid=\"career-tab-kansberoepen\">");
        sb.Append("<p class=\"carriere-segmented__meta\"><span class=\"uwv-kans__region\">");
        sb.Append(WebUtility.HtmlEncode(data.KansDataset is null
            ? UwvMockupCopy.Kansberoepen.Region(lang)
            : En(lang, data.KansDataset.RegionKeyEn, data.KansDataset.RegionKeyNl)));
        sb.Append("</span></p><section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-kans\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-kans\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.Title(lang)));
        sb.Append("</h2>");
        AppendMeerLink(sb, lang, CareerSsrTabs.Kansberoepen, "kans", entries.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\">");
        var take = VisibleCount("kans", entries.Count, data.ExpandedGroups);
        foreach (var job in entries.Take(take))
        {
            sb.Append("<li class=\"carriere-segmented__card carriere-segmented__card--rich\"><div class=\"carriere-segmented__card-body\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.ProspectEn, job.ProspectNl)));
            sb.Append("</p><a class=\"uwv-kans__map-link\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.VacanciesLink(lang, job.VacancyCount)));
            sb.Append("</a></div></li>");
        }

        sb.Append("</ul></section></div>");
    }

    private static void AppendMeerLink(
        StringBuilder sb,
        string lang,
        string tab,
        string groupId,
        int total,
        IReadOnlyCollection<string> expanded)
    {
        if (total <= 3)
        {
            return;
        }

        var isOpen = expanded.Contains(groupId);
        var next = new HashSet<string>(expanded, StringComparer.OrdinalIgnoreCase);
        if (isOpen)
        {
            next.Remove(groupId);
        }
        else
        {
            next.Add(groupId);
        }

        sb.Append("<a class=\"carriere-segmented__meer\" href=\"");
        sb.Append(WebUtility.HtmlEncode(CareerClientDreamSaver.BuildCareerUrl(tab, next)));
        sb.Append("\" data-enhance-nav=\"false\" data-testid=\"career-meer-");
        sb.Append(groupId);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.Meer(lang)));
        sb.Append("</a>");
    }

    private static int VisibleCount(string groupId, int total, IReadOnlyCollection<string> expanded)
        => expanded.Contains(groupId) ? total : Math.Min(3, total);

    private static string En(string lang, string en, string nl)
        => UwvMockupCopy.IsEnglish(lang) ? en : nl;
}
