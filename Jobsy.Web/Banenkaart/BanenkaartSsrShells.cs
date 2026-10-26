using System.Net;
using System.Text;
using Jobsy.Web.Hosting;
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
        BanenkaartSsrData.PageModel model,
        string? scriptNonce = null,
        string? candidateDisplayName = null)
    {
        var sb = new StringBuilder();
        var q = model.Query;
        var viewModeClass = model.Query.DesktopListMode ? " show-list jobsy-discovery--list" : " show-map";
        var viewDataAttr = model.Query.DesktopListMode ? " data-kb-view=\"list\"" : " data-kb-view=\"map\"";
        sb.Append("<div class=\"kb-banenkaart-ssr-chrome\" data-testid=\"kb-banenkaart-ssr-chrome\">");
        AppendNearbyHeroStrip(sb);
        sb.Append("</div>");
        sb.Append("<div class=\"jobsy-discovery jobsy-discovery--lagen").Append(viewModeClass).Append(viewDataAttr);
        if (model.IsAuthenticated)
        {
            sb.Append(" data-kb-strip-external=\"1\"");
        }
        if (!string.IsNullOrWhiteSpace(q.Error))
        {
            sb.Append(" kb-ssr-has-error");
        }

        if (model.ShowEmptyTravelMessage)
        {
            sb.Append(" kb-ssr-travel-empty");
        }

        sb.Append("\" data-banenkaart-ssr=\"1\" data-testid=\"banenkaart-ssr-root\">");
        if (!string.IsNullOrWhiteSpace(antiforgeryToken))
        {
            sb.Append("<meta name=\"jobsy-request-verification-token\" content=\"")
                .Append(WebUtility.HtmlEncode(antiforgeryToken))
                .Append("\" />");
        }

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

        if (!model.IsAuthenticated)
        {
            AppendMockupTopBar(sb, culture, model, candidateDisplayName);
        }
        sb.Append("<div class=\"main kb-lagen-main\">");
        AppendVacancyPane(sb, culture, model);
        AppendMapPane(sb, culture, model);
        sb.Append("</div>");
        AppendFilterSheet(sb, culture, antiforgeryToken, model);
        AppendMapFab(sb, culture);

        var nonceAttr = string.IsNullOrWhiteSpace(scriptNonce)
            ? ""
            : " nonce=\"" + WebUtility.HtmlEncode(scriptNonce.Trim()) + "\"";
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/jobsy-geo-lite.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/jobsy-basemap-warm.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/banenkaart-map-icons.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/banenkaart-catalog.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/banenkaart-map-layers.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("<script src=\"").Append(WwwrootAssetCatalog.UrlLeadingSlash("js/banenkaart-ssr.js")).Append("\" crossorigin=\"anonymous\" defer")
            .Append(nonceAttr)
            .Append("></script>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendMockupPanelHead(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        var q = model.Query;
        sb.Append("<div class=\"ph kb-panel-head discovery-toolbar--desktop\" role=\"toolbar\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]))
            .Append("\" data-testid=\"kb-panel-head\"><div class=\"srow mobfirst kb-panel-head__filters-row\">");
        sb.Append("<div class=\"field grow\">")
            .Append(NavIcons.Search.Replace("viewBox", "class=\"ic\" viewBox", StringComparison.Ordinal))
            .Append("<label class=\"visually-hidden\" for=\"discovery-keyword\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Keyword"]))
            .Append("</label><input id=\"discovery-keyword\" type=\"search\" inputmode=\"search\" autocomplete=\"off\" data-testid=\"kb-keyword-search\" placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.Filter.KeywordPlaceholder"]))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(q.SearchQuery ?? ""))
            .Append("\" name=\"q\" form=\"kb-filter-form\" /></div>");
        sb.Append("<button type=\"button\" class=\"btn sec kb-filters-button\" data-testid=\"kb-filters-button\" aria-expanded=\"false\" aria-controls=\"discovery-filters\">")
            .Append(NavIcons.Filters.Replace("viewBox", "class=\"ic\" viewBox", StringComparison.Ordinal))
            .Append(WebUtility.HtmlEncode(culture["Discovery.Filters"]));
        if (model.ActiveFilterCount > 0)
        {
            sb.Append("<span class=\"jobsy-action__badge\" data-testid=\"kb-filters-badge\">")
                .Append(model.ActiveFilterCount)
                .Append("</span>");
        }

        sb.Append("</button>");
        if (!model.IsCandidate || model.OpenForWork)
        {
            var matchHref = model.IsAuthenticated ? "/candidate/match" : "/login?returnUrl=%2Fcandidate%2Fmatch";
            sb.Append("<a class=\"btn match deskonly kb-match-button\" data-testid=\"kb-match-button\" href=\"")
                .Append(matchHref)
                .Append("\">")
                .Append(NavIcons.Match.Replace("viewBox", "class=\"ic\" viewBox", StringComparison.Ordinal))
                .Append("<span class=\"kb-match-button__label\">")
                .Append(WebUtility.HtmlEncode(culture["Nav.Match"]))
                .Append("</span></a>");
        }

        sb.Append("</div>");
        AppendAddressField(sb, culture, "discovery-address", model.AddressLabel, "addr", labeled: true);
        BanenkaartSsrActiveFilters.Append(sb, culture, model);
        sb.Append("</div>");
    }

    private static void AppendMockupResultsRow(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        sb.Append("<div class=\"rh results-meta results-meta--ssr\" data-testid=\"kb-results-meta\"><div><b class=\"mobonly\" data-testid=\"kb-mobile-result-count\">")
            .Append(WebUtility.HtmlEncode(model.ResultCountHeading))
            .Append("</b><b class=\"deskonly results-meta__count\" data-testid=\"kb-desktop-result-count\">")
            .Append(WebUtility.HtmlEncode(model.ResultCountHeading))
            .Append("</b>");
        if (!string.IsNullOrWhiteSpace(model.ResultCountSubtitle))
        {
            sb.Append("<small class=\"results-meta__sub muted\">")
                .Append(WebUtility.HtmlEncode(model.ResultCountSubtitle))
                .Append("</small>");
        }

        sb.Append("</div><div class=\"srow\">");
        sb.Append("<label class=\"sel deskonly results-meta__sort\"><span class=\"visually-hidden\">")
            .Append(WebUtility.HtmlEncode(culture["Discovery.Sort"]))
            .Append("</span><select class=\"results-meta__sort-select\" data-testid=\"kb-list-sort\" form=\"kb-filter-form\" name=\"sort\">");
        var sort = model.Query.SortMode;
        if (model.ShowFitFilters)
        {
            sb.Append("<option value=\"match\"")
                .Append(string.Equals(sort, "match", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                .Append('>')
                .Append(WebUtility.HtmlEncode(culture["Discovery.SortMatch"]))
                .Append("</option>");
        }

        if (model.Query.OriginLat is not null)
        {
            sb.Append("<option value=\"distance\"")
                .Append(string.Equals(sort, "distance", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                .Append('>')
                .Append(WebUtility.HtmlEncode(culture["Kb.List.SortNear"]))
                .Append("</option>");
        }

        sb.Append("<option value=\"title\"")
            .Append(string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
            .Append('>')
            .Append(WebUtility.HtmlEncode(culture["Discovery.SortAlphaAsc"]))
            .Append("</option></select></label>");
        if (!model.IsCandidate || model.OpenForWork)
        {
            var matchHref = model.IsAuthenticated ? "/candidate/match" : "/login?returnUrl=%2Fcandidate%2Fmatch";
            sb.Append("<a class=\"btn match mobonly\" data-testid=\"kb-match-button\" href=\"")
                .Append(matchHref)
                .Append("\" aria-label=\"")
                .Append(WebUtility.HtmlEncode(culture["Nav.Match"]))
                .Append("\">")
                .Append(NavIcons.Match.Replace("viewBox", "class=\"ic\" viewBox", StringComparison.Ordinal))
                .Append("</a>");
        }

        sb.Append("<div class=\"seg kb-view-toggle\" role=\"radiogroup\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Kb.View.ToggleAria"]))
            .Append("\" data-testid=\"kb-view-toggle\" data-kb-view-toggle-desktop=\"1\">");
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
        sb.Append("<div class=\"addr kb-panel-head__address kb-address");
        if (!string.IsNullOrWhiteSpace(cssClass) && cssClass != "addr")
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
            sb.Append("<label for=\"")
                .Append(inputId)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(culture["Discovery.Address"]))
                .Append("</label>");
        }

        sb.Append("<div class=\"srow\"><div class=\"field grow");
        if (hasOrigin)
        {
            sb.Append(" val");
        }

        sb.Append(" kb-address__field\"><input id=\"")
            .Append(inputId)
            .Append("\" type=\"text\" role=\"combobox\" autocomplete=\"off\" aria-autocomplete=\"list\" data-testid=\"kb-address-input\" placeholder=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.AddressPlaceholder"]))
            .Append("\" value=\"")
            .Append(WebUtility.HtmlEncode(value ?? ""))
            .Append("\" data-kb-address=\"1\" /></div>");
        sb.Append("<button type=\"button\" class=\"sq kb-address__geo\" data-testid=\"kb-address-locate\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Discovery.UseMyLocation"]))
            .Append("\">")
            .Append("<svg class=\"ic\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3\"/><circle cx=\"12\" cy=\"12\" r=\"7.5\"/></svg></button>");
        if (hasOrigin)
        {
            sb.Append("<button type=\"button\" class=\"sq deskonly kb-address__clear\" data-testid=\"kb-address-clear\" aria-label=\"")
                .Append(WebUtility.HtmlEncode(culture["Common.Clear"]))
                .Append("\"><svg class=\"ic\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6 6 18\" /></svg></button>");
        }

        sb.Append("</div><ul class=\"address-suggestions\" role=\"listbox\" data-kb-address-suggest=\"1\" hidden></ul></div>");
    }

    private static void AppendMockupTopBar(
        StringBuilder sb,
        CultureState culture,
        BanenkaartSsrData.PageModel model,
        string? candidateDisplayName)
    {
        var tagline = culture["Brand.Tagline"];
        var userLabel = !string.IsNullOrWhiteSpace(candidateDisplayName)
            ? candidateDisplayName.Trim()
            : culture["Auth.Login"];
        var notificationsHref = model.IsAuthenticated ? "/candidate/notifications" : "/login?returnUrl=%2Fbanenkaart";
        var accountHref = model.IsAuthenticated ? "/candidate/profile" : "/login?returnUrl=%2Fbanenkaart";

        sb.Append("<header class=\"top kb-mockup-top\" data-testid=\"kb-mockup-top\">");
        sb.Append("<a class=\"brand\" href=\"/\" aria-label=\"Lobsy\">");
        sb.Append("<span class=\"lobsy-logo lobsy-logo--horizontal brand__logo kb-mockup-top__logo\" role=\"img\" aria-label=\"Lobsy\">");
        sb.Append("<img class=\"lobsy-logo__img\" src=\"")
            .Append(WebUtility.HtmlEncode(BrandImages.HorizontalSvg))
            .Append("\" alt=\"\" width=\"154\" height=\"44\" decoding=\"async\" data-logo-fallback=\"1\" draggable=\"false\" />");
        sb.Append("</span><small class=\"kb-mockup-top__tagline\">")
            .Append(WebUtility.HtmlEncode(tagline)).Append("</small></a>");
        sb.Append("<div class=\"tr\">");
        sb.Append("<a class=\"round\" href=\"").Append(WebUtility.HtmlEncode(notificationsHref))
            .Append("\" aria-label=\"").Append(WebUtility.HtmlEncode(culture["Nav.Notifications"])).Append("\">");
        sb.Append("<svg class=\"ic\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M6 16V11a6 6 0 0 1 12 0v5l1.5 2h-15zM10 20.5h4\"/></svg></a>");
        sb.Append("<span class=\"ctl lang\" lang=\"nl\"><i class=\"flag\" aria-hidden=\"true\"></i>NL");
        sb.Append("<svg class=\"ic s\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M7 10l5 5 5-5\"/></svg></span>");
        sb.Append("<a class=\"user\" href=\"").Append(WebUtility.HtmlEncode(accountHref)).Append("\">");
        sb.Append("<span class=\"un\">").Append(WebUtility.HtmlEncode(userLabel)).Append("</span>");
        sb.Append("<span class=\"round\" aria-hidden=\"true\">");
        sb.Append("<svg class=\"ic\" viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"8.5\" r=\"3.5\"/><path d=\"M5 20c1-4 4-6 7-6s6 2 7 6\"/></svg>");
        sb.Append("</span></a></div></header>");
    }

    private static void AppendNearbyHeroStrip(StringBuilder sb)
    {
        KreeftHeaderStripHtml.Append(
            sb,
            KreeftHeaderStripAssets.BanenkaartDesk,
            KreeftHeaderStripAssets.BanenkaartMob,
            "banenkaart",
            "kb-banenkaart-header-strip kreeft-header-strip");
    }

    private static void AppendVacancyPane(StringBuilder sb, CultureState culture, BanenkaartSsrData.PageModel model)
    {
        sb.Append("<aside class=\"vacancy-pane panel\">");
        AppendMockupPanelHead(sb, culture, model);
        AppendMockupResultsRow(sb, culture, model);
        sb.Append("<div class=\"list vacancy-list\" data-testid=\"vacancy-list\">");
        if (model.ShowEmptyTravelMessage)
        {
            sb.Append("<p class=\"state-message\" data-testid=\"kb-empty-travel\">")
                .Append(WebUtility.HtmlEncode(culture["Kb.Map.NoVacanciesInTravel"]))
                .Append("</p><button type=\"button\" class=\"filter-sheet__cancel kb-widen-filters-btn\" data-testid=\"kb-widen-filters\">")
                .Append(WebUtility.HtmlEncode("Filters verruimen"))
                .Append("</button>");
        }

        if (model.Vacancies.Count == 0 && !model.ShowEmptyTravelMessage)
        {
            sb.Append("<p class=\"state-message\">")
                .Append(WebUtility.HtmlEncode(culture.Format("Kb.Filter.ResultCount", 0)))
                .Append("</p>");
        }
        else
        {
            BanenkaartSsrListHtml.AppendLagenMockupListRows(sb, culture, model.Query, model.Vacancies);
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
        sb.Append("<section class=\"map-pane mapw\" aria-label=\"")
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
            .Append("</div><div class=\"job-map-placeholder\" aria-hidden=\"true\"></div><div id=\"job-map\" class=\"job-map\" data-testid=\"job-map\"></div>");
        sb.Append("<div class=\"kb-map-legend-host\" data-testid=\"kb-map-legend-root\"></div>");
        sb.Append("<button type=\"button\" class=\"legbtn mobonly kb-map-legbtn\" data-testid=\"kb-map-legbtn\" aria-expanded=\"false\">");
        sb.Append("<b aria-hidden=\"true\">?</b>Uitleg kaart</button>");
        sb.Append("<div class=\"scrim mobonly kb-map-legend-scrim\" data-testid=\"kb-map-legend-scrim\" hidden aria-hidden=\"true\"></div>");
        sb.Append("<div class=\"sheet mobonly kb-map-legend-sheet\" data-testid=\"kb-map-legend-sheet\" role=\"dialog\" aria-labelledby=\"kb-legend-sheet-title\" hidden aria-hidden=\"true\"></div>");
        sb.Append("</div></section>");
    }

    private static void AppendFilterSheet(StringBuilder sb, CultureState culture, string token, BanenkaartSsrData.PageModel model)
    {
        var q = model.Query;
        sb.Append("<div class=\"filter-sheet-backdrop kb-filter-drawer-backdrop\" data-testid=\"kb-filter-backdrop\" aria-hidden=\"true\"></div>");
        sb.Append("<div id=\"discovery-filters\" class=\"filter-sheet kb-filter-drawer\" role=\"dialog\" aria-modal=\"false\" aria-hidden=\"true\" inert aria-labelledby=\"filter-sheet-title\" data-testid=\"kb-filter-sheet\">");
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

        if (model.ShowFitFilters || q.OriginLat is not null)
        {
            var sort = model.Query.SortMode;
            sb.Append("<section class=\"filter-section\" data-filter-section=\"sort\"><h3>")
                .Append(WebUtility.HtmlEncode(culture["Discovery.Sort"]))
                .Append("</h3><label class=\"sel results-meta__sort\"><span class=\"visually-hidden\">")
                .Append(WebUtility.HtmlEncode(culture["Discovery.Sort"]))
                .Append("</span><select class=\"results-meta__sort-select\" data-testid=\"kb-sheet-sort\" name=\"sort\">");
            if (model.ShowFitFilters)
            {
                sb.Append("<option value=\"match\"")
                    .Append(string.Equals(sort, "match", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                    .Append('>')
                    .Append(WebUtility.HtmlEncode(culture["Discovery.SortMatch"]))
                    .Append("</option>");
            }

            if (q.OriginLat is not null)
            {
                sb.Append("<option value=\"distance\"")
                    .Append(string.Equals(sort, "distance", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                    .Append('>')
                    .Append(WebUtility.HtmlEncode(culture["Kb.List.SortNear"]))
                    .Append("</option>");
            }

            sb.Append("<option value=\"title\"")
                .Append(string.Equals(sort, "title", StringComparison.OrdinalIgnoreCase) ? " selected" : "")
                .Append('>')
                .Append(WebUtility.HtmlEncode(culture["Discovery.SortAlphaAsc"]))
                .Append("</option></select></label></section>");
        }

        sb.Append("<section class=\"filter-section\" data-filter-section=\"travel\"><h3>")
            .Append(WebUtility.HtmlEncode(culture["Discovery.TravelSlider"]))
            .Append("</h3><div class=\"filter-sheet__presets\" role=\"group\">");
        foreach (var m in new[] { 10, 20, 30, 45, 60 })
        {
            var presetSelected = q.MaxTravelMinutes is int sel && m == sel ? " is-selected" : "";
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
            .Append(q.MaxTravelMinutes ?? KbMapStart.DefaultMaxTravelMinutes)
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
