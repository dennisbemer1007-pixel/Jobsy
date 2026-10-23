using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrShells
{
    internal static string BuildPage(
        CultureState culture,
        string antiforgeryToken,
        BanenkaartSsrData.PageModel model)
    {
        var sb = new StringBuilder();
        var q = model.Query;
        var listModeClass = model.Query.DesktopListMode ? " jobsy-discovery--list" : "";
        sb.Append("<div class=\"jobsy-discovery show-map").Append(listModeClass);
        if (!string.IsNullOrWhiteSpace(q.Error))
        {
            sb.Append(" kb-ssr-has-error");
        }

        if (model.ShowEmptyTravelMessage)
        {
            sb.Append(" kb-ssr-travel-empty");
        }

        sb.Append("\" data-banenkaart-ssr=\"1\" data-testid=\"banenkaart-ssr-root\">");

        if (!string.IsNullOrWhiteSpace(q.Error))
        {
            sb.Append("<p class=\"state-message error kb-ssr-error\" role=\"alert\">")
                .Append(WebUtility.HtmlEncode(q.Error))
                .Append("</p>");
        }

        if (q.Saved)
        {
            sb.Append("<p class=\"state-message success\" role=\"status\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Map.Saved"]))
                .Append("</p>");
        }

        if (model.CompanyMapEnabled)
        {
            sb.Append("<div class=\"bk-map-toggle bk-map-toggle--banen\" role=\"tablist\" aria-label=\"")
                .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Jobs"]))
                .Append("\"><a href=\"")
                .Append(KbRoutes.Map)
                .Append("\" class=\"is-active\" role=\"tab\" aria-selected=\"true\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Jobs"]))
                .Append("</a><a href=\"")
                .Append(KbRoutes.CompanyMap)
                .Append("\" role=\"tab\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.MapToggle.Companies"]))
                .Append("</a></div>");
        }

        AppendNearbyHeroStrip(sb, culture);
        AppendChrome(sb, culture, model);
        AppendVacancyPane(sb, culture, model);
        AppendMapPane(sb, culture, model);
        AppendFilterSheet(sb, culture, antiforgeryToken, model);
        AppendMapFab(sb, culture);

        sb.Append("<script src=\"/js/jobsy-geo-lite.js?v=20261023-24\" crossorigin=\"anonymous\" defer></script>");
        sb.Append("<script src=\"/js/jobsy-basemap-warm.js?v=20261023-24\" crossorigin=\"anonymous\" defer></script>");
        sb.Append("<script src=\"/js/banenkaart-catalog.js?v=20261023-24\" crossorigin=\"anonymous\" defer></script>");
        sb.Append("<script src=\"/js/banenkaart-ssr.js?v=20261023-24\" crossorigin=\"anonymous\" defer></script>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendChrome(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        var q = model.Query;
        sb.Append("<div class=\"jobsy-chrome\"><h1 class=\"visually-hidden\">")
            .Append(WebUtility.HtmlEncode(culture["Seo.HomeHeading"]))
            .Append("</h1>");

        sb.Append("<div class=\"kb-filter-bar\" role=\"toolbar\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("\" data-testid=\"kb-panel-head-mobile\"><div class=\"kb-filter-bar__row kb-panel-head__search-row\">");
        sb.Append("<div class=\"kb-chrome-search kb-filter-search kb-filter-search--bar\"><span class=\"kb-filter-search__icon\" aria-hidden=\"true\">")
            .Append(NavIcons.Search)
            .Append("</span><label class=\"visually-hidden\" for=\"discovery-keyword-mobile\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Keyword"]))
            .Append("</label><input id=\"discovery-keyword-mobile\" type=\"text\" inputmode=\"search\" autocomplete=\"off\" data-testid=\"kb-keyword-search\" placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.KeywordPlaceholder"]))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(q.SearchQuery ?? ""))
            .Append("\" name=\"q\" form=\"kb-filter-form\" /></div>");
        sb.Append("<button type=\"button\" class=\"kb-filters-button\" data-testid=\"kb-filters-button\" aria-expanded=\"false\" aria-controls=\"discovery-filters\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.FiltersAria"]))
            .Append("\"><span class=\"kb-filters-button__icon\" aria-hidden=\"true\">")
            .Append(NavIcons.Filters)
            .Append("</span><span>")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("</span>");
        if (model.ActiveFilterCount > 0)
        {
            sb.Append("<span class=\"jobsy-action__badge\" data-testid=\"kb-filters-badge\">")
                .Append(model.ActiveFilterCount)
                .Append("</span>");
        }

        sb.Append("</button></div>");

        sb.Append("<div class=\"kb-filter-bar__row kb-panel-head__address-row\">");
        AppendAddressField(sb, culture, "discovery-address", model.AddressLabel, "kb-panel-head__address");
        sb.Append("</div>");
        BanenkaartSsrActiveFilters.Append(sb, culture, model);

        sb.Append("<div class=\"kb-filter-bar__row kb-filter-bar__summary\"><div class=\"kb-filter-bar__count-block\"><p class=\"kb-filter-bar__count\" data-testid=\"kb-mobile-result-count\">")
            .Append(WebUtility.HtmlEncode(model.ResultCountHeading))
            .Append("</p>");
        if (!string.IsNullOrWhiteSpace(model.ResultCountSubtitle))
        {
            sb.Append("<p class=\"kb-filter-bar__count-sub muted\">")
                .Append(WebUtility.HtmlEncode(model.ResultCountSubtitle))
                .Append("</p>");
        }

        sb.Append("</div>");
        if (!model.IsCandidate || model.OpenForWork)
        {
            var matchHref = model.IsAuthenticated ? "/candidate/match" : "/login?returnUrl=%2Fcandidate%2Fmatch";
            sb.Append("<a class=\"kb-match-button kb-match-button--icon jobsy-action--match kb-filter-bar__match\" data-testid=\"kb-match-button\" href=\"")
                .Append(matchHref)
                .Append("\" aria-label=\"")
                .Append(WebUtility.HtmlEncode(culture["Nav.Match"]))
                .Append("\"><span class=\"kb-match-button__icon\" aria-hidden=\"true\">")
                .Append(NavIcons.Match)
                .Append("</span><span class=\"kb-match-button__label\">")
                .Append(WebUtility.HtmlEncode(culture["Nav.Match"]))
                .Append("</span></a>");
        }

        sb.Append("<div class=\"kb-view-toggle kb-view-toggle--mobile\" role=\"radiogroup\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.ToggleAria"]))
            .Append("\" data-testid=\"kb-view-toggle\">");
        sb.Append("<button type=\"button\" class=\"kb-view-toggle__btn is-active\" role=\"radio\" aria-checked=\"true\" data-testid=\"kb-view-kaart\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.Map"]))
            .Append("</button><button type=\"button\" class=\"kb-view-toggle__btn\" role=\"radio\" aria-checked=\"false\" data-testid=\"kb-view-lijst\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.List"]))
            .Append("</button></div></div></div>");

        sb.Append("<div class=\"kb-panel-head discovery-toolbar--desktop\" role=\"toolbar\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("\" data-testid=\"kb-panel-head\">");
        AppendAddressField(sb, culture, "discovery-address-desktop", model.AddressLabel, "kb-panel-head__address", labeled: true);
        sb.Append("<div class=\"kb-panel-head__filters-row\">");
        sb.Append("<div class=\"kb-chrome-search kb-filter-search\"><span class=\"kb-filter-search__icon\" aria-hidden=\"true\">")
            .Append(NavIcons.Search)
            .Append("</span><input id=\"discovery-keyword-desktop\" type=\"search\" data-testid=\"kb-keyword-search\" placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.KeywordPlaceholder"]))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(q.SearchQuery ?? ""))
            .Append("\" name=\"q\" form=\"kb-filter-form\" /></div>");
        sb.Append("<button type=\"button\" class=\"kb-filters-button kb-panel-head__filters\" data-testid=\"kb-filters-button\" aria-expanded=\"false\" aria-controls=\"discovery-filters\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("</button>");
        if (!model.IsCandidate || model.OpenForWork)
        {
            var matchHref = model.IsAuthenticated ? "/candidate/match" : "/login?returnUrl=%2Fcandidate%2Fmatch";
            sb.Append("<a class=\"kb-match-button kb-match-button--icon jobsy-action--match\" data-testid=\"kb-match-button\" href=\"")
                .Append(matchHref)
                .Append("\"><span class=\"kb-match-button__icon\" aria-hidden=\"true\">")
                .Append(NavIcons.Match)
                .Append("</span><span class=\"kb-match-button__label\">")
                .Append(WebUtility.HtmlEncode(culture["Nav.Match"]))
                .Append("</span></a>");
        }

        sb.Append("</div>");
        BanenkaartSsrActiveFilters.Append(sb, culture, model);
        sb.Append("</div></div>");
    }

    private static void AppendAddressField(
        StringBuilder sb,
        CultureState culture,
        string inputId,
        string? value,
        string? cssClass = null,
        bool labeled = false)
    {
        var hasOrigin = !string.IsNullOrWhiteSpace(value);
        sb.Append("<div class=\"kb-address");
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            sb.Append(' ').Append(cssClass);
        }

        if (hasOrigin)
        {
            sb.Append(" kb-address--with-clear");
        }

        sb.Append("\">");
        if (labeled)
        {
            sb.Append("<label class=\"kb-address__label\" for=\"")
                .Append(inputId)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(culture["Discovery.Address"]))
                .Append("</label>");
        }

        sb.Append("<div class=\"kb-address__field\"><input id=\"")
            .Append(inputId)
            .Append("\" type=\"text\" role=\"combobox\" autocomplete=\"off\" aria-autocomplete=\"list\" data-testid=\"kb-address-input\" placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.AddressPlaceholder"]))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(value ?? ""))
            .Append("\" data-kb-address=\"1\" />");
        sb.Append("<button type=\"button\" class=\"kb-address__geo\" data-testid=\"kb-address-locate\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.UseMyLocation"]))
            .Append("\"><span class=\"kb-address__geo-icon\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"><circle cx=\"12\" cy=\"12\" r=\"3\"/><circle cx=\"12\" cy=\"12\" r=\"7\"/><path d=\"M12 3v2.5M12 18.5V21M3 12h2.5M18.5 12H21\"/></svg></span></button>");
        if (hasOrigin)
        {
            sb.Append("<button type=\"button\" class=\"kb-address__clear\" data-testid=\"kb-address-clear\" aria-label=\"")
                .Append(WebUtility.HtmlEncode(culture["Common.Clear"]))
                .Append("\"><svg class=\"kb-address__clear-icon\" viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M18 6 6 18\" /><path d=\"m6 6 12 12\" /></svg></button>");
        }

        sb.Append("</div><ul class=\"address-suggestions\" role=\"listbox\" data-kb-address-suggest=\"1\" hidden></ul></div>");
    }

    private static void AppendNearbyHeroStrip(StringBuilder sb, CultureState culture)
    {
        sb.Append("<header class=\"kb-nearby-banner kb-nearby-banner--strip kb-nearby-banner--strip-wide kb-banenkaart-header-strip\" data-testid=\"kb-nearby-banner\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.NearbyTitle"]))
            .Append("\">");
        sb.Append("<div class=\"kb-banenkaart-header-strip__inner\" aria-hidden=\"true\">");
        AppendHeroStripPicture(sb);
        sb.Append("</div></header>");
    }

    private static void AppendHeroStripPicture(StringBuilder sb)
    {
        sb.Append("<picture>");
        sb.Append("<source media=\"(max-width: 600px)\" type=\"image/webp\" srcset=\"")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripMobileWebp))
            .Append(" 390w, ")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripMobileWebp2x))
            .Append(" 780w\" />");
        sb.Append("<source media=\"(min-width: 601px)\" type=\"image/webp\" srcset=\"")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripDesktopWebp))
            .Append(" 1440w, ")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripDesktopWebp2x))
            .Append(" 2880w\" />");
        sb.Append("<source media=\"(max-width: 600px)\" srcset=\"")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripMobileJpg))
            .Append(" 390w, ")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripMobileJpg2x))
            .Append(" 780w\" />");
        sb.Append("<img class=\"kb-banenkaart-header-strip__img\" src=\"")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripDesktopJpg))
            .Append("\" srcset=\"")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripDesktopJpg))
            .Append(" 1440w, ")
            .Append(WebUtility.HtmlEncode(CandidateIllustratedImages.BanenkaartHeroStripDesktopJpg2x))
            .Append(" 2880w\" sizes=\"100vw\" alt=\"\" width=\"1440\" height=\"150\" loading=\"lazy\" decoding=\"async\" fetchpriority=\"low\" />");
        sb.Append("</picture>");
    }

    private static void AppendVacancyPane(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        sb.Append("<aside class=\"vacancy-pane\"><div class=\"results-meta results-meta--ssr\" data-testid=\"kb-results-meta\">");
        sb.Append("<div class=\"results-meta__text\"><p class=\"results-meta__count\" data-testid=\"kb-desktop-result-count\">")
            .Append(WebUtility.HtmlEncode(model.ResultCountHeading))
            .Append("</p>");
        if (!string.IsNullOrWhiteSpace(model.ResultCountSubtitle))
        {
            sb.Append("<p class=\"results-meta__sub muted\">")
                .Append(WebUtility.HtmlEncode(model.ResultCountSubtitle))
                .Append("</p>");
        }

        sb.Append("</div><div class=\"results-meta__actions\">");
        sb.Append("<label class=\"results-meta__sort\"><span class=\"visually-hidden\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Sort"]))
            .Append("</span><select class=\"results-meta__sort-select\" data-testid=\"kb-list-sort\" form=\"kb-filter-form\" name=\"sort\">");
        var sort = model.Query.SortMode;
        if (model.Query.OriginLat is not null)
        {
            sb.Append("<option value=\"distance\"")
                .Append(string.Equals(sort, "distance", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                .Append('>')
                .Append(WebUtility.HtmlEncode(culture["Kb.List.SortNear"]))
                .Append("</option>");
        }

        sb.Append("<option value=\"title\"")
            .Append(string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(sort) ? " selected" : "")
            .Append('>')
            .Append(WebUtility.HtmlEncode(culture["Discovery.SortAlphaAsc"]))
            .Append("</option></select></label>");
        sb.Append("<div class=\"kb-view-toggle kb-view-toggle--desktop\" role=\"radiogroup\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.ToggleAria"]))
            .Append("\" data-testid=\"kb-view-toggle-desktop\">");
        sb.Append("<button type=\"button\" class=\"kb-view-toggle__btn")
            .Append(model.Query.DesktopListMode ? "" : " is-active")
            .Append("\" role=\"radio\" aria-checked=\"")
            .Append(model.Query.DesktopListMode ? "false" : "true")
            .Append("\" data-testid=\"kb-view-kaart\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.Map"]))
            .Append("</button><button type=\"button\" class=\"kb-view-toggle__btn")
            .Append(model.Query.DesktopListMode ? " is-active" : "")
            .Append("\" role=\"radio\" aria-checked=\"")
            .Append(model.Query.DesktopListMode ? "true" : "false")
            .Append("\" data-testid=\"kb-view-lijst\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.List"]))
            .Append("</button></div></div></div>");
        sb.Append("<div class=\"vacancy-list\" data-testid=\"vacancy-list\">");
        if (model.ShowEmptyTravelMessage)
        {
            sb.Append("<p class=\"state-message\" data-testid=\"kb-empty-travel\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Map.NoVacanciesInTravel"]))
                .Append("</p>");
        }

        if (model.Vacancies.Count == 0 && !model.ShowEmptyTravelMessage)
        {
            sb.Append("<p class=\"state-message\">")
                .Append(WebUtility.HtmlEncode(culture.Format("Kb.Filter.ResultCount", 0)))
                .Append("</p>");
        }
        else
        {
            BanenkaartSsrListHtml.AppendListRows(sb, culture, model.Query, model.Vacancies, compact: true);
        }

        if (model.HasMoreList)
        {
            sb.Append("<button type=\"button\" class=\"vacancy-list__more\" data-testid=\"kb-load-more\">")
                .Append(WebUtility.HtmlEncode("Meer laden"))
                .Append("</button>");
        }

        sb.Append("</div></aside>");
    }

    private static void AppendMapPane(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        sb.Append("<section class=\"map-pane\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Page.JobMapTitle"]))
            .Append("\">");
        if (model.ShowStartPrompt)
        {
            sb.Append("<div class=\"kb-start-prompt\" role=\"dialog\" aria-labelledby=\"kb-start-title\"><h2 id=\"kb-start-title\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Start.Title"]))
                .Append("</h2><p class=\"kb-start-prompt__hint\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Start.Hint"]))
                .Append("</p>");
            AppendAddressField(sb, culture, "discovery-address-prompt", null);
            sb.Append("<div class=\"kb-start-prompt__actions\"><button type=\"button\" class=\"kb-start-prompt__primary\" data-testid=\"kb-start-locate\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Start.UseMyLocation"]))
                .Append("</button><button type=\"button\" class=\"kb-start-prompt__later\" data-testid=\"kb-start-later\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Start.Later"]))
                .Append("</button></div></div>");
        }

        sb.Append("<div class=\"map-pane__top-chrome\" data-testid=\"kb-map-top-chrome\"></div>");
        sb.Append("<div class=\"map-stage is-live\"><div id=\"jobsy-map-boot\" hidden>")
            .Append(model.MapBootJson)
            .Append("</div><div class=\"job-map-placeholder\" aria-hidden=\"true\"></div><div id=\"job-map\" class=\"job-map\" data-testid=\"job-map\"></div></div></section>");
    }

    private static void AppendFilterSheet(StringBuilder sb, CultureState culture, string token, BanenkaartSsrData.PageModel model)
    {
        var q = model.Query;
        sb.Append("<div class=\"filter-sheet-backdrop kb-filter-drawer-backdrop\" data-testid=\"kb-filter-backdrop\" aria-hidden=\"true\"></div>");
        sb.Append("<div id=\"discovery-filters\" class=\"filter-sheet kb-filter-drawer\" role=\"dialog\" aria-modal=\"false\" aria-hidden=\"true\" aria-labelledby=\"filter-sheet-title\" data-testid=\"kb-filter-sheet\">");
        sb.Append("<div class=\"filter-sheet__header\"><h2 id=\"filter-sheet-title\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("</h2><button type=\"button\" class=\"filter-sheet__close\" data-testid=\"kb-filter-close\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Common.Close"]))
            .Append("\"><span aria-hidden=\"true\">")
            .Append(NavIcons.Close)
            .Append("</span></button></div>");

        sb.Append("<form id=\"kb-filter-form\" method=\"post\" action=\"")
            .Append(BanenkaartSsrQuery.ApplyPath)
            .Append("\" class=\"filter-sheet__body\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"")
            .Append(WebUtility.HtmlEncode(token))
            .Append("\" />");

        sb.Append("<section class=\"filter-section\" data-filter-section=\"travel\"><h3>")
            .Append(WebUtility.HtmlEncode(culture["Discovery.TravelSlider"]))
            .Append("</h3><div class=\"filter-sheet__presets\" role=\"group\">");
        foreach (var m in new[] { 10, 20, 30, 45, 60 })
        {
            var presetSelected = m == q.MaxTravelMinutes ? " is-selected" : "";
            sb.Append("<button type=\"button\" class=\"filter-sheet__chip")
                .Append(presetSelected)
                .Append("\" data-kb-travel-preset=\"")
                .Append(m)
                .Append("\" data-testid=\"kb-sheet-travel-")
                .Append(m)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(culture.Format("Kb.Filter.MinutesPreset", m)))
                .Append("</button>");
        }

        sb.Append("</div><input type=\"range\" min=\"5\" max=\"90\" step=\"5\" name=\"maxMinutes\" data-testid=\"kb-sheet-travel-slider\" value=\"")
            .Append(q.MaxTravelMinutes)
            .Append("\" /><div class=\"filter-sheet__modes\" role=\"group\">");
        foreach (var mode in new[] { "Lopend", "Fiets", "Auto", "OV" })
        {
            var selected = string.Equals(q.Transport, mode, StringComparison.Ordinal) ? " is-selected" : "";
            sb.Append("<button type=\"button\" class=\"filter-sheet__mode")
                .Append(selected)
                .Append("\" data-kb-transport=\"")
                .Append(WebUtility.HtmlEncode(mode))
                .Append("\" data-testid=\"kb-sheet-transport-")
                .Append(mode)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(UiLabels.Transport(culture, mode)))
                .Append("</button>");
        }

        sb.Append("</div><input type=\"hidden\" name=\"transport\" id=\"kb-filter-transport\" value=\"")
            .Append(WebUtility.HtmlEncode(q.Transport))
            .Append("\" /></section>");

        sb.Append("<section class=\"filter-section\" data-filter-section=\"hours\"><h3>")
            .Append(WebUtility.HtmlEncode(culture["Discovery.HoursSlider"]))
            .Append(" <span class=\"filter-section__value\">")
            .Append(WebUtility.HtmlEncode(culture.Format("Kb.Filter.HoursValue", q.MinHoursPerWeek, q.MaxHoursPerWeek)))
            .Append("</span></h3><label>min <input type=\"number\" name=\"minHours\" min=\"0\" max=\"40\" value=\"")
            .Append(q.MinHoursPerWeek)
            .Append("\" /></label><label>max <input type=\"number\" name=\"maxHours\" min=\"0\" max=\"40\" value=\"")
            .Append(q.MaxHoursPerWeek)
            .Append("\" /></label></section>");

        if (model.Categories.Any(c => c.ShowInMapFilter))
        {
            sb.Append("<section class=\"filter-section\" data-filter-section=\"category\"><h3>")
                .Append(WebUtility.HtmlEncode(culture["Kb.Filter.WorkType"]))
                .Append("</h3><div class=\"filter-sheet__chips\">");
            foreach (var cat in model.Categories.Where(c => c.ShowInMapFilter && c.IsActive))
            {
                var selected = q.CategoryIds.Contains(cat.Id);
                sb.Append("<label class=\"filter-sheet__chip\"><input type=\"checkbox\" name=\"categoryId\" value=\"")
                    .Append(cat.Id.ToString("D"))
                    .Append('"');
                if (selected)
                {
                    sb.Append(" checked");
                }

                sb.Append(" data-testid=\"kb-sheet-category-")
                    .Append(WebUtility.HtmlEncode(cat.Slug))
                    .Append("\" /> ")
                    .Append(WebUtility.HtmlEncode(cat.Name))
                    .Append("</label>");
            }

            sb.Append("</div></section>");
        }

        sb.Append("<section class=\"filter-section\" data-filter-section=\"branch\"><h3>")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.Category"]))
            .Append("</h3>");
        foreach (var label in model.BranchOptions)
        {
            var selected = q.WorkTypes.Contains(label, StringComparer.OrdinalIgnoreCase);
            sb.Append("<label class=\"filter-check\"><input type=\"checkbox\" name=\"workType\" value=\"")
                .Append(WebUtility.HtmlEncode(label))
                .Append('"');
            if (selected)
            {
                sb.Append(" checked");
            }

            sb.Append(" /> ")
                .Append(WebUtility.HtmlEncode(UiLabels.WorkType(culture, label)))
                .Append("</label>");
        }

        sb.Append("</section><input type=\"hidden\" name=\"originLat\" id=\"kb-filter-origin-lat\" value=\"")
            .Append(q.OriginLat?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "")
            .Append("\" /><input type=\"hidden\" name=\"originLng\" id=\"kb-filter-origin-lng\" value=\"")
            .Append(q.OriginLng?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "")
            .Append("\" /><input type=\"hidden\" name=\"originLabel\" id=\"kb-filter-origin-label\" value=\"")
            .Append(WebUtility.HtmlEncode(q.OriginLabel ?? ""))
            .Append("\" /></form>");

        sb.Append("<div class=\"filter-sheet__footer\"><form method=\"post\" action=\"")
            .Append(BanenkaartSsrQuery.ResetPath)
            .Append("\"><input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"")
            .Append(WebUtility.HtmlEncode(token))
            .Append("\" /><button type=\"submit\" class=\"filter-sheet__cancel\" data-testid=\"kb-filter-cancel\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.Wissen"]))
            .Append("</button></form><button type=\"submit\" class=\"filter-sheet__apply\" data-testid=\"kb-filter-apply\" form=\"kb-filter-form\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Filter.Show"]));
        sb.Append("</button></div></div>");
    }

    private static void AppendMapFab(StringBuilder sb, CultureState culture)
    {
        sb.Append("<button type=\"button\" class=\"kb-map-fab\" data-testid=\"kb-map-fab\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.OpenMap"]))
            .Append("\">")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.OpenMap"]))
            .Append("</button>");
    }
}
