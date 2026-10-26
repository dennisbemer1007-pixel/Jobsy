using System.Net;
using System.Text;
using Jobsy.Core.Uwv;
using Jobsy.Web.Hosting;
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
        AppendHeroStrips(sb, data.ActiveTab);
        sb.Append("<div class=\"journey-page career-page career-page--illustrated panel-page--cand-ill\" data-testid=\"career-page-ssr\" data-career-active-tab=\"");
        sb.Append(WebUtility.HtmlEncode(data.ActiveTab));
        sb.Append("\">");
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

        AppendVoorJouPanel(sb, culture, data, antiforgeryToken, data.ActiveTab);
        AppendOmscholenPanel(sb, culture, data, data.ActiveTab);
        AppendKansberoepenPanel(sb, culture, data, data.ActiveTab);

        sb.Append("<p class=\"carriere-segmented__footnote\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.Footnote(culture.Language)));
        sb.Append("</p></div>");
        sb.Append("<script src=\"");
        sb.Append(WwwrootAssetCatalog.Url("js/career-ssr.js"));
        sb.Append("\" defer></script></div></div>");
        return sb.ToString();
    }

    private static void AppendHeroStrips(StringBuilder sb, string activeTab)
    {
        var omscholenActive = string.Equals(activeTab, CareerSsrTabs.Omscholen, StringComparison.OrdinalIgnoreCase);
        sb.Append("<div data-career-strip-wrap=\"carriere\"");
        if (omscholenActive)
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.CarriereDesk, KreeftHeaderStripAssets.CarriereMob, "carriere");
        sb.Append("</div><div data-career-strip-wrap=\"omscholen\"");
        if (!omscholenActive)
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.OmscholenDesk, KreeftHeaderStripAssets.OmscholenMob, "omscholen");
        sb.Append("</div>");
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
        AppendTabButton(sb, CareerSsrTabs.VoorJou, UwvMockupCopy.CarriereSegmented.TabVoorJou(lang), activeTab);
        AppendTabButton(sb, CareerSsrTabs.Omscholen, UwvMockupCopy.CarriereSegmented.TabOmscholen(lang), activeTab);
        AppendTabButton(sb, CareerSsrTabs.Kansberoepen, UwvMockupCopy.CarriereSegmented.TabKansberoepen(lang), activeTab);
        sb.Append("</div></div>");
    }

    private static void AppendTabButton(StringBuilder sb, string tab, string label, string activeTab)
    {
        var selected = string.Equals(tab, activeTab, StringComparison.OrdinalIgnoreCase);
        sb.Append("<button type=\"button\" class=\"carriere-segmented__tab");
        if (selected)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" role=\"tab\" data-career-tab=\"");
        sb.Append(tab);
        sb.Append("\" aria-selected=\"");
        sb.Append(selected ? "true" : "false");
        sb.Append("\" id=\"carriere-tab-");
        sb.Append(tab);
        sb.Append("\" aria-controls=\"carriere-panel-");
        sb.Append(tab);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</button>");
    }

    private static void BeginPanel(StringBuilder sb, string tab, string activeTab, string testId)
    {
        sb.Append("<div class=\"carriere-segmented__panel\" role=\"tabpanel\" data-career-panel=\"");
        sb.Append(tab);
        sb.Append("\" id=\"carriere-panel-");
        sb.Append(tab);
        sb.Append("\" data-testid=\"");
        sb.Append(testId);
        sb.Append("\" aria-labelledby=\"carriere-tab-");
        sb.Append(tab);
        sb.Append('"');
        if (!string.Equals(tab, activeTab, StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
    }

    private static void AppendVoorJouPanel(
        StringBuilder sb,
        CultureState culture,
        CareerSsrData.PageData data,
        string antiforgeryToken,
        string activeTab)
    {
        var lang = culture.Language;
        BeginPanel(sb, CareerSsrTabs.VoorJou, activeTab, "career-tab-voor-jou");
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
        sb.Append("<div class=\"career-note career-note--soft carriere-segmented__dream-coach\" role=\"note\"><p><strong>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.DreamCoachTitle(lang)));
        sb.Append("</strong> ");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.DreamCoachBody(lang)));
        sb.Append("</p></div>");

        AppendSimilarJobs(sb, lang, culture, data);
        AppendStrongMatches(sb, lang, culture, data);
        sb.Append("</div>");
    }

    private static void AppendSimilarJobs(StringBuilder sb, string lang, CultureState culture, CareerSsrData.PageData data)
    {
        var jobs = UwvMockupData.SimilarJobs;
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-similar\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-similar\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(data.EmptyProfile ? UwvMockupCopy.Similar.EmptyTitle(lang) : UwvMockupCopy.Similar.Title(lang)));
        sb.Append("</h2>");
        AppendMeerButton(sb, culture, "similar", jobs.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\" data-career-card-group=\"similar\">");
        foreach (var job in jobs)
        {
            sb.Append("<li class=\"carriere-segmented__card\" data-career-card-item><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.WhyEn, job.WhyNl)));
            sb.Append("</p><p class=\"carriere-segmented__route muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.RouteEn, job.RouteNl)));
            sb.Append("</p></li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendStrongMatches(StringBuilder sb, string lang, CultureState culture, CareerSsrData.PageData data)
    {
        var jobs = UwvMockupData.StrongMatchJobs;
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-strong\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-strong\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.StrongMatches.Title(lang)));
        sb.Append("</h2>");
        AppendMeerButton(sb, culture, "strong", jobs.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\" data-career-card-group=\"strong\">");
        foreach (var job in jobs)
        {
            sb.Append("<li class=\"carriere-segmented__card\" data-career-card-item><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.WhyEn, job.WhyNl)));
            sb.Append("</p><p class=\"carriere-segmented__route muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.RouteEn, job.RouteNl)));
            sb.Append("</p>");
            if (job.Baangarantie)
            {
                AppendBaangarantieRouteBadge(sb, lang);
            }

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

    private static void AppendOmscholenPanel(StringBuilder sb, CultureState culture, CareerSsrData.PageData data, string activeTab)
    {
        var lang = culture.Language;
        BeginPanel(sb, CareerSsrTabs.Omscholen, activeTab, "career-tab-omscholen");
        var sectors = UwvMockupData.OmscholenSectors;
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-omscholen\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-omscholen\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.Title(lang)));
        sb.Append("</h2>");
        AppendMeerButton(sb, culture, "omscholen", sectors.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\" data-career-card-group=\"omscholen\">");
        foreach (var (sector, icon) in sectors)
        {
            sb.Append("<li class=\"carriere-segmented__card carriere-segmented__card--rich\" data-career-card-item>");
            CareerSsrSectorIcon.Append(sb, icon, "carriere-segmented__card-icon");
            sb.Append("<div class=\"carriere-segmented__card-body\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, sector.NameEn, sector.NameNl)));
            sb.Append(" · ");
            sb.Append(WebUtility.HtmlEncode(En(lang, sector.DetailEn, sector.DetailNl)));
            sb.Append("</h3>");
            if (sector.HasBaangarantie)
            {
                AppendBaangarantieRouteBadge(sb, lang);
            }

            sb.Append("<a class=\"uwv-inline-link\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.MapLink(lang)));
            sb.Append("</a></div></li>");
        }

        sb.Append("</ul></section>");
        AppendLeerbanen(sb, lang, culture, data);
        sb.Append("<aside class=\"uwv-omscholen__aside carriere-segmented__aside\"><p>");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.Werkcentrum(lang)));
        sb.Append(" <a href=\"https://www.werkcentrum.nl/\" rel=\"noopener noreferrer\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.WerkcentrumLink(lang)));
        sb.Append("</a></p><p class=\"uwv-omscholen__ww\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.WwNote(lang)));
        sb.Append(" <a href=\"https://www.uwv.nl/\" rel=\"noopener noreferrer\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.WwLink(lang)));
        sb.Append("</a></p></aside></div>");
    }

    private static void AppendKansberoepenPanel(StringBuilder sb, CultureState culture, CareerSsrData.PageData data, string activeTab)
    {
        var lang = culture.Language;
        var entries = data.KansDataset?.Entries ?? [];
        BeginPanel(sb, CareerSsrTabs.Kansberoepen, activeTab, "career-tab-kansberoepen");
        sb.Append("<p class=\"carriere-segmented__meta\"><span class=\"uwv-kans__region\">");
        sb.Append(WebUtility.HtmlEncode(data.KansDataset is null
            ? UwvMockupCopy.Kansberoepen.Region(lang)
            : En(lang, data.KansDataset.RegionKeyEn, data.KansDataset.RegionKeyNl)));
        sb.Append("</span> <span class=\"uwv-block__source\">");
        sb.Append(WebUtility.HtmlEncode(data.KansDataset is null
            ? UwvMockupCopy.Kansberoepen.Source(lang)
            : En(lang, data.KansDataset.SourceLineEn, data.KansDataset.SourceLineNl)));
        sb.Append("</span></p><section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-kans\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-kans\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.Title(lang)));
        sb.Append("</h2>");
        AppendMeerButton(sb, culture, "kans", entries.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\" data-career-card-group=\"kans\">");
        foreach (var job in entries)
        {
            sb.Append("<li class=\"carriere-segmented__card carriere-segmented__card--rich\" data-career-card-item>");
            CareerSsrSectorIcon.Append(sb, job.SectorIcon, "carriere-segmented__card-icon");
            sb.Append("<div class=\"carriere-segmented__card-body\"><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.TitleEn, job.TitleNl)));
            sb.Append("</h3>");
            if (job.Baangarantie)
            {
                AppendBaangarantieBadge(sb, lang);
            }

            sb.Append("<p>");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.ProspectEn, job.ProspectNl)));
            sb.Append("</p><p class=\"carriere-segmented__route muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, job.RouteEn, job.RouteNl)));
            sb.Append("</p><a class=\"uwv-kans__map-link\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Kansberoepen.VacanciesLink(lang, job.VacancyCount)));
            sb.Append("</a></div></li>");
        }

        sb.Append("</ul></section></div>");
    }

    private static void AppendMeerButton(
        StringBuilder sb,
        CultureState culture,
        string groupId,
        int total,
        IReadOnlyCollection<string> expanded)
    {
        if (total <= 3)
        {
            return;
        }

        var lang = culture.Language;
        var isOpen = expanded.Contains(groupId);
        sb.Append("<button type=\"button\" class=\"carriere-segmented__meer\" data-career-meer=\"");
        sb.Append(groupId);
        sb.Append("\" data-testid=\"career-meer-");
        sb.Append(groupId);
        sb.Append("\" aria-expanded=\"");
        sb.Append(isOpen ? "true" : "false");
        sb.Append("\" data-career-meer-more=\"");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.CarriereSegmented.Meer(lang)));
        sb.Append("\" data-career-meer-less=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Training.LearnGrow.ShowLess"]));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(isOpen ? culture["Training.LearnGrow.ShowLess"] : UwvMockupCopy.CarriereSegmented.Meer(lang)));
        sb.Append("</button>");
    }

    private static void AppendLeerbanen(StringBuilder sb, string lang, CultureState culture, CareerSsrData.PageData data)
    {
        var leerbanen = UwvMockupData.Leerbanen;
        sb.Append("<section class=\"carriere-segmented__group\" aria-labelledby=\"carriere-group-leerbanen\">");
        sb.Append("<div class=\"carriere-segmented__group-head\"><h2 id=\"carriere-group-leerbanen\" class=\"carriere-segmented__group-title\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Apprenticeships (BBL)" : "Leerbanen BBL"));
        sb.Append("</h2>");
        AppendMeerButton(sb, culture, "leerbanen", leerbanen.Count, data.ExpandedGroups);
        sb.Append("</div><ul class=\"carriere-segmented__cards\" data-career-card-group=\"leerbanen\">");
        foreach (var lb in leerbanen)
        {
            sb.Append("<li class=\"carriere-segmented__card\" data-career-card-item><h3>");
            sb.Append(WebUtility.HtmlEncode(En(lang, lb.TitleEn, lb.TitleNl)));
            sb.Append("</h3><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(En(lang, lb.CompanyEn, lb.CompanyNl)));
            sb.Append("</p><a class=\"uwv-inline-link\" href=\"");
            sb.Append(WebUtility.HtmlEncode(lb.MapHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Omscholen.MapLink(lang)));
            sb.Append("</a></li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendBaangarantieRouteBadge(StringBuilder sb, string lang)
    {
        sb.Append("<span class=\"uwv-badge uwv-badge--guarantee\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Job guarantee route" : "Baangarantie-route"));
        sb.Append("</span>");
    }

    private static void AppendBaangarantieBadge(StringBuilder sb, string lang)
    {
        sb.Append("<span class=\"uwv-badge uwv-badge--guarantee\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? "Job guarantee" : "Baangarantie"));
        sb.Append("</span>");
    }

    private static string En(string lang, string en, string nl)
        => UwvMockupCopy.IsEnglish(lang) ? en : nl;
}
