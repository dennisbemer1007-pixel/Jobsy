window.jobMap = (function () {
    let map = null;
    let clusterGroup = null;
    let markersById = {};
    /** @type {Object.<string, Array>} lat,lng → marker records (6-decimal coordKey). */
    let markersByCoordKey = {};
    let lastClusterTapAt = 0;
    let originMarker = null;
    let travelRingLayers = [];
    let travelRingGeo = null;
    let travelOptions = { maxMinutes: 20, transport: "Fiets", radiusKm: null };
    let activeClusterPopup = null;
    let openCallback = null;
    let outsideClickCloserBound = false;
    let highlightSeed = 0;
    let firstSizedFit = false;
    let lastFitPoints = [];
    let tileLayer = null;
    let renderedMarkers = [];
    let lastOrigin = null;
    let selectedId = null;
    let zoomHandlerBound = false;

    let cameraLocked = false;
    let originHasBeenFramed = false;
    let banenkaartMapRestoreAttempted = false;
    const BK_MAP_VIEW_KEY = "jobsy.bk.mapView.v1";
    let originNeedsFrame = false;
    let ringRedrawBound = false;
    let ringStyleHandlerBound = false;
    let ringRedrawTries = 0;
    let deferMapReveal = false;
    let kkMode = false;
    let companyMapMode = false;
    const COMPANY_OPEN_PIN_COLOR = "#E87722";
    let kkSelectCallback = null;
    const KK_SOURCE = "jobsy-kk-areas";
    const KK_FILL = "jobsy-kk-areas-fill";
    const KK_LINE = "jobsy-kk-areas-line";
    const KK_LABELS_SOURCE = "jobsy-kk-areas-labels";
    const KK_LABELS = "jobsy-kk-areas-labels-layer";
    const KK_LABELS_HALO = "jobsy-kk-areas-labels-halo";
    const KK_RADIUS_SOURCE = "jobsy-kk-radius";
    const KK_RADIUS_FILL = "jobsy-kk-radius-fill";
    const KK_RADIUS_LINE = "jobsy-kk-radius-line";
    let kkViewOptions = null;
    let kkInitialFitDone = false;
    let detailCache = {};
    let pinsUrl = null;
    let pinsBaseUrl = null;
    let illustratedPopup = false;
    let illustratedTravelSpot = "";
    let illustratedVacancySpots = {};
    let pinsFetchGen = 0;
    let viewportPinsBound = false;
    let catalogModeActive = false;
    let lagenLayout = false;
    let catalogMapReadySent = false;
    let bootZoomOutsideTravel = false;
    let pendingCatalogPins = null;
    let pendingCatalogMeta = null;
    let viewportListDotNet = null;
    /** Suppress moveend-driven pin/discover churn during programmatic camera moves / resize. */
    let viewportNotifySuppressed = 0;
    let mapResizePausedUntil = 0;
    let viewportBootstrapDone = false;
    let lastUserGestureAt = 0;
    let userGesturePending = false;
    let setVacanciesCallCount = 0;
    let viewportNotifyCount = 0;
    let travelRingDrawCount = 0;
    const ORIGIN_MOVE_MIN_METERS = 500;
    /** Localized map UI strings from VacancyDiscovery (nl fallbacks). */
    let uiLabels = {
        vacanciesAtPlace: "{count} banen",
        vacancyAtPlace: "1 baan",
        vacanciesAtPlaceWithPlace: "{count} banen in {place}",
        vacancyAtPlaceWithPlace: "1 baan in {place}",
        vacanciesInView: "{count} vacatures in beeld",
        prevVacancy: "Vorige vacature",
        nextVacancy: "Volgende vacature",
        vacancyOf: "Vacature {current} van {total}",
        close: "Sluiten",
        apply: "Solliciteer",
        viewJob: "Bekijk deze baan",
        whyPrefix: "Waarom:",
        travelFromHome: "{minutes} min {mode} van huis",
        save: "Bewaar",
        saved: "Bewaard",
        fitStrong: "Sterke match",
        fitGood: "Goede match",
        fitSome: "Enige match",
        fitPastWhy: "Past bij je: {reason}",
        fitGate: "Maak je paspoort af",
        hoursSingle: "{hours} uur",
        hoursRange: "{min}–{max} uur",
        passportHref: "/profiel",
        featured: "Uitgelicht",
        unavailableTitle: "Vacature niet beschikbaar",
        unavailableHint: "Probeer het opnieuw of open de vacaturepagina.",
        retry: "Opnieuw proberen",
        view: "Bekijk",
        pagerNav: "Vacatures op deze locatie",
        canSave: false
    };
    let selectedClusterKey = null;
    let clusterUiState = null;
    let clusterSheetEl = null;
    let clusterChipEl = null;
    let clusterEscapeBound = false;
    let isochroneCache = {};
    let isochroneLabelMarkers = [];

    // Fallback only when the index has no pins. Prefer the precomputed view from #jobsy-map-boot.
    const NL_CENTER = [52.15, 5.2913];
    const NL_ZOOM = 7;
    const NL_BOUNDS = [[50.29, 2.81], [53.33, 8.44]];

    function isInNetherlands(lat, lng) {
        return Number.isFinite(lat) && Number.isFinite(lng)
            && lat >= NL_BOUNDS[0][0] && lat <= NL_BOUNDS[1][0]
            && lng >= NL_BOUNDS[0][1] && lng <= NL_BOUNDS[1][1];
    }

    let locateMessageTimer = null;

    function showLocateMessage(text) {
        if (!map) {
            return;
        }
        const host = map.getContainer();
        if (!host) {
            return;
        }
        let note = host.querySelector(".job-map-locate__message");
        if (!note) {
            note = document.createElement("p");
            note.className = "job-map-locate__message";
            note.setAttribute("role", "status");
            host.appendChild(note);
        }
        if (locateMessageTimer) {
            clearTimeout(locateMessageTimer);
            locateMessageTimer = null;
        }
        note.textContent = text || "";
        note.hidden = !text;
        if (text) {
            locateMessageTimer = setTimeout(function () {
                note.hidden = true;
                note.textContent = "";
                locateMessageTimer = null;
            }, 4000);
        }
    }

    function detailHrefForVacancy(id) {
        const base = "/vacancies/" + encodeURIComponent(String(id || ""));
        const q = [];
        if (travelOptions.transport) {
            q.push("transport=" + encodeURIComponent(String(travelOptions.transport)));
        }
        if (travelOptions.maxMinutes != null) {
            q.push("maxMinutes=" + encodeURIComponent(String(travelOptions.maxMinutes)));
        }
        if (lastOrigin && Number.isFinite(lastOrigin.lat) && Number.isFinite(lastOrigin.lng)) {
            q.push("originLat=" + encodeURIComponent(String(lastOrigin.lat)));
            q.push("originLng=" + encodeURIComponent(String(lastOrigin.lng)));
        }
        return q.length ? base + "?" + q.join("&") : base;
    }
    // Keep in sync with VacancyMapViewCalculator.FilledLocationZoom (fits default 30-min fiets ring).
    const FILLED_LOCATION_ZOOM = 12;
    const BASEMAP_MAX_ZOOM = (window.jobsyMapLibre && window.jobsyMapLibre.BASEMAP_MAX_ZOOM) || 14;
    const BANEN_MIN_ZOOM = 7;

    const CLUSTER_OPTS = {
        cluster: true,
        clusterRadius: 50,
        clusterMaxZoom: BASEMAP_MAX_ZOOM - 1,
        // Must not exceed OpenFreeMap planet maxzoom (tiles vanish above source max).
        disableClusteringAtZoom: BASEMAP_MAX_ZOOM,
        maxClusterRadius: 50,
        removeOutsideVisibleBounds: false
    };

    function clampBanenZoom(z) {
        var n = Number(z);
        if (!Number.isFinite(n)) {
            return BANEN_MIN_ZOOM;
        }
        return Math.min(BASEMAP_MAX_ZOOM, Math.max(BANEN_MIN_ZOOM, n));
    }

    function applyBasemapZoomLimits() {
        if (!map || typeof map.setMaxZoom !== "function") {
            return;
        }
        try {
            map.setMinZoom(BANEN_MIN_ZOOM);
            map.setMaxZoom(BASEMAP_MAX_ZOOM);
        } catch (eLimits) { /* ignore */ }
    }

    function enforceBanenZoomClamp() {
        if (!map) {
            return;
        }
        try {
            var z = map.getZoom();
            var clamped = clampBanenZoom(z);
            if (Math.abs(clamped - z) > 0.001) {
                map.setZoom(clamped);
            }
        } catch (eClamp) { /* ignore */ }
    }
    const PIN_SOURCE = "jobsy-pins";
    const PIN_LAYER_CLUSTERS = "jobsy-pins-clusters";
    const PIN_LAYER_CLUSTER_COUNT = "jobsy-pins-cluster-count";
    const PIN_LAYER_UNCLUSTERED = "jobsy-pins-unclustered";
    const PIN_LAYER_UNCLUSTERED_GLYPH = "jobsy-pins-unclustered-glyph";
    const PIN_LAYER_UNCLUSTERED_SYMBOL = "jobsy-pins-unclustered-symbol";
    const PIN_LAYER_SELECTED = "jobsy-pins-selected";
    const LAGEN_CLUSTER_GREEN = "#1E7A4C";
    const PIN_SOURCE_AGENCY_AREAS = "jobsy-agency-areas";
    const PIN_LAYER_AGENCY_AREAS_FILL = "jobsy-agency-areas-fill";
    const PIN_LAYER_AGENCY_AREAS_LINE = "jobsy-agency-areas-line";
    const PIN_LAYER_CLUSTER_AGENCY_BADGE = "jobsy-pins-cluster-agency-badge";
    const AGENCY_PIN_MIN_ZOOM = CLUSTER_OPTS.disableClusteringAtZoom;
    const AGENCY_PIN_COLOR = "#4f46e5";
    const AGENCY_LABEL = "via uitzendbureau";
    const UNCLUSTERED_PIN_FILTER = [
        "all",
        ["!", ["has", "point_count"]],
        [
            "any",
            ["!=", ["coalesce", ["get", "agency"], 0], 1],
            [">=", ["zoom"], AGENCY_PIN_MIN_ZOOM]
        ]
    ];

    /** Lagen: agency/intermediary pins must appear once clusters dissolve (zoom > clusterMaxZoom). */
    function unclusteredPinFilter() {
        const agencyMinZ = lagenLayout ? 10 : AGENCY_PIN_MIN_ZOOM;
        return [
            "all",
            ["!", ["has", "point_count"]],
            [
                "any",
                ["!=", ["coalesce", ["get", "agency"], 0], 1],
                [">=", ["zoom"], agencyMinZ]
            ]
        ];
    }

    function lagenPinSymbolImageIds() {
        return ["jobsy-pin-w", "jobsy-pin-w-selected", "jobsy-pin-i", "jobsy-pin-i-selected"];
    }

    function lagenPinSymbolsRenderable() {
        if (!map) {
            return false;
        }
        const ids = lagenPinSymbolImageIds();
        for (let i = 0; i < ids.length; i++) {
            if (!map.hasImage(ids[i])) {
                return false;
            }
        }
        return true;
    }

    // On-road cruise km/h. Keep in sync with TravelReach.SpeedKmPerHour.
    const CRUISE_KM_H = {
        Fiets: 18.0,
        Auto: 80.0,
        OV: 25.0,
        Lopend: 5.0
    };
    // Road / crow-flies. Keep in sync with TravelReach.RoadCircuity (bike 1.7 ≈ OSRM).
    const ROAD_CIRCUITY = {
        Fiets: 1.7,
        Auto: 1.35,
        OV: 1.5,
        Lopend: 1.4
    };

    const TRANSPORT_LABEL = {
        Fiets: "fietsen",
        Auto: "rijden",
        OV: "OV",
        Lopend: "lopen"
    };

    function transportVerb(mode) {
        const t = canonicalTransport(mode);
        if (t === "Auto") return (uiLabels && uiLabels.transportCar) || TRANSPORT_LABEL.Auto;
        if (t === "OV") return (uiLabels && uiLabels.transportTransit) || TRANSPORT_LABEL.OV;
        if (t === "Lopend") return (uiLabels && uiLabels.transportWalk) || TRANSPORT_LABEL.Lopend;
        return (uiLabels && uiLabels.transportBike) || TRANSPORT_LABEL.Fiets;
    }

    function travelFallbackVerb() {
        return (uiLabels && uiLabels.transportDefault) || "reistijd";
    }

    function vacancyFallbackTitle() {
        return (uiLabels && uiLabels.vacancyFallback) || "Vacature";
    }

    function noVacanciesLabel() {
        return (uiLabels && uiLabels.noVacancies) || "Geen vacatures";
    }

    function canonicalTransport(t) {
        const raw = String(t || "").trim();
        const compact = raw.toLowerCase().replace(/[\s-]/g, "");
        if (compact === "ebike" || compact === "ebikes") {
            return "Fiets";
        }
        return raw || "Fiets";
    }

    function workTypeGlyph(workType) {
        const t = String(workType || "").toLowerCase();
        if (t.indexOf("horeca") >= 0) return "☕";
        if (t.indexOf("winkel") >= 0 || t.indexOf("retail") >= 0 || t.indexOf("supermarkt") >= 0) return "🛒";
        if (t.indexOf("logistiek") >= 0) return "📦";
        if (t.indexOf("zorg") >= 0) return "✚";
        if (t.indexOf("kantoor") >= 0) return "💼";
        if (t.indexOf("bouw") >= 0) return "🔧";
        if (t.indexOf("tuinbouw") >= 0) return "🌿";
        if (t.indexOf("schoonmaak") >= 0) return "✨";
        if (t.indexOf("productie") >= 0) return "🏭";
        return "";
    }

    /** ASCII-only glyphs for MapLibre symbol layers (no emoji / PUA font fetches). */
    function workTypeMapGlyph(workType) {
        const t = String(workType || "").toLowerCase();
        if (t.indexOf("horeca") >= 0) return "H";
        if (t.indexOf("winkel") >= 0 || t.indexOf("retail") >= 0 || t.indexOf("supermarkt") >= 0) return "W";
        if (t.indexOf("logistiek") >= 0) return "L";
        if (t.indexOf("zorg") >= 0) return "+";
        if (t.indexOf("kantoor") >= 0) return "K";
        if (t.indexOf("bouw") >= 0) return "B";
        if (t.indexOf("tuinbouw") >= 0) return "T";
        if (t.indexOf("schoonmaak") >= 0) return "S";
        if (t.indexOf("productie") >= 0) return "P";
        return "";
    }

    function markerClassName(featured, selected, matchBand) {
        const classes = ["job-marker"];
        if (featured) {
            classes.push("job-marker--featured");
        }
        if (selected) {
            classes.push("job-marker--active");
        }
        if (matchBand) {
            classes.push("job-marker--match-" + matchBand);
        }
        return classes.join(" ");
    }

    function markerInnerHtml(featured, workType, categoryColor, matchPercent, matchBand) {
        const glyph = workTypeGlyph(workType);
        const pulse = featured
            ? "<span class=\"job-marker__pulse\" aria-hidden=\"true\"></span>"
            : "";
        const color = (categoryColor && /^#[0-9A-Fa-f]{6}$/.test(categoryColor))
            ? categoryColor
            : "";
        const style = color
            ? " style=\"--map-pin:" + color + ";--map-pin-deep:" + color + ";--map-pin-glow:" + color + "66\""
            : "";
        const match = !illustratedPopup && matchPercent != null && matchPercent !== ""
            ? "<span class=\"job-marker__match job-marker__match--" + escapeHtml(String(matchBand || "orange")) +
              "\">" + escapeHtml(String(matchPercent)) + "%</span>"
            : "";
        return pulse + match + "<span class=\"job-marker__glyph\"" + style + " aria-hidden=\"true\">" + glyph + "</span>";
    }

    function fillMarkerElement(el, featured, selected, workType, categoryColor, matchPercent, matchBand) {
        el.className = markerClassName(featured, selected, matchBand);
        el.innerHTML = markerInnerHtml(featured, workType, categoryColor, matchPercent, matchBand);
        return el;
    }

    function workTypeOf(v) {
        return Array.isArray(v.workTypes) && v.workTypes.length
            ? v.workTypes[0]
            : (v.workType || "");
    }

    function workTypeFallbackPhoto(v) {
        const wt = String(workTypeOf(v) || "").toLowerCase();
        if (wt.indexOf("logist") >= 0) {
            return "/images/vacancies/logistiek.webp";
        }
        if (wt.indexOf("zorg") >= 0 || wt.indexOf("care") >= 0) {
            return "/images/vacancies/zorg.webp";
        }
        if (wt.indexOf("horeca") >= 0) {
            return "/images/vacancies/horeca.webp";
        }
        if (wt.indexOf("winkel") >= 0 || wt.indexOf("retail") >= 0) {
            return "/images/vacancies/winkel.webp";
        }
        if (wt.indexOf("bouw") >= 0) {
            return "/images/vacancies/bouw.webp";
        }
        if (wt.indexOf("tuin") >= 0) {
            return "/images/vacancies/tuinbouw.webp";
        }
        if (wt.indexOf("kantoor") >= 0 || wt.indexOf("office") >= 0) {
            return "/images/vacancies/kantoor.webp";
        }
        if (wt.indexOf("product") >= 0) {
            return "/images/vacancies/productie.webp";
        }
        if (wt.indexOf("schoon") >= 0) {
            return "/images/vacancies/schoonmaak.webp";
        }
        return "/images/vacancies/flex.webp";
    }

    function illustratedSpotForVacancy(v) {
        const spots = illustratedVacancySpots || {};
        const pick = function (key) {
            const k = String(key || "").toLowerCase();
            if (spots[k]) {
                return String(spots[k]);
            }
            return spots.default ? String(spots.default) : "";
        };
        const title = String(v && v.title ? v.title : "").toLowerCase();
        const company = String(v && v.company ? v.company : "").toLowerCase();
        const wt = String(workTypeOf(v) || "").toLowerCase();
        const hay = title + " " + company + " " + wt;
        if (hay.indexOf("schoon") >= 0) {
            return pick("schoonmaak");
        }
        if (hay.indexOf("zorg") >= 0 || hay.indexOf("care") >= 0) {
            return pick("zorg");
        }
        if (hay.indexOf("order") >= 0 || hay.indexOf("magaz") >= 0 || hay.indexOf("logist") >= 0 || hay.indexOf("pakhuis") >= 0) {
            return pick("logistiek");
        }
        if (hay.indexOf("horeca") >= 0 || hay.indexOf("brasserie") >= 0 || hay.indexOf("chef") >= 0) {
            return pick("horeca");
        }
        if (hay.indexOf("winkel") >= 0 || hay.indexOf("retail") >= 0 || hay.indexOf("mode") >= 0) {
            return pick("retail");
        }
        if (hay.indexOf("bouw") >= 0 || hay.indexOf("tuin") >= 0 || hay.indexOf("groen") >= 0) {
            return pick("bouw");
        }
        if (hay.indexOf("kantoor") >= 0 || hay.indexOf("office") >= 0) {
            return pick("kantoor");
        }
        if (hay.indexOf("dier") >= 0) {
            return pick("dieren");
        }
        if (hay.indexOf("techn") >= 0 || hay.indexOf("monteur") >= 0) {
            return pick("techniek");
        }
        return pick("default");
    }

    function popupRootClass(extra) {
        let cls = "map-popup map-popup--calm";
        if (illustratedPopup) {
            cls += " map-popup--illustrated";
        }
        if (extra) {
            cls += " " + extra;
        }
        return cls;
    }

    function formatWage(wage) {
        if (wage == null || wage === "") return "";
        return Number(wage).toFixed(2).replace(".", ",");
    }

    function isNarrowViewport() {
        // DS breakpoints: mobile layout below 900px (file 03).
        return (window.innerWidth || 0) < 900;
    }

    /** Lagen Funda popup (mockup c8ee9298): desktop always; mobile when catalog capture mode. */
    function lagenUsesFundaPopup() {
        return lagenLayout && (catalogModeActive || !isNarrowViewport());
    }

    // Center-anchored pins: 34px job / 44px cluster. Tip sits on the top of the marker.
    const JOB_POPUP_OFFSET = { bottom: [0, -18] };
    const CLUSTER_POPUP_OFFSET = { bottom: [0, -23] };

    function isFeaturedVacancy(v) {
        return !!(v && (v.highlighted === true || v.isFeatured === true || v.featured === true));
    }

    function jobPopupOptions(featured) {
        const vw = window.innerWidth || 360;
        const narrow = isNarrowViewport();
        const width = narrow
            ? Math.max(280, Math.min(340, vw - 24))
            : 520;
        return {
            className: "job-map-popup" + (featured ? " job-map-popup--featured featured-job" : ""),
            maxWidth: width + "px",
            anchor: "bottom",
            offset: JOB_POPUP_OFFSET,
            closeOnClick: true,
            closeButton: true
        };
    }

    function clusterPopupOptions(featured) {
        const opts = jobPopupOptions(featured);
        opts.className = opts.className + " job-map-popup--cluster";
        opts.offset = CLUSTER_POPUP_OFFSET;
        return opts;
    }

    function syncFeaturedPopupClass(popup, vacancy) {
        const el = popup && typeof popup.getElement === "function" ? popup.getElement() : null;
        if (!el || !el.classList) {
            return;
        }

        const featured = isFeaturedVacancy(vacancy);
        el.classList.toggle("job-map-popup--featured", featured);
        el.classList.toggle("featured-job", featured);
        const content = el.querySelector(".maplibregl-popup-content");
        if (content && content.classList) {
            content.classList.toggle("featured-job", featured);
        }
    }

    function applyPopupOptions(popup, vacancy) {
        if (!popup) {
            return;
        }
        const opts = clusterPopupOptions(isFeaturedVacancy(vacancy));
        popup.options = popup.options || {};
        popup.options.className = opts.className;
        popup.options.maxWidth = opts.maxWidth;
        popup.options.anchor = opts.anchor;
        popup.options.offset = opts.offset;
        if (typeof popup.setOffset === "function") {
            popup.setOffset(opts.offset);
        }

        const el = typeof popup.getElement === "function" ? popup.getElement() : null;
        if (el) {
            el.classList.add("job-map-popup", "job-map-popup--cluster");
            el.classList.remove(
                "job-map-popup--with-wages",
                "job-map-popup--with-type",
                "job-cluster-popup",
                "job-cluster-popup--with-wages"
            );
        }
    }

    function hasWageBands(v) {
        return Array.isArray(v.wageBands) && v.wageBands.length > 0;
    }

    function wageInlineHtml(v) {
        if (v.wageLabel) {
            return "<p class=\"map-popup__wage map-popup__wage--masked\">" + escapeHtml(v.wageLabel) + "</p>";
        }
        if (v.wage == null || v.wage === "") {
            return "";
        }
        return "<p class=\"map-popup__wage\">€ " + formatWage(v.wage) + " <span class=\"map-popup__wage-unit\">/uur</span></p>";
    }

    function wageTableRowsHtml(bands) {
        return bands.map(function (b) {
            return "<tr><th>" + escapeHtml(String(b.label || b.ageYears || "")) + "</th>" +
                "<td>€ " + formatWage(b.hourlyRate) + "</td></tr>";
        }).join("");
    }

    function wageInfoHtml(v) {
        if (!hasWageBands(v)) {
            return "";
        }
        return (
            "<button type=\"button\" class=\"map-popup__wage-info\" aria-expanded=\"false\" " +
                "aria-controls=\"wage-popover-" + escapeAttr(v.id) + "\" " +
                "aria-label=\"Uurlonen per leeftijd\">€</button>" +
            "<div id=\"wage-popover-" + escapeAttr(v.id) + "\" class=\"map-popup__wage-popover\" hidden>" +
                "<p class=\"map-popup__wage-popover-title\">Uurlonen</p>" +
                "<table class=\"map-popup__wage-table\"><tbody>" + wageTableRowsHtml(v.wageBands) + "</tbody></table>" +
            "</div>"
        );
    }

    function specIcon(kind) {
        if (kind === "travel") {
            return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
                "<circle cx=\"12\" cy=\"12\" r=\"8.25\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "<path d=\"M12 7.5v5l3.2 1.9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>" +
                "</svg>";
        }
        if (kind === "work") {
            return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
                "<rect x=\"4\" y=\"8\" width=\"16\" height=\"11\" rx=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "<path d=\"M9 8V6.8A1.8 1.8 0 0 1 10.8 5h2.4A1.8 1.8 0 0 1 15 6.8V8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "</svg>";
        }
        return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
            "<path d=\"M5 15.5 7.2 8.8A2 2 0 0 1 9.1 7.5h5.8a2 2 0 0 1 1.9 1.3L19 15.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>" +
            "<circle cx=\"8\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<circle cx=\"16\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "</svg>";
    }

    function travelLineHtml(v) {
        if (v.travelMinutes == null) {
            return "<p class=\"map-popup__travel map-popup__travel--empty\" aria-hidden=\"true\"></p>";
        }
        const transport = String(v.transportLabel || transportVerb(v.transport) || travelFallbackVerb());
        return (
            "<p class=\"map-popup__travel\">" +
                specIcon("travel") +
                "<span>± " + escapeHtml(String(v.travelMinutes)) + " min " + escapeHtml(transport) + "</span>" +
            "</p>"
        );
    }

    function specsHtml(v) {
        const parts = [];

        const workTypes = Array.isArray(v.workTypes) ? v.workTypes : [];
        const primaryWork = workTypes[0] || v.workType || "";
        if (primaryWork) {
            parts.push(
                "<span class=\"map-popup__spec\">" + specIcon("work") +
                "<span>" + escapeHtml(String(primaryWork)) + "</span></span>"
            );
        }

        const transports = Array.isArray(v.transport) ? v.transport : [];
        if (transports.length) {
            parts.push(
                "<span class=\"map-popup__spec\">" + specIcon("transport") +
                "<span>" + escapeHtml(transports.join(", ")) + "</span></span>"
            );
        }

        if (!parts.length) {
            return "<div class=\"map-popup__specs map-popup__specs--empty\" aria-hidden=\"true\"></div>";
        }
        return "<div class=\"map-popup__specs\">" + parts.join("") + "</div>";
    }

    function typeChipHtml(v) {
        if (!v.typeBadgeLabel) {
            return "";
        }
        const color = safeBadgeColor(v.typeBadgeColor || v.categoryColor);
        return (
            "<span class=\"map-popup__type-chip\" style=\"--badge-color:" +
            escapeAttr(color) +
            "\">" +
            escapeHtml(String(v.typeBadgeLabel)) +
            "</span>"
        );
    }

    function mountWageControls(popupEl) {
        if (!popupEl) {
            return;
        }
        const content = popupEl.querySelector(".maplibregl-popup-content") || popupEl;
        const btnInContent = content
            ? content.querySelector(".map-popup__wage-info")
            : null;
        const popoverInContent = content
            ? content.querySelector(".map-popup__wage-popover")
            : null;
        const typeInContent = content
            ? content.querySelector(".map-popup__type-chip")
            : null;

        Array.prototype.slice.call(popupEl.children).forEach(function (child) {
            if (!child.classList) {
                return;
            }
            const isChrome = child.classList.contains("map-popup__wage-info")
                || child.classList.contains("map-popup__wage-popover")
                || child.classList.contains("map-popup__type-chip");
            if (isChrome
                && child !== btnInContent
                && child !== popoverInContent
                && child !== typeInContent) {
                child.remove();
            }
        });

        if (btnInContent) {
            popupEl.appendChild(btnInContent);
            popupEl.classList.add("job-map-popup--with-wages");
        } else {
            popupEl.classList.remove("job-map-popup--with-wages");
        }
        if (popoverInContent) {
            popupEl.appendChild(popoverInContent);
        }
        if (typeInContent) {
            popupEl.appendChild(typeInContent);
            popupEl.classList.add("job-map-popup--with-type");
        } else {
            popupEl.classList.remove("job-map-popup--with-type");
        }
    }

    function closeAllWagePopovers(root) {
        const scope = root || document;
        scope.querySelectorAll(".map-popup__wage-popover:not([hidden])").forEach(function (pop) {
            pop.setAttribute("hidden", "");
        });
        scope.querySelectorAll(".map-popup__wage-info.is-open").forEach(function (btn) {
            btn.classList.remove("is-open");
            btn.setAttribute("aria-expanded", "false");
        });
    }

    function stopEvent(ev) {
        if (!ev) {
            return;
        }
        if (typeof ev.stopPropagation === "function") {
            ev.stopPropagation();
        }
        if (typeof ev.preventDefault === "function") {
            ev.preventDefault();
        }
        if (typeof ev.stopImmediatePropagation === "function") {
            ev.stopImmediatePropagation();
        }
    }

    function bindWageInfoInteractions(popupEl) {
        if (!popupEl) {
            return;
        }

        mountWageControls(popupEl);

        popupEl.querySelectorAll(".map-popup__wage-info").forEach(function (btn) {
            if (btn.dataset.boundWageInfo) {
                return;
            }
            btn.dataset.boundWageInfo = "1";
            btn.addEventListener("click", function (ev) {
                stopEvent(ev);
                const controlId = btn.getAttribute("aria-controls");
                const popover = controlId
                    ? popupEl.querySelector("#" + controlId)
                    : popupEl.querySelector(".map-popup__wage-popover");
                if (!popover) {
                    return;
                }
                const willOpen = popover.hasAttribute("hidden");
                closeAllWagePopovers(popupEl);
                if (willOpen) {
                    popover.removeAttribute("hidden");
                    btn.classList.add("is-open");
                    btn.setAttribute("aria-expanded", "true");
                }
            });
        });
    }

    function buildingIconHtml() {
        return "<svg class=\"map-popup__co-icon\" viewBox=\"0 0 24 24\" width=\"14\" height=\"14\" aria-hidden=\"true\" focusable=\"false\">" +
            "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"1\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<path d=\"M9 7h1M14 7h1M9 11h1M14 11h1M9 15h1M14 15h1M10 21v-3h4v3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "</svg>";
    }

    function sparkIconHtml() {
        return "<svg class=\"map-popup__why-icon\" viewBox=\"0 0 24 24\" width=\"14\" height=\"14\" aria-hidden=\"true\" focusable=\"false\">" +
            "<path d=\"M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>" +
            "</svg>";
    }

    function heartIconHtml(filled) {
        if (filled) {
            return "<svg class=\"map-popup__heart-icon\" viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" aria-hidden=\"true\" focusable=\"false\">" +
                "<path fill=\"currentColor\" d=\"M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z\"/>" +
                "</svg>";
        }
        return "<svg class=\"map-popup__heart-icon\" viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" aria-hidden=\"true\" focusable=\"false\">" +
            "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z\"/>" +
            "</svg>";
    }

    function hoursMetaText(v) {
        const min = v.minHoursPerWeek != null ? Number(v.minHoursPerWeek) : NaN;
        const max = v.maxHoursPerWeek != null ? Number(v.maxHoursPerWeek) : NaN;
        const fmt = function (n) {
            return Number.isInteger(n) ? String(n) : String(n).replace(".", ",");
        };
        if (Number.isFinite(min) && Number.isFinite(max)) {
            if (min === max) {
                return labelFormat(uiLabels.hoursSingle || "{hours} uur", { hours: fmt(min) });
            }
            return labelFormat(uiLabels.hoursRange || "{min}–{max} uur", { min: fmt(min), max: fmt(max) });
        }
        if (Number.isFinite(min)) {
            return labelFormat(uiLabels.hoursSingle || "{hours} uur", { hours: fmt(min) });
        }
        if (Number.isFinite(max)) {
            return labelFormat(uiLabels.hoursSingle || "{hours} uur", { hours: fmt(max) });
        }
        return "";
    }

    function metaRowHtml(v) {
        if (lagenLayout) {
            return "";
        }
        const parts = [];
        if (v.travelMinutes != null && v.travelMinutes !== "") {
            const mode = String(v.transportLabel || transportVerb(v.transport) || travelFallbackVerb());
            parts.push(
                "<span class=\"map-popup__meta-item map-popup__meta-item--travel\">" +
                    "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"15\" height=\"15\" aria-hidden=\"true\" focusable=\"false\">" +
                        "<circle cx=\"6\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                        "<circle cx=\"18\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                        "<path d=\"M6 16l4-7h5l3 7M10 9 8.5 6H7M15 9l-3 7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>" +
                    "</svg>" +
                    "<span>" + escapeHtml(String(v.travelMinutes)) + " min</span>" +
                    "<span class=\"map-popup__meta-mode\" hidden>" + escapeHtml(mode) + "</span>" +
                "</span>"
            );
        }
        const hours = hoursMetaText(v);
        if (hours) {
            parts.push(
                "<span class=\"map-popup__meta-item\">" +
                    "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"15\" height=\"15\" aria-hidden=\"true\" focusable=\"false\">" +
                        "<circle cx=\"12\" cy=\"12\" r=\"8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                        "<path d=\"M12 8v4l3 2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>" +
                    "</svg>" +
                    "<span>" + escapeHtml(hours) + "</span></span>"
            );
        }
        if (v.wageLabel) {
            parts.push("<span class=\"map-popup__meta-item\">" + escapeHtml(String(v.wageLabel)) + "</span>");
        } else if (v.wage != null && v.wage !== "") {
            parts.push("<span class=\"map-popup__meta-item\">€ " + escapeHtml(formatWage(v.wage)) + "</span>");
        }
        if (!parts.length) {
            return "";
        }
        const inner = parts.join("");
        return "<div class=\"map-popup__meta\">" + inner + "</div>";
    }

    function agencyPopupLineHtml(v) {
        if (!isAgencyPin(v)) {
            return "";
        }
        return (
            "<p class=\"map-popup__agency\">" +
                "<span class=\"map-popup__agency-glyph\" aria-hidden=\"true\">◆</span>" +
                "<span>" + escapeHtml(AGENCY_LABEL) + "</span>" +
            "</p>"
        );
    }

    function companyLineHtml(v, detailHref) {
        const agencyLine = agencyPopupLineHtml(v);
        if (agencyLine && v.company) {
            const companyHref = v.companyHref ? String(v.companyHref) : detailHref;
            return agencyLine +
                "<a class=\"map-popup__company map-popup__cta\" href=\"" + escapeAttr(companyHref) + "\"" +
                    (companyHref === detailHref ? " data-job-id=\"" + escapeAttr(v.id) + "\"" : "") + ">" +
                    buildingIconHtml() +
                    "<b>" + escapeHtml(v.company || "") + "</b>" +
                "</a>";
        }
        if (agencyLine) {
            return agencyLine;
        }
        if (v.offeredBy) {
            return (
                "<p class=\"map-popup__company map-popup__company--via\">" +
                    buildingIconHtml() +
                    "<span>" + escapeHtml(String(v.offeredBy)) + "</span>" +
                "</p>"
            );
        }
        const companyHref = v.companyHref ? String(v.companyHref) : detailHref;
        return (
            "<a class=\"map-popup__company map-popup__cta\" href=\"" + escapeAttr(companyHref) + "\"" +
                (companyHref === detailHref ? " data-job-id=\"" + escapeAttr(v.id) + "\"" : "") + ">" +
                buildingIconHtml() +
                "<b>" + escapeHtml(v.company || "") + "</b>" +
            "</a>"
        );
    }

    function fitWhyReason(v) {
        return v.fitWhyLine ? String(v.fitWhyLine).trim() : "";
    }

    function whyLineHtml(v) {
        const why = fitWhyReason(v);
        if (!why) {
            return "";
        }
        if (illustratedPopup) {
            return "";
        }
        const prefix = uiLabels.whyPrefix || "Waarom:";
        const pastLabel = labelFormat(uiLabels.fitPastWhy || "Past bij je: {reason}", { reason: why });
        if (why === pastLabel || pastLabel.endsWith(why)) {
            return "";
        }
        return (
            "<p class=\"map-popup__why\">" +
                sparkIconHtml() +
                "<span>" + escapeHtml(prefix) + " " + escapeHtml(why) + "</span>" +
            "</p>"
        );
    }

    function heartButtonHtml(v) {
        const liked = !!v.liked;
        const label = liked
            ? (uiLabels.saved || "Bewaard")
            : (uiLabels.save || "Bewaar");
        return (
            "<button type=\"button\" class=\"map-popup__heart" + (liked ? " is-saved" : "") + "\" " +
                "data-save-job=\"" + escapeAttr(v.id) + "\" " +
                "aria-pressed=\"" + (liked ? "true" : "false") + "\" " +
                "aria-label=\"" + escapeAttr(label) + "\" title=\"" + escapeAttr(label) + "\">" +
                heartIconHtml(liked) +
            "</button>"
        );
    }

    function lagenOrgLineHtml(v) {
        if (v.offeredBy) {
            return (
                "<p class=\"map-popup__company map-popup__company--via kb-popup__org\">" +
                    escapeHtml(String(v.offeredBy)) +
                "</p>"
            );
        }
        if (v.company) {
            return (
                "<p class=\"map-popup__company kb-popup__org\">" +
                    escapeHtml(String(v.company)) +
                "</p>"
            );
        }
        return "";
    }

    function lagenTravelLineHtml(v) {
        if (v.travelMinutes == null || v.travelMinutes === "") {
            return "";
        }
        const ic = window.jobsyBanenkaartMapIcons;
        const bike = ic && ic.bikeSvg ? ic.bikeSvg : "";
        const mins = escapeHtml(String(v.travelMinutes)) + " min";
        if (lagenLayout) {
            const mode = String(v.transportLabel || transportVerb(v.transport) || travelFallbackVerb());
            return "<div class=\"rt kb-popup__travel\">" + bike + "<span>"
                + escapeHtml(String(v.travelMinutes)) + " min " + escapeHtml(mode) + "</span></div>";
        }
        const mode = String(v.transportLabel || transportVerb(v.transport) || travelFallbackVerb());
        return (
            "<p class=\"map-popup__meta kb-popup__travel\">" +
                bike +
                "<span>" + mins + " " + escapeHtml(mode) + "</span>" +
            "</p>"
        );
    }

    function lagenDescriptionLine(v) {
        const raw = v && (v.summary || v.shortDescription || v.descriptionLine || v.whyLine || v.categoryName || "");
        const text = String(raw || "").trim();
        if (!text) {
            return "";
        }
        return escapeHtml(text);
    }

    function lagenPopupIsEnglish() {
        try {
            const lang = (document.documentElement && document.documentElement.lang) || "";
            return String(lang).toLowerCase().indexOf("en") === 0;
        } catch (eLang) {
            return false;
        }
    }

    function lagenIsIntermediaryPin(v) {
        if (!v) {
            return false;
        }
        if (v.offeredBy) {
            return true;
        }
        const kind = String(v.pinKind || v.agency || "");
        return kind === "intermediary" || kind === "1";
    }

    function lagenChevronRightSvg() {
        return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M10 7l5 5-5 5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";
    }

    function lagenChevronLeftSvg() {
        return "<svg class=\"ic\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M14.5 6l-6 6 6 6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";
    }

    function lagenCloseSvg() {
        return "<svg class=\"ic\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\"/></svg>";
    }

    function lagenClockSvg() {
        return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/><path d=\"M12 8v4.5l3 2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\"/></svg>";
    }

    function lagenDocSvg() {
        return "<svg class=\"ic s\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M7 3.5h7l4 4V20H7z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linejoin=\"round\"/><path d=\"M14 3.5V8h4M10 12h5M10 15.5h5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\"/></svg>";
    }

    function lagenWageAgeLabel(ageYears) {
        const en = lagenPopupIsEnglish();
        const age = Number(ageYears);
        if (age >= 21) {
            return en ? "21 years and over" : "21 jaar en ouder";
        }
        if (age === 14) {
            return en ? "14 years" : "14 jaar";
        }
        return en ? (age + " years") : (age + " jaar");
    }

    function lagenVacancyCounter(current, total) {
        const en = lagenPopupIsEnglish();
        if (en) {
            return current + " of " + total;
        }
        return current + " van " + total;
    }

    function normalizeLagenWageBands(v) {
        if (Array.isArray(v.wageBands) && v.wageBands.length) {
            return v.wageBands.slice();
        }
        if (Array.isArray(v.wageByAge) && v.wageByAge.length) {
            return v.wageByAge.map(function (b) {
                return {
                    ageYears: b.ageYears != null ? b.ageYears : b.AgeYears,
                    hourlyRate: b.hourlyRate != null ? b.hourlyRate : b.HourlyRate,
                    label: b.label || b.Label || ""
                };
            });
        }
        const adult = Number(v.wage);
        if (!Number.isFinite(adult) || adult <= 0) {
            return [];
        }
        const fracs = [
            { ageYears: 15, f: 0.30 },
            { ageYears: 16, f: 0.345 },
            { ageYears: 17, f: 0.395 },
            { ageYears: 18, f: 0.50 },
            { ageYears: 19, f: 0.60 },
            { ageYears: 20, f: 0.80 },
            { ageYears: 21, f: 1.00 }
        ];
        return fracs.map(function (row) {
            return {
                ageYears: row.ageYears,
                hourlyRate: Math.round(adult * row.f * 100) / 100,
                label: String(row.ageYears)
            };
        });
    }

    function lagenHighlightBandAge(bands, candidateAge) {
        const age = Number(candidateAge);
        if (!Number.isFinite(age)) {
            return null;
        }
        const clamped = Math.max(14, Math.min(99, age));
        let match = null;
        bands.forEach(function (b) {
            const ay = Number(b.ageYears);
            if (Number.isFinite(ay) && ay <= clamped) {
                match = ay;
            }
        });
        return match;
    }

    function lagenWageTooltipHtml(v, open) {
        const bands = normalizeLagenWageBands(v);
        if (!bands.length) {
            return "";
        }
        const en = lagenPopupIsEnglish();
        const title = en
            ? (uiLabels.wageTooltipTitleEn || "Hourly wage (gross)")
            : (uiLabels.wageTooltipTitle || "Uurloon (bruto)");
        const candidateAge = uiLabels.candidateAgeYears != null
            ? Number(uiLabels.candidateAgeYears)
            : 21;
        const highlightAge = lagenHighlightBandAge(bands, candidateAge);
        const rows = [];
        const age15 = bands.find(function (b) { return Number(b.ageYears) === 15; }) || bands[0];
        rows.push({ ageYears: 14, hourlyRate: age15.hourlyRate, me: candidateAge === 14 });
        bands.forEach(function (b) {
            const ay = Number(b.ageYears);
            rows.push({
                ageYears: ay,
                hourlyRate: b.hourlyRate,
                me: highlightAge != null && ay === highlightAge
            });
        });
        const uniq = [];
        rows.forEach(function (r) {
            if (!uniq.some(function (u) { return u.ageYears === r.ageYears; })) {
                uniq.push(r);
            }
        });
        const body = uniq.map(function (r) {
            const cls = r.me ? "tr2 me" : "tr2";
            return "<div class=\"" + cls + "\"><span>" + escapeHtml(lagenWageAgeLabel(r.ageYears)) +
                "</span><span>€ " + formatWage(r.hourlyRate) + "</span></div>";
        }).join("");
        return "<div class=\"tip\" role=\"tooltip\"" + (open ? "" : " hidden") + "><b>" + escapeHtml(title) +
            "</b>" + body + "</div>";
    }

    let lastLagenPopupPhotoMetrics = null;

    function lagenMapPopupPhotoUrl(vacancyId, mobile) {
        if (vacancyId == null || vacancyId === "") {
            return "";
        }
        const variant = mobile ? "mobile" : "desktop";
        return "/api/vacancies/" + encodeURIComponent(String(vacancyId)) +
            "/map-popup-photo?variant=" + variant;
    }

    function lagenJobHasMapPopupPhoto(job) {
        if (!job) {
            return false;
        }
        if (job.hasMapPopupPhoto === false || job.hasMapPopupPhoto === 0) {
            return false;
        }
        const u = job.imageUrl || job.thumbnailUrl || job.photoUrl || job.ImageUrl;
        return !!(u && String(u).trim());
    }

    function lagenCategoryPhotoPlaceholderHtml(v) {
        const hex = (v && (v.categoryColor || v.colour || v.categoryColorHex)) || "#3366cc";
        const safe = String(hex).replace(/[^#a-fA-F0-9]/g, "") || "#3366cc";
        return "<div class=\"fph fph--category\" style=\"background:" +
            escapeAttr(safe) + "\" aria-hidden=\"true\"></div>";
    }

    function banenkaartReturnUrl() {
        try {
            return window.location.pathname + window.location.search;
        } catch (eUrl) {
            return "/banenkaart";
        }
    }

    function lagenWorkplacePhotoShell(v) {
        if (!v || v.id == null) {
            return "<div class=\"fph fph--empty\" aria-hidden=\"true\"></div>";
        }
        if (!lagenJobHasMapPopupPhoto(v)) {
            return lagenCategoryPhotoPlaceholderHtml(v);
        }
        return "<div class=\"fph fph--await\" data-lagen-photo-slot=\"1\" data-lagen-photo-id=\"" +
            escapeAttr(String(v.id)) + "\"></div>";
    }

    function loadLagenPopupPhoto(root, job, opts) {
        opts = opts || {};
        if (!root || !job || job.id == null) {
            return;
        }
        const slot = root.querySelector("[data-lagen-photo-slot]");
        if (!slot) {
            return;
        }
        slot.replaceChildren();
        const mobile = opts.mobile != null ? !!opts.mobile : isNarrowViewport();
        if (!lagenJobHasMapPopupPhoto(job)) {
            slot.outerHTML = lagenCategoryPhotoPlaceholderHtml(job);
            return;
        }
        const url = lagenMapPopupPhotoUrl(job.id, mobile);
        if (!url) {
            slot.classList.add("fph--empty");
            return;
        }
        const started = typeof performance !== "undefined" && performance.now
            ? performance.now()
            : Date.now();
        slot.classList.remove("fph--await", "fph--empty");
        slot.classList.add("fph--loading");
        const img = document.createElement("img");
        img.alt = "";
        img.decoding = "async";
        img.setAttribute("width", mobile ? "248" : "640");
        img.setAttribute("height", mobile ? "248" : "360");
        fetch(url, { credentials: "same-origin" })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("photo-http");
                }
                return res.blob();
            })
            .then(function (blob) {
                const ms = Math.round((typeof performance !== "undefined" ? performance.now() : Date.now()) - started);
                lastLagenPopupPhotoMetrics = {
                    vacancyId: String(job.id),
                    variant: mobile ? "mobile" : "desktop",
                    bytes: blob.size,
                    kb: Math.round(blob.size / 102.4) / 10,
                    ms: ms,
                    url: url
                };
                img.onload = function () {
                    slot.classList.remove("fph--loading");
                };
                img.src = URL.createObjectURL(blob);
                slot.appendChild(img);
            })
            .catch(function () {
                slot.outerHTML = lagenCategoryPhotoPlaceholderHtml(job);
            });
    }

    function prefetchLagenPopupPhoto(job, mobile) {
        if (!job || job.id == null || !lagenJobHasMapPopupPhoto(job)) {
            return;
        }
        const url = lagenMapPopupPhotoUrl(job.id, mobile);
        if (!url) {
            return;
        }
        try {
            fetch(url, { credentials: "same-origin", priority: "low" }).catch(function () { });
        } catch (eFetch) { }
    }

    function lagenContractText(v) {
        const wt = String(v.workType || (v.workTypes && v.workTypes[0]) || "").trim();
        if (wt) {
            return wt;
        }
        const en = lagenPopupIsEnglish();
        return en ? "Flexible contract" : "Flexibel contract";
    }

    function lagenTravelFactText(v) {
        if (v.travelMinutes == null || v.travelMinutes === "") {
            return "";
        }
        const mode = String(v.transportLabel || transportVerb(v.transport) || travelFallbackVerb());
        return String(v.travelMinutes) + " min " + mode;
    }

    function lagenTypeLabelHtml(v) {
        const ic = window.jobsyBanenkaartMapIcons;
        const via = lagenIsIntermediaryPin(v);
        const en = lagenPopupIsEnglish();
        const text = via
            ? (en ? "Via intermediary" : "Via intermediair")
            : (en ? "At employer" : "Bij werkgever");
        const pinSvg = ic && (via ? ic.pinI : ic.pinW)
            ? (via ? ic.pinI(0.46, false, false) : ic.pinW(0.46))
            : "";
        return "<span class=\"flab " + (via ? "i" : "w") + "\">" + pinSvg + escapeHtml(text) + "</span>";
    }

    function lagenHeartHtml(v) {
        const liked = !!v.liked;
        const label = liked
            ? (uiLabels.saved || "Bewaard")
            : (uiLabels.save || "Bewaar");
        return (
            "<span class=\"lagen-heart-slot\" data-lagen-heart-slot=\"1\">" +
            "<button type=\"button\" class=\"hrt map-popup__heart" + (liked ? " is-saved" : "") + "\" " +
                "data-lagen-heart=\"1\" data-save-job=\"" + escapeAttr(v.id) + "\" " +
                "aria-pressed=\"" + (liked ? "true" : "false") + "\" " +
                "aria-label=\"" + escapeAttr(label) + "\">" +
                heartIconHtml(liked) +
            "</button></span>"
        );
    }

    function buildLagenFundaPopupHtml(v, opts) {
        opts = opts || {};
        const mobile = !!opts.mobile;
        const page = Math.max(1, Number(opts.page) || 1);
        const pageCount = Math.max(1, Number(opts.pageCount) || 1);
        const cluster = !!opts.cluster || pageCount > 1;
        const wageOpen = !!opts.wageTipOpen;
        const detailHref = detailHrefForVacancy(v.id);
        const title = escapeHtml(v.title || vacancyFallbackTitle());
        const orgName = escapeHtml(String(v.company || ""));
        const viaPrefix = lagenIsIntermediaryPin(v) ? "via " : "";
        const place = escapeHtml(String(v.place || v.address || ""));
        const hours = escapeHtml(hoursMetaText(v) || (lagenPopupIsEnglish() ? "Hours vary" : "Uren variabel"));
        const contract = escapeHtml(lagenContractText(v));
        const travel = lagenTravelFactText(v);
        const ic = window.jobsyBanenkaartMapIcons;
        const bike = ic && ic.bikeSvg ? ic.bikeSvg.replace("class=\"ic\"", "class=\"ic s\"") : "";
        const nav = cluster
            ? "<div class=\"fnav\" role=\"navigation\" aria-label=\"" + escapeAttr(uiLabels.pagerNav || "Vacatures") + "\">" +
                "<button type=\"button\" class=\"fb\" data-lagen-prev aria-label=\"" + escapeAttr(uiLabels.prevVacancy || "Vorige") + "\">" +
                    lagenChevronLeftSvg() + "</button>" +
                "<span class=\"fcount\" data-lagen-counter aria-live=\"polite\">" +
                    escapeHtml(lagenVacancyCounter(page, pageCount)) + "</span>" +
                "<button type=\"button\" class=\"fb\" data-lagen-next aria-label=\"" + escapeAttr(uiLabels.nextVacancy || "Volgende") + "\">" +
                    lagenChevronRightSvg() + "</button>" +
            "</div>"
            : "";
        const tip = lagenWageTooltipHtml(v, wageOpen);
        const euroBtn = tip
            ? "<button type=\"button\" class=\"fb euro" + (wageOpen ? " on" : "") + "\" data-lagen-wage aria-expanded=\"" +
                (wageOpen ? "true" : "false") + "\" aria-label=\"" +
                escapeAttr(lagenPopupIsEnglish() ? "View hourly wage" : "Uurloon bekijken") + "\">€" + tip + "</button>"
            : "<span class=\"fb euro is-disabled\" aria-hidden=\"true\">€</span>";
        const photoBlock = lagenWorkplacePhotoShell(v);
        const bodyInner = mobile
            ? "<div class=\"fbody\"><div class=\"fbody-media\">" + photoBlock + "</div><div class=\"finfo\">" +
                "<div class=\"flrow\">" + lagenTypeLabelHtml(v) + lagenHeartHtml(v) + "</div>" +
                "<h3>" + title + "</h3>" +
                "<div class=\"femp\">" + viaPrefix + orgName + "</div>" +
                (place ? "<div class=\"fpl\">" + place + "</div>" : "") +
                "<div class=\"ffacts\">" +
                    (travel ? "<span>" + bike + escapeHtml(travel) + "</span>" : "") +
                    "<span>" + lagenClockSvg() + hours + "</span>" +
                "</div>" +
                (contract ? "<div class=\"fcontract\">" + lagenDocSvg() + contract + "</div>" : "") +
                "</div></div>"
            : photoBlock +
                "<div class=\"finfo\"><div class=\"flrow\">" + lagenTypeLabelHtml(v) + lagenHeartHtml(v) + "</div>" +
                "<h3>" + title + "</h3>" +
                "<div class=\"femp\">" + viaPrefix + orgName + "</div>" +
                (place ? "<div class=\"fpl\">" + place + "</div>" : "") +
                "<div class=\"ffacts\">" +
                    (travel ? "<span>" + bike + escapeHtml(travel) + "</span>" : "") +
                    "<span>" + lagenClockSvg() + hours + "</span>" +
                    "<span>" + lagenDocSvg() + contract + "</span>" +
                "</div></div>";
        return (
            "<div class=\"fcw kb-lagen-fcw" + (mobile ? " m" : "") + (wageOpen ? " lagen-wage-open" : "") + "\" data-lagen-fcw=\"1\">" +
                "<div class=\"fctl\">" + nav + "<span class=\"sp\"></span>" + euroBtn +
                "<button type=\"button\" class=\"fb fx\" data-lagen-close aria-label=\"" +
                    escapeAttr(uiLabels.close || "Sluiten") + "\">" + lagenCloseSvg() + "</button></div>" +
                "<div class=\"fc" + (mobile ? " m" : "") + "\">" + bodyInner +
                "<button type=\"button\" class=\"fgo\" data-job-id=\"" + escapeAttr(v.id) +
                    "\"><span>" +
                    (lagenPopupIsEnglish() ? "View vacancy" : "Bekijk vacature") + "</span>" +
                    lagenChevronRightSvg() + "</button></div></div>"
        );
    }

    function buildLagenCatalogPopupHtml(v, sheetMode) {
        return buildLagenFundaPopupHtml(v, {
            mobile: !!sheetMode,
            page: 1,
            pageCount: 1,
            cluster: false,
            wageTipOpen: false
        });
    }

    function buildLagenMobileSheetShell(viewportInner) {
        return "<div class=\"fcwrap kb-lagen-fcwrap\">" + viewportInner + "</div>";
    }

    function buildPopupHtml(v) {
        if (lagenUsesFundaPopup()) {
            return buildLagenCatalogPopupHtml(v, lagenLayout && isNarrowViewport());
        }
        let mediaInner = "";
        let mediaClassFinal = "map-popup__media";
        const photoSrc = v.imageUrl ? String(v.imageUrl) : "";
        const logoSrc = v.logoUrl ? String(v.logoUrl) : "";
        // Work-type SVG placeholders look empty next to a real company logo — prefer the logo.
        const photoIsWorkTypePlaceholder = photoSrc.indexOf("/images/vacancies/") === 0;
        if (photoSrc && !(photoIsWorkTypePlaceholder && logoSrc)) {
            const fb = logoSrc && logoSrc !== photoSrc
                ? " data-fallback-src=\"" + escapeAttr(logoSrc) + "\""
                : "";
            mediaInner +=
                "<img class=\"map-popup__photo\" src=\"" + escapeAttr(photoSrc) +
                "\" alt=\"\" width=\"88\" height=\"88\" loading=\"lazy\" decoding=\"async\" data-logo-fallback=\"photo\"" + fb + " />";
        } else if (logoSrc) {
            mediaClassFinal = "map-popup__media map-popup__media--logo-only";
            mediaInner +=
                "<img class=\"map-popup__media-logo\" src=\"" + escapeAttr(logoSrc) + "\" alt=\"" +
                escapeAttr(v.company) + " logo\" width=\"88\" height=\"88\" loading=\"lazy\" decoding=\"async\" data-logo-fallback=\"photo\" />";
        } else {
            const fallbackPhoto = workTypeFallbackPhoto(v);
            const useIllustrated = illustratedPopup;
            const illustratedFallback = illustratedSpotForVacancy(v);
            const src = useIllustrated && illustratedFallback ? illustratedFallback : fallbackPhoto;
            mediaClassFinal = "map-popup__media map-popup__media--figure map-popup__media--illustrated";
            mediaInner =
                "<img class=\"map-popup__photo map-popup__photo--figure map-popup__photo--illustrated\" src=\"" +
                escapeAttr(src) +
                "\" alt=\"\" width=\"400\" height=\"267\" loading=\"lazy\" decoding=\"async\" />";
        }

        const detailHref = detailHrefForVacancy(v.id);
        const viewLabel = escapeHtml(uiLabels.viewJob || "Bekijk deze baan");

        return (
            "<div class=\"" + popupRootClass("") + "\">" +
                "<div class=\"map-popup__main\">" +
                    "<a class=\"" + mediaClassFinal + "\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                        mediaInner +
                    "</a>" +
                    "<div class=\"map-popup__body\">" +
                        "<a class=\"map-popup__title map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                            escapeHtml(v.title || vacancyFallbackTitle()) +
                        "</a>" +
                        companyLineHtml(v, detailHref) +
                        matchLineHtml(v) +
                        metaRowHtml(v) +
                    "</div>" +
                "</div>" +
                whyLineHtml(v) +
                "<div class=\"map-popup__actions\">" +
                    "<a class=\"map-popup__view map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                        viewLabel + " <span aria-hidden=\"true\">›</span>" +
                    "</a>" +
                    heartButtonHtml(v) +
                "</div>" +
            "</div>"
        );
    }

    function fitBandLabel(band) {
        const b = String(band || "good").toLowerCase();
        if (b === "strong" || b === "green") {
            return String(uiLabels.fitStrong || "Sterke match");
        }
        if (b === "some" || b === "orange" || b === "red") {
            return String(uiLabels.fitSome || "Enige match");
        }
        return String(uiLabels.fitGood || "Goede match");
    }

    function matchLineHtml(v) {
        if (lagenLayout) {
            return "";
        }
        if (v.fitGate === "closed") {
            const href = String(uiLabels.passportHref || "/profiel");
            const label = String(v.fitGateLabel || uiLabels.fitGate || "Maak je paspoort af");
            return (
                "<p class=\"map-popup__match kb-fit kb-fit--gate\">" +
                    "<a class=\"map-popup__gate-link\" href=\"" + escapeAttr(href) + "\">" +
                        escapeHtml(label) +
                    "</a>" +
                "</p>"
            );
        }
        const why = v.fitWhyLine ? String(v.fitWhyLine).trim() : "";
        const hasFit = why || v.matchPercent != null && v.matchPercent !== "";
        if (!hasFit) {
            return "";
        }
        const band = String(v.matchColorBand || "good");
        const text = why
            ? labelFormat(uiLabels.fitPastWhy || "Past bij je: {reason}", { reason: why })
            : fitBandLabel(band);
        return (
            "<p class=\"map-popup__match match-score--" + escapeHtml(band) + "\">" +
                "<span class=\"map-popup__fit-pill kb-fit kb-fit--calm match-score--" + escapeHtml(band) + "\">" +
                    escapeHtml(text) +
                "</span>" +
            "</p>"
        );
    }

    function cultureFitHtml(v) {
        if (!v.cultureFitLabel) {
            return "";
        }
        const band = String(v.cultureFitBand || "mid");
        return (
            "<p class=\"map-popup__culture culture-fit culture-fit--" + escapeHtml(band) + "\">" +
                escapeHtml(String(v.cultureFitLabel)) +
            "</p>"
        );
    }

    function pushBomStatusHtml(v) {
        if (!v.pushBomActive) {
            return "";
        }
        return "<p class=\"map-popup__status\">PushBom actief</p>";
    }

    const CLUSTER_PAGE_SIZE = 1;

    function highlightShuffleRank(id, explicitRank) {
        if (Number.isFinite(explicitRank)) {
            return explicitRank >>> 0;
        }
        let h = highlightSeed >>> 0;
        const s = String(id || "");
        for (let i = 0; i < s.length; i++) {
            h ^= s.charCodeAt(i);
            h = Math.imul(h, 16777619);
        }
        return h >>> 0;
    }

    function clusterJobsFromMarkers(childMarkers) {
        return childMarkers
            .map(function (marker) {
                return marker.options && marker.options.jobData;
            })
            .filter(Boolean)
            .sort(function (a, b) {
                const ah = a.highlighted ? 1 : 0;
                const bh = b.highlighted ? 1 : 0;
                if (bh !== ah) {
                    return bh - ah;
                }
                if (ah) {
                    const ra = highlightShuffleRank(a.id, a.highlightRank);
                    const rb = highlightShuffleRank(b.id, b.highlightRank);
                    if (ra !== rb) {
                        return ra - rb;
                    }
                    return String(a.title || "").localeCompare(String(b.title || ""), "nl");
                }
                return 0;
            });
    }

    function labelFormat(template, vars) {
        let out = String(template || "");
        Object.keys(vars || {}).forEach(function (key) {
            out = out.split("{" + key + "}").join(String(vars[key]));
        });
        return out;
    }

    function clusterPlaceLabel(jobs) {
        for (let i = 0; i < (jobs || []).length; i++) {
            const j = jobs[i];
            const place = (j && (j.place || j.city || "")) || "";
            if (place) {
                return String(place);
            }
            const addr = j && j.address ? String(j.address) : "";
            if (addr) {
                const parts = addr.split(",");
                const last = parts[parts.length - 1].trim();
                if (last) {
                    return last;
                }
            }
        }
        return "";
    }

    function clusterTitleText(total, place) {
        const n = Math.max(0, Number(total) || 0);
        if (companyMapMode) {
            if (n === 1) {
                return place
                    ? labelFormat(uiLabels.vacancyAtPlaceWithPlace || "1 bedrijf in {place}", { place: place })
                    : (uiLabels.vacancyAtPlace || "1 bedrijf");
            }
            return place
                ? labelFormat(uiLabels.vacanciesAtPlaceWithPlace || "{count} bedrijven in {place}", { count: n, place: place })
                : labelFormat(uiLabels.vacanciesAtPlace || "{count} bedrijven", { count: n });
        }
        if (n === 1) {
            return place
                ? labelFormat(uiLabels.vacancyAtPlaceWithPlace || "1 baan in {place}", { place: place })
                : (uiLabels.vacancyAtPlace || "1 baan");
        }
        return place
            ? labelFormat(uiLabels.vacanciesAtPlaceWithPlace, { count: n, place: place })
            : labelFormat(uiLabels.vacanciesAtPlace, { count: n });
    }

    function travelFromHomeText(job) {
        if (!job || job.travelMinutes == null || job.travelMinutes === "") {
            return "";
        }
        const mode = String(job.transportLabel || transportVerb(job.transport || (travelOptions && travelOptions.transport)) || travelFallbackVerb());
        return labelFormat(uiLabels.travelFromHome || "{minutes} min {mode} van huis", {
            minutes: String(job.travelMinutes),
            mode: mode
        });
    }

    function buildClusterDotsHtml(total, current) {
        const n = Math.max(1, Number(total) || 1);
        const cur = Math.min(Math.max(1, Number(current) || 1), n);
        if (n <= 1) {
            return "<div class=\"map-cluster-card__dots\" data-cluster-dots hidden></div>";
        }
        let html = "<div class=\"map-cluster-card__dots\" data-cluster-dots aria-hidden=\"true\">";
        for (let i = 1; i <= n; i++) {
            html += "<i" + (i === cur ? " class=\"is-on\"" : "") + "></i>";
        }
        return html + "</div>";
    }

    function buildClusterChromeHtml(total, place) {
        const title = clusterTitleText(total, place);
        const pagerHidden = total <= 1 ? " hidden" : "";
        return (
            "<div class=\"map-cluster-card\" data-cluster-card>" +
                "<div class=\"map-cluster-card__grab\" aria-hidden=\"true\"></div>" +
                "<div class=\"map-cluster-card__chrome\">" +
                    "<div class=\"map-cluster-card__heading\">" +
                        "<span class=\"map-cluster-card__title\" data-cluster-title>" + escapeHtml(title) + "</span>" +
                        "<p class=\"map-cluster-card__sub\" data-cluster-travel></p>" +
                    "</div>" +
                    "<div class=\"map-cluster-card__pager\" role=\"navigation\" aria-label=\"" +
                        escapeAttr(uiLabels.pagerNav) + "\"" + pagerHidden + ">" +
                        "<button type=\"button\" class=\"map-cluster-card__nav\" data-cluster-prev aria-label=\"" +
                            escapeAttr(uiLabels.prevVacancy) + "\">‹</button>" +
                        "<span class=\"map-cluster-card__counter\" data-cluster-counter aria-live=\"polite\">1 / " +
                            total + "</span>" +
                        "<button type=\"button\" class=\"map-cluster-card__nav\" data-cluster-next aria-label=\"" +
                            escapeAttr(uiLabels.nextVacancy) + "\">›</button>" +
                    "</div>" +
                    "<button type=\"button\" class=\"map-cluster-card__close\" data-cluster-close aria-label=\"" +
                        escapeAttr(uiLabels.close || "Sluiten") + "\">×</button>" +
                "</div>" +
                "<div class=\"map-cluster-card__body\" data-cluster-body>" +
                    "<div class=\"map-cluster-card__viewport\" data-cluster-viewport></div>" +
                    buildClusterDotsHtml(total, 1) +
                "</div>" +
            "</div>"
        );
    }

    /** Pin-data card inside the fixed frame (no shimmer skeleton). */
    function buildClusterPinHtml(job) {
        if (!job) {
            return "<div class=\"map-popup\"><div class=\"map-popup__main\"><div class=\"map-popup__body\">" +
                "<p class=\"map-popup__company\">" + escapeHtml(noVacanciesLabel()) + "</p></div></div></div>";
        }
        if (lagenUsesFundaPopup() && clusterUiState) {
            return buildLagenFundaPopupHtml(job, {
                mobile: isNarrowViewport(),
                page: clusterUiState.page,
                pageCount: clusterUiState.pageCount,
                cluster: clusterUiState.pageCount > 1,
                wageTipOpen: !!clusterUiState.wageTipOpen
            });
        }
        if (job._detailLoaded) {
            return buildPopupHtml(job);
        }
        if (job._unavailable) {
            return unavailablePopupHtml(job.id);
        }
        const pinView = Object.assign({}, job, {
            title: job.title || vacancyFallbackTitle(),
            company: job.company || "",
            address: job.address || "",
            imageUrl: job.imageUrl || null,
            logoUrl: job.logoUrl || null
        });
        return buildPopupHtml(pinView);
    }

    /** @deprecated kept for static guards / single-pin callers — cluster paging never uses this. */
    function buildClusterPagerHtml(current, pageCount) {
        if (pageCount <= 1) {
            return "";
        }
        return (
            "<div class=\"map-popup__pager\" role=\"navigation\" aria-label=\"" + escapeAttr(uiLabels.pagerNav) + "\">" +
                "<button type=\"button\" class=\"map-popup__pager-nav\" data-cluster-page=\"" + (current - 1) + "\"" +
                    (current <= 1 ? " disabled" : "") + " aria-label=\"" + escapeAttr(uiLabels.prevVacancy) + "\">‹</button>" +
                "<span class=\"map-popup__pager-status\">" + current + " / " + pageCount + "</span>" +
                "<button type=\"button\" class=\"map-popup__pager-nav\" data-cluster-page=\"" + (current + 1) + "\"" +
                    (current >= pageCount ? " disabled" : "") + " aria-label=\"" + escapeAttr(uiLabels.nextVacancy) + "\">›</button>" +
            "</div>"
        );
    }

    function buildClusterSingleHtml(childMarkers, page, jobOverride) {
        const jobs = clusterJobsFromMarkers(childMarkers);
        const total = jobs.length;
        const pageCount = Math.max(1, Math.ceil(total / CLUSTER_PAGE_SIZE));
        const current = Math.min(Math.max(1, page || 1), pageCount);
        const job = jobOverride || jobs[current - 1];
        return buildClusterChromeHtml(pageCount, clusterPlaceLabel(jobs)) +
            "<div class=\"map-cluster-card__viewport-legacy\">" + buildClusterPinHtml(job) + "</div>";
    }

    let clusterPageGen = 0;

    function resolveJobFromCache(job) {
        if (!job || !job.id) {
            return Promise.resolve(job);
        }
        const key = String(job.id);
        const cached = detailCache[key];
        if (!cached) {
            return Promise.resolve(job);
        }
        return Promise.resolve(cached).then(function (card) {
            if (!card) {
                return Object.assign({}, job, { _unavailable: true });
            }
            return mapCardToPopup(card, job);
        });
    }

    function prefetchClusterCards(jobs) {
        const ids = (jobs || []).map(function (j) { return j && j.id; }).filter(Boolean);
        const batches = [];
        for (let i = 0; i < ids.length; i += 25) {
            batches.push(ids.slice(i, i + 25));
        }
        return Promise.all(batches.map(function (batch) {
            return fetchVacancyCards(batch);
        }));
    }

    function preloadClusterImages(jobs, fromPage) {
        if (lagenUsesFundaPopup()) {
            const page = Math.max(1, fromPage || 1);
            const next = jobs && jobs[page];
            if (next) {
                prefetchLagenPopupPhoto(next, isNarrowViewport());
            }
            return;
        }
        const start = Math.max(0, (fromPage || 1) - 1);
        for (let i = start; i < Math.min(jobs.length, start + 3); i++) {
            const j = jobs[i];
            if (!j || !j.imageUrl) {
                continue;
            }
            try {
                const img = new Image();
                img.decoding = "async";
                img.src = String(j.imageUrl);
            } catch (e) { }
        }
    }

    function updateClusterChrome(state) {
        if (!state || !state.root) {
            return;
        }
        if (lagenUsesFundaPopup()) {
            const total = state.pageCount;
            const current = state.page;
            const lagenCounter = state.root && state.root.querySelector("[data-lagen-counter]");
            if (lagenCounter) {
                lagenCounter.textContent = lagenVacancyCounter(current, total);
            }
            syncSheetChromeOffset();
            return;
        }
        if (lagenLayout && isNarrowViewport()) {
            syncSheetChromeOffset();
            return;
        }
        const total = state.pageCount;
        const current = state.page;
        const counter = state.root.querySelector("[data-cluster-counter]");
        const prev = state.root.querySelector("[data-cluster-prev]");
        const next = state.root.querySelector("[data-cluster-next]");
        const title = state.root.querySelector("[data-cluster-title]");
        const travel = state.root.querySelector("[data-cluster-travel]");
        const dots = state.root.querySelector("[data-cluster-dots]");
        const pager = state.root.querySelector(".map-cluster-card__pager");
        if (counter) {
            counter.textContent = current + " / " + total;
            counter.setAttribute("aria-label", labelFormat(uiLabels.vacancyOf, {
                current: current,
                total: total
            }));
        }
        if (pager) {
            if (total <= 1) {
                pager.setAttribute("hidden", "");
            } else {
                pager.removeAttribute("hidden");
            }
        }
        if (prev) {
            if (current <= 1) {
                prev.setAttribute("disabled", "");
            } else {
                prev.removeAttribute("disabled");
            }
        }
        if (next) {
            if (current >= total) {
                next.setAttribute("disabled", "");
            } else {
                next.removeAttribute("disabled");
            }
        }
        if (title) {
            title.textContent = clusterTitleText(total, clusterPlaceLabel(state.jobs));
        }
        if (travel) {
            const job = state.jobs && state.jobs[current - 1];
            const text = travelFromHomeText(job);
            travel.textContent = text;
            travel.hidden = !text;
        }
        if (dots) {
            if (total <= 1) {
                dots.setAttribute("hidden", "");
                dots.innerHTML = "";
            } else {
                dots.removeAttribute("hidden");
                let html = "";
                for (let i = 1; i <= total; i++) {
                    html += "<i" + (i === current ? " class=\"is-on\"" : "") + "></i>";
                }
                dots.innerHTML = html;
            }
        }
        syncSheetChromeOffset();
    }

    function syncSheetChromeOffset() {
        if (!map) {
            return;
        }
        const pane = map.getContainer().closest(".map-pane") || map.getContainer();
        if (!pane) {
            return;
        }
        const sheet = pane.querySelector(".fcwrap.kb-lagen-fcwrap, .map-cluster-sheet--lagen, .map-cluster-sheet");
        const h = sheet && sheet.offsetParent !== null ? sheet.offsetHeight : 0;
        pane.style.setProperty("--map-cluster-sheet-h", h + "px");
    }

    function setHeartButtonState(btn, liked) {
        if (!btn) {
            return;
        }
        const on = !!liked;
        btn.classList.toggle("is-saved", on);
        btn.setAttribute("aria-pressed", on ? "true" : "false");
        const label = on ? (uiLabels.saved || "Bewaard") : (uiLabels.save || "Bewaar");
        btn.setAttribute("aria-label", label);
        btn.setAttribute("title", label);
        btn.innerHTML = heartIconHtml(on);
    }

    function readAntiforgeryHeader() {
        var form = document.getElementById("kb-filter-form");
        if (!form || !form.querySelector) {
            return null;
        }
        var input = form.querySelector('input[name="__RequestVerificationToken"]');
        if (!input || !input.value) {
            return null;
        }
        return { RequestVerificationToken: input.value };
    }

    function likeMutationHeaders() {
        var headers = { Accept: "application/json" };
        var af = readAntiforgeryHeader();
        if (af) {
            headers.RequestVerificationToken = af.RequestVerificationToken;
        }
        return headers;
    }

    function fetchLikedStatus(id) {
        if (!id || !uiLabels.canSave) {
            return Promise.resolve(false);
        }
        return fetch("/api/vacancies/" + encodeURIComponent(String(id)) + "/like", {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        }).then(function (res) {
            if (!res.ok) {
                return false;
            }
            return res.json().then(function (body) {
                return !!(body && (body.liked === true || body.Liked === true));
            }).catch(function () { return false; });
        }).catch(function () { return false; });
    }

    function toggleLiked(id, nextLiked) {
        const url = "/api/vacancies/" + encodeURIComponent(String(id)) + "/like";
        return fetch(url, {
            method: nextLiked ? "POST" : "DELETE",
            credentials: "same-origin",
            headers: likeMutationHeaders()
        }).then(function (res) {
            if (res.status === 401 || res.status === 403) {
                window.location.href = "/login?returnUrl=" +
                    encodeURIComponent(banenkaartReturnUrl());
                return null;
            }
            if (!res.ok) {
                throw new Error("like-failed");
            }
            return res.json().then(function (body) {
                return !!(body && (body.liked === true || body.Liked === true));
            });
        });
    }

    function rememberJobLiked(id, liked, childMarkers) {
        if (!id || !clusterUiState || !clusterUiState.jobs) {
            return;
        }
        clusterUiState.jobs.forEach(function (job, idx) {
            if (job && String(job.id) === String(id)) {
                clusterUiState.jobs[idx] = Object.assign({}, job, { liked: !!liked });
            }
        });
        (childMarkers || []).forEach(function (marker) {
            if (marker.options && marker.options.jobData &&
                String(marker.options.jobData.id) === String(id)) {
                marker.options.jobData = Object.assign({}, marker.options.jobData, { liked: !!liked });
            }
        });
    }

    let lagenHeartGuardBound = false;

    function ensureLagenHeartInteractionGuard() {
        if (lagenHeartGuardBound || typeof document === "undefined") {
            return;
        }
        lagenHeartGuardBound = true;
        document.addEventListener("submit", function (ev) {
            const sub = ev && ev.submitter;
            if (sub && sub.closest &&
                sub.closest("[data-lagen-heart], button.map-popup__heart[data-save-job]")) {
                stopEvent(ev);
            }
        }, true);
    }

    function bindSaveJobButtons(root, childMarkers) {
        if (!root) {
            return;
        }
        ensureLagenHeartInteractionGuard();
        root.querySelectorAll("[data-save-job]").forEach(function (btn) {
            if (btn.dataset.boundSave) {
                return;
            }
            btn.dataset.boundSave = "1";
            btn.setAttribute("type", "button");
            const id = btn.getAttribute("data-save-job");
            if (uiLabels.canSave) {
                fetchLikedStatus(id).then(function (liked) {
                    if (!btn.isConnected || btn.dataset.saveBusy === "1") {
                        return;
                    }
                    setHeartButtonState(btn, liked);
                    rememberJobLiked(id, liked, childMarkers);
                });
            }
            function stopHeartMapBubble(ev) {
                stopEvent(ev);
            }
            btn.addEventListener("pointerdown", stopHeartMapBubble, true);
            btn.addEventListener("mousedown", stopHeartMapBubble, true);
            btn.addEventListener("touchstart", stopHeartMapBubble, { capture: true, passive: false });
            btn.addEventListener("click", function (ev) {
                stopHeartMapBubble(ev);
                if (!uiLabels.canSave) {
                    window.location.href = "/login?returnUrl=" +
                        encodeURIComponent(banenkaartReturnUrl());
                    return;
                }
                if (btn.dataset.saveBusy === "1") {
                    return;
                }
                const next = btn.getAttribute("aria-pressed") !== "true";
                btn.dataset.saveBusy = "1";
                setHeartButtonState(btn, next);
                toggleLiked(id, next).then(function (liked) {
                    if (liked == null) {
                        return;
                    }
                    setHeartButtonState(btn, liked);
                    rememberJobLiked(id, liked, childMarkers);
                }).catch(function () {
                    setHeartButtonState(btn, !next);
                }).finally(function () {
                    delete btn.dataset.saveBusy;
                });
            });
        });
    }

    function bindClusterSlideInteractions(root, childMarkers) {
        if (!root) {
            return;
        }
        bindWageInfoInteractions(root);
        bindMatchHelpClicks(root);
        root.querySelectorAll(".map-popup__cta, a.map-popup__media").forEach(function (cta) {
            if (cta.dataset.boundNav) {
                return;
            }
            cta.dataset.boundNav = "1";
            cta.addEventListener("click", function () {
                const id = cta.getAttribute("data-job-id");
                if (id) {
                    notifyOpen(id);
                }
            });
        });
        root.querySelectorAll("[data-retry-card]").forEach(function (btn) {
            if (btn.dataset.boundRetry) {
                return;
            }
            btn.dataset.boundRetry = "1";
            btn.addEventListener("click", function (ev) {
                stopEvent(ev);
                const id = btn.getAttribute("data-retry-card");
                delete detailCache[String(id || "")];
                if (clusterUiState) {
                    const page = clusterUiState.page;
                    fetchVacancyCard(id).then(function () {
                        if (clusterUiState && clusterUiState.page === page) {
                            renderClusterPage(activeClusterPopup, childMarkers, page, 0, true);
                        }
                    });
                }
            });
        });
        bindSaveJobButtons(root, childMarkers);
    }

    function setClusterSlideHtml(viewport, html, direction, replaceInPlace) {
        if (!viewport) {
            return;
        }
        const incoming = document.createElement("div");
        incoming.className = "map-cluster-card__slide is-active";
        incoming.innerHTML = html;

        const current = viewport.querySelector(".map-cluster-card__slide.is-active") ||
            viewport.querySelector(".map-cluster-card__slide");

        if (!current || replaceInPlace || !direction) {
            viewport.innerHTML = "";
            viewport.appendChild(incoming);
            return incoming;
        }

        const leaveClass = direction > 0 ? "is-leave-to-start" : "is-leave-to-end";
        const enterClass = direction > 0 ? "is-enter-from-end" : "is-enter-from-start";
        incoming.className = "map-cluster-card__slide " + enterClass;
        viewport.appendChild(incoming);

        const reduce = prefersReducedMotion();
        const run = function () {
            current.classList.add("is-animating", leaveClass);
            current.classList.remove("is-active");
            incoming.classList.add("is-animating", "is-active");
            incoming.classList.remove(enterClass);
        };

        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () { requestAnimationFrame(run); });
        } else {
            run();
        }

        const cleanup = function () {
            if (current.parentNode === viewport) {
                viewport.removeChild(current);
            }
            incoming.classList.remove("is-animating");
        };
        if (reduce) {
            setTimeout(cleanup, 130);
        } else {
            incoming.addEventListener("transitionend", cleanup, { once: true });
            setTimeout(cleanup, 280);
        }
        return incoming;
    }

    function renderClusterPage(popup, childMarkers, page, direction, replaceInPlace) {
        if (!clusterUiState) {
            return;
        }
        const state = clusterUiState;
        const jobs = state.jobs;
        const pageCount = state.pageCount;
        const current = Math.min(Math.max(1, page || 1), pageCount);
        const job = jobs[current - 1];
        const gen = ++clusterPageGen;
        state.page = current;
        updateClusterChrome(state);

        const paint = function (resolved, inPlace) {
            if (gen !== clusterPageGen || !clusterUiState || clusterUiState !== state) {
                return;
            }
            if (resolved && resolved.id) {
                childMarkers.forEach(function (marker) {
                    if (marker.options && marker.options.jobData &&
                        String(marker.options.jobData.id) === String(resolved.id)) {
                        marker.options.jobData = resolved;
                    }
                });
                jobs[current - 1] = resolved;
            }
            if (lagenUsesFundaPopup()) {
                const mobile = isNarrowViewport();
                const html = buildLagenFundaPopupHtml(resolved || job, {
                    mobile: mobile,
                    page: current,
                    pageCount: pageCount,
                    cluster: pageCount > 1,
                    wageTipOpen: !!state.wageTipOpen
                });
                if (state.root) {
                    const parent = state.root.parentNode;
                    state.root.outerHTML = html;
                    state.root = parent && parent.querySelector("[data-lagen-fcw]");
                    const host = activeClusterPopup && activeClusterPopup.getElement();
                    bindLagenFundaInteractions(host || parent, childMarkers);
                    if (state.root) {
                        loadLagenPopupPhoto(state.root, resolved || job, { mobile: mobile });
                    }
                    const nextJob = jobs[current];
                    if (nextJob) {
                        prefetchLagenPopupPhoto(nextJob, isNarrowViewport());
                    }
                    if (!mobile && activeClusterPopup && !state.docked) {
                        scheduleLagenDesktopPopupFit(activeClusterPopup);
                    }
                }
            } else {
                const html = buildClusterPinHtml(resolved || job);
                const slide = setClusterSlideHtml(state.viewport, html, direction || 0, !!inPlace || !!replaceInPlace);
                bindClusterSlideInteractions(slide || state.viewport, childMarkers);
            }
            preloadClusterImages(jobs, current);
            if (popup && typeof popup.update === "function" && !state.docked) {
                try { popup.update(); } catch (e) { }
            }
        };

        // Prefer synchronous cache hit so paging never awaits.
        const key = job && job.id != null ? String(job.id) : "";
        const cached = key && detailCache[key];
        if (cached && cached.then === undefined) {
            // unlikely — detailCache stores promises
        }
        if (cached) {
            Promise.resolve(cached).then(function (card) {
                if (gen !== clusterPageGen) {
                    return;
                }
                if (card === null) {
                    paint(Object.assign({}, job, { _unavailable: true }), replaceInPlace);
                    return;
                }
                if (card && typeof card === "object" && card.id != null) {
                    paint(mapCardToPopup(card, job), replaceInPlace);
                    return;
                }
                paint(job, true);
            });
            return;
        }

        paint(job, true);
        resolveJobFromCache(job).then(function (resolved) {
            if (gen !== clusterPageGen) {
                return;
            }
            if (resolved && resolved._detailLoaded) {
                paint(resolved, true);
            }
        });
    }

    function bindClusterPopupInteractions(popup, childMarkers) {
        const state = clusterUiState;
        if (!state || !state.root) {
            return;
        }
        const root = state.root;
        if (root.dataset.clusterChromeBound === "1") {
            return;
        }
        root.dataset.clusterChromeBound = "1";

        const prev = root.querySelector("[data-cluster-prev]");
        const next = root.querySelector("[data-cluster-next]");
        const closeBtn = root.querySelector("[data-cluster-close]");
        const viewport = state.viewport;

        function go(delta, focusBtn) {
            const target = state.page + delta;
            if (target < 1 || target > state.pageCount) {
                return;
            }
            renderClusterPage(popup, childMarkers, target, delta, false);
            if (focusBtn && typeof focusBtn.focus === "function") {
                focusBtn.focus();
            }
        }

        if (prev) {
            prev.addEventListener("click", function (ev) {
                stopEvent(ev);
                go(-1, prev);
            });
        }
        if (next) {
            next.addEventListener("click", function (ev) {
                stopEvent(ev);
                go(1, next);
            });
        }
        if (closeBtn) {
            closeBtn.addEventListener("click", function (ev) {
                stopEvent(ev);
                closeActivePopup();
            });
        }

        // Optional swipe on content viewport
        if (viewport) {
            let startX = 0;
            let startY = 0;
            let tracking = false;
            viewport.addEventListener("touchstart", function (ev) {
                if (!ev.touches || ev.touches.length !== 1) {
                    return;
                }
                tracking = true;
                startX = ev.touches[0].clientX;
                startY = ev.touches[0].clientY;
            }, { passive: true });
            viewport.addEventListener("touchend", function (ev) {
                if (!tracking || !ev.changedTouches || !ev.changedTouches.length) {
                    tracking = false;
                    return;
                }
                tracking = false;
                const dx = ev.changedTouches[0].clientX - startX;
                const dy = ev.changedTouches[0].clientY - startY;
                if (Math.abs(dx) < Math.abs(dy) || Math.abs(dx) < viewport.clientWidth * 0.3) {
                    return;
                }
                go(dx < 0 ? 1 : -1, null);
            }, { passive: true });
        }
    }

    function ensureClusterChip(count) {
        const pane = map && map.getContainer() ? map.getContainer().closest(".map-pane") : null;
        if (!pane) {
            return null;
        }
        let chip = pane.querySelector(".map-cluster-chip");
        if (!chip) {
            chip = document.createElement("button");
            chip.type = "button";
            chip.className = "map-cluster-chip";
            chip.addEventListener("click", function (ev) {
                stopEvent(ev);
                closeActivePopup();
            });
            pane.appendChild(chip);
        }
        chip.innerHTML = "<span>" + escapeHtml(labelFormat(uiLabels.vacanciesInView, { count: count })) +
            "</span><span class=\"map-cluster-chip__chevron\" aria-hidden=\"true\">▾</span>";
        chip.hidden = false;
        clusterChipEl = chip;
        return chip;
    }

    function setClusterOpenChrome(open, visibleCount) {
        const pane = map && map.getContainer() ? map.getContainer().closest(".map-pane") : null;
        if (pane) {
            pane.classList.toggle("is-cluster-open", !!open);
        }
        if (open) {
            ensureClusterChip(visibleCount || 0);
        } else if (clusterChipEl) {
            clusterChipEl.hidden = true;
        }
    }

    function clearSelectedCluster() {
        selectedClusterKey = null;
        if (!map) {
            return;
        }
        try {
            if (map.getSource("jobsy-selected-cluster")) {
                map.getSource("jobsy-selected-cluster").setData({
                    type: "FeatureCollection",
                    features: []
                });
            }
        } catch (e) { }
    }

    function ensureSelectedClusterLayers() {
        if (!map) {
            return;
        }
        const sourceId = "jobsy-selected-cluster";
        const haloId = sourceId + "-halo";
        const circleId = sourceId + "-circle";
        const countId = sourceId + "-count";
        try {
            if (!map.getSource(sourceId)) {
                map.addSource(sourceId, {
                    type: "geojson",
                    data: { type: "FeatureCollection", features: [] }
                });
            }
            if (!map.getLayer(haloId)) {
                map.addLayer({
                    id: haloId,
                    type: "circle",
                    source: sourceId,
                    paint: {
                        "circle-color": "#f54a1b",
                        "circle-radius": 25,
                        "circle-opacity": 0.28,
                        "circle-blur": 0.45
                    }
                });
            }
            if (!map.getLayer(circleId)) {
                map.addLayer({
                    id: circleId,
                    type: "circle",
                    source: sourceId,
                    paint: {
                        "circle-color": "#f54a1b",
                        "circle-radius": 18,
                        "circle-stroke-width": 2,
                        "circle-stroke-color": "#ffffff"
                    }
                });
            }
            if (!map.getLayer(countId)) {
                map.addLayer({
                    id: countId,
                    type: "symbol",
                    source: sourceId,
                    layout: {
                        "text-field": ["to-string", ["coalesce", ["get", "count"], 0]],
                        "text-size": 14,
                        "text-font": ["Noto Sans Bold"],
                        "text-allow-overlap": true
                    },
                    paint: { "text-color": "#ffffff" }
                });
            }
        } catch (e) { }
    }

    function setSelectedCluster(lngLat, count) {
        if (!map || !lngLat) {
            return;
        }
        ensureSelectedClusterLayers();
        selectedClusterKey = String(lngLat[0]) + "," + String(lngLat[1]);
        try {
            map.getSource("jobsy-selected-cluster").setData({
                type: "FeatureCollection",
                features: [{
                    type: "Feature",
                    geometry: { type: "Point", coordinates: lngLat },
                    properties: { count: String(count || "") }
                }]
            });
        } catch (e) { }
    }

    function panClusterAboveSheet(lngLat) {
        if (!map || !lngLat || !isNarrowViewport()) {
            return;
        }
        try {
            const p = map.project(lngLat);
            const focus = mapFocusRect();
            const container = map.getContainer().getBoundingClientRect();
            const y = container.top + p.y;
            const x = container.left + p.x;
            let dx = 0;
            let dy = 0;
            if (y > focus.bottom - 24) {
                dy = y - (focus.top + focus.height * 0.45);
            } else if (y < focus.top + 24) {
                dy = y - (focus.top + 40);
            }
            if (x < focus.left + 20) {
                dx = x - (focus.left + 40);
            } else if (x > focus.right - 20) {
                dx = x - (focus.right - 40);
            }
            if (Math.abs(dx) > 1 || Math.abs(dy) > 1) {
                map.panBy([dx, dy], { duration: prefersReducedMotion() ? 0 : 280 });
            }
        } catch (e) { }
    }

    function onClusterEscape(ev) {
        if (ev && ev.key === "Escape") {
            closeActivePopup();
        }
    }

    function bindClusterEscape() {
        if (clusterEscapeBound) {
            return;
        }
        clusterEscapeBound = true;
        document.addEventListener("keydown", onClusterEscape);
    }

    function unbindClusterEscape() {
        if (!clusterEscapeBound) {
            return;
        }
        clusterEscapeBound = false;
        document.removeEventListener("keydown", onClusterEscape);
    }

    function createDockedClusterController(html) {
        const pane = map.getContainer().closest(".map-pane") || map.getContainer();
        const root = document.createElement("div");
        root.className = "map-cluster-sheet";
        root.innerHTML = html;
        pane.appendChild(root);
        clusterSheetEl = root;
        return {
            docked: true,
            getElement: function () { return root; },
            remove: function () {
                if (root.parentNode) {
                    root.parentNode.removeChild(root);
                }
                if (clusterSheetEl === root) {
                    clusterSheetEl = null;
                }
            },
            setHTML: function () { /* chrome is fixed — paging mutates viewport only */ },
            update: function () { },
            setLngLat: function () { },
            on: function () { return this; }
        };
    }

    function createLagenFundaMobileController(html) {
        const host = map.getContainer().closest(".map-stage")
            || map.getContainer().closest(".map-pane")
            || map.getContainer();
        const root = document.createElement("div");
        root.className = "fcwrap kb-lagen-fcwrap";
        root.innerHTML = html;
        host.appendChild(root);
        clusterSheetEl = root;
        return {
            docked: true,
            getElement: function () { return root; },
            remove: function () {
                if (root.parentNode) {
                    root.parentNode.removeChild(root);
                }
                if (clusterSheetEl === root) {
                    clusterSheetEl = null;
                }
            },
            setHTML: function () { },
            update: function () { },
            setLngLat: function () { },
            on: function () { return this; }
        };
    }

    function setLagenWageTipOpen(root, open) {
        if (!root) {
            return;
        }
        const fcw = root.classList && root.classList.contains("fcw")
            ? root
            : root.querySelector("[data-lagen-fcw]");
        if (!fcw) {
            return;
        }
        fcw.classList.toggle("lagen-wage-open", !!open);
        const btn = fcw.querySelector("[data-lagen-wage]");
        const tip = fcw.querySelector(".tip");
        if (btn) {
            btn.classList.toggle("on", !!open);
            btn.setAttribute("aria-expanded", open ? "true" : "false");
        }
        if (tip) {
            if (open) {
                tip.removeAttribute("hidden");
            } else {
                tip.setAttribute("hidden", "");
            }
        }
        if (clusterUiState) {
            clusterUiState.wageTipOpen = !!open;
        }
    }

    function bindLagenFundaInteractions(root, childMarkers) {
        if (!root) {
            return;
        }
        const fcw = root.querySelector("[data-lagen-fcw]") || root;
        const firstBind = fcw.dataset.lagenFundaBound !== "1";
        if (firstBind) {
            fcw.dataset.lagenFundaBound = "1";
            const closeBtn = fcw.querySelector("[data-lagen-close]");
            if (closeBtn) {
                closeBtn.addEventListener("click", function (ev) {
                    stopEvent(ev);
                    closeActivePopup();
                });
            }
            const prev = fcw.querySelector("[data-lagen-prev]");
            const next = fcw.querySelector("[data-lagen-next]");
            if (prev) {
                prev.addEventListener("click", function (ev) {
                    stopEvent(ev);
                    if (!clusterUiState) {
                        return;
                    }
                    renderClusterPage(activeClusterPopup, childMarkers, clusterUiState.page - 1, -1, true);
                });
            }
            if (next) {
                next.addEventListener("click", function (ev) {
                    stopEvent(ev);
                    if (!clusterUiState) {
                        return;
                    }
                    renderClusterPage(activeClusterPopup, childMarkers, clusterUiState.page + 1, 1, true);
                });
            }
            const wageBtn = fcw.querySelector("[data-lagen-wage]");
            if (wageBtn) {
                if (isNarrowViewport()) {
                    wageBtn.addEventListener("click", function (ev) {
                        stopEvent(ev);
                        const open = !fcw.classList.contains("lagen-wage-open");
                        setLagenWageTipOpen(fcw, open);
                    });
                } else {
                    wageBtn.addEventListener("mouseenter", function () {
                        setLagenWageTipOpen(fcw, true);
                    });
                    wageBtn.addEventListener("mouseleave", function () {
                        setLagenWageTipOpen(fcw, false);
                    });
                    wageBtn.addEventListener("focus", function () {
                        setLagenWageTipOpen(fcw, true);
                    });
                    wageBtn.addEventListener("blur", function () {
                        setLagenWageTipOpen(fcw, false);
                    });
                }
            }
            fcw.querySelectorAll(".fgo, .map-popup__cta").forEach(function (cta) {
                if (cta.dataset.bound) {
                    return;
                }
                cta.dataset.bound = "1";
                cta.addEventListener("click", function (ev) {
                    stopEvent(ev);
                    const id = cta.getAttribute("data-job-id");
                    if (id) {
                        writeBanenkaartMapView();
                        notifyOpen(id);
                        window.location.assign(detailHrefForVacancy(id));
                    }
                });
            });
        }
        bindSaveJobButtons(fcw, childMarkers || []);
        const job = clusterUiState && clusterUiState.jobs
            ? clusterUiState.jobs[(clusterUiState.page || 1) - 1]
            : null;
        if (job) {
            loadLagenPopupPhoto(fcw, job, { mobile: isNarrowViewport() });
        }
    }

    function openLagenFundaPopup(childMarkers, lngLat) {
        if (!map || !childMarkers || !childMarkers.length) {
            return;
        }
        closeActivePopup();
        const jobs = clusterJobsFromMarkers(childMarkers);
        const pageCount = Math.max(1, jobs.length);
        const firstJob = jobs[0] || {};
        const ll = lngLat || [childMarkers[0].lng, childMarkers[0].lat];
        const mobile = isNarrowViewport();
        const viewLock = mobile
            ? { zoom: map.getZoom(), center: map.getCenter() }
            : null;
        const html = buildLagenFundaPopupHtml(firstJob, {
            mobile: mobile,
            page: 1,
            pageCount: pageCount,
            cluster: pageCount > 1,
            wageTipOpen: false
        });
        highlight(firstJob.id);
        setSelectedCluster(ll, pageCount);
        setClusterOpenChrome(true, Object.keys(markersById).length);
        bindClusterEscape();
        if (mobile) {
            activeClusterPopup = createLagenFundaMobileController(html);
            clusterUiState = {
                jobs: jobs,
                page: 1,
                pageCount: pageCount,
                childMarkers: childMarkers,
                root: activeClusterPopup.getElement().querySelector("[data-lagen-fcw]") ||
                    activeClusterPopup.getElement(),
                viewport: null,
                docked: true,
                lngLat: ll,
                wageTipOpen: false
            };
            bindLagenFundaInteractions(activeClusterPopup.getElement(), childMarkers);
            panClusterAboveSheet(ll);
            if (viewLock) {
                try {
                    map.jumpTo({
                        center: viewLock.center,
                        zoom: viewLock.zoom,
                        duration: 0
                    });
                } catch (eLock) { }
            }
        } else {
            activeClusterPopup = popupFromOpts({
                className: "maplibregl-popup pop fpop kb-popup-lagen-pop",
                closeButton: false,
                closeOnClick: false,
                maxWidth: "420px",
                offset: 16,
                anchor: "bottom"
            }, ll, html);
            const popRoot = activeClusterPopup.getElement();
            clusterUiState = {
                jobs: jobs,
                page: 1,
                pageCount: pageCount,
                childMarkers: childMarkers,
                root: popRoot && popRoot.querySelector("[data-lagen-fcw]"),
                viewport: null,
                docked: false,
                lngLat: ll,
                wageTipOpen: false
            };
            bindLagenFundaInteractions(popRoot, childMarkers);
            bindLagenDesktopPopupMapSync();
            scheduleLagenDesktopPopupFit(activeClusterPopup, ll);
        }
        writeBanenkaartMapView();
        if (firstJob.id) {
            fetchVacancyCard(firstJob.id).then(function (card) {
                if (!clusterUiState || clusterUiState.childMarkers !== childMarkers || !card) {
                    return;
                }
                const full = mapCardToPopup(card, firstJob);
                jobs[0] = full;
                renderClusterPage(activeClusterPopup, childMarkers, clusterUiState.page || 1, 0, true);
                if (!isNarrowViewport() && activeClusterPopup) {
                    scheduleLagenDesktopPopupFit(activeClusterPopup);
                }
            });
        }
    }

    function eventTargetInsidePopup(ev, popup) {
        if (!ev) {
            return false;
        }
        const target = ev.target || ev.srcElement;
        if (target && target.closest && target.closest(".fcwrap, .kb-lagen-fcw, .map-cluster-sheet, .map-cluster-chip")) {
            return true;
        }
        if (!popup) {
            return false;
        }
        const popupEl = typeof popup.getElement === "function" ? popup.getElement() : null;
        return !!(popupEl && target && popupEl.contains(target));
    }

    function eventTargetInsideWagePopover(ev) {
        const target = ev && (ev.target || ev.srcElement);
        return !!(target && target.closest &&
            target.closest(".map-popup__wage-info, .map-popup__wage-popover"));
    }

    function clearSelectedPinVisual() {
        if (selectedId != null && map && map.getSource(PIN_SOURCE)) {
            try {
                map.setFeatureState({ source: PIN_SOURCE, id: String(selectedId) }, { selected: false });
            } catch (e) { }
        }
        selectedId = null;
        refreshAgencyAreas();
    }

    function closeActivePopup() {
        if (activeClusterPopup) {
            try { activeClusterPopup.remove(); } catch (e) { }
            activeClusterPopup = null;
        }
        if (clusterSheetEl && clusterSheetEl.parentNode) {
            try { clusterSheetEl.parentNode.removeChild(clusterSheetEl); } catch (e) { }
            clusterSheetEl = null;
        }
        clusterUiState = null;
        clearSelectedCluster();
        setClusterOpenChrome(false);
        unbindClusterEscape();
        syncSheetChromeOffset();
        clearSelectedPinVisual();
        if (document.body) {
            document.body.classList.remove("s-popup");
        }
    }

    function closePopupsIfClickOutside(ev) {
        if (!map) {
            return;
        }

        // Cluster tap wins: MapLibre layer click + container capture can race.
        if (lastClusterTapAt && (Date.now() - lastClusterTapAt) < 500) {
            return;
        }

        if (!eventTargetInsideWagePopover(ev)) {
            closeAllWagePopovers(map.getContainer());
        }

        if (activeClusterPopup && eventTargetInsidePopup(ev, activeClusterPopup)) {
            return;
        }

        const target = ev.target || ev.srcElement;
        const onMarkerOrCluster = !!(target && target.closest &&
            target.closest(".job-cluster, .job-marker, .vacancy-detail-marker"));

        if (onMarkerOrCluster) {
            return;
        }

        closeActivePopup();
    }

    function coordKey(lat, lng) {
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            return null;
        }
        return la.toFixed(6) + "," + ln.toFixed(6);
    }

    function markerByLeafId(rawId) {
        if (rawId == null || rawId === "") {
            return null;
        }
        return markersById[rawId] || markersById[String(rawId)] || null;
    }

    function closeWagePopoverIfOutside(ev) {
        if (!eventTargetInsideWagePopover(ev)) {
            closeAllWagePopovers();
        }
    }

    function bindOutsideClickCloser() {
        if (!map || outsideClickCloserBound) {
            return;
        }
        outsideClickCloserBound = true;
        map.getContainer().addEventListener("click", closePopupsIfClickOutside, true);
        document.addEventListener("click", closeWagePopoverIfOutside, true);
    }

    function unbindOutsideClickCloser() {
        document.removeEventListener("click", closeWagePopoverIfOutside, true);
        if (!map || !outsideClickCloserBound) {
            outsideClickCloserBound = false;
            return;
        }
        map.getContainer().removeEventListener("click", closePopupsIfClickOutside, true);
        outsideClickCloserBound = false;
    }

    function notifyOpen(id) {
        writeBanenkaartMapView();
        if (openCallback) {
            try {
                openCallback.invokeMethodAsync("OnMapVacancyOpened", id);
            } catch {
                // ignore disposed circuit
            }
        }
    }

    function notifyMatchExplain(id) {
        if (openCallback) {
            try {
                openCallback.invokeMethodAsync("OnMapMatchExplainRequested", id);
            } catch {
                // ignore disposed circuit
            }
        }
    }

    function bindMatchHelpClicks(root) {
        if (!root || typeof root.querySelectorAll !== "function") {
            return;
        }
        var buttons = root.querySelectorAll(".map-popup__match-help");
        for (var i = 0; i < buttons.length; i++) {
            (function (btn) {
                if (btn.dataset.matchHelpBound === "1") {
                    return;
                }
                btn.dataset.matchHelpBound = "1";
                btn.addEventListener("click", function (ev) {
                    ev.preventDefault();
                    ev.stopPropagation();
                    var id = btn.getAttribute("data-job-id");
                    if (id) {
                        notifyMatchExplain(id);
                    }
                });
            })(buttons[i]);
        }
    }

    function popupFromOpts(opts, lngLat, html) {
        const popup = new maplibregl.Popup(opts)
            .setLngLat(lngLat)
            .setHTML(html)
            .addTo(map);
        popup.on("open", function () {
            try {
                bindMatchHelpClicks(popup.getElement());
            } catch {
                // ignore
            }
        });
        popup.on("close", function () {
            if (activeClusterPopup === popup) {
                activeClusterPopup = null;
                clusterUiState = null;
                clearSelectedCluster();
                setClusterOpenChrome(false);
                unbindClusterEscape();
            }
        });
        // MapLibre may already be open before the listener is attached.
        try {
            bindMatchHelpClicks(popup.getElement());
        } catch {
            // ignore
        }
        return popup;
    }

    function prefersReducedMotion() {
        return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    }

    function mapFocusRect() {
        const container = map.getContainer();
        const mapRect = container.getBoundingClientRect();
        let top = mapRect.top;
        let bottom = mapRect.bottom;
        const pane = container.closest(".map-pane");
        if (pane) {
            const carousel = pane.querySelector(".highlight-carousel--map");
            if (carousel && carousel.offsetParent !== null && !pane.classList.contains("is-cluster-open")) {
                const cr = carousel.getBoundingClientRect();
                if (cr.bottom > top && cr.top < mapRect.bottom) {
                    top = Math.max(top, cr.bottom);
                }
            }
            const chip = pane.querySelector(".map-cluster-chip");
            if (chip && !chip.hidden && chip.offsetParent !== null) {
                const chipRect = chip.getBoundingClientRect();
                if (chipRect.bottom > top) {
                    top = Math.max(top, chipRect.bottom);
                }
            }
            const sheet = pane.querySelector(".map-cluster-sheet");
            if (sheet && sheet.offsetParent !== null) {
                const sr = sheet.getBoundingClientRect();
                if (sr.top < bottom && sr.top > mapRect.top) {
                    bottom = Math.min(bottom, sr.top);
                }
            }
        }
        const pad = 12;
        const left = mapRect.left + pad;
        const right = mapRect.right - pad;
        bottom -= pad;
        top += pad;
        return {
            left: left,
            top: top,
            right: right,
            bottom: bottom,
            width: right - left,
            height: bottom - top
        };
    }

    function centerPopupInView(popup, instant, layoutAttempt) {
        if (!map || !popup) {
            return;
        }
        const el = typeof popup.getElement === "function" ? popup.getElement() : null;
        if (!el) {
            return;
        }
        layoutAttempt = layoutAttempt || 0;

        const run = function () {
            if (!map || activeClusterPopup !== popup) {
                return;
            }
            const pr = el.getBoundingClientRect();
            const vr = mapFocusRect();
            if (pr.width < 8 || pr.height < 8 || vr.width < 32 || vr.height < 32) {
                if (layoutAttempt < 10) {
                    if (typeof requestAnimationFrame === "function") {
                        requestAnimationFrame(function () {
                            centerPopupInView(popup, instant, layoutAttempt + 1);
                        });
                    } else {
                        setTimeout(function () {
                            centerPopupInView(popup, instant, layoutAttempt + 1);
                        }, 16);
                    }
                }
                return;
            }

            let dx;
            let dy;
            const lagenMinimalFit = lagenUsesFundaPopup() && !isNarrowViewport() &&
                el.classList && el.classList.contains("kb-popup-lagen-pop");
            if (lagenMinimalFit) {
                dx = 0;
                dy = 0;
                if (pr.right > vr.right) {
                    dx += pr.right - vr.right;
                }
                if (pr.left < vr.left) {
                    dx += pr.left - vr.left;
                }
                if (pr.bottom > vr.bottom) {
                    dy += pr.bottom - vr.bottom;
                }
                if (pr.top < vr.top) {
                    dy += pr.top - vr.top;
                }
            } else if (pr.width >= vr.width) {
                dx = pr.left - vr.left;
            } else {
                dx = (pr.left + pr.width / 2) - (vr.left + vr.width / 2);
            }
            if (!lagenMinimalFit) {
                if (pr.height >= vr.height) {
                    dy = pr.top - vr.top;
                } else {
                    dy = (pr.top + pr.height / 2) - (vr.top + vr.height / 2);
                }
            }

            if (Math.abs(dx) < 1 && Math.abs(dy) < 1) {
                return;
            }

            var duration = instant || prefersReducedMotion() ? 0 : 320;
            map.panBy([dx, dy], { duration: duration });
        };

        if (instant) {
            run();
            return;
        }
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(run);
            });
        } else {
            setTimeout(run, 0);
        }
    }

    function readStoredBanenkaartMapView() {
        try {
            const raw = sessionStorage.getItem(BK_MAP_VIEW_KEY);
            if (!raw) {
                return null;
            }
            return JSON.parse(raw);
        } catch (eRead) {
            return null;
        }
    }

    function writeBanenkaartMapView() {
        if (!map || !lagenLayout) {
            return;
        }
        const c = map.getCenter();
        const z = map.getZoom();
        if (!c || !Number.isFinite(z)) {
            return;
        }
        const payload = {
            lat: c.lat,
            lng: c.lng,
            zoom: z,
            vacancyId: selectedId != null ? String(selectedId) : null,
            popupPage: clusterUiState && clusterUiState.page ? clusterUiState.page : null,
            popupOpen: !!activeClusterPopup,
            savedAt: Date.now()
        };
        try {
            sessionStorage.setItem(BK_MAP_VIEW_KEY, JSON.stringify(payload));
        } catch (eWrite) { }
    }

    function tryRestoreBanenkaartMapViewOnce() {
        if (banenkaartMapRestoreAttempted || !map || !lagenLayout) {
            return false;
        }
        if (Object.keys(markersById).length < 1) {
            return false;
        }
        const stored = readStoredBanenkaartMapView();
        if (!stored || !Number.isFinite(stored.lat) || !Number.isFinite(stored.lng) || !Number.isFinite(stored.zoom)) {
            banenkaartMapRestoreAttempted = true;
            return false;
        }
        if (Date.now() - (stored.savedAt || 0) > 30 * 60 * 1000) {
            banenkaartMapRestoreAttempted = true;
            return false;
        }
        banenkaartMapRestoreAttempted = true;
        window.__jobsyBkRestoringView = true;
        originHasBeenFramed = true;
        try {
            map.jumpTo({
                center: [stored.lng, stored.lat],
                zoom: stored.zoom,
                duration: 0
            });
        } catch (eJump) { }
        finishOpeningFrame();
        if (stored.vacancyId && markersById[stored.vacancyId]) {
            const rec = markersById[stored.vacancyId];
            if (stored.popupOpen) {
                openVacancyPopup(rec);
                if (clusterUiState && stored.popupPage > 1 && clusterUiState.pageCount >= stored.popupPage) {
                    renderClusterPage(
                        activeClusterPopup,
                        clusterUiState.childMarkers,
                        stored.popupPage,
                        0,
                        true);
                }
            } else {
                highlight(stored.vacancyId);
            }
        }
        setTimeout(function () {
            window.__jobsyBkRestoringView = false;
            if (activeClusterPopup && !isNarrowViewport()) {
                scheduleLagenDesktopPopupFit(activeClusterPopup);
            }
        }, 50);
        return true;
    }

    function agencyRadiusKm(raw) {
        if (!raw) {
            return 0;
        }
        const km = raw.publicMapRadiusKm != null ? raw.publicMapRadiusKm
            : (raw.PublicMapRadiusKm != null ? raw.PublicMapRadiusKm : 0);
        const n = Number(km);
        return n === 5 || n === 10 ? n : (n > 0 ? 2 : 0);
    }

    function isAgencyPin(raw) {
        return agencyRadiusKm(raw) > 0;
    }

    function normalizePin(raw) {
        if (!raw) {
            return null;
        }
        const id = raw.id != null ? String(raw.id) : (raw.Id != null ? String(raw.Id) : "");
        if (!id) {
            return null;
        }
        const lat = Number(raw.lat != null ? raw.lat : raw.Lat);
        const lng = Number(raw.lng != null ? raw.lng : raw.Lng);
        const radiusKm = agencyRadiusKm(raw);
        const colour = raw.categoryColor || raw.colour || raw.Colour || null;
        const matchPercent = raw.matchPercent != null ? raw.matchPercent
            : (raw.MatchPercent != null ? raw.MatchPercent : null);
        const workTypes = Array.isArray(raw.workTypes) ? raw.workTypes : null;
        const workType = raw.workType || (workTypes && workTypes[0]) || "";
        const highlighted = raw.highlighted === true || raw.isHighlighted === true || raw.Highlighted === true;
        const highlightRank = raw.highlightRank != null ? raw.highlightRank
            : (raw.HighlightRank != null ? raw.HighlightRank : undefined);
        const matchColorBand = raw.matchColorBand || raw.MatchColorBand || null;
        const openForInitiative = raw.openForInitiative === true || raw.OpenForInitiative === true;
        const isIntermediary = raw.isIntermediary === true || raw.IsIntermediary === true;
        const kansberoep = raw.kansberoep === true || raw.kansberoep === 1 || raw.Kansberoep === true;
        return Object.assign({}, raw, {
            id: id,
            lat: lat,
            lng: lng,
            kansberoep: kansberoep,
            categoryColor: colour,
            colour: colour,
            publicMapRadiusKm: radiusKm > 0 ? radiusKm : null,
            agency: radiusKm > 0,
            matchPercent: matchPercent,
            workType: workType,
            workTypes: workTypes || (workType ? [workType] : []),
            highlighted: highlighted,
            highlightRank: highlightRank,
            matchColorBand: matchColorBand,
            openForInitiative: openForInitiative,
            isIntermediary: isIntermediary
        });
    }

    function skeletonPopupHtml(pin) {
        // Same structure as buildPopupHtml so the sheet does not jump when filled.
        return (
            "<div class=\"" + popupRootClass("map-popup--loading map-popup--skeleton") + "\" aria-busy=\"true\">" +
                "<div class=\"map-popup__main\">" +
                    "<div class=\"map-popup__media map-popup__media--logo-only map-popup__shimmer\" aria-hidden=\"true\"></div>" +
                    "<div class=\"map-popup__body\">" +
                        "<p class=\"map-popup__title map-popup__shimmer-line\">&nbsp;</p>" +
                        "<p class=\"map-popup__company map-popup__shimmer-line\">&nbsp;</p>" +
                        "<p class=\"map-popup__match map-popup__shimmer-line\">&nbsp;</p>" +
                        "<div class=\"map-popup__meta map-popup__shimmer-line\">&nbsp;</div>" +
                    "</div>" +
                "</div>" +
                "<div class=\"map-popup__actions\">" +
                    "<span class=\"map-popup__view map-popup__shimmer-line\" aria-hidden=\"true\">&nbsp;</span>" +
                    "<span class=\"map-popup__heart map-popup__shimmer-line\" aria-hidden=\"true\">&nbsp;</span>" +
                "</div>" +
            "</div>"
        );
    }

    function unavailablePopupHtml(id) {
        return (
            "<div class=\"map-popup map-popup--calm\">" +
                "<div class=\"map-popup__main\">" +
                    "<div class=\"map-popup__media map-popup__media--empty\" aria-hidden=\"true\">" +
                        "<span class=\"map-popup__media-empty\">" +
                            "<svg viewBox=\"0 0 24 24\" focusable=\"false\"><path fill=\"currentColor\" d=\"M12 1a5 5 0 0 0-5 5v2H6a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V10a2 2 0 0 0-2-2h-1V6a5 5 0 0 0-5-5zm-3 7V6a3 3 0 1 1 6 0v2H9zm3 5a1.75 1.75 0 1 1 0 3.5A1.75 1.75 0 0 1 12 13z\"/></svg>" +
                        "</span>" +
                    "</div>" +
                    "<div class=\"map-popup__body\">" +
                        "<p class=\"map-popup__title\">" + escapeHtml(uiLabels.unavailableTitle) + "</p>" +
                        "<p class=\"map-popup__company\">" + escapeHtml(uiLabels.unavailableHint) + "</p>" +
                    "</div>" +
                "</div>" +
                "<div class=\"map-popup__actions\">" +
                    "<button type=\"button\" class=\"map-popup__view\" data-retry-card=\"" +
                        escapeAttr(String(id || "")) + "\">" + escapeHtml(uiLabels.retry) + "</button>" +
                    "<button type=\"button\" class=\"map-popup__heart\" data-save-job=\"" +
                        escapeAttr(String(id || "")) + "\" aria-label=\"" +
                        escapeAttr(uiLabels.save || "Bewaar") + "\">" + heartIconHtml(false) + "</button>" +
                "</div>" +
            "</div>"
        );
    }

    function mapCardToPopup(card, pin) {
        if (!card) {
            return pin;
        }
        const workTypes = Array.isArray(card.workTypes) ? card.workTypes : [];
        const thumb = card.thumbnailUrl || card.imageUrl || null;
        return Object.assign({}, pin, {
            title: card.title || pin.title || vacancyFallbackTitle(),
            company: card.companyName || pin.company || "",
            companyHref: card.kvkNumber && card.vestigingsnummer
                ? "/" + card.kvkNumber + "/" + card.vestigingsnummer
                : null,
            offeredBy: card.offeredByLabel || null,
            address: card.companyAddress || card.place || "",
            place: card.place || "",
            logoUrl: card.logoUrl || null,
            imageUrl: thumb,
            workTypes: workTypes,
            workType: workTypes[0] || pin.workType || "",
            categoryColor: pin.categoryColor || card.categoryColorHex || null,
            highlighted: card.isHighlighted === true || !!pin.highlighted,
            travelMinutes: card.travelMinutes != null ? card.travelMinutes : pin.travelMinutes,
            matchPercent: card.matchPercent != null ? card.matchPercent : pin.matchPercent,
            matchColorBand: card.matchColorBand || pin.matchColorBand,
            fitGate: card.fitGate || pin.fitGate || null,
            fitWhyLine: card.fitWhyLine || pin.fitWhyLine || null,
            rankLowerReason: card.rankLowerReason || pin.rankLowerReason || null,
            wage: card.hourlyWage != null && card.wageVisible !== false ? card.hourlyWage : null,
            wageLabel: card.wageLabel || card.displayWageLabel || null,
            wageByAge: Array.isArray(card.wageByAge) ? card.wageByAge : (Array.isArray(card.WageByAge) ? card.WageByAge : null),
            wageBands: Array.isArray(card.wageByAge)
                ? card.wageByAge.map(function (b) {
                    return {
                        ageYears: b.ageYears != null ? b.ageYears : b.AgeYears,
                        hourlyRate: b.hourlyRate != null ? b.hourlyRate : b.HourlyRate,
                        label: b.label || b.Label || ""
                    };
                })
                : (pin.wageBands || null),
            minHoursPerWeek: card.minHoursPerWeek != null ? card.minHoursPerWeek : pin.minHoursPerWeek,
            maxHoursPerWeek: card.maxHoursPerWeek != null ? card.maxHoursPerWeek : pin.maxHoursPerWeek,
            summary: card.summary || card.shortDescription || pin.summary || null,
            shortDescription: card.shortDescription || card.summary || pin.shortDescription || null,
            descriptionLine: card.descriptionLine || card.teaser || pin.descriptionLine || null,
            liked: pin.liked === true,
            transport: travelOptions.transport || pin.transport,
            transportLabel: transportVerb(travelOptions.transport) || pin.transportLabel,
            _detailLoaded: true
        });
    }

    /** @deprecated use fetchVacancyCard — kept as alias for tests/guards */
    function fetchVacancyDetail(id) {
        return fetchVacancyCard(id);
    }

    function sleepMs(ms) {
        return new Promise(function (resolve) { setTimeout(resolve, ms); });
    }

    /** Retry-After seconds → ms; default 700, cap 1500. */
    function retryAfterMs(res) {
        const raw = res && res.headers && res.headers.get
            ? res.headers.get("Retry-After")
            : null;
        let ms = 700;
        if (raw != null && String(raw).trim() !== "") {
            const sec = Number(raw);
            if (Number.isFinite(sec) && sec >= 0) {
                ms = Math.round(sec * 1000);
            }
        }
        return Math.max(0, Math.min(1500, ms || 700));
    }

    function isRetryableStatus(status) {
        return status === 429 || status >= 500;
    }

    function fetchJsonWithOneRetry(url) {
        const opts = {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        };
        return fetch(url, opts).then(function (res) {
            if (res.ok) {
                return res.json();
            }
            if (!isRetryableStatus(res.status)) {
                throw new Error("http " + res.status);
            }
            // Keep callers on skeleton while we wait Retry-After, then retry once.
            return sleepMs(retryAfterMs(res)).then(function () {
                return fetch(url, opts).then(function (retryRes) {
                    if (!retryRes.ok) {
                        throw new Error("http " + retryRes.status);
                    }
                    return retryRes.json();
                });
            });
        });
    }

    function cardQueryString() {
        const q = [];
        if (travelOptions.transport) {
            q.push("transport=" + encodeURIComponent(String(travelOptions.transport)));
        }
        if (travelOptions.maxMinutes != null) {
            q.push("maxMinutes=" + encodeURIComponent(String(travelOptions.maxMinutes)));
        }
        if (lastOrigin && Number.isFinite(lastOrigin.lat) && Number.isFinite(lastOrigin.lng)) {
            q.push("originLat=" + encodeURIComponent(String(lastOrigin.lat)));
            q.push("originLng=" + encodeURIComponent(String(lastOrigin.lng)));
        }
        return q.length ? "?" + q.join("&") : "";
    }

    function fetchVacancyCard(id) {
        const key = String(id || "");
        if (!key) {
            return Promise.resolve(null);
        }
        if (detailCache[key]) {
            return detailCache[key];
        }
        detailCache[key] = fetchJsonWithOneRetry(
            "/api/vacancies/" + encodeURIComponent(key) + "/card" + cardQueryString()
        )
            .then(function (card) {
                return card;
            })
            .catch(function () {
                // Do not cache failures — retry must hit the network again.
                delete detailCache[key];
                return null;
            });
        return detailCache[key];
    }

    function fetchVacancyCards(ids) {
        const list = (ids || []).map(function (id) { return String(id || ""); }).filter(Boolean);
        if (list.length === 0) {
            return Promise.resolve([]);
        }
        const missing = list.filter(function (id) { return !detailCache[id]; });
        const batch = missing.length === 0
            ? Promise.resolve([])
            : fetchJsonWithOneRetry(
                "/api/vacancies/cards?ids=" + missing.map(encodeURIComponent).join(",") + cardQueryString()
            )
                .then(function (rows) {
                    (Array.isArray(rows) ? rows : []).forEach(function (card) {
                        if (card && card.id != null) {
                            detailCache[String(card.id)] = Promise.resolve(card);
                        }
                    });
                    missing.forEach(function (id) {
                        if (!detailCache[id]) {
                            detailCache[id] = Promise.resolve(null);
                        }
                    });
                })
                .catch(function () {
                    missing.forEach(function (id) { delete detailCache[id]; });
                });
        return batch.then(function () {
            return Promise.all(list.map(function (id) {
                return Promise.resolve(detailCache[id] || null);
            }));
        });
    }

    let pinsAbort = null;
    let pinsEtag = null;
    /** Last successful pins JSON body — redraw from this on HTTP 304. */
    let pinsCachedPayload = null;
    let pinsReloadTimer = null;
    /** How many MapLibre instances this module created (boot + Blazor should stay at 1). */
    let mapCreateCount = 0;
    /** In-memory pins by filter state (transport stripped — rings are client-side). */
    const pinsByFilterKey = Object.create(null);
    let pinsNetworkFetchCount = 0;
    let lastPinsSignature = "";
    let lastIsoModeNotified = "";
    let lastRingDrawKey = "";

    function stripViewportParams(url) {
        return String(url || "")
            .replace(/([?&])(minLat|minLng|maxLat|maxLng|zoom)=[^&]*/gi, "$1")
            .replace(/[?&]+$/g, "")
            .replace(/\?&/g, "?");
    }

    function readViewportBounds() {
        if (!map || typeof map.getZoom !== "function") {
            return null;
        }
        var z = Math.round(map.getZoom() * 10) / 10;
        var minLat;
        var minLng;
        var maxLat;
        var maxLng;
        try {
            if (typeof map.unproject === "function" && typeof map.getContainer === "function") {
                var host = map.getContainer();
                var w = host && host.clientWidth ? host.clientWidth : 0;
                var h = host && host.clientHeight ? host.clientHeight : 0;
                if (w > 0 && h > 0) {
                    var nw = map.unproject([0, 0]);
                    var se = map.unproject([w, h]);
                    minLat = Math.min(nw.lat, se.lat);
                    maxLat = Math.max(nw.lat, se.lat);
                    minLng = Math.min(nw.lng, se.lng);
                    maxLng = Math.max(nw.lng, se.lng);
                }
            }
            if (!Number.isFinite(minLat) && typeof map.getBounds === "function") {
                var b = map.getBounds();
                var sw = b.getSouthWest();
                var ne = b.getNorthEast();
                minLat = sw.lat;
                minLng = sw.lng;
                maxLat = ne.lat;
                maxLng = ne.lng;
            }
        } catch (eBounds) {
            return null;
        }
        if (!Number.isFinite(minLat) || !Number.isFinite(minLng)
            || !Number.isFinite(maxLat) || !Number.isFinite(maxLng)) {
            return null;
        }
        return {
            minLat: minLat,
            minLng: minLng,
            maxLat: maxLat,
            maxLng: maxLng,
            zoom: z
        };
    }

    function viewportQueryString() {
        // Lagen: keep the full travel-radius pin set; list/carousel use discover viewport, not /pins bbox.
        if (lagenLayout) {
            return "";
        }
        var box = readViewportBounds();
        if (!box) {
            return "";
        }
        return "&minLat=" + encodeURIComponent(String(box.minLat))
            + "&minLng=" + encodeURIComponent(String(box.minLng))
            + "&maxLat=" + encodeURIComponent(String(box.maxLat))
            + "&maxLng=" + encodeURIComponent(String(box.maxLng))
            + "&zoom=" + encodeURIComponent(String(Math.round(box.zoom)));
    }

    /** Filter cache key: filter state only (no transport, no viewport bbox). */
    function pinsFilterKey(url) {
        if (!url) {
            return "";
        }
        try {
            const u = new URL(url, window.location.origin);
            u.searchParams.delete("transport");
            u.searchParams.delete("minLat");
            u.searchParams.delete("minLng");
            u.searchParams.delete("maxLat");
            u.searchParams.delete("maxLng");
            u.searchParams.delete("zoom");
            const keys = Array.prototype.slice.call(u.searchParams.keys()).sort();
            const parts = [];
            for (let i = 0; i < keys.length; i++) {
                const k = keys[i];
                const vals = u.searchParams.getAll(k).slice().sort();
                for (let j = 0; j < vals.length; j++) {
                    parts.push(k + "=" + vals[j]);
                }
            }
            return u.pathname + "?" + parts.join("&");
        } catch (e) {
            return String(url).replace(/([?&])transport=[^&]*/gi, "$1").replace(/[?&]$/, "");
        }
    }

    function rememberPinsFilter(url, payload, etag) {
        const key = pinsFilterKey(url);
        if (!key) {
            return;
        }
        const pins = (Array.isArray(payload) ? payload : []).map(normalizePin).filter(Boolean);
        pinsByFilterKey[key] = {
            payload: payload,
            pins: pins,
            etag: etag || null
        };
    }

    function clearPinsFetchState() {
        pinsUrl = null;
        pinsBaseUrl = null;
        try {
            window.__jobsyPinsPrefetch = null;
            window.__jobsyPinsPrefetchUrl = null;
        } catch (eClear) { }
    }

    function fetchPins(url) {
        if (catalogModeActive) {
            return Promise.resolve([]);
        }
        if (!url) {
            return Promise.resolve([]);
        }
        pinsBaseUrl = stripViewportParams(String(url));
        const viewportQs = viewportQueryString();
        pinsUrl = pinsBaseUrl + viewportQs;
        const filterKey = pinsFilterKey(pinsUrl);
        const cached = !viewportQs && filterKey ? pinsByFilterKey[filterKey] : null;
        if (cached && cached.pins) {
            // Transport-only changes reuse the same pin set; travel rings update separately.
            pinsCachedPayload = cached.payload;
            if (cached.etag) {
                pinsEtag = cached.etag;
            }
            setVacancies(cached.pins);
            return Promise.resolve(cached.pins);
        }

        const gen = ++pinsFetchGen;
        if (pinsAbort) {
            try { pinsAbort.abort(); } catch (e) { }
        }
        pinsAbort = typeof AbortController !== "undefined" ? new AbortController() : null;
        const headers = { Accept: "application/json" };
        if (pinsEtag) {
            headers["If-None-Match"] = pinsEtag;
        }
        pinsNetworkFetchCount += 1;
        return fetch(pinsUrl, {
            credentials: "same-origin",
            headers: headers,
            signal: pinsAbort ? pinsAbort.signal : undefined
        })
            .then(function (res) {
                if (gen !== pinsFetchGen) {
                    return null;
                }
                if (res.status === 304) {
                    // Server says unchanged — redraw from the payload we kept with the ETag.
                    return pinsCachedPayload;
                }
                if (!res.ok) {
                    throw new Error("pins " + res.status);
                }
                const etag = res.headers && res.headers.get ? res.headers.get("ETag") : null;
                if (etag) {
                    pinsEtag = etag;
                }
                return res.json().then(function (body) {
                    pinsCachedPayload = body;
                    rememberPinsFilter(pinsUrl, body, etag);
                    return body;
                });
            })
            .then(function (data) {
                if (gen !== pinsFetchGen || data == null) {
                    return;
                }
                const pins = (Array.isArray(data) ? data : []).map(normalizePin).filter(Boolean);
                if (!pinsByFilterKey[filterKey]) {
                    rememberPinsFilter(pinsUrl, data, pinsEtag);
                }
                if (lagenLayout && pins.length === 0 && Object.keys(markersById).length > 0) {
                    return;
                }
                setVacancies(pins);
            })
            .catch(function (err) {
                if (err && err.name === "AbortError") {
                    return;
                }
                /* keep boot/circuit pins */
            });
    }

    function bindSinglePopupEl(popup, full) {
        const el = popup && popup.getElement();
        if (!el) {
            return;
        }
        bindWageInfoInteractions(el);
        el.querySelectorAll(".map-popup__cta, a.map-popup__media").forEach(function (cta) {
            if (cta.dataset.bound) {
                return;
            }
            cta.dataset.bound = "1";
            cta.addEventListener("click", function () {
                notifyOpen(full.id);
            });
        });
        el.querySelectorAll("[data-retry-card]").forEach(function (btn) {
            if (btn.dataset.boundRetry) {
                return;
            }
            btn.dataset.boundRetry = "1";
            btn.addEventListener("click", function (ev) {
                stopEvent(ev);
                delete detailCache[String(full.id || "")];
                if (recordForRetry) {
                    openVacancyPopup(recordForRetry);
                }
            });
        });
        bindSaveJobButtons(el, full ? [full] : []);
        centerPopupInView(popup);
    }

    let recordForRetry = null;

    function afterFirstPaint(fn) {
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(fn);
            });
        } else {
            setTimeout(fn, 0);
        }
    }

    function openVacancyPopup(record) {
        if (!map || !record) {
            return;
        }
        if (lagenUsesFundaPopup()) {
            openLagenFundaPopup([record], [record.lng, record.lat]);
            return;
        }
        openClusterList([record], [record.lng, record.lat]);
    }

    function openClusterList(childMarkers, lngLat) {
        if (!map || !childMarkers || childMarkers.length === 0) {
            return;
        }
        if (lagenUsesFundaPopup()) {
            openLagenFundaPopup(childMarkers, lngLat);
            return;
        }

        closeActivePopup();

        const jobs = clusterJobsFromMarkers(childMarkers);
        const pageCount = Math.max(1, jobs.length);
        const firstJob = jobs[0];
        const ll = lngLat || [childMarkers[0].lng, childMarkers[0].lat];
        // Fixed-size chrome + skeleton card so the sheet appears on the same tap frame.
        const chromeHtml = buildClusterChromeHtml(pageCount, clusterPlaceLabel(jobs));
        const skeletonCard = skeletonPopupHtml(firstJob || {});
        let sheetHtml;
        if (lagenLayout && isNarrowViewport()) {
            sheetHtml = buildLagenMobileSheetShell(skeletonCard);
        } else {
            sheetHtml = chromeHtml.replace(
                "data-cluster-viewport\"></div>",
                "data-cluster-viewport\">" + skeletonCard + "</div>");
        }
        const visibleCount = Object.keys(markersById).length;

        setSelectedCluster(ll, pageCount);
        setClusterOpenChrome(true, visibleCount);
        bindClusterEscape();

        // Always docked (pin + cluster) — file 03 one docked popup.
        activeClusterPopup = createDockedClusterController(sheetHtml);
        try {
            const el = activeClusterPopup.getElement();
            if (el && el.classList) {
                el.classList.add("map-popup--docked");
            }
        } catch (e) { }

        const root = activeClusterPopup.getElement();
        const cardRoot = root.querySelector("[data-cluster-card]") || root;
        clusterUiState = {
            jobs: jobs,
            page: 1,
            pageCount: pageCount,
            childMarkers: childMarkers,
            root: cardRoot,
            viewport: cardRoot.querySelector("[data-cluster-viewport]"),
            docked: true,
            lngLat: ll
        };

        const gen = ++clusterPageGen;

        // Fill content + start fly/ease only after the skeleton has painted.
        afterFirstPaint(function () {
            if (!clusterUiState || clusterUiState.childMarkers !== childMarkers) {
                return;
            }
            bindClusterPopupInteractions(activeClusterPopup, childMarkers);
            updateClusterChrome(clusterUiState);
            renderClusterPage(activeClusterPopup, childMarkers, 1, 0, true);
            panClusterAboveSheet(ll);

            prefetchClusterCards(jobs).then(function () {
                if (!clusterUiState || clusterUiState.childMarkers !== childMarkers) {
                    return;
                }
                updateClusterChrome(clusterUiState);
                renderClusterPage(activeClusterPopup, childMarkers, clusterUiState.page, 0, true);
            });

            if (firstJob && firstJob.id) {
                fetchVacancyCard(firstJob.id).then(function (card) {
                    if (!clusterUiState || clusterUiState.childMarkers !== childMarkers) {
                        return;
                    }
                    if (card && clusterUiState.page === 1) {
                        const full = mapCardToPopup(card, firstJob);
                        jobs[0] = full;
                        childMarkers.forEach(function (marker) {
                            if (marker.options && marker.options.jobData &&
                                String(marker.options.jobData.id) === String(firstJob.id)) {
                                marker.options.jobData = full;
                            }
                        });
                        renderClusterPage(activeClusterPopup, childMarkers, 1, 0, true);
                    }
                });
            }
        });
    }

    function ringMinutes(maxMinutes) {
        const max = Math.max(5, Number(maxMinutes) || 30);
        // Always show 3 areas. For a chosen max ≤30, show 10/20/30.
        if (max <= 30) {
            return [10, 20, 30];
        }
        const step = max <= 60 ? 15 : Math.max(10, Math.round(max / 3 / 5) * 5);
        const rings = [];
        for (let m = step; m < max; m += step) {
            rings.push(m);
        }
        rings.push(max);
        return rings.slice(-3);
    }

    function chosenRingMinutes() {
        return Math.max(5, Number(travelOptions.maxMinutes) || 30);
    }

    function readLagenLayoutFromOptions(options) {
        if (!options) {
            return false;
        }
        if (options.lagenLayout === true) {
            return true;
        }
        const mi = options.mapInit;
        return !!(mi && mi.lagenLayout === true);
    }

    function lagenTravelRingMinutes() {
        return [10, 20, 30];
    }

    function travelRingLineDashPaint() {
        if (lagenLayout) {
            return [
                "match", ["coalesce", ["get", "minutes"], 0],
                10, ["literal", [1, 0]],
                20, ["literal", [3, 1.6]],
                30, ["literal", [0.6, 1.4]],
                ["literal", [1, 0]]
            ];
        }
        return [
            "case",
            [">=", ["coalesce", ["get", "minutes"], 0], 30],
            ["literal", [2, 2]],
            ["literal", [1, 0]]
        ];
    }

    function metersPerMinute(transport) {
        const mode = canonicalTransport(transport);
        const cruise = CRUISE_KM_H[mode] || CRUISE_KM_H.Fiets;
        const circuity = ROAD_CIRCUITY[mode] || ROAD_CIRCUITY.Fiets;
        return (cruise * 1000 / 60) / circuity;
    }

    function clearTravelRingLabels() {
        travelRingLayers.forEach(function (layer) {
            if (layer && typeof layer.remove === "function") {
                layer.remove();
            }
        });
        travelRingLayers = [];
        (isochroneLabelMarkers || []).forEach(function (m) {
            try { if (m && typeof m.remove === "function") m.remove(); } catch (e) { }
        });
        isochroneLabelMarkers = [];
    }

    function clearTravelRings() {
        if (map && travelRingGeo) {
            try {
                travelRingGeo.ids.forEach(function (id) {
                    if (map.getLayer(id)) {
                        map.removeLayer(id);
                    }
                });
                if (map.getSource(travelRingGeo.sourceId)) {
                    map.removeSource(travelRingGeo.sourceId);
                }
            } catch (e) { }
        }
        travelRingGeo = null;
        lastRingDrawKey = "";
        clearTravelRingLabels();
    }

    function travelRingSourceReady() {
        return !!(map && typeof map.getSource === "function" && map.getSource("jobsy-travel-rings"));
    }

    function bindTravelRingStyleGuard() {
        if (!map || ringStyleHandlerBound) {
            return;
        }
        ringStyleHandlerBound = true;
        // Only restore rings when the style is replaced — not on every idle frame.
        map.on("styledata", onTravelRingStyleData);
    }

    function onTravelRingStyleData() {
        if (!lastOrigin || !map) {
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            return;
        }
        if (travelRingSourceReady()) {
            return;
        }
        drawTravelRings(lastOrigin.lat, lastOrigin.lng);
    }

    function maxRingRadiusMeters() {
        const speed = metersPerMinute(travelOptions.transport || "Fiets");
        const maxMin = lagenLayout
            ? Math.max(30, Number(travelOptions.maxMinutes) || 30)
            : (Number(travelOptions.maxMinutes) || 30);
        const travelMeters = maxMin * speed;
        const radiusKm = Number(travelOptions.radiusKm);
        if (Number.isFinite(radiusKm) && radiusKm > 0) {
            return Math.min(travelMeters, radiusKm * 1000);
        }
        return travelMeters;
    }

    function ringRadiusForMinutes(mins) {
        const speed = metersPerMinute(travelOptions.transport || "Fiets");
        const uncapped = mins * speed;
        const cap = maxRingRadiusMeters();
        return Math.min(uncapped, cap);
    }

    function destinationLngLat(lat, lng, distanceM, bearingDeg) {
        const R = 6378137;
        const br = bearingDeg * Math.PI / 180;
        const lat1 = lat * Math.PI / 180;
        const lng1 = lng * Math.PI / 180;
        const ang = distanceM / R;
        const lat2 = Math.asin(Math.sin(lat1) * Math.cos(ang) + Math.cos(lat1) * Math.sin(ang) * Math.cos(br));
        const lng2 = lng1 + Math.atan2(
            Math.sin(br) * Math.sin(ang) * Math.cos(lat1),
            Math.cos(ang) - Math.sin(lat1) * Math.sin(lat2)
        );
        return [lng2 * 180 / Math.PI, lat2 * 180 / Math.PI];
    }

    function circlePolygon(lat, lng, radiusM) {
        const coords = [];
        for (let i = 0; i <= 64; i++) {
            coords.push(destinationLngLat(lat, lng, radiusM, i * (360 / 64)));
        }
        return [coords];
    }

    function scheduleTravelRingRedraw() {
        if (!map || ringRedrawBound || ringRedrawTries >= 12) {
            return;
        }
        ringRedrawBound = true;
        ringRedrawTries += 1;
        map.once("idle", function () {
            ringRedrawBound = false;
            if (lastOrigin) {
                drawTravelRings(lastOrigin.lat, lastOrigin.lng);
            }
        });
    }

    function eachTravelRing(lat, lng, fn) {
        if (lagenLayout) {
            const chosen = chosenRingMinutes();
            lagenTravelRingMinutes().forEach(function (mins, index) {
                const radius = ringRadiusForMinutes(mins);
                if (radius >= 40) {
                    fn(mins, index, radius, mins === chosen);
                }
            });
            return;
        }
        const mins = chosenRingMinutes();
        const radius = ringRadiusForMinutes(mins);
        if (radius >= 40) {
            fn(mins, 0, radius, true);
        }
    }

    function buildTravelRingFeatures(lat, lng) {
        const features = [];
        const chosen = chosenRingMinutes();
        eachTravelRing(lat, lng, function (mins, index, radius, isChosen) {
            features.push({
                type: "Feature",
                properties: {
                    index: index,
                    minutes: mins,
                    chosen: isChosen || mins === chosen ? 1 : 0,
                    outer: isChosen ? 1 : 0
                },
                geometry: { type: "Polygon", coordinates: circlePolygon(lat, lng, radius) }
            });
        });
        // Largest first so fills stack darkest inside when sorted by paint.
        features.sort(function (a, b) {
            return (b.properties.minutes || 0) - (a.properties.minutes || 0);
        });
        return features;
    }

    function isochroneCacheKey(lat, lng, mode, minutes) {
        return lat.toFixed(3) + "|" + lng.toFixed(3) + "|" + mode + "|" + minutes.join(",");
    }

    function fetchIsochrones(lat, lng) {
        const mode = canonicalTransport(travelOptions.transport || "Fiets");
        const minutes = lagenLayout ? lagenTravelRingMinutes() : [chosenRingMinutes()];
        const key = isochroneCacheKey(lat, lng, mode, minutes);
        if (isochroneCache[key]) {
            return Promise.resolve(isochroneCache[key]);
        }
        // OV: no transit isochrones on the public Valhalla instance.
        if (mode === "OV") {
            isochroneCache[key] = Promise.resolve(null);
            return isochroneCache[key];
        }
        const url = "/api/travel/isochrones?lat=" + encodeURIComponent(lat) +
            "&lng=" + encodeURIComponent(lng) +
            "&mode=" + encodeURIComponent(mode) +
            "&minutes=" + encodeURIComponent(minutes.join(","));
        isochroneCache[key] = fetch(url, {
            credentials: "same-origin",
            headers: { Accept: "application/geo+json, application/json" }
        }).then(function (res) {
            if (!res.ok) {
                return null;
            }
            return res.json();
        }).then(function (fc) {
            if (!fc || !Array.isArray(fc.features) || !fc.features.length) {
                return null;
            }
            return fc;
        }).catch(function () {
            return null;
        });
        return isochroneCache[key];
    }

    function featuresFromIsochroneFc(fc) {
        const chosen = chosenRingMinutes();
        const features = (fc.features || []).map(function (f, index) {
            const mins = Number((f.properties && (f.properties.minutes != null
                ? f.properties.minutes
                : f.properties.contour)) || 0);
            return {
                type: "Feature",
                properties: {
                    index: index,
                    minutes: mins,
                    chosen: mins === chosen ? 1 : 0,
                    outer: mins === chosen ? 1 : 0
                },
                geometry: f.geometry
            };
        }).filter(function (f) {
            return f.geometry && f.properties.minutes > 0;
        });
        features.sort(function (a, b) {
            return (b.properties.minutes || 0) - (a.properties.minutes || 0);
        });
        return features;
    }

    function transportIconChar(transport) {
        const t = canonicalTransport(transport);
        if (t === "Auto") return "🚗";
        if (t === "Lopend") return "🚶";
        if (t === "OV") return "🚌";
        return "🚲";
    }

    function labelBearingForMinutes(mins, index) {
        if (lagenLayout) {
            if (mins <= 10) return 180;
            if (mins <= 20) return 135;
            return 315;
        }
        // Mockup bearings ≈ 30° (10), 190° (20), 160° (30).
        if (mins <= 10) return 30;
        if (mins <= 20) return 190;
        if (index === 0) return 160;
        return 125;
    }

    function pointOnRingEdge(lat, lng, mins, index, feature) {
        const bearing = labelBearingForMinutes(mins, index);
        if (lagenLayout) {
            return destinationLngLat(lat, lng, ringRadiusForMinutes(mins), bearing);
        }
        if (feature && feature.geometry && feature.geometry.type === "Polygon") {
            const ring = feature.geometry.coordinates && feature.geometry.coordinates[0];
            if (ring && ring.length > 8) {
                // Pick the vertex closest to the preferred bearing from origin.
                let best = null;
                let bestDiff = 1e9;
                for (let i = 0; i < ring.length; i++) {
                    const c = ring[i];
                    const dLng = (c[0] - lng) * Math.cos(lat * Math.PI / 180);
                    var dLat = c[1] - lat;
                    var ang = Math.atan2(dLng, dLat) * 180 / Math.PI;
                    if (ang < 0) ang += 360;
                    var diff = Math.abs(ang - bearing);
                    if (diff > 180) diff = 360 - diff;
                    if (diff < bestDiff) {
                        bestDiff = diff;
                        best = c;
                    }
                }
                if (best) {
                    return best;
                }
            }
        }
        return destinationLngLat(lat, lng, ringRadiusForMinutes(mins), bearing);
    }

    function placeTravelRingLabels(lat, lng, features) {
        clearTravelRingLabels();
        if (!map || typeof maplibregl === "undefined" || !maplibregl.Marker) {
            return;
        }
        const transport = canonicalTransport(travelOptions.transport || "Fiets");
        const labelVerb = transportVerb(transport) || travelFallbackVerb();
        const chosen = chosenRingMinutes();
        const list = features || buildTravelRingFeatures(lat, lng);
        list.forEach(function (feature, index) {
            const mins = Number(feature.properties && feature.properties.minutes) || 0;
            if (!mins) {
                return;
            }
            const isChosen = !!feature.properties.chosen || mins === chosen;
            const ll = pointOnRingEdge(lat, lng, mins, index, feature);
            const el = document.createElement("div");
            const labelText = mins + " min " + labelVerb;
            if (lagenLayout) {
                const ic = window.jobsyBanenkaartMapIcons;
                const bike = ic && ic.bikeSvg ? ic.bikeSvg : "";
                el.className = "map-iso-label kb-rl";
                el.setAttribute("title", labelText);
                el.setAttribute("aria-label", labelText);
                el.innerHTML = bike + "<span>" + escapeHtml(String(mins)) + " min</span>";
            } else {
                el.className = "map-iso-label" + (isChosen ? " map-iso-label--chosen" : "");
                el.setAttribute("title", labelText);
                el.setAttribute("aria-label", labelText);
                if (isChosen) {
                    el.innerHTML = "<span class=\"map-iso-label__icon\" aria-hidden=\"true\">" +
                        transportIconChar(transport) + "</span>" + mins + " min";
                } else {
                    el.textContent = mins + " min";
                }
            }
            let anchor = "center";
            if (lagenLayout) {
                if (mins <= 10) {
                    anchor = "bottom";
                } else if (mins <= 20) {
                    anchor = "bottom-right";
                } else {
                    anchor = "top-left";
                }
            }
            try {
                const marker = new maplibregl.Marker({ element: el, anchor: anchor })
                    .setLngLat(ll)
                    .addTo(map);
                isochroneLabelMarkers.push(marker);
            } catch (e) { }
        });
        if (!lagenLayout) {
            clampIsoLabelMarkersToMap();
        }
    }

    function clampIsoLabelMarkersToMap() {
        if (lagenLayout || !map || !isochroneLabelMarkers.length) {
            return;
        }
        const container = map.getContainer();
        if (!container) {
            return;
        }
        const cr = container.getBoundingClientRect();
        const padBottom = 88;
        isochroneLabelMarkers.forEach(function (marker) {
            const el = marker.getElement();
            if (!el) {
                return;
            }
            let r = el.getBoundingClientRect();
            let dx = 0;
            let dy = 0;
            if (r.bottom > cr.bottom - padBottom) {
                dy = r.bottom - (cr.bottom - padBottom);
            }
            if (r.top < cr.top + 8) {
                dy = r.top - (cr.top + 8);
            }
            if (r.right > cr.right - 12) {
                dx = r.right - (cr.right - 12);
            }
            if (r.left < cr.left + 12) {
                dx = r.left - (cr.left + 12);
            }
            if (!dx && !dy) {
                return;
            }
            const pt = map.project(marker.getLngLat());
            marker.setLngLat(map.unproject([pt.x - dx, pt.y - dy]));
        });
    }

    function readCssToken(name, fallback) {
        try {
            const v = getComputedStyle(document.documentElement).getPropertyValue(name);
            if (v && v.trim()) {
                return v.trim();
            }
        } catch (e) { }
        return fallback;
    }

    function ringBrandColors() {
        if (lagenLayout) {
            return { fill: "#1F2F55", line: "#1F2F55" };
        }
        const brand = readCssToken("--brand", "#0f2d5c");
        const brandDeep = readCssToken("--brand-deep", brand);
        return { fill: brand, line: brandDeep || brand };
    }

    function setIsoMode(mode) {
        try {
            const normalized = mode === "real" ? "real" : "approx";
            const host = map && map.getContainer ? map.getContainer() : null;
            if (!host) {
                return;
            }
            host.setAttribute("data-iso-mode", normalized);
            const pane = host.closest ? host.closest(".map-pane") : null;
            if (pane) {
                pane.setAttribute("data-iso-mode", normalized);
            }
            if (normalized === lastIsoModeNotified) {
                return;
            }
            lastIsoModeNotified = normalized;
            if (typeof onIsoModeChange === "function") {
                onIsoModeChange(normalized);
            }
        } catch (e) { }
    }

    let onIsoModeChange = null;

    function ensureTravelRingLayers(sourceId) {
        const fillId = sourceId + "-fill";
        const haloId = sourceId + "-halo";
        const lineId = sourceId + "-line";
        const beforeId = map.getLayer(PIN_LAYER_CLUSTERS) ? PIN_LAYER_CLUSTERS : undefined;
        const colors = ringBrandColors();

        const lagenFillOpacity = lagenLayout
            ? ["match", ["coalesce", ["get", "minutes"], 0], 10, 0.10, 20, 0.06, 30, 0.035, 0.06]
            : [
                "match", ["coalesce", ["get", "minutes"], 0],
                10, 0.12,
                20, 0.09,
                30, 0.06,
                45, 0.05,
                0.08
            ];
        const lagenLineWidth = lagenLayout
            ? ["match", ["coalesce", ["get", "minutes"], 0], 10, 3, 20, 2.6, 30, 3, 2.5]
            : [
                "case",
                ["==", ["coalesce", ["get", "chosen"], 0], 1],
                2.5,
                1.5
            ];

        if (!map.getLayer(fillId)) {
            map.addLayer({
                id: fillId,
                type: "fill",
                source: sourceId,
                paint: {
                    "fill-color": colors.fill,
                    "fill-opacity": lagenFillOpacity
                }
            }, beforeId);
        }
        if (!map.getLayer(haloId)) {
            map.addLayer({
                id: haloId,
                type: "line",
                source: sourceId,
                paint: {
                    "line-color": "#ffffff",
                    "line-width": [
                        "case",
                        ["==", ["coalesce", ["get", "chosen"], 0], 1],
                        4.5,
                        3
                    ],
                    "line-opacity": lagenLayout ? 0 : 0.85
                },
                layout: {
                    visibility: lagenLayout ? "none" : "visible"
                }
            }, beforeId);
        }
        if (!map.getLayer(lineId)) {
            map.addLayer({
                id: lineId,
                type: "line",
                source: sourceId,
                paint: {
                    "line-color": colors.line,
                    "line-width": lagenLineWidth,
                    "line-opacity": 0.9,
                    "line-dasharray": travelRingLineDashPaint()
                },
                layout: lagenLayout ? {
                    "line-cap": [
                        "match", ["coalesce", ["get", "minutes"], 0],
                        30, "round",
                        "butt"
                    ],
                    "line-join": "round"
                } : {}
            }, beforeId);
        } else {
            try {
                map.setPaintProperty(lineId, "line-color", colors.line);
                map.setPaintProperty(lineId, "line-dasharray", travelRingLineDashPaint());
                map.setPaintProperty(fillId, "fill-color", colors.fill);
            } catch (e) { }
        }
        // Rings must never capture clicks.
        [fillId, haloId, lineId].forEach(function (id) {
            try {
                map.on("click", id, function (ev) {
                    if (ev && ev.originalEvent && typeof ev.originalEvent.stopPropagation === "function") {
                        /* allow click-through by not stopping — MapLibre still hits top layer */
                    }
                });
                map.setLayoutProperty(id, "visibility", "visible");
            } catch (e) { }
        });
        return [fillId, haloId, lineId];
    }

    function applyTravelRingData(features, lat, lng) {
        if (!features.length) {
            clearTravelRings();
            return;
        }
        const sourceId = "jobsy-travel-rings";
        const data = { type: "FeatureCollection", features: features };
        try {
            if (map.getSource(sourceId)) {
                map.getSource(sourceId).setData(data);
            } else {
                map.addSource(sourceId, { type: "geojson", data: data });
            }
            const ids = ensureTravelRingLayers(sourceId);
            travelRingGeo = { sourceId: sourceId, ids: ids };
            ringRedrawTries = 0;
            placeTravelRingLabels(lat, lng, features);
            try {
                const pinSrc = map.getSource(PIN_SOURCE);
                if (pinSrc && typeof pinSrc.setData === "function") {
                    pinSrc.setData(pinsGeoJson());
                }
            } catch (e) { }
        } catch (e) {
            scheduleTravelRingRedraw();
        }
    }

    function drawTravelRings(lat, lng) {
        if (!map || typeof map.addSource !== "function") {
            return;
        }
        if (kkMode) {
            drawKkRadiusRing(lat, lng);
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            scheduleTravelRingRedraw();
            return;
        }

        const ringKey = String(lat) + "," + String(lng) + "|"
            + (lagenLayout ? "lagen:10,20,30" : String(travelOptions.maxMinutes)) + "|"
            + String(travelOptions.transport || "");
        if (ringKey === lastRingDrawKey && travelRingGeo) {
            return;
        }
        lastRingDrawKey = ringKey;
        travelRingDrawCount += 1;

        const drawGen = ++ringRedrawTries;
        function applyIsochroneFc(fc) {
            if (!map || !lastOrigin || Math.abs(lastOrigin.lat - lat) > 1e-7 ||
                Math.abs(lastOrigin.lng - lng) > 1e-7) {
                return;
            }
            if (!fc) {
                if (lagenLayout) {
                    clearTravelRings();
                    lastRingDrawKey = ringKey;
                    setIsoMode("none");
                } else {
                    try { console.info("[lobsy] isochrone fallback: circles"); } catch (e) { }
                    setIsoMode("approx");
                }
                return;
            }
            const isoFeatures = featuresFromIsochroneFc(fc);
            if (!isoFeatures.length) {
                if (lagenLayout) {
                    clearTravelRings();
                    lastRingDrawKey = ringKey;
                    setIsoMode("none");
                } else {
                    setIsoMode("approx");
                }
                return;
            }
            applyTravelRingData(isoFeatures, lat, lng);
            setIsoMode("real");
            ringRedrawTries = Math.min(ringRedrawTries, drawGen);
        }

        function loadIsochronesWhenIdle() {
            fetchIsochrones(lat, lng).then(applyIsochroneFc);
        }

        if (lagenLayout) {
            clearTravelRings();
            lastRingDrawKey = ringKey;
            setIsoMode("pending");
            scheduleIsoLoad(loadIsochronesWhenIdle);
            return;
        }

        // Circles first, swap isochrones when they arrive (no flash).
        const circleFeatures = buildTravelRingFeatures(lat, lng);
        if (!circleFeatures.length) {
            clearTravelRings();
            return;
        }
        applyTravelRingData(circleFeatures, lat, lng);
        setIsoMode("approx");
        scheduleIsoLoad(loadIsochronesWhenIdle);

        function scheduleIsoLoad(fn) {
            if (map && typeof map.once === "function") {
                if (typeof map.isStyleLoaded === "function" && map.isStyleLoaded()
                    && map.areTilesLoaded && map.areTilesLoaded()) {
                    if (typeof requestIdleCallback === "function") {
                        requestIdleCallback(fn, { timeout: 5000 });
                    } else {
                        setTimeout(fn, 32);
                    }
                } else {
                    map.once("idle", function () {
                        if (typeof requestIdleCallback === "function") {
                            requestIdleCallback(fn, { timeout: 5000 });
                        } else {
                            setTimeout(fn, 32);
                        }
                    });
                }
            } else {
                fn();
            }
        }
    }

    function ringBounds(lat, lng, radiusM) {
        const bounds = new maplibregl.LngLatBounds();
        [0, 90, 180, 270].forEach(function (bearing) {
            bounds.extend(destinationLngLat(lat, lng, radiusM, bearing));
        });
        return bounds;
    }

    function overlayFitPadding() {
        if (lagenLayout && map) {
            if (isNarrowViewport()) {
                const pane = map.getContainer().closest(".map-pane");
                const sheet = pane ? pane.querySelector(".map-cluster-sheet--lagen") : null;
                const bottom = sheet && sheet.offsetHeight > 0 ? sheet.offsetHeight + 16 : 48;
                return { top: 22, right: 4, bottom: bottom, left: 4 };
            }
            return { top: 80, left: 150, right: 0, bottom: 0 };
        }
        const edge = 36;
        const padding = { top: edge, right: edge, bottom: edge, left: edge };
        if (!map) {
            return padding;
        }
        const container = map.getContainer();
        const pane = container && container.closest ? container.closest(".map-pane") : null;
        if (pane) {
            const carousel = pane.querySelector(".highlight-carousel--map");
            if (carousel && carousel.offsetParent !== null && carousel.offsetHeight > 0
                && !pane.classList.contains("is-cluster-open")) {
                padding.top = Math.max(padding.top, carousel.offsetHeight + 28);
            }
            const sheet = pane.querySelector(".map-cluster-sheet");
            if (sheet && sheet.offsetParent !== null && sheet.offsetHeight > 0) {
                padding.bottom = Math.max(padding.bottom, sheet.offsetHeight + 20);
            }
        }
        const locate = container ? container.querySelector(".job-map-locate") : null;
        if (locate && locate.offsetHeight) {
            // Locate sits at the corner; zoom + 3D sit above it (bottom: 58px).
            padding.bottom = Math.max(padding.bottom, 110);
        }
        padding.right = Math.max(padding.right, 52);
        return padding;
    }

    function sameOrigin(lat, lng) {
        if (!lastOrigin) {
            return false;
        }
        if (Math.abs(lastOrigin.lat - lat) < 1e-7 && Math.abs(lastOrigin.lng - lng) < 1e-7) {
            return true;
        }
        return haversineMeters(lastOrigin.lat, lastOrigin.lng, lat, lng) < ORIGIN_MOVE_MIN_METERS;
    }

    function markUserMapGesture() {
        lastUserGestureAt = Date.now();
        userGesturePending = true;
    }

    function runWithViewportNotifySuppressed(fn) {
        viewportNotifySuppressed += 1;
        try {
            fn();
        } finally {
            setTimeout(function () {
                viewportNotifySuppressed = Math.max(0, viewportNotifySuppressed - 1);
            }, 650);
        }
    }

    function fitToOriginRings(animate) {
        if (lagenLayout && activeClusterPopup) {
            return;
        }
        if (window.__jobsyBkRestoringView) {
            return;
        }
        if (!map || !lastOrigin) {
            return;
        }
        let radius = maxRingRadiusMeters() * 1.12;
        if (lagenLayout) {
            const chosenM = chosenRingMinutes();
            if (isNarrowViewport()) {
                radius = ringRadiusForMinutes(Math.max(chosenM, 30)) * 1.14;
            } else {
                radius = ringRadiusForMinutes(Math.max(chosenM, 30)) * 1.05;
            }
        }
        if (!(radius > 40)) {
            finishOpeningFrame();
            return;
        }
        const opening = deferMapReveal || originHasBeenFramed !== true;
        const opts = {
            padding: overlayFitPadding(),
            maxZoom: lagenLayout
                ? Math.min(isNarrowViewport() ? 10.35 : 11.85, BASEMAP_MAX_ZOOM)
                : Math.min(13, BASEMAP_MAX_ZOOM),
            animate: animate === true && !opening && !prefersReducedMotion(),
            duration: animate === true && !opening && !prefersReducedMotion() ? 450 : 0
        };
        if (!mapHasUsableSize()) {
            originNeedsFrame = true;
            safeJumpTo({
                center: [lastOrigin.lng, lastOrigin.lat],
                zoom: FILLED_LOCATION_ZOOM
            });
            finishOpeningFrame();
            return;
        }
        runWithViewportNotifySuppressed(function () {
            try {
                let bounds = ringBounds(lastOrigin.lat, lastOrigin.lng, radius);
                if (lagenLayout && isNarrowViewport() && catalogModeActive) {
                    Object.keys(markersById).forEach(function (id) {
                        const rec = markersById[id];
                        if (rec && Number.isFinite(rec.lat) && Number.isFinite(rec.lng)) {
                            bounds.extend([rec.lng, rec.lat]);
                        }
                    });
                }
                safeFitBounds(bounds, opts);
                originNeedsFrame = false;
                originHasBeenFramed = true;
                cameraLocked = true;
                firstSizedFit = true;
                finishOpeningFrame();
                map.once("idle", function () {
                    if (lastOrigin) {
                        drawTravelRings(lastOrigin.lat, lastOrigin.lng);
                    }
                });
            } catch (e) {
                originNeedsFrame = true;
                safeJumpTo({
                    center: [lastOrigin.lng, lastOrigin.lat],
                    zoom: FILLED_LOCATION_ZOOM
                });
                finishOpeningFrame();
            }
        });
    }

    function normalizeTravelOptions(options) {
        if (!options) return;
        if (options.maxMinutes != null) {
            travelOptions.maxMinutes = Number(options.maxMinutes) || travelOptions.maxMinutes;
        }
        if (options.transport) {
            travelOptions.transport = String(options.transport);
        }
        if (options.radiusKm != null) {
            const rk = Number(options.radiusKm);
            if (Number.isFinite(rk) && rk > 0) {
                travelOptions.radiusKm = rk;
            }
        }
    }

    function readCoord(obj, names) {
        if (!obj) {
            return NaN;
        }
        for (let i = 0; i < names.length; i++) {
            const n = Number(obj[names[i]]);
            if (Number.isFinite(n)) {
                return n;
            }
        }
        return NaN;
    }

    function pointFromVacancy(v) {
        const lat = readCoord(v, ["lat", "Lat", "latitude", "Latitude"]);
        const lng = readCoord(v, ["lng", "Lng", "lon", "Lon", "longitude", "Longitude"]);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return null;
        }
        return [lat, lng];
    }

    function collectVacancyPoints(vacancies) {
        const points = [];
        (vacancies || []).forEach(function (v) {
            const pt = pointFromVacancy(v);
            if (pt) {
                points.push(pt);
            }
        });
        return points;
    }

    function readOpeningView(raw) {
        if (!raw || typeof raw !== "object") {
            return null;
        }
        var lat = Number(raw.lat);
        var lng = Number(raw.lng);
        var zoom = Number(raw.zoom);
        if ((!Number.isFinite(lat) || !Number.isFinite(lng)) && Array.isArray(raw.center) && raw.center.length >= 2) {
            lng = Number(raw.center[0]);
            lat = Number(raw.center[1]);
        }
        if (!Number.isFinite(zoom)) {
            zoom = Number(raw.zoom);
        }
        if (!Number.isFinite(lat) || !Number.isFinite(lng) || !Number.isFinite(zoom)) {
            return null;
        }
        return { lat: lat, lng: lng, zoom: zoom };
    }

    function readFilledOrigin() {
        try {
            if (!window.jobsyGeo || typeof window.jobsyGeo.getStoredOrigin !== "function") {
                return null;
            }
            const stored = window.jobsyGeo.getStoredOrigin();
            if (!stored) {
                return null;
            }
            const lat = Number(stored.lat);
            const lng = Number(stored.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)
                || Math.abs(lat) > 90 || Math.abs(lng) > 180) {
                return null;
            }
            return { lat: lat, lng: lng, zoom: FILLED_LOCATION_ZOOM };
        } catch (e) {
            return null;
        }
    }

    function openingCamera(openingPoints, openingView, preferFilledLocation) {
        if (preferFilledLocation !== false) {
            const filled = readFilledOrigin();
            if (filled) {
                return { center: [filled.lng, filled.lat], zoom: filled.zoom, locked: true };
            }
        }
        const view = readOpeningView(openingView);
        if (view) {
            return { center: [view.lng, view.lat], zoom: view.zoom, locked: true };
        }
        if (openingPoints && openingPoints.length) {
            const startBounds = boundsFromPoints(openingPoints);
            const start = startBounds.getCenter();
            return { center: [start.lng, start.lat], zoom: zoomForPoints(openingPoints), locked: true };
        }
        return { center: [NL_CENTER[1], NL_CENTER[0]], zoom: NL_ZOOM, locked: true };
    }

    function safeJumpTo(opts) {
        if (!map || !opts) {
            return;
        }
        runWithViewportNotifySuppressed(function () {
            try {
                map.jumpTo(opts);
            } catch (e) { }
        });
    }

    function safeEaseTo(opts) {
        if (!map || !opts) {
            return;
        }
        runWithViewportNotifySuppressed(function () {
            try {
                map.easeTo(opts);
            } catch (e) { }
        });
    }

    function lockCamera(openingPoints, openingView, preferFilledLocation) {
        if (cameraLocked || !map) {
            return;
        }
        const opening = openingCamera(openingPoints, openingView, preferFilledLocation);
        if (!opening.locked) {
            return;
        }
        safeJumpTo({ center: opening.center, zoom: opening.zoom });
        cameraLocked = true;
        firstSizedFit = true;
    }

    function zoomForPoints(points) {
        if (!points.length) {
            return 8;
        }
        let minLat = 90, maxLat = -90, minLng = 180, maxLng = -180;
        points.forEach(function (p) {
            minLat = Math.min(minLat, p[0]);
            maxLat = Math.max(maxLat, p[0]);
            minLng = Math.min(minLng, p[1]);
            maxLng = Math.max(maxLng, p[1]);
        });
        const span = Math.max(maxLng - minLng, maxLat - minLat);
        if (span < 0.08) return 13;
        if (span < 0.2) return 12;
        if (span < 0.5) return 11;
        if (span < 1.2) return 10;
        if (span < 2.5) return 9;
        return 8;
    }

    function scheduleLagenLayoutResize() {
        if (!lagenLayout || !map) {
            return;
        }
        [0, 80, 200, 450, 900].forEach(function (ms) {
            setTimeout(function () {
                invalidate();
            }, ms);
        });
    }

    function syncLagenAttributionCompact() {
        if (!lagenLayout || !map || typeof map.getContainer !== "function") {
            return;
        }
        const attrib = map.getContainer().querySelector(".maplibregl-ctrl-attrib");
        if (!attrib) {
            return;
        }
        if (isNarrowViewport()) {
            attrib.classList.remove("maplibregl-compact");
            attrib.classList.add("maplibregl-compact-show");
        } else {
            attrib.classList.add("maplibregl-compact");
            attrib.classList.remove("maplibregl-compact-show");
        }
    }

    function ensureLagenPopInView(popup) {
        fitLagenDesktopPopup(popup);
    }

    function fitLagenDesktopPopup(popup, lngLat, instant) {
        if (!popup || !map || isNarrowViewport()) {
            return;
        }
        if (lngLat && typeof popup.setLngLat === "function") {
            try {
                popup.setLngLat(lngLat);
            } catch (eLl) { }
        } else if (clusterUiState && clusterUiState.lngLat && typeof popup.setLngLat === "function") {
            try {
                popup.setLngLat(clusterUiState.lngLat);
            } catch (eLl2) { }
        }
        centerPopupInView(popup, !!instant, 0);
    }

    function scheduleLagenDesktopPopupFit(popup, lngLat) {
        if (!popup || !map || isNarrowViewport()) {
            return;
        }
        var pass = 0;
        function tick() {
            if (!activeClusterPopup || activeClusterPopup !== popup) {
                return;
            }
            fitLagenDesktopPopup(popup, lngLat, pass > 0);
            pass += 1;
            if (pass < 2) {
                if (typeof requestAnimationFrame === "function") {
                    requestAnimationFrame(function () {
                        requestAnimationFrame(tick);
                    });
                } else {
                    setTimeout(tick, 32);
                }
            }
        }
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(tick);
            });
        } else {
            setTimeout(tick, 0);
        }
    }

    function bindLagenDesktopPopupMapSync() {
        if (!map || map._jobsyLagenDesktopPopSync) {
            return;
        }
        map._jobsyLagenDesktopPopSync = true;
        var moveTimer = null;
        map.on("moveend", function () {
            if (!activeClusterPopup || !clusterUiState || clusterUiState.docked || isNarrowViewport()) {
                return;
            }
            if (moveTimer) {
                clearTimeout(moveTimer);
            }
            moveTimer = setTimeout(function () {
                moveTimer = null;
                writeBanenkaartMapView();
                scheduleLagenDesktopPopupFit(activeClusterPopup);
            }, 80);
        });
    }

    function revealMapStage() {
        if (!map || deferMapReveal) {
            return;
        }
        const stage = map.getContainer() && map.getContainer().closest(".map-stage");
        if (stage) {
            stage.classList.add("is-live");
        }
        if (lagenLayout) {
            scheduleLagenLayoutResize();
        }
    }

    function finishOpeningFrame() {
        deferMapReveal = false;
        revealMapStage();
    }

    function fitOriginWithPinsOutsideTravel() {
        if (!map || !lastOrigin) {
            return;
        }
        var points = vacancyPoints();
        var ringRadius = maxRingRadiusMeters() * 1.12;
        var bounds = ringBounds(lastOrigin.lat, lastOrigin.lng, ringRadius);
        if (points.length) {
            points.forEach(function (p) {
                bounds.extend([p[1], p[0]]);
            });
        }
        runWithViewportNotifySuppressed(function () {
            try {
                safeFitBounds(bounds, {
                    padding: overlayFitPadding(),
                    maxZoom: 11,
                    animate: !prefersReducedMotion(),
                    duration: prefersReducedMotion() ? 0 : 450
                });
                originHasBeenFramed = true;
                finishOpeningFrame();
            } catch (eFit) {
                fitToOriginRings(true);
            }
        });
    }

    function flushPendingCatalogPins() {
        if (!pendingCatalogPins || !clusterGroup || !map) {
            return;
        }
        const pins = pendingCatalogPins;
        const meta = pendingCatalogMeta || {};
        pendingCatalogPins = null;
        pendingCatalogMeta = null;
        setVacanciesFromCatalog(pins, meta);
    }

    function emitCatalogMapReady() {
        if (catalogMapReadySent || !catalogModeActive) {
            return;
        }
        catalogMapReadySent = true;
        try {
            document.dispatchEvent(new CustomEvent("jobsy-bk-map-ready", { bubbles: true }));
        } catch (eReady) { }
        try {
            const root = document.querySelector("[data-banenkaart-ssr=\"1\"]");
            if (root) {
                root.dataset.banenkaartMapReady = "1";
            }
        } catch (eDom) { }
    }

    function scheduleCatalogMapReadyAfterIdle() {
        if (!catalogModeActive || catalogMapReadySent || !map) {
            return;
        }
        const onIdle = function () {
            emitCatalogMapReady();
        };
        if (typeof map.isStyleLoaded === "function" && map.isStyleLoaded()) {
            map.once("idle", onIdle);
        } else if (typeof map.once === "function") {
            map.once("load", function () {
                map.once("idle", onIdle);
            });
        }
    }

    function setVacanciesFromCatalog(pins, meta) {
        meta = meta || {};
        if (!clusterGroup || !map) {
            pendingCatalogPins = pins || [];
            pendingCatalogMeta = meta;
            return;
        }
        if (meta.zoomOutsideTravel) {
            bootZoomOutsideTravel = true;
        }
        setVacancies(pins || []);
        scheduleCatalogMapReadyAfterIdle();
        if (window.__jobsyBkRestoringView) {
            return;
        }
        if (bootZoomOutsideTravel && lastOrigin && !lagenLayout) {
            fitOriginWithPinsOutsideTravel();
        } else if (lastOrigin && !meta.skipAutoFit) {
            fitToOriginRings(true);
        }
    }

    function mapHasUsableSize() {
        if (!map) {
            return false;
        }
        const c = map.getContainer();
        return !!(c && c.clientWidth >= 32 && c.clientHeight >= 32);
    }

    function fitPaddingTotals(padding) {
        if (padding == null) {
            return { h: 96, v: 96 };
        }
        if (typeof padding === "number") {
            return { h: padding * 2, v: padding * 2 };
        }
        return {
            h: (Number(padding.left) || 0) + (Number(padding.right) || 0),
            v: (Number(padding.top) || 0) + (Number(padding.bottom) || 0)
        };
    }

    function safeFitBounds(bounds, opts) {
        if (!map || !bounds) {
            return false;
        }
        if (typeof bounds.isEmpty === "function" && bounds.isEmpty()) {
            return false;
        }
        if (!mapHasUsableSize()) {
            originNeedsFrame = true;
            return false;
        }
        const c = map.getContainer();
        const totals = fitPaddingTotals(opts && opts.padding);
        if (totals.h >= c.clientWidth - 8 || totals.v >= c.clientHeight - 8) {
            originNeedsFrame = true;
            try {
                const center = bounds.getCenter();
                safeJumpTo({
                    center: [center.lng, center.lat],
                    zoom: map.getZoom() || FILLED_LOCATION_ZOOM
                });
            } catch (eJump) { /* ignore */ }
            return false;
        }
        try {
            map.fitBounds(bounds, opts || {});
            originNeedsFrame = false;
            return true;
        } catch (eFit) {
            originNeedsFrame = true;
            return false;
        }
    }

    function vacancyPoints() {
        return Object.keys(markersById).map(function (id) {
            return [markersById[id].lat, markersById[id].lng];
        });
    }

    function boundsFromPoints(points) {
        const bounds = new maplibregl.LngLatBounds();
        points.forEach(function (p) {
            bounds.extend([p[1], p[0]]);
        });
        return bounds;
    }

    function fitMapToVacancies(markerBounds) {
        if (!map) return;

        const points = Array.isArray(markerBounds) && markerBounds.length
            ? markerBounds.slice()
            : vacancyPoints();
        lastFitPoints = points.slice();
        if (points.length === 0) {
            return;
        }

        if (cameraLocked || firstSizedFit) {
            ensureVacancyTiles();
            revealMapStage();
            return;
        }

        const opening = !firstSizedFit;
        const opts = { padding: 48, maxZoom: 13, animate: !opening, duration: opening ? 0 : 450 };
        const bounds = boundsFromPoints(points);
        if (mapHasUsableSize()) {
            try {
                safeFitBounds(bounds, opts);
            } catch (e) { }
            firstSizedFit = true;
        } else {
            safeJumpTo({
                center: bounds.getCenter(),
                zoom: zoomForPoints(points)
            });
        }
        ensureVacancyTiles();
        revealMapStage();
    }

    function ensureVacancyTiles() {
        if (!map || tileLayer) {
            return;
        }
        tileLayer = { kind: "openfreemap-vector" };
        const fire = function () {
            revealMapStage();
            if (openCallback && typeof openCallback.invokeMethodAsync === "function") {
                openCallback.invokeMethodAsync("OnMapTilesReady");
            }
        };
        if (map.loaded()) {
            map.once("idle", fire);
        } else {
            map.once("load", function () {
                map.once("idle", fire);
            });
        }
    }

    function pinKindForVacancy(v) {
        if (!lagenLayout) {
            return "";
        }
        const ic = window.jobsyBanenkaartMapIcons;
        if (ic && typeof ic.isIntermediaryRow === "function" && ic.isIntermediaryRow(v)) {
            return "intermediary";
        }
        if (isAgencyPin(v) || v.isIntermediary === true || v.agency === 1 || v.agency === true) {
            return "intermediary";
        }
        return "employer";
    }

    function pinsGeoJson() {
        const origin = lastOrigin;
        const outerM = origin ? maxRingRadiusMeters() : 0;
        return {
            type: "FeatureCollection",
            features: Object.keys(markersById).map(function (id) {
                const record = markersById[id];
                const v = record.options.jobData || {};
                const agency = (isAgencyPin(v) || (companyMapMode && v.isIntermediary === true)) ? 1 : 0;
                const pinKind = pinKindForVacancy(v);
                const openInit = companyMapMode && v.openForInitiative === true;
                const kansberoep = v.kansberoep === true || v.kansberoep === 1 ? 1 : 0;
                let outside = 0;
                if (v.outsideTravel || v.dimmed) {
                    outside = 1;
                } else if (origin && outerM > 0) {
                    const d = haversineMeters(origin.lat, origin.lng, record.lat, record.lng);
                    if (d > outerM * 1.02) {
                        outside = 1;
                    }
                }
                return {
                    type: "Feature",
                    id: id,
                    geometry: { type: "Point", coordinates: [record.lng, record.lat] },
                    properties: {
                        id: String(id),
                        featured: (kansberoep || isFeaturedVacancy(v)) ? 1 : 0,
                        kansberoep: kansberoep,
                        agency: agency,
                        radiusKm: agency ? Number(agencyRadiusKm(v) || 0) : 0,
                        workType: workTypeOf(v) || "",
                        glyph: kansberoep ? "★" : (agency ? "◆" : workTypeMapGlyph(workTypeOf(v))),
                        colour: kansberoep ? "#c9a227" : (openInit ? COMPANY_OPEN_PIN_COLOR : (agency ? AGENCY_PIN_COLOR : (v.categoryColor || v.colour || ""))),
                        matchPercent: v.matchPercent == null ? -1 : Number(v.matchPercent),
                        matchBand: v.matchColorBand || "orange",
                        outside: outside ? 1 : 0,
                        pinKind: pinKind
                    }
                };
            })
        };
    }

    function agencyAreasGeoJson() {
        if (selectedId == null) {
            return { type: "FeatureCollection", features: [] };
        }
        const record = markersById[selectedId];
        if (!record) {
            return { type: "FeatureCollection", features: [] };
        }
        const v = record.options.jobData || {};
        const km = agencyRadiusKm(v);
        if (km <= 0) {
            return { type: "FeatureCollection", features: [] };
        }
        return {
            type: "FeatureCollection",
            features: [{
                type: "Feature",
                properties: { radiusKm: km, pinId: String(selectedId) },
                geometry: {
                    type: "Polygon",
                    coordinates: circlePolygon(record.lat, record.lng, km * 1000)
                }
            }]
        };
    }

    function refreshAgencyAreas() {
        if (lagenLayout) {
            if (!map || !map.getSource(PIN_SOURCE_AGENCY_AREAS)) {
                return;
            }
            const source = map.getSource(PIN_SOURCE_AGENCY_AREAS);
            if (source && typeof source.setData === "function") {
                source.setData({ type: "FeatureCollection", features: [] });
            }
            [PIN_LAYER_AGENCY_AREAS_FILL, PIN_LAYER_AGENCY_AREAS_LINE].forEach(function (layerId) {
                try {
                    if (map.getLayer(layerId)) {
                        map.setLayoutProperty(layerId, "visibility", "none");
                    }
                } catch (eHide) { }
            });
            return;
        }
        if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) {
            return;
        }
        ensureAgencyAreaLayers();
        const source = map.getSource(PIN_SOURCE_AGENCY_AREAS);
        if (source && typeof source.setData === "function") {
            source.setData(agencyAreasGeoJson());
        }
    }

    function haversineMeters(lat1, lng1, lat2, lng2) {
        const toRad = Math.PI / 180;
        const dLat = (lat2 - lat1) * toRad;
        const dLng = (lng2 - lng1) * toRad;
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * toRad) * Math.cos(lat2 * toRad) *
            Math.sin(dLng / 2) * Math.sin(dLng / 2);
        return 6371000 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function ensureAgencyAreaLayers() {
        if (!map) {
            return;
        }
        if (!map.getSource(PIN_SOURCE_AGENCY_AREAS)) {
            const beforeId = map.getLayer(PIN_LAYER_CLUSTERS) ? PIN_LAYER_CLUSTERS : undefined;
            map.addSource(PIN_SOURCE_AGENCY_AREAS, {
                type: "geojson",
                data: agencyAreasGeoJson()
            });
            map.addLayer({
                id: PIN_LAYER_AGENCY_AREAS_FILL,
                type: "fill",
                source: PIN_SOURCE_AGENCY_AREAS,
                paint: {
                    "fill-color": AGENCY_PIN_COLOR,
                    "fill-opacity": 0.12
                }
            }, beforeId);
            map.addLayer({
                id: PIN_LAYER_AGENCY_AREAS_LINE,
                type: "line",
                source: PIN_SOURCE_AGENCY_AREAS,
                paint: {
                    "line-color": AGENCY_PIN_COLOR,
                    "line-width": 2,
                    "line-opacity": 0.55
                }
            }, beforeId);
        }
    }

    function loadSvgAsMapImage(imageId, svgString, width, height, done) {
        if (!map || map.hasImage(imageId)) {
            if (done) {
                done();
            }
            return;
        }
        const img = new Image();
        const url = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(svgString);
        img.onload = function () {
            try {
                const canvas = document.createElement("canvas");
                canvas.width = width;
                canvas.height = height;
                const ctx = canvas.getContext("2d");
                ctx.drawImage(img, 0, 0, width, height);
                if (!map.hasImage(imageId)) {
                    map.addImage(imageId, {
                        width: width,
                        height: height,
                        data: new Uint8Array(ctx.getImageData(0, 0, width, height).data.buffer),
                        sdf: false
                    });
                }
            } catch (eImg) { }
            if (done) {
                done();
            }
        };
        img.onerror = function () {
            if (done) {
                done();
            }
        };
        img.src = url;
    }

    function ensureLagenPinImages(done) {
        if (!map || !lagenLayout) {
            if (done) {
                done();
            }
            return;
        }
        if (map._jobsyLagenPinImagesReady) {
            if (done) {
                done();
            }
            return;
        }
        const ic = window.jobsyBanenkaartMapIcons;
        if (!ic || typeof ic.pinW !== "function" || typeof ic.pinI !== "function") {
            setTimeout(function () {
                ensureLagenPinImages(done);
            }, 40);
            return;
        }
        const specs = [
            { id: "jobsy-pin-w", svg: ic.pinW(1), w: 40, h: 50 },
            { id: "jobsy-pin-w-selected", svg: ic.pinW(1, true), w: 40, h: 50 },
            { id: "jobsy-pin-i", svg: ic.pinI(1, false, true), w: 46, h: 50 },
            { id: "jobsy-pin-i-selected", svg: ic.pinI(1, true, true), w: 46, h: 50 }
        ];
        let pending = specs.length;
        specs.forEach(function (spec) {
            loadSvgAsMapImage(spec.id, spec.svg, spec.w, spec.h, function () {
                pending -= 1;
                if (pending <= 0) {
                    map._jobsyLagenPinImagesReady = lagenPinSymbolsRenderable();
                    applyLagenPinLayerStyles();
                    if (!map._jobsyLagenPinImagesReady) {
                        setTimeout(function () {
                            ensureLagenPinImages(done);
                        }, 120);
                        return;
                    }
                    if (done) {
                        done();
                    }
                }
            });
        });
    }

    function lagenUnclusteredIconLayout() {
        // MapLibre layout properties cannot use feature-state; selection uses circle paint + highlight().
        return {
            "icon-image": [
                "case",
                ["==", ["coalesce", ["get", "pinKind"], ""], "intermediary"],
                "jobsy-pin-i",
                "jobsy-pin-w"
            ],
            "icon-size": 1,
            "icon-anchor": "bottom",
            "icon-allow-overlap": true,
            "icon-ignore-placement": true
        };
    }

    function applyLagenPinLayerStyles() {
        if (!map || !lagenLayout) {
            return;
        }
        const clusterLayers = [PIN_LAYER_CLUSTERS, PIN_LAYER_CLUSTERS + "-halo"];
        clusterLayers.forEach(function (layerId) {
            try {
                if (map.getLayer(layerId)) {
                    map.setPaintProperty(layerId, "circle-color", LAGEN_CLUSTER_GREEN);
                }
            } catch (eCl) { }
        });
        try {
            if (map.getLayer(PIN_LAYER_CLUSTER_AGENCY_BADGE)) {
                map.setLayoutProperty(PIN_LAYER_CLUSTER_AGENCY_BADGE, "visibility", "none");
            }
        } catch (eBadge) { }
        const symbolsReady = lagenPinSymbolsRenderable();
        [PIN_LAYER_UNCLUSTERED, PIN_LAYER_UNCLUSTERED_GLYPH].forEach(function (layerId) {
            try {
                if (map.getLayer(layerId)) {
                    map.setLayoutProperty(layerId, "visibility", symbolsReady ? "none" : "visible");
                }
            } catch (ePin) { }
        });
        [PIN_LAYER_AGENCY_AREAS_FILL, PIN_LAYER_AGENCY_AREAS_LINE].forEach(function (layerId) {
            try {
                if (map.getLayer(layerId)) {
                    map.setLayoutProperty(layerId, "visibility", "none");
                }
            } catch (eArea) { }
        });
        if (!map.getLayer(PIN_LAYER_UNCLUSTERED_SYMBOL)) {
            map.addLayer({
                id: PIN_LAYER_UNCLUSTERED_SYMBOL,
                type: "symbol",
                source: PIN_SOURCE,
                filter: unclusteredPinFilter(),
                layout: lagenUnclusteredIconLayout()
            });
            if (!map._jobsyLagenPinClicksBound) {
                map._jobsyLagenPinClicksBound = true;
                map.on("click", PIN_LAYER_UNCLUSTERED_SYMBOL, onPinClick);
                map.on("mouseenter", PIN_LAYER_UNCLUSTERED_SYMBOL, function () {
                    map.getCanvas().style.cursor = "pointer";
                });
                map.on("mouseleave", PIN_LAYER_UNCLUSTERED_SYMBOL, function () {
                    map.getCanvas().style.cursor = "";
                });
            }
            try {
                map.setLayoutProperty(PIN_LAYER_UNCLUSTERED_SYMBOL, "visibility", symbolsReady ? "visible" : "none");
            } catch (eVisNew) { }
        } else {
            const layout = lagenUnclusteredIconLayout();
            Object.keys(layout).forEach(function (key) {
                try {
                    map.setLayoutProperty(PIN_LAYER_UNCLUSTERED_SYMBOL, key, layout[key]);
                } catch (eLay) { }
            });
            try {
                map.setFilter(PIN_LAYER_UNCLUSTERED_SYMBOL, unclusteredPinFilter());
            } catch (eFlt) { }
            try {
                map.setLayoutProperty(PIN_LAYER_UNCLUSTERED_SYMBOL, "visibility", symbolsReady ? "visible" : "none");
            } catch (eVis) { }
        }
    }

    function resetLagenPinSourceIfNeeded() {
        if (!map || !lagenLayout || map._jobsyLagenPinClusterVer === 9) {
            return;
        }
        const layerIds = [
            PIN_LAYER_UNCLUSTERED_SYMBOL,
            PIN_LAYER_SELECTED,
            PIN_LAYER_UNCLUSTERED_GLYPH,
            PIN_LAYER_UNCLUSTERED,
            PIN_LAYER_UNCLUSTERED + "-hit",
            PIN_LAYER_CLUSTER_COUNT,
            PIN_LAYER_CLUSTER_AGENCY_BADGE,
            PIN_LAYER_CLUSTERS,
            PIN_LAYER_CLUSTERS + "-hit",
            PIN_LAYER_CLUSTERS + "-halo"
        ];
        layerIds.forEach(function (layerId) {
            try {
                if (map.getLayer(layerId)) {
                    map.removeLayer(layerId);
                }
            } catch (eRm) { }
        });
        try {
            if (map.getSource(PIN_SOURCE)) {
                map.removeSource(PIN_SOURCE);
            }
        } catch (eSrc) { }
        map._jobsyLagenPinClusterVer = 9;
        map._jobsyLagenPinClicksBound = false;
    }

    function ensurePinImages() {
        if (!map || map._jobsyPinImages) {
            return;
        }
        map._jobsyPinImages = true;
        // Soft glow for featured pins (drawn under the pin circle).
        const size = 64;
        const canvas = document.createElement("canvas");
        canvas.width = size;
        canvas.height = size;
        const ctx = canvas.getContext("2d");
        const g = ctx.createRadialGradient(size / 2, size / 2, 4, size / 2, size / 2, size / 2);
        g.addColorStop(0, "rgba(201,162,39,0.55)");
        g.addColorStop(1, "rgba(201,162,39,0)");
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, size, size);
        if (!map.hasImage("jobsy-featured-glow")) {
            map.addImage("jobsy-featured-glow", {
                width: size,
                height: size,
                data: new Uint8Array(ctx.getImageData(0, 0, size, size).data.buffer)
            });
        }
    }

    function ensurePinLayers() {
        if (!map) {
            return;
        }
        ensurePinImages();
        if (lagenLayout) {
            resetLagenPinSourceIfNeeded();
        }
        if (!map.getSource(PIN_SOURCE)) {
            map.addSource(PIN_SOURCE, {
                type: "geojson",
                data: pinsGeoJson(),
                cluster: true,
                clusterRadius: lagenLayout ? 16 : CLUSTER_OPTS.clusterRadius,
                clusterMaxZoom: lagenLayout ? (BASEMAP_MAX_ZOOM - 1) : CLUSTER_OPTS.clusterMaxZoom,
                clusterProperties: {
                    agency_sum: ["+", ["coalesce", ["get", "agency"], 0]]
                },
                promoteId: "id"
            });
        }
        const PIN_LAYER_CLUSTER_HALO = PIN_LAYER_CLUSTERS + "-halo";
        const PIN_LAYER_CLUSTER_HIT = PIN_LAYER_CLUSTERS + "-hit";
        const PIN_LAYER_UNCLUSTERED_HIT = PIN_LAYER_UNCLUSTERED + "-hit";
        const legacyAgencyPinLabelLayer = ["jobsy-pins", "agency-label"].join("-");
        const legacyClusterAgencyLabelLayer = ["jobsy-pins-cluster", "agency-label"].join("-");
        [PIN_LAYER_CLUSTER_AGENCY_BADGE, legacyAgencyPinLabelLayer, legacyClusterAgencyLabelLayer]
            .forEach(function (legacyId) {
                try {
                    if (map.getLayer(legacyId)) {
                        map.removeLayer(legacyId);
                    }
                } catch (eLegacy) { }
            });
        if (!map.getLayer(PIN_LAYER_CLUSTER_HALO)) {
            map.addLayer({
                id: PIN_LAYER_CLUSTER_HALO,
                type: "circle",
                source: PIN_SOURCE,
                filter: ["has", "point_count"],
                paint: {
                    "circle-color": "#16a34a",
                    "circle-radius": [
                        "step", ["to-number", ["coalesce", ["get", "point_count"], 0], 0],
                        20, 10, 22, 30, 25
                    ],
                    "circle-opacity": 0.22,
                    "circle-blur": 0.4
                }
            });
        }
        if (!map.getLayer(PIN_LAYER_CLUSTER_HIT)) {
            map.addLayer({
                id: PIN_LAYER_CLUSTER_HIT,
                type: "circle",
                source: PIN_SOURCE,
                filter: ["has", "point_count"],
                paint: {
                    "circle-color": "#000000",
                    "circle-radius": 22,
                    "circle-opacity": 0
                }
            });
        }
        if (!map.getLayer(PIN_LAYER_CLUSTERS)) {
            map.addLayer({
                id: PIN_LAYER_CLUSTERS,
                type: "circle",
                source: PIN_SOURCE,
                filter: ["has", "point_count"],
                paint: {
                    "circle-color": "#16a34a",
                    "circle-radius": [
                        "step", ["to-number", ["coalesce", ["get", "point_count"], 0], 0],
                        15, 10, 17, 30, 20
                    ],
                    "circle-stroke-width": 2,
                    "circle-stroke-color": "#ffffff"
                }
            });
            map.addLayer({
                id: PIN_LAYER_CLUSTER_COUNT,
                type: "symbol",
                source: PIN_SOURCE,
                filter: ["has", "point_count"],
                layout: {
                    "text-field": ["coalesce", ["get", "point_count_abbreviated"], ["to-string", ["coalesce", ["get", "point_count"], 0]]],
                    "text-size": 13,
                    "text-font": ["Noto Sans Bold"],
                    "text-allow-overlap": true
                },
                paint: { "text-color": "#ffffff" }
            });
            map.addLayer({
                id: PIN_LAYER_CLUSTER_AGENCY_BADGE,
                type: "symbol",
                source: PIN_SOURCE,
                filter: ["all", ["has", "point_count"], [">", ["coalesce", ["get", "agency_sum"], 0], 0]],
                layout: {
                    "text-field": ["concat", "◆", ["to-string", ["coalesce", ["get", "agency_sum"], 0]]],
                    "text-size": 10,
                    "text-offset": [1.15, 1.15],
                    "text-font": ["Noto Sans Bold"],
                    "text-allow-overlap": true,
                    "text-ignore-placement": true
                },
                paint: {
                    "text-color": AGENCY_PIN_COLOR,
                    "text-halo-color": "#ffffff",
                    "text-halo-width": 1.5
                }
            });
            map.addLayer({
                id: PIN_LAYER_UNCLUSTERED_HIT,
                type: "circle",
                source: PIN_SOURCE,
                filter: unclusteredPinFilter(),
                paint: {
                    "circle-color": "#000000",
                    "circle-radius": 22,
                    "circle-opacity": 0
                }
            });
            map.addLayer({
                id: PIN_LAYER_UNCLUSTERED,
                type: "circle",
                source: PIN_SOURCE,
                filter: unclusteredPinFilter(),
                paint: {
                    "circle-color": [
                        "case",
                        ["==", ["coalesce", ["feature-state", "selected"], false], true], "#f54a1b",
                        ["==", ["coalesce", ["get", "kansberoep"], 0], 1], "#c9a227",
                        ["==", ["coalesce", ["get", "agency"], 0], 1], AGENCY_PIN_COLOR,
                        ["==", ["coalesce", ["get", "featured"], 0], 1], "#c9a227",
                        [
                            "case",
                            // Avoid literal null in the style (MapLibre warns); empty string means "no colour".
                            ["all", ["has", "colour"], ["!=", ["coalesce", ["get", "colour"], ""], ""]],
                            ["coalesce", ["get", "colour"], "#16a34a"],
                            "#16a34a"
                        ]
                    ],
                    "circle-radius": [
                        "case",
                        ["==", ["coalesce", ["feature-state", "selected"], false], true], 10,
                        ["==", ["coalesce", ["get", "agency"], 0], 1], 8,
                        ["==", ["coalesce", ["get", "featured"], 0], 1], 8,
                        7
                    ],
                    "circle-stroke-width": 2,
                    "circle-stroke-color": [
                        "case",
                        ["==", ["coalesce", ["feature-state", "selected"], false], true], "#f54a1b",
                        "#ffffff"
                    ],
                    "circle-opacity": [
                        "case",
                        ["==", ["coalesce", ["get", "outside"], 0], 1],
                        0.35,
                        0.95
                    ]
                }
            });
            map.addLayer({
                id: PIN_LAYER_UNCLUSTERED_GLYPH,
                type: "symbol",
                source: PIN_SOURCE,
                filter: [
                    "all",
                    unclusteredPinFilter(),
                    ["has", "glyph"],
                    ["!=", ["coalesce", ["get", "glyph"], ""], ""]
                ],
                layout: {
                    "text-field": ["coalesce", ["get", "glyph"], ""],
                    "text-size": 10,
                    "text-allow-overlap": true,
                    "text-ignore-placement": true
                },
                paint: {
                    "text-opacity": [
                        "case",
                        ["==", ["coalesce", ["get", "outside"], 0], 1],
                        0.35,
                        1
                    ]
                }
            });
        } else if (!map.getLayer(PIN_LAYER_CLUSTER_AGENCY_BADGE)) {
            map.addLayer({
                id: PIN_LAYER_CLUSTER_AGENCY_BADGE,
                type: "symbol",
                source: PIN_SOURCE,
                filter: ["all", ["has", "point_count"], [">", ["coalesce", ["get", "agency_sum"], 0], 0]],
                layout: {
                    "text-field": ["concat", "◆", ["to-string", ["coalesce", ["get", "agency_sum"], 0]]],
                    "text-size": 10,
                    "text-offset": [1.15, 1.15],
                    "text-font": ["Noto Sans Bold"],
                    "text-allow-overlap": true,
                    "text-ignore-placement": true
                },
                paint: {
                    "text-color": AGENCY_PIN_COLOR,
                    "text-halo-color": "#ffffff",
                    "text-halo-width": 1.5
                }
            });
            [PIN_LAYER_UNCLUSTERED, PIN_LAYER_UNCLUSTERED_HIT, PIN_LAYER_UNCLUSTERED_GLYPH].forEach(function (layerId) {
                try {
                    if (map.getLayer(layerId)) {
                        map.setFilter(layerId, unclusteredPinFilter());
                    }
                } catch (eFilter) { }
            });
            [PIN_LAYER_CLUSTERS, PIN_LAYER_CLUSTER_HALO].forEach(function (layerId) {
                try {
                    if (map.getLayer(layerId)) {
                        map.setPaintProperty(layerId, "circle-color", "#16a34a");
                    }
                } catch (ePaint) { }
            });
        }
        if (lagenLayout) {
            applyLagenPinLayerStyles();
        } else if (map.getLayer(PIN_LAYER_UNCLUSTERED_SYMBOL)) {
            try {
                map.setLayoutProperty(PIN_LAYER_UNCLUSTERED_SYMBOL, "visibility", "none");
            } catch (eSymHide) { }
            [PIN_LAYER_UNCLUSTERED, PIN_LAYER_UNCLUSTERED_GLYPH].forEach(function (layerId) {
                try {
                    if (map.getLayer(layerId)) {
                        map.setLayoutProperty(layerId, "visibility", "visible");
                    }
                } catch (eShow) { }
            });
        }
        if (!map._jobsyPinClicksBound) {
            map._jobsyPinClicksBound = true;
            map.on("click", PIN_LAYER_CLUSTER_HIT, onClusterClick);
            map.on("click", PIN_LAYER_CLUSTERS, onClusterClick);
            map.on("touchend", PIN_LAYER_CLUSTER_HIT, onClusterClick);
            map.on("touchend", PIN_LAYER_CLUSTERS, onClusterClick);
            map.on("click", PIN_LAYER_UNCLUSTERED_HIT, onPinClick);
            map.on("click", PIN_LAYER_UNCLUSTERED, onPinClick);
            map.on("click", PIN_LAYER_UNCLUSTERED_GLYPH, onPinClick);
            map.on("mouseenter", PIN_LAYER_CLUSTERS, function () { map.getCanvas().style.cursor = "pointer"; });
            map.on("mouseleave", PIN_LAYER_CLUSTERS, function () { map.getCanvas().style.cursor = ""; });
            map.on("mouseenter", PIN_LAYER_UNCLUSTERED, function () { map.getCanvas().style.cursor = "pointer"; });
            map.on("mouseleave", PIN_LAYER_UNCLUSTERED, function () { map.getCanvas().style.cursor = ""; });
        }
        muteBaseMapStyle();
    }

    function muteBaseMapStyle() {
        if (!map || map._jobsyBaseMuted) {
            return;
        }
        map._jobsyBaseMuted = true;
        const style = map.getStyle && map.getStyle();
        if (!style || !Array.isArray(style.layers)) {
            return;
        }
        style.layers.forEach(function (layer) {
            if (!layer || !layer.id || String(layer.id).indexOf("jobsy-") === 0) {
                return;
            }
            try {
                if (layer.type === "background" && map.getPaintProperty(layer.id, "background-color") != null) {
                    map.setPaintProperty(layer.id, "background-color", "#eef1f4");
                }
                if (layer.type === "fill") {
                    const id = String(layer.id).toLowerCase();
                    if (id.indexOf("water") >= 0) {
                        map.setPaintProperty(layer.id, "fill-color", "#c5d4e0");
                    } else if (id.indexOf("park") >= 0 || id.indexOf("landuse") >= 0 || id.indexOf("grass") >= 0) {
                        map.setPaintProperty(layer.id, "fill-color", "#e7ebe6");
                        try { map.setPaintProperty(layer.id, "fill-opacity", 0.55); } catch (e) { }
                    } else if (id.indexOf("building") >= 0) {
                        map.setPaintProperty(layer.id, "fill-color", "#dde2e6");
                    }
                }
                if (layer.type === "line") {
                    const id = String(layer.id).toLowerCase();
                    if (id.indexOf("road") >= 0 || id.indexOf("highway") >= 0 || id.indexOf("street") >= 0) {
                        try { map.setPaintProperty(layer.id, "line-color", "#d5dbe2"); } catch (e) { }
                        try { map.setPaintProperty(layer.id, "line-opacity", 0.85); } catch (e) { }
                    }
                }
            } catch (e) { }
        });
    }

    function openClusterListFromClusterId(source, clusterId, coords, total) {
        if (!source || clusterId == null || typeof source.getClusterLeaves !== "function") {
            return;
        }
        const cap = Math.min(Math.max(Number(total) || 2, 2), 100);
        Promise.resolve(source.getClusterLeaves(clusterId, cap, 0))
            .then(function (leaves) {
                if (!map) {
                    return;
                }
                const childMarkers = (leaves || [])
                    .map(function (leaf) {
                        return markerByLeafId(leaf.properties && leaf.properties.id);
                    })
                    .filter(Boolean);
                if (childMarkers.length >= 1) {
                    openClusterList(childMarkers, coords);
                }
            })
            .catch(function () { });
    }

    function onClusterClick(ev) {
        if (!map || !ev.features || !ev.features.length) {
            return;
        }
        if (lagenLayout && !isNarrowViewport()) {
            const feature = ev.features[0];
            const props = feature.properties || {};
            const source = map.getSource(PIN_SOURCE);
            const coords = feature.geometry && feature.geometry.coordinates;
            if (source && coords && props.cluster_id != null) {
                if (ev.originalEvent && typeof ev.originalEvent.stopPropagation === "function") {
                    ev.originalEvent.stopPropagation();
                }
                lastClusterTapAt = Date.now();
                openClusterListFromClusterId(
                    source,
                    props.cluster_id,
                    coords,
                    Math.max(Number(props.point_count) || 2, 2));
                return;
            }
        }
        const feature = ev.features[0];
        const props = feature.properties || {};
        const source = map.getSource(PIN_SOURCE);
        if (!source) {
            return;
        }
        if (ev.originalEvent && typeof ev.originalEvent.stopPropagation === "function") {
            ev.originalEvent.stopPropagation();
        }
        lastClusterTapAt = Date.now();
        const clusterId = props.cluster_id;
        if (clusterId == null) {
            return;
        }
        const coords = feature.geometry && feature.geometry.coordinates;
        if (!coords) {
            return;
        }
        const total = Math.max(Number(props.point_count) || 2, 2);
        const zoomNow = map.getZoom();
        const maxZ = BASEMAP_MAX_ZOOM;

        function openCompactListAtMax() {
            openClusterListFromClusterId(source, clusterId, coords, total);
        }

        function easeToClusterZoom(targetZoom) {
            map.easeTo({
                center: coords,
                zoom: clampBanenZoom(targetZoom),
                duration: 320
            });
        }

        if (zoomNow >= maxZ - 0.02) {
            openCompactListAtMax();
            return;
        }

        if (typeof source.getClusterExpansionZoom !== "function") {
            const fallback = clampBanenZoom(Math.min(zoomNow + 1.75, maxZ));
            if (fallback <= zoomNow + 0.05) {
                openCompactListAtMax();
            } else {
                easeToClusterZoom(fallback);
            }
            return;
        }

        Promise.resolve(source.getClusterExpansionZoom(clusterId))
            .then(function (expansionZoom) {
                if (!map) {
                    return;
                }
                const exp = typeof expansionZoom === "number" ? expansionZoom : zoomNow + 2;
                let target = clampBanenZoom(Math.min(exp, maxZ));
                if (map.getZoom() >= maxZ - 0.02 || target <= map.getZoom() + 0.05) {
                    openCompactListAtMax();
                    return;
                }
                easeToClusterZoom(target);
            })
            .catch(function () {
                if (!map) {
                    return;
                }
                const target = clampBanenZoom(Math.min(map.getZoom() + 1.5, maxZ));
                if (target <= map.getZoom() + 0.05) {
                    openCompactListAtMax();
                } else {
                    easeToClusterZoom(target);
                }
            });
    }

    function onPinClick(ev) {
        // Cluster layer click can also hit unclustered layers in the same gesture.
        if (lastClusterTapAt && (Date.now() - lastClusterTapAt) < 500) {
            return;
        }
        if (!ev.features || !ev.features.length) {
            return;
        }
        const id = ev.features[0].properties && ev.features[0].properties.id;
        const record = markerByLeafId(id);
        if (!record) {
            return;
        }
        const key = coordKey(record.lat, record.lng);
        const sameSpot = key && markersByCoordKey[key] ? markersByCoordKey[key] : null;
        if (sameSpot && sameSpot.length > 1) {
            lastClusterTapAt = Date.now();
            openClusterList(sameSpot, [record.lng, record.lat]);
            return;
        }
        // Prefetch card on interaction (also bound via pointerdown on canvas).
        if (typeof fetchVacancyCard === "function") {
            fetchVacancyCard(id);
        }
        highlight(id);
        openVacancyPopup(record);
    }

    function clearRenderedMarkers() {
        renderedMarkers = [];
        // Native layers keep data in the GeoJSON source — nothing DOM to clear.
    }

    function refreshClusters() {
        if (!map) {
            return;
        }
        if (!map.isStyleLoaded || !map.isStyleLoaded()) {
            map.once("load", refreshClusters);
            return;
        }
        function pushPinData() {
            ensurePinLayers();
            if (lagenLayout) {
                applyLagenPinLayerStyles();
            }
            const source = map.getSource(PIN_SOURCE);
            if (source && typeof source.setData === "function") {
                source.setData(pinsGeoJson());
            }
            refreshAgencyAreas();
            if (selectedId != null) {
                highlight(selectedId);
            }
        }
        if (lagenLayout) {
            pushPinData();
            ensureLagenPinImages(function () {
                applyLagenPinLayerStyles();
                const source = map.getSource(PIN_SOURCE);
                if (source && typeof source.setData === "function") {
                    source.setData(pinsGeoJson());
                }
                if (selectedId != null) {
                    highlight(selectedId);
                }
            });
            return;
        }
        pushPinData();
    }

    function restoreOverlays() {
        if (kkMode) {
            refreshKkAreasData();
            if (lastOrigin) {
                drawKkRadiusRing(lastOrigin.lat, lastOrigin.lng);
            }
            if (window.jobsyMapLibre) {
                window.jobsyMapLibre.hideChrome(map);
            }
            kkResizeMapAndFit(false);
            return;
        }
        refreshClusters();
        flushPendingCatalogPins();
        if (lastOrigin) {
            ensureOriginMarker(lastOrigin.lat, lastOrigin.lng);
            drawTravelRings(lastOrigin.lat, lastOrigin.lng);
        }
        if (window.jobsyMapLibre) {
            window.jobsyMapLibre.hideChrome(map);
        }
        syncLagenAttributionCompact();
        if (lagenLayout && map && !map._jobsyLagenAttribSyncBound) {
            map._jobsyLagenAttribSyncBound = true;
            map.on("idle", syncLagenAttributionCompact);
            map.on("resize", syncLagenAttributionCompact);
        }
        if (lagenLayout) {
            scheduleLagenLayoutResize();
        }
    }

    function createMapInstance(el, openingPoints, openingView, preferFilledLocation) {
        firstSizedFit = false;
        lastFitPoints = [];
        tileLayer = null;
        selectedId = null;
        cameraLocked = false;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        ringRedrawBound = false;
        ringStyleHandlerBound = false;
        ringRedrawTries = 0;

        const opening = openingCamera(openingPoints, openingView, preferFilledLocation);
        if (openingPoints && openingPoints.length) {
            lastFitPoints = openingPoints.slice();
        }

        mapCreateCount += 1;
        map = window.jobsyMapLibre.createMap(el, {
            center: opening.center,
            zoom: opening.zoom,
            controlsPosition: "bottom-right",
            compactAttribution: !!(lagenLayout || catalogModeActive)
        });
        cameraLocked = opening.locked;
        firstSizedFit = true;
        applyBasemapZoomLimits();
        map.on("zoomend", enforceBanenZoomClamp);
        map.on("moveend", enforceBanenZoomClamp);
        map._jobsyOnStyleRestored = restoreOverlays;
        map.on("load", function () {
            restoreOverlays();
        });
        if (typeof map.loaded === "function" && map.loaded()) {
            restoreOverlays();
        }
        window.addEventListener("resize", invalidate);
        // Pane width can change without a window resize (e.g. after the mobile
        // grid overflow fix shrinks .map-pane from ~662px to 390px). Keep the
        // MapLibre canvas in sync with the visible pane.
        if (typeof ResizeObserver === "function" && el) {
            try {
                if (map._resizeObserver) {
                    try { map._resizeObserver.disconnect(); } catch (e0) { /* ignore */ }
                }
                map._resizeObserver = new ResizeObserver(function () {
                    if (kkMode) {
                        kkResizeMapAndFit(false);
                    } else {
                        invalidate();
                        if (map && Object.keys(markersById).length > 0) {
                            refreshClusters();
                        }
                        flushPendingCatalogPins();
                    }
                });
                map._resizeObserver.observe(el);
                const mapStage = el.closest && el.closest(".map-stage");
                if (mapStage && mapStage !== el) {
                    map._resizeObserver.observe(mapStage);
                }
                const mapPane = el.closest && el.closest(".map-pane");
                if (mapPane && mapPane !== el && mapPane !== mapStage) {
                    map._resizeObserver.observe(mapPane);
                }
            } catch (eRo) { /* ignore */ }
        }
        // First layout pass after Blazor attach may still be mid-reflow.
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () { invalidate(); });
        } else {
            setTimeout(invalidate, 0);
        }
    }

    /**
     * Blazor interactive attach often replaces #job-map with a fresh empty host.
     * Move the live MapLibre DOM into that host and rebind internals — do not
     * dispose()/createMap a second instance (that raced pins ETag → empty map).
     */
    function adoptMapContainer(host) {
        if (!map || !host) {
            return false;
        }
        const old = typeof map.getContainer === "function" ? map.getContainer() : null;
        if (!old) {
            return false;
        }
        if (old === host) {
            return !!host.isConnected;
        }
        if (!host.isConnected) {
            return false;
        }
        try {
            if (old.classList && host.classList) {
                old.classList.forEach(function (cls) {
                    if (cls) {
                        host.classList.add(cls);
                    }
                });
            }
            while (old.firstChild) {
                host.appendChild(old.firstChild);
            }
            if (map._resizeObserver) {
                try { map._resizeObserver.unobserve(old); } catch (e1) { }
                try { map._resizeObserver.observe(host); } catch (e2) { }
                const mapStage = host.closest && host.closest(".map-stage");
                if (mapStage && mapStage !== host) {
                    try { map._resizeObserver.observe(mapStage); } catch (e3) { }
                }
                const mapPane = host.closest && host.closest(".map-pane");
                if (mapPane && mapPane !== host && mapPane !== mapStage) {
                    try { map._resizeObserver.observe(mapPane); } catch (e4) { }
                }
            }
            map._container = host;
            if (typeof map.resize === "function") {
                map.resize();
            }
            refreshClusters();
            flushPendingCatalogPins();
            return !!(host.isConnected && map.getContainer() === host);
        } catch (err) {
            return false;
        }
    }

    function bindMapRuntime() {
        if (!map) {
            return;
        }
        if (!clusterGroup) {
            clusterGroup = {
                refreshClusters: refreshClusters,
                clearLayers: function () {
                    clearRenderedMarkers();
                    markersById = {};
                    markersByCoordKey = {};
                },
                zoomToShowLayer: function (record, cb) {
                    if (lagenLayout && catalogModeActive && isNarrowViewport()) {
                        if (typeof cb === "function") {
                            cb();
                        }
                        return;
                    }
                    map.easeTo({
                        center: [record.lng, record.lat],
                        zoom: clampBanenZoom(Math.max(map.getZoom(), CLUSTER_OPTS.disableClusteringAtZoom)),
                        duration: 280
                    });
                    map.once("idle", function () {
                        refreshClusters();
                        if (typeof cb === "function") {
                            cb();
                        }
                    });
                }
            };
        }
        // Prefetch card on touch/pointer before click so the popup fills faster.
        if (!zoomHandlerBound) {
            zoomHandlerBound = true;
            const prefetchAtPoint = function (ev) {
                if (!ev || !ev.point || !map) {
                    return;
                }
                const prefetchLayers = [PIN_LAYER_UNCLUSTERED, PIN_LAYER_UNCLUSTERED_GLYPH];
                if (map.getLayer(PIN_LAYER_UNCLUSTERED_SYMBOL)) {
                    prefetchLayers.push(PIN_LAYER_UNCLUSTERED_SYMBOL);
                }
                const feats = map.queryRenderedFeatures(ev.point, {
                    layers: prefetchLayers
                });
                if (feats && feats[0] && feats[0].properties && feats[0].properties.id) {
                    fetchVacancyCard(feats[0].properties.id);
                }
            };
            map.on("pointerdown", prefetchAtPoint);
            map.on("touchstart", prefetchAtPoint);
        }
        addLocateControl();
        bindOutsideClickCloser();
        bindTravelRingStyleGuard();
        if (!map._jobsyIsoClampBound) {
            map._jobsyIsoClampBound = true;
            map.on("moveend", function () {
                if (isochroneLabelMarkers.length) {
                    clampIsoLabelMarkersToMap();
                }
            });
            map.on("zoomend", function () {
                if (isochroneLabelMarkers.length) {
                    clampIsoLabelMarkersToMap();
                }
            });
        }
        flushPendingCatalogPins();
    }

    function readBootPayload() {
        const node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return { pins: [], view: null, preferFilledLocation: true, pinsUrl: null };
        }
        try {
            const parsed = JSON.parse(node.textContent);
            if (Array.isArray(parsed)) {
                return { pins: parsed, view: null, preferFilledLocation: true, pinsUrl: null };
            }
            const pins = parsed && Array.isArray(parsed.pins) ? parsed.pins : [];
            let view = parsed ? readOpeningView(parsed.view) : null;
            const preferFilledLocation = !parsed || parsed.preferFilledLocation !== false;
            if (preferFilledLocation) {
                const filled = readFilledOrigin();
                if (filled) {
                    view = filled;
                }
            }
            const bootPinsUrl = parsed && parsed.pinsUrl ? String(parsed.pinsUrl) : null;
            const catalogUrl = parsed && parsed.catalogUrl ? String(parsed.catalogUrl) : null;
            return {
                pins: pins,
                view: view,
                preferFilledLocation: preferFilledLocation,
                pinsUrl: bootPinsUrl,
                catalogUrl: catalogUrl,
                catalogMode: !!(parsed && parsed.catalogMode),
                illustratedPopup: !!(parsed && parsed.illustratedPopup),
                zoomToPinsOutsideTravel: !!(parsed && parsed.zoomToPinsOutsideTravel),
                mapInit: parsed && parsed.mapInit ? parsed.mapInit : null,
                origin: parsed && parsed.origin ? parsed.origin : null
            };
        } catch (e) {
            return { pins: [], view: null, preferFilledLocation: true, pinsUrl: null };
        }
    }

    function reconcileBootFromDom() {
        const bootPayload = readBootPayload();
        if (!bootPayload) {
            return;
        }
        catalogModeActive = bootPayload.catalogMode === true;
        if (catalogModeActive) {
            clearPinsFetchState();
        }
        const mapInit = bootPayload.mapInit || null;
        lagenLayout = !!(mapInit && mapInit.lagenLayout);
        if (lagenLayout) {
            illustratedPopup = false;
        }
        if (mapInit && mapInit.labels && typeof mapInit.labels === "object") {
            uiLabels = Object.assign({}, uiLabels, mapInit.labels);
            uiLabels.canSave = mapInit.labels.canSave === true || mapInit.labels.canSave === "true";
        }
        let travel = null;
        if (mapInit && mapInit.travel) {
            travel = mapInit.travel;
            normalizeTravelOptions(travel);
        }
        let origin = bootPayload.origin || null;
        if (!origin && mapInit && mapInit.filters
            && mapInit.filters.originLat != null
            && mapInit.filters.originLng != null) {
            origin = {
                lat: Number(mapInit.filters.originLat),
                lng: Number(mapInit.filters.originLng)
            };
        }
        if (origin && typeof setOrigin === "function") {
            try {
                setOrigin(origin.lat, origin.lng, travel || travelOptions);
            } catch (eOrigin) { }
        }
        if (catalogModeActive) {
            syncCatalogToMap();
        }
    }

    /**
     * Start the map from #jobsy-map-boot before the Blazor circuit is up.
     * Blazor jobMap.init reuses that MapLibre instance (same host or adoptMapContainer).
     */
    function boot(elementId) {
        const id = elementId || "job-map";
        if (typeof maplibregl === "undefined" || !window.jobsyMapLibre) {
            return;
        }
        const el = document.getElementById(id);
        if (!el) {
            return;
        }
        if (map && typeof map.getContainer === "function") {
            const host = map.getContainer();
            if (host === el && el.isConnected) {
                reconcileBootFromDom();
                return;
            }
            // Boot map already exists on a prior host — adopt into current #job-map.
            if (adoptMapContainer(el)) {
                reconcileBootFromDom();
                return;
            }
        }
        const bootPayload = readBootPayload();
        const mapInit = bootPayload.mapInit || null;
        let origin = bootPayload.origin || null;
        let travel = null;
        if (mapInit) {
            if (mapInit.travel) {
                travel = mapInit.travel;
            }
            if (!origin && mapInit.filters
                && mapInit.filters.originLat != null
                && mapInit.filters.originLng != null) {
                origin = {
                    lat: Number(mapInit.filters.originLat),
                    lng: Number(mapInit.filters.originLng)
                };
            }
        }
        try {
            init(id, bootPayload.pins || [], {
                view: bootPayload.view,
                preferFilledLocation: bootPayload.preferFilledLocation !== false,
                pinsUrl: bootPayload.pinsUrl || null,
                catalogUrl: bootPayload.catalogUrl || null,
                catalogMode: bootPayload.catalogMode === true,
                illustratedPopup: bootPayload.illustratedPopup,
                mapInit: mapInit,
                origin: origin,
                travel: travel,
                zoomToPinsOutsideTravel: bootPayload.zoomToPinsOutsideTravel
            });
        } catch (_err) {
            // Circuit init will retry.
        }
    }

    function readBootFiltersFromDom() {
        try {
            const node = document.getElementById("jobsy-map-boot");
            if (!node || !node.textContent) {
                return null;
            }
            const parsed = JSON.parse(node.textContent);
            return parsed && parsed.mapInit && parsed.mapInit.filters ? parsed.mapInit.filters : null;
        } catch (e) {
            return null;
        }
    }

    function syncCatalogToMap() {
        if (!catalogModeActive || !window.jobsyBanenkaartCatalog) {
            return;
        }
        const filters = readBootFiltersFromDom();
        const apply = function () {
            if (typeof window.jobsyBanenkaartCatalog.applyToMap === "function") {
                window.jobsyBanenkaartCatalog.applyToMap(filters);
            }
        };
        if (typeof window.jobsyBanenkaartCatalog.isReady === "function"
            && window.jobsyBanenkaartCatalog.isReady()) {
            apply();
            return;
        }
        if (typeof window.jobsyBanenkaartCatalog.load === "function") {
            window.jobsyBanenkaartCatalog.load().then(apply).catch(function () { });
        }
    }

    function kkProp(a, camel, pascal) {
        if (!a) {
            return undefined;
        }
        if (a[camel] != null) {
            return a[camel];
        }
        return a[pascal];
    }

    function kkLooksLikeLatLngPair(x, y) {
        return Math.abs(x) > 20 && Math.abs(y) < 20;
    }

    function kkNormalizeLngLatPair(x, y) {
        if (!Number.isFinite(x) || !Number.isFinite(y)) {
            return null;
        }
        if (kkLooksLikeLatLngPair(x, y)) {
            return [y, x];
        }
        return [x, y];
    }

    function kkAreasGeoJson(areas, selectedKey) {
        var features = (areas || []).map(function (a) {
            var ring = a.polygonRing || a.PolygonRing || [];
            var coords = ring.map(function (p) {
                if (!p || p.length < 2) {
                    return null;
                }
                var pair = kkNormalizeLngLatPair(Number(p[0]), Number(p[1]));
                return pair;
            }).filter(Boolean);
            if (coords.length && (coords[0][0] !== coords[coords.length - 1][0] || coords[0][1] !== coords[coords.length - 1][1])) {
                coords.push(coords[0]);
            }
            return {
                type: "Feature",
                properties: {
                    key: a.areaKey || a.AreaKey,
                    label: a.label || a.Label,
                    count: kkProp(a, "candidateCount", "CandidateCount"),
                    selected: (a.areaKey || a.AreaKey) === selectedKey
                },
                geometry: { type: "Polygon", coordinates: [coords] }
            };
        });
        return { type: "FeatureCollection", features: features };
    }

    var KK_LABEL_OFFSETS = [
        [0, 0],
        [0.85, -0.65],
        [-0.85, -0.65],
        [0.65, 0.75],
        [-0.65, 0.75]
    ];

    function kkLabelOffsetIndex(key, idx) {
        if (!key) {
            return idx % KK_LABEL_OFFSETS.length;
        }
        var h = 0;
        for (var i = 0; i < key.length; i++) {
            h = ((h << 5) - h) + key.charCodeAt(i);
            h |= 0;
        }
        return Math.abs(h) % KK_LABEL_OFFSETS.length;
    }

    function kkLabelsGeoJson(areas) {
        var features = (areas || []).map(function (a, idx) {
            var lat = kkProp(a, "centerLat", "CenterLat");
            var lng = kkProp(a, "centerLng", "CenterLng");
            var count = kkProp(a, "candidateCount", "CandidateCount");
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
                return null;
            }
            var key = a.areaKey || a.AreaKey;
            var off = KK_LABEL_OFFSETS[kkLabelOffsetIndex(key, idx)];
            var labelLng = lng + off[0] * 0.018;
            var labelLat = lat + off[1] * 0.018;
            return {
                type: "Feature",
                properties: {
                    key: key,
                    count: count != null ? String(count) : ""
                },
                geometry: { type: "Point", coordinates: [labelLng, labelLat] }
            };
        }).filter(Boolean);
        return { type: "FeatureCollection", features: features };
    }

    function applyKkViewOptions(viewOpts) {
        if (!viewOpts) {
            return;
        }
        kkViewOptions = viewOpts;
        var travelKm = Number(kkProp(viewOpts, "travelKm", "TravelKm"));
        if (Number.isFinite(travelKm) && travelKm > 0) {
            travelOptions.radiusKm = travelKm;
        }
        var originLat = kkProp(viewOpts, "originLat", "OriginLat");
        var originLng = kkProp(viewOpts, "originLng", "OriginLng");
        if (Number.isFinite(originLat) && Number.isFinite(originLng)) {
            lastOrigin = { lat: originLat, lng: originLng };
        }
    }

    var KK_FIT_PADDING = 48;

    function kkUnionZoneBounds(areas) {
        var bounds = new maplibregl.LngLatBounds();
        var hasPoint = false;
        (areas || []).forEach(function (a, idx) {
            var ring = a.polygonRing || a.PolygonRing || [];
            ring.forEach(function (p) {
                if (!p || p.length < 2) {
                    return;
                }
                var pair = kkNormalizeLngLatPair(Number(p[0]), Number(p[1]));
                if (pair) {
                    bounds.extend(pair);
                    hasPoint = true;
                }
            });
            var lat = kkProp(a, "centerLat", "CenterLat");
            var lng = kkProp(a, "centerLng", "CenterLng");
            if (Number.isFinite(lat) && Number.isFinite(lng)) {
                bounds.extend([lng, lat]);
                hasPoint = true;
                var key = a.areaKey || a.AreaKey;
                var off = KK_LABEL_OFFSETS[kkLabelOffsetIndex(key, idx)];
                bounds.extend([lng + off[0] * 0.018, lat + off[1] * 0.018]);
            }
        });
        return hasPoint ? bounds : null;
    }

    function syncKkMobileInviteChrome() {
        try {
            var nav = document.querySelector(".wg-bottom-nav");
            var invite = document.querySelector(".kk-mobile-sheet__invite");
            if (!nav || !invite) {
                return;
            }
            var style = window.getComputedStyle(nav);
            if (style.display === "none" || style.visibility === "hidden") {
                return;
            }
            var h = Math.ceil(nav.getBoundingClientRect().height);
            if (h > 0) {
                document.documentElement.style.setProperty("--kk-wg-nav-offset", h + "px");
            }
        } catch (eSync) { /* ignore */ }
    }

    function kkResizeMapAndFit(animate) {
        if (!map || !kkMode) {
            return;
        }
        var runFit = function () {
            try {
                var container = typeof map.getContainer === "function" ? map.getContainer() : null;
                if (container) {
                    container.style.width = "100%";
                    container.style.height = "100%";
                }
                if (typeof map.resize === "function") {
                    map.resize();
                }
            } catch (e0) { /* ignore */ }
            if (mapHasUsableSize()) {
                fitKkView(animate);
            }
            syncKkMobileInviteChrome();
        };
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(runFit);
            });
        } else {
            setTimeout(runFit, 0);
        }
    }

    function kkScheduleLayoutPasses() {
        if (!map || !kkMode) {
            return;
        }
        syncKkMobileInviteChrome();
        [0, 80, 200, 450, 900].forEach(function (delay) {
            setTimeout(function () {
                kkResizeMapAndFit(false);
                syncKkMobileInviteChrome();
            }, delay);
        });
    }

    function drawKkRadiusRing(lat, lng) {
        if (!map || !kkMode || typeof map.addSource !== "function") {
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            map.once("load", function () { drawKkRadiusRing(lat, lng); });
            return;
        }
        clearTravelRings();
        var travelKm = Number(travelOptions.radiusKm) || 25;
        var radiusM = travelKm * 1000;
        var feature = {
            type: "Feature",
            properties: {},
            geometry: { type: "Polygon", coordinates: circlePolygon(lat, lng, radiusM) }
        };
        var data = { type: "FeatureCollection", features: [feature] };
        try {
            if (!map.getSource(KK_RADIUS_SOURCE)) {
                map.addSource(KK_RADIUS_SOURCE, { type: "geojson", data: data });
                map.addLayer({
                    id: KK_RADIUS_FILL,
                    type: "fill",
                    source: KK_RADIUS_SOURCE,
                    paint: {
                        "fill-color": "rgba(15, 45, 92, 0.05)"
                    }
                }, KK_FILL);
                map.addLayer({
                    id: KK_RADIUS_LINE,
                    type: "line",
                    source: KK_RADIUS_SOURCE,
                    paint: {
                        "line-color": "rgba(15, 45, 92, 0.32)",
                        "line-width": 1.25,
                        "line-dasharray": [2, 2.5]
                    }
                }, KK_FILL);
            } else {
                map.getSource(KK_RADIUS_SOURCE).setData(data);
            }
        } catch (e) { /* ignore */ }
    }

    function fitKkView(animate) {
        if (!map || !kkMode) {
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            map.once("load", function () { fitKkView(animate); });
            return;
        }
        try {
            if (typeof map.resize === "function") {
                map.resize();
            }
        } catch (eResize) { /* ignore */ }
        if (!mapHasUsableSize()) {
            return;
        }
        applyKkViewOptions(kkViewOptions);
        if (lastOrigin) {
            drawKkRadiusRing(lastOrigin.lat, lastOrigin.lng);
        }
        var travelKm = Number(travelOptions.radiusKm) || 25;
        var bounds = kkUnionZoneBounds(kkAreasCache);
        if (!bounds || bounds.isEmpty()) {
            return;
        }
        var sw = bounds.getSouthWest();
        var ne = bounds.getNorthEast();
        try {
            console.info("[jobMap.kk] fitBounds", {
                sw: [sw.lng, sw.lat],
                ne: [ne.lng, ne.lat],
                areas: (kkAreasCache || []).length,
                travelKm: travelKm,
                origin: lastOrigin
            });
        } catch (logErr) { /* ignore */ }
        runWithViewportNotifySuppressed(function () {
            try {
                safeFitBounds(bounds, {
                    padding: KK_FIT_PADDING,
                    maxZoom: 11.25,
                    animate: animate === true && !prefersReducedMotion(),
                    duration: animate === true && !prefersReducedMotion() ? 450 : 0
                });
                map.once("idle", function () {
                    var z = map.getZoom();
                    if (z < 9.75) {
                        safeFitBounds(bounds, {
                            padding: KK_FIT_PADDING,
                            maxZoom: 11.25,
                            animate: false,
                            duration: 0
                        });
                    }
                });
            } catch (e) { /* ignore */ }
        });
    }

    function panKkToSelectedArea(selectedKey) {
        if (!map || !selectedKey) {
            return;
        }
        var area = (kkAreasCache || []).find(function (a) {
            return (a.areaKey || a.AreaKey) === selectedKey;
        });
        if (!area) {
            return;
        }
        var lat = kkProp(area, "centerLat", "CenterLat");
        var lng = kkProp(area, "centerLng", "CenterLng");
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return;
        }
        runWithViewportNotifySuppressed(function () {
            try {
                map.easeTo({ center: [lng, lat], duration: prefersReducedMotion() ? 0 : 450 });
            } catch (e) { /* ignore */ }
        });
    }

    var kkAreasCache = [];
    var kkSelectedKey = null;

    function ensureKkLayers() {
        if (!map || !kkMode) {
            return;
        }
        if (!map.getSource(KK_SOURCE)) {
            map.addSource(KK_SOURCE, { type: "geojson", data: kkAreasGeoJson([], null) });
            map.addLayer({
                id: KK_FILL,
                type: "fill",
                source: KK_SOURCE,
                paint: {
                    "fill-color": [
                        "case",
                        ["boolean", ["get", "selected"], false],
                        "rgba(220, 80, 70, 0.38)",
                        "rgba(220, 80, 70, 0.22)"
                    ],
                    "fill-outline-color": "rgba(180, 50, 45, 0.55)"
                }
            });
            map.addLayer({
                id: KK_LINE,
                type: "line",
                source: KK_SOURCE,
                paint: {
                    "line-color": [
                        "case",
                        ["boolean", ["get", "selected"], false],
                        "rgba(180, 45, 40, 0.95)",
                        "rgba(200, 70, 65, 0.65)"
                    ],
                    "line-width": [
                        "case",
                        ["boolean", ["get", "selected"], false],
                        2.5,
                        1.25
                    ]
                }
            });
            map.addSource(KK_LABELS_SOURCE, { type: "geojson", data: kkLabelsGeoJson([]) });
            map.addLayer({
                id: KK_LABELS_HALO,
                type: "circle",
                source: KK_LABELS_SOURCE,
                paint: {
                    "circle-radius": 15,
                    "circle-color": "#fffcfa",
                    "circle-stroke-color": "rgba(190, 60, 55, 0.85)",
                    "circle-stroke-width": 2
                }
            });
            map.addLayer({
                id: KK_LABELS,
                type: "symbol",
                source: KK_LABELS_SOURCE,
                layout: {
                    "text-field": ["to-string", ["coalesce", ["get", "count"], 0]],
                    "text-size": 13,
                    "text-font": ["Noto Sans Bold"],
                    "text-allow-overlap": true,
                    "text-ignore-placement": true
                },
                paint: {
                    "text-color": "#7f1d1d",
                    "text-halo-color": "#fff",
                    "text-halo-width": 1.25
                }
            });
            map.on("click", KK_FILL, function (e) {
                if (!e.features || !e.features.length) {
                    return;
                }
                var key = e.features[0].properties.key;
                if (kkSelectCallback && key) {
                    kkSelectCallback(key);
                }
            });
            map.getCanvas().style.cursor = "pointer";
        }
    }

    function refreshKkLabelsData() {
        if (!map || !kkMode) {
            return;
        }
        var source = map.getSource(KK_LABELS_SOURCE);
        if (source && typeof source.setData === "function") {
            source.setData(kkLabelsGeoJson(kkAreasCache));
        }
    }

    function refreshKkAreasData() {
        if (!map || !kkMode) {
            return;
        }
        if (!map.isStyleLoaded || !map.isStyleLoaded()) {
            map.once("load", refreshKkAreasData);
            return;
        }
        ensureKkLayers();
        var source = map.getSource(KK_SOURCE);
        if (source && typeof source.setData === "function") {
            source.setData(kkAreasGeoJson(kkAreasCache, kkSelectedKey));
        }
        refreshKkLabelsData();
    }

    function initKandidatenkaart(elementId, options) {
        options = options || {};
        var el = document.getElementById(elementId);
        if (!el) {
            throw new Error("Map element #" + elementId + " not found");
        }
        if (map) {
            dispose();
        }
        kkMode = true;
        kkInitialFitDone = false;
        if (options.dotNetRef) {
            kkSelectCallback = function (key) {
                try {
                    options.dotNetRef.invokeMethodAsync("OnAreaSelected", key);
                } catch (e) { /* circuit gone */ }
            };
        } else {
            kkSelectCallback = typeof options.onAreaSelect === "function" ? options.onAreaSelect : null;
        }
        kkAreasCache = options.areas || [];
        kkSelectedKey = options.selectedAreaKey || null;
        applyKkViewOptions({
            originLat: options.originLat,
            originLng: options.originLng,
            travelKm: options.travelKm
        });
        var centerLng = lastOrigin ? lastOrigin.lng : 5.3878;
        var centerLat = lastOrigin ? lastOrigin.lat : 52.1561;
        createMapInstance(el, [], { lat: centerLat, lng: centerLng, zoom: 10.5 }, false);
        setVacancies([]);
        map.once("load", function () {
            refreshKkAreasData();
            kkResizeMapAndFit(false);
            kkScheduleLayoutPasses();
            kkInitialFitDone = true;
            syncKkMobileInviteChrome();
            if (typeof window !== "undefined" && !window._kkInviteNavResizeBound) {
                window._kkInviteNavResizeBound = true;
                window.addEventListener("resize", syncKkMobileInviteChrome, { passive: true });
            }
        });
    }

    function setKandidatenkaartAreas(areas, selectedKey, viewOptions) {
        kkAreasCache = areas || [];
        var prevKey = kkSelectedKey;
        kkSelectedKey = selectedKey || null;
        if (viewOptions) {
            applyKkViewOptions(viewOptions);
        }
        refreshKkAreasData();
        if (viewOptions) {
            kkResizeMapAndFit(kkInitialFitDone);
        } else if (selectedKey && selectedKey !== prevKey) {
            panKkToSelectedArea(selectedKey);
        }
    }

    function init(elementId, vacancies, options) {
        kkMode = false;
        if (typeof maplibregl === "undefined") {
            throw new Error("MapLibre GL JS (maplibregl) is not loaded");
        }
        if (!window.jobsyMapLibre) {
            throw new Error("jobsyMapLibre is not loaded");
        }

        const el = document.getElementById(elementId);
        if (!el) {
            throw new Error("Map element #" + elementId + " not found");
        }

        const openingPoints = collectVacancyPoints(vacancies || []);
        const openingView = options && options.view ? options.view : null;
        const preferFilledLocation = !options || options.preferFilledLocation !== false;
        const filledOrigin = preferFilledLocation ? readFilledOrigin() : null;
        const hasOrigin = !!(options && options.origin) || !!filledOrigin;
        let live = !!(map && typeof map.getContainer === "function"
            && map.getContainer() === el && el.isConnected);

        if (!live && map) {
            // Early boot map is still alive but Blazor swapped the #job-map node —
            // attach handlers to the existing MapLibre instance instead of dispose+rebuild.
            if (adoptMapContainer(el)) {
                live = true;
            } else {
                dispose();
            }
        }

        if (!live) {
            deferMapReveal = hasOrigin;
            try {
                createMapInstance(el, openingPoints, openingView, preferFilledLocation);
            } catch (e) {
                createMapInstance(el, openingPoints, null, false);
            }
        }

        companyMapMode = !!(options && options.mapMode === "companies");
        if (companyMapMode) {
            uiLabels = Object.assign({}, uiLabels, {
                vacanciesAtPlace: "{count} bedrijven",
                vacancyAtPlace: "1 bedrijf",
                vacanciesAtPlaceWithPlace: "{count} bedrijven in {place}",
                vacancyAtPlaceWithPlace: "1 bedrijf in {place}",
                vacanciesInView: "{count} bedrijven in beeld",
                viewJob: "Bekijk bedrijf",
                apply: "Ik wil hier werken"
            });
        }
        openCallback = options && options.dotNetRef ? options.dotNetRef : null;
        viewportListDotNet = options && options.syncViewportList && options.dotNetRef
            ? options.dotNetRef
            : null;
        const labelSource = (options && options.labels && typeof options.labels === "object")
            ? options.labels
            : (options && options.mapInit && options.mapInit.labels && typeof options.mapInit.labels === "object"
                ? options.mapInit.labels
                : null);
        if (labelSource) {
            uiLabels = Object.assign({}, uiLabels, labelSource);
            uiLabels.canSave = labelSource.canSave === true || labelSource.canSave === "true";
        }
        normalizeTravelOptions(options && options.travel);
        highlightSeed = options && Number.isFinite(Number(options.highlightSeed))
            ? (Number(options.highlightSeed) >>> 0)
            : 0;
        const nextPinsUrl = options && options.pinsUrl ? String(options.pinsUrl) : null;
        if (nextPinsUrl) {
            pinsBaseUrl = stripViewportParams(nextPinsUrl);
            pinsUrl = pinsBaseUrl + viewportQueryString();
        }
        lagenLayout = readLagenLayoutFromOptions(options);
        if (lagenLayout) {
            illustratedPopup = false;
        } else {
            illustratedPopup = !!(options && options.illustratedPopup);
        }
        illustratedTravelSpot = options && options.illustratedTravelSpot
            ? String(options.illustratedTravelSpot)
            : "";
        illustratedVacancySpots = options && options.illustratedVacancySpots
            ? options.illustratedVacancySpots
            : {};

        catalogModeActive = !!(options && options.catalogMode);
        if (catalogModeActive) {
            clearPinsFetchState();
        }
        bootZoomOutsideTravel = !!(options && options.zoomToPinsOutsideTravel);

        bindMapRuntime();
        if (!catalogModeActive && (!live || !viewportPinsBound)) {
            bindViewportPinsReload();
        }

        // Early boot() may already have painted pins — do not re-fetch (one pins request/load).
        const alreadyPinned = live && Object.keys(markersById).length > 0;
        if (!alreadyPinned) {
            const seedPins = (vacancies || []).map(normalizePin).filter(Boolean);
            if (seedPins.length > 0) {
                setVacancies(seedPins);
            } else {
                // Prefer #jobsy-map-boot compact pins until HTTP pins arrive — never wait on the circuit.
                try {
                    const bootPayload = readBootPayload();
                    if (!pinsUrl && bootPayload.pinsUrl) {
                        pinsUrl = String(bootPayload.pinsUrl);
                    }
                    if (bootPayload && Array.isArray(bootPayload.pins) && bootPayload.pins.length) {
                        setVacancies(bootPayload.pins.map(normalizePin).filter(Boolean));
                    } else {
                        setVacancies([]);
                    }
                } catch (e) {
                    setVacancies([]);
                }
            }
            // Apply early prefetch (started from maps-loader / boot JSON) before a new fetch.
            // Match by filter key so Fiets/Auto prefetch URLs share one body.
            if (pinsUrl && !viewportQueryString() && window.__jobsyPinsPrefetch
                && pinsFilterKey(window.__jobsyPinsPrefetchUrl) === pinsFilterKey(pinsUrl)) {
                const gen = ++pinsFetchGen;
                Promise.resolve(window.__jobsyPinsPrefetch).then(function (data) {
                    if (gen !== pinsFetchGen || !data) {
                        if (pinsUrl) {
                            fetchPins(pinsUrl);
                        }
                        return;
                    }
                    rememberPinsFilter(pinsUrl, data, window.__jobsyPinsEtag || null);
                    const pins = (Array.isArray(data) ? data : []).map(normalizePin).filter(Boolean);
                    if (pins.length) {
                        setVacancies(pins);
                    } else if (pinsUrl) {
                        fetchPins(pinsUrl);
                    }
                });
            } else if (pinsUrl && !catalogModeActive) {
                fetchPins(pinsUrl);
            }
        } else if (!pinsUrl) {
            try {
                const bootPayload = readBootPayload();
                if (bootPayload.pinsUrl) {
                    pinsUrl = String(bootPayload.pinsUrl);
                }
            } catch (e) { }
        }
        var originApplied = false;
        if (options && options.origin) {
            try {
                setOrigin(options.origin.lat, options.origin.lng, options.travel);
                originApplied = true;
            } catch (e) { }
        } else if (filledOrigin) {
            try {
                setOrigin(filledOrigin.lat, filledOrigin.lng, options && options.travel);
                originApplied = true;
            } catch (e) { }
        }
        ensureVacancyTiles();
        if (!originApplied) {
            finishOpeningFrame();
        }

        if (catalogModeActive) {
            syncCatalogToMap();
        }

        invalidate();
    }

    function locateIconHtml() {
        return (
            "<svg class=\"job-map-locate__icon\" viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" aria-hidden=\"true\">" +
                "<circle cx=\"12\" cy=\"12\" r=\"3.2\" fill=\"currentColor\"/>" +
                "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
                    "d=\"M12 3v2.5M12 18.5V21M3 12h2.5M18.5 12H21\"/>" +
                "<circle cx=\"12\" cy=\"12\" r=\"7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/>" +
            "</svg>"
        );
    }

    function addLocateControl() {
        if (!map) return;
        const host = map.getContainer();
        if (host.querySelector(".job-map-locate")) {
            syncLocateButton();
            return;
        }

        const bar = document.createElement("div");
        bar.className = "job-map-locate";
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "job-map-locate__btn";
        btn.title = "Mijn locatie";
        btn.setAttribute("aria-label", "Mijn locatie");
        btn.innerHTML = locateIconHtml();
        bar.appendChild(btn);
        host.appendChild(bar);

        btn.addEventListener("click", function (ev) {
            stopEvent(ev);
            if (btn.classList.contains("is-busy")) return;
            btn.classList.add("is-busy");
            const done = function () {
                btn.classList.remove("is-busy");
            };

            if (openCallback) {
                openCallback.invokeMethodAsync("OnMapLocateClicked")
                    .then(done, done);
            } else if (window.jobsyGeo && typeof window.jobsyGeo.requestLocation === "function") {
                window.jobsyGeo.requestLocation()
                    .then(function (pos) {
                        if (!isInNetherlands(pos.lat, pos.lng)) {
                            showLocateMessage("Je locatie ligt buiten Nederland");
                            return;
                        }
                        showLocateMessage("");
                        setOrigin(pos.lat, pos.lng, travelOptions);
                        fitToOriginRings(true);
                    })
                    .catch(function (err) {
                        showLocateMessage(err && err.message ? err.message : "Locatie kon niet worden bepaald.");
                    })
                    .then(done, done);
            } else {
                done();
            }
        });

        syncLocateButton();
    }

    function syncLocateButton() {
        if (!map) return;
        const btn = map.getContainer().querySelector(".job-map-locate__btn");
        if (!btn) return;
        if (originMarker) {
            btn.classList.add("is-active");
        } else {
            btn.classList.remove("is-active");
        }
    }

    function pinsSignature(vacancies) {
        const list = vacancies || [];
        if (!list.length) {
            return "0";
        }
        let sig = String(list.length) + ":";
        for (let i = 0; i < list.length; i++) {
            const v = normalizePin(list[i]);
            if (v && v.id != null) {
                sig += String(v.id) + ",";
            }
        }
        return sig;
    }

    function setVacancies(vacancies) {
        if (!clusterGroup) return;
        setVacanciesCallCount += 1;

        const nextSig = pinsSignature(vacancies);
        if (nextSig === lastPinsSignature && Object.keys(markersById).length > 0) {
            return;
        }
        lastPinsSignature = nextSig;

        clusterGroup.clearLayers();
        markersById = {};
        markersByCoordKey = {};
        const bounds = [];

        (vacancies || []).forEach(function (raw) {
            const v = normalizePin(raw);
            if (!v) {
                return;
            }
            const pt = pointFromVacancy(v);
            if (!pt) {
                return;
            }
            const lat = pt[0];
            const lng = pt[1];
            const idKey = String(v.id);
            const record = {
                id: v.id,
                lat: lat,
                lng: lng,
                marker: null,
                element: null,
                options: { jobData: v },
                getLatLng: function () { return { lat: lat, lng: lng }; },
                getLngLat: function () { return { lng: lng, lat: lat }; }
            };
            markersById[idKey] = record;
            markersById[v.id] = record;
            const key = coordKey(lat, lng);
            if (key) {
                if (!markersByCoordKey[key]) {
                    markersByCoordKey[key] = [];
                }
                markersByCoordKey[key].push(record);
            }
            bounds.push([lat, lng]);
        });

        refreshClusters();
        ensureVacancyTiles();
        revealMapStage();
        tryRestoreBanenkaartMapViewOnce();
    }

    function reloadPins(url) {
        if (catalogModeActive) {
            return Promise.resolve([]);
        }
        if (url) {
            pinsBaseUrl = stripViewportParams(String(url));
        }
        if (pinsReloadTimer) {
            clearTimeout(pinsReloadTimer);
            pinsReloadTimer = null;
        }
        return new Promise(function (resolve) {
            pinsReloadTimer = setTimeout(function () {
                pinsReloadTimer = null;
                resolve(fetchPins(pinsBaseUrl || pinsUrl));
            }, 200);
        });
    }

    function notifyViewportList() {
        if (!viewportListDotNet || !map) {
            return;
        }
        try {
            var box = readViewportBounds();
            if (!box) {
                return;
            }
            viewportNotifyCount += 1;
            viewportListDotNet.invokeMethodAsync(
                "OnMapViewportChanged",
                box.minLat,
                box.minLng,
                box.maxLat,
                box.maxLng,
                box.zoom);
        } catch (e) { /* circuit */ }
    }

    function viewportUserIntent() {
        if (userGesturePending) {
            userGesturePending = false;
            return true;
        }
        return Date.now() - lastUserGestureAt < 900;
    }

    function bindViewportPinsReload() {
        if (catalogModeActive || !map) {
            return;
        }
        if (map._jobsyViewportPinsSchedule) {
            try {
                map.off("moveend", map._jobsyViewportPinsSchedule);
                map.off("zoomend", map._jobsyViewportPinsSchedule);
            } catch (eOff) { /* ignore */ }
        }
        if (!map._jobsyUserGestureBound) {
            map._jobsyUserGestureBound = true;
            map.on("dragstart", function (ev) {
                if (ev && ev.originalEvent) {
                    markUserMapGesture();
                }
            });
            map.on("zoomstart", function (ev) {
                if (ev && ev.originalEvent) {
                    markUserMapGesture();
                }
            });
        }
        viewportPinsBound = true;
        var debounce = null;
        function schedule() {
            if (viewportNotifySuppressed > 0) {
                return;
            }
            if (debounce) {
                clearTimeout(debounce);
            }
            debounce = setTimeout(function () {
                debounce = null;
                if (viewportNotifySuppressed > 0) {
                    return;
                }
                const user = viewportUserIntent();
                if (!viewportBootstrapDone) {
                    viewportBootstrapDone = true;
                    reloadPins();
                    notifyViewportList();
                    return;
                }
                if (!user) {
                    return;
                }
                reloadPins();
                notifyViewportList();
            }, 300);
        }
        map._jobsyViewportPinsSchedule = schedule;
        map.on("moveend", schedule);
        map.on("zoomend", schedule);
        if (!map._jobsyViewportPinsBootstrapped) {
            map._jobsyViewportPinsBootstrapped = true;
            setTimeout(schedule, 80);
        }
    }

    function ensureOriginMarker(la, ln) {
        if (originMarker) {
            originMarker.setLngLat([ln, la]);
            return;
        }
        const el = document.createElement("div");
        if (lagenLayout) {
            el.className = "job-map-origin home";
            el.innerHTML = "<span class=\"hd\" aria-hidden=\"true\"><svg class=\"ic\" viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\"><path d=\"M4 11l8-6.5 8 6.5M6.5 9.5V19h11V9.5M10 19v-5h4v5\"/></svg></span><span class=\"hl\">Jouw adres</span>";
            originMarker = new maplibregl.Marker({
                element: el,
                anchor: "top",
                offset: [0, -20],
                pitchAlignment: "viewport",
                rotationAlignment: "viewport"
            })
                .setLngLat([ln, la])
                .addTo(map);
            return;
        }
        el.className = "job-map-origin";
        el.title = "Jouw locatie";
        originMarker = new maplibregl.Marker({
            element: el,
            anchor: "center",
            pitchAlignment: "viewport",
            rotationAlignment: "viewport"
        })
            .setLngLat([ln, la])
            .addTo(map);
    }

    function setOrigin(lat, lng, travel) {
        if (!map) {
            finishOpeningFrame();
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            finishOpeningFrame();
            return;
        }

        const originChanged = !sameOrigin(la, ln);
        normalizeTravelOptions(travel);
        lastOrigin = { lat: la, lng: ln };

        ensureOriginMarker(la, ln);
        drawTravelRings(la, ln);
        syncLocateButton();

        if (window.__jobsyBkRestoringView) {
            finishOpeningFrame();
            return;
        }
        if (tryRestoreBanenkaartMapViewOnce()) {
            return;
        }
        if (originChanged || !originHasBeenFramed) {
            fitToOriginRings(originHasBeenFramed);
        } else {
            finishOpeningFrame();
        }
    }

    function setTravelOptions(options) {
        const prevRadius = lastOrigin ? maxRingRadiusMeters() : 0;
        normalizeTravelOptions(options);
        if (lastOrigin) {
            drawTravelRings(lastOrigin.lat, lastOrigin.lng);
            if (Math.abs(maxRingRadiusMeters() - prevRadius) > 1) {
                fitToOriginRings(true);
            }
        }
    }

    function clearOrigin() {
        clearTravelRings();
        lastOrigin = null;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        if (originMarker) {
            originMarker.remove();
            originMarker = null;
        }
        syncLocateButton();
    }

    function panTo(lat, lng, zoom) {
        if (!map) {
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            return;
        }
        const opts = { center: [ln, la], duration: 400 };
        const z = Number(zoom);
        if (Number.isFinite(z) && z > 0) {
            opts.zoom = z;
        }
        safeEaseTo(opts);
    }

    function jumpToLocation(lat, lng, zoom) {
        if (!map) {
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            return;
        }
        const opts = { center: [ln, la] };
        const z = Number(zoom);
        if (Number.isFinite(z) && z > 0) {
            opts.zoom = z;
        }
        safeJumpTo(opts);
        cameraLocked = true;
        firstSizedFit = true;
    }

    function notifyFilterDrawerToggle() {
        mapResizePausedUntil = (typeof performance !== "undefined" ? performance.now() : Date.now()) + 380;
        setTimeout(function () {
            mapResizePausedUntil = 0;
            invalidate();
        }, 400);
    }

    function capturePageScroll() {
        var snap = { winY: 0, rootTop: 0, root: null };
        try {
            snap.winY = window.scrollY || 0;
        } catch (eWin) { /* ignore */ }
        try {
            if (window.jobsyDom && typeof window.jobsyDom.scrollRoot === "function") {
                snap.root = window.jobsyDom.scrollRoot();
                if (snap.root && snap.root !== document.documentElement && snap.root !== document.body) {
                    snap.rootTop = snap.root.scrollTop || 0;
                }
            }
        } catch (eRoot) { /* ignore */ }
        return snap;
    }

    function restorePageScroll(snap) {
        if (!snap) {
            return;
        }
        try {
            window.scrollTo(0, snap.winY || 0);
        } catch (eWin) { /* ignore */ }
        try {
            if (snap.root && typeof snap.root.scrollTop === "number") {
                snap.root.scrollTop = snap.rootTop || 0;
            }
        } catch (eRoot) { /* ignore */ }
    }

    function invalidate() {
        if (!map) {
            return;
        }
        if (mapResizePausedUntil && typeof performance !== "undefined" && performance.now() < mapResizePausedUntil) {
            return;
        }
        var scrollSnap = capturePageScroll();
        runWithViewportNotifySuppressed(function () {
            map.resize();
            if (kkMode) {
                kkResizeMapAndFit(false);
                return;
            }
            if (originNeedsFrame && lastOrigin) {
                fitToOriginRings(false);
            }
        });
        restorePageScroll(scrollSnap);
    }

    function isAlive() {
        if (!map || !clusterGroup) {
            return false;
        }
        const container = typeof map.getContainer === "function" ? map.getContainer() : null;
        return !!(container && container.isConnected);
    }

    function applyMarkerSelected(el, selected) {
        if (!el || !el.classList) {
            return;
        }
        el.classList.toggle("job-marker--active", !!selected);
    }

    function highlight(id) {
        if (id == null && activeClusterPopup) {
            return;
        }
        const prev = selectedId;
        selectedId = id;
        if (!map || !map.getSource(PIN_SOURCE)) {
            return;
        }
        if (prev != null) {
            try { map.setFeatureState({ source: PIN_SOURCE, id: String(prev) }, { selected: false }); } catch (e) { }
        }
        if (id != null) {
            try { map.setFeatureState({ source: PIN_SOURCE, id: String(id) }, { selected: true }); } catch (e) { }
        }
        refreshAgencyAreas();
    }

    function focus(id) {
        const record = markersById[id];
        if (!record || !map || !clusterGroup) {
            return;
        }

        highlight(id);

        clusterGroup.zoomToShowLayer(record, function () {
            openVacancyPopup(record);
        });
    }

    function normalizeCompanyId(value) {
        return String(value ?? "").toLowerCase().replace(/[{}-]/g, "");
    }

    function openCompanyClusterPopup(childMarkers) {
        if (!map || !childMarkers || childMarkers.length === 0) {
            return;
        }
        openClusterList(childMarkers, [childMarkers[0].lng, childMarkers[0].lat]);
    }

    /**
     * Deep-link helper for raamflyer QR: center on a vestiging and open the cluster
     * popup when 2+ vacancies share that company.
     */
    function focusCompany(companyId) {
        if (!map || !clusterGroup || companyId == null || companyId === "") {
            return;
        }

        const wanted = normalizeCompanyId(companyId);
        const markers = [];
        Object.keys(markersById).forEach(function (key) {
            const record = markersById[key];
            const data = record.options.jobData || {};
            if (normalizeCompanyId(data.companyId) === wanted) {
                markers.push(record);
            }
        });

        if (markers.length === 0) {
            return;
        }

        if (markers.length === 1) {
            focus(markers[0].options.jobData && markers[0].options.jobData.id);
            return;
        }

        invalidate();
        const points = markers.map(function (m) { return [m.lat, m.lng]; });
        safeFitBounds(boundsFromPoints(points), {
            padding: 64,
            maxZoom: BASEMAP_MAX_ZOOM,
            animate: true,
            duration: 450
        });
        setTimeout(function () {
            invalidate();
            openCompanyClusterPopup(markers);
        }, 450);
    }

    function dispose() {
        window.removeEventListener("resize", invalidate);
        closeActivePopup();
        openCallback = null;
        clearTravelRings();
        lastOrigin = null;
        originMarker = null;
        clearRenderedMarkers();
        if (map) {
            if (map._resizeObserver) {
                try { map._resizeObserver.disconnect(); } catch (eRoDispose) { /* ignore */ }
                map._resizeObserver = null;
            }
            unbindOutsideClickCloser();
            map.remove();
            map = null;
        }
        clusterGroup = null;
        markersById = {};
        markersByCoordKey = {};
        lastClusterTapAt = 0;
        lastFitPoints = [];
        firstSizedFit = false;
        cameraLocked = false;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        ringRedrawBound = false;
        ringStyleHandlerBound = false;
        ringRedrawTries = 0;
        deferMapReveal = false;
        tileLayer = null;
        selectedId = null;
        zoomHandlerBound = false;
        viewportPinsBound = false;
        // Stale If-None-Match after dispose+rebuild used to yield 304 with no body
        // while markers were already cleared — empty map. Always drop etag + cache.
        pinsEtag = null;
        pinsCachedPayload = null;
        if (pinsAbort) {
            try { pinsAbort.abort(); } catch (e) { }
            pinsAbort = null;
        }
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;");
    }

    function escapeAttr(value) {
        return escapeHtml(value).replaceAll("'", "&#39;");
    }

    function safeBadgeColor(color) {
        const s = String(color ?? "").trim();
        return /^#[0-9A-Fa-f]{6}$/.test(s) ? s : "#64748b";
    }

    document.addEventListener("jobsy-bk-catalog-pins", function (ev) {
        if (!catalogModeActive || !ev || !ev.detail) {
            return;
        }
        if (typeof setVacanciesFromCatalog === "function") {
            setVacanciesFromCatalog(ev.detail.pins || [], ev.detail.meta || {});
        }
    });

    document.addEventListener("jobsy-bk-catalog-ready", function () {
        if (catalogModeActive) {
            syncCatalogToMap();
        }
    });

    (function bindMapLifecycleResume() {
        if (window.__jobsyMapLifecycleBound) {
            return;
        }
        window.__jobsyMapLifecycleBound = true;
        function onVisible() {
            if (document.visibilityState && document.visibilityState !== "visible") {
                return;
            }
            invalidate();
        }
        document.addEventListener("visibilitychange", onVisible);
        window.addEventListener("pageshow", function () {
            onVisible();
            if (lagenLayout && map) {
                banenkaartMapRestoreAttempted = false;
                tryRestoreBanenkaartMapViewOnce();
            }
        });
    })();

    return {
        boot,
        init,
        setVacancies,
        setVacanciesFromCatalog,
        reloadPins,
        setOrigin,
        panTo,
        jumpToLocation,
        fitToOriginRings,
        setTravelOptions,
        clearOrigin,
        highlight,
        focus,
        focusCompany,
        dispose,
        initKandidatenkaart,
        setKandidatenkaartAreas,
        invalidate,
        notifyFilterDrawerToggle,
        isAlive,
        /** @internal Playwright / diagnostics */
        __testOpenVacancyById: function (id) {
            const key = id != null ? String(id) : "";
            const rec = key ? markersById[key] : null;
            if (rec) {
                openVacancyPopup(rec);
                return true;
            }
            return false;
        },
        __testGetMap: function () { return map; },
        __testGetPinCount: function () { return Object.keys(markersById).length; },
        __testGetLastPopupPhotoMetrics: function () {
            return lastLagenPopupPhotoMetrics ? Object.assign({}, lastLagenPopupPhotoMetrics) : null;
        },
        __testGetRenderedPinCount: function () {
            if (!map) {
                return 0;
            }
            const layers = [
                PIN_LAYER_CLUSTERS,
                PIN_LAYER_UNCLUSTERED_SYMBOL,
                PIN_LAYER_UNCLUSTERED
            ];
            let total = 0;
            layers.forEach(function (layerId) {
                try {
                    total += (map.queryRenderedFeatures({ layers: [layerId] }) || []).length;
                } catch (eFeat) { }
            });
            return total;
        },
        /** @internal Lagen pin layer diagnostics for Playwright. */
        __testLagenPinLayerDebug: function () {
            if (!map) {
                return { ok: false };
            }
            function vis(id) {
                try {
                    return map.getLayer(id) ? map.getLayoutProperty(id, "visibility") : null;
                } catch (eV) {
                    return null;
                }
            }
            return {
                lagenLayout: lagenLayout,
                zoom: map.getZoom(),
                pinData: Object.keys(markersById).length,
                rendered: (function () {
                    const layers = [
                        PIN_LAYER_CLUSTERS,
                        PIN_LAYER_UNCLUSTERED_SYMBOL,
                        PIN_LAYER_UNCLUSTERED
                    ];
                    let total = 0;
                    layers.forEach(function (layerId) {
                        try {
                            total += (map.queryRenderedFeatures({ layers: [layerId] }) || []).length;
                        } catch (eDbg) { }
                    });
                    return total;
                })(),
                symbolsRenderable: lagenPinSymbolsRenderable(),
                imagesReady: !!map._jobsyLagenPinImagesReady,
                clusterVer: map._jobsyLagenPinClusterVer,
                layers: {
                    clusters: vis(PIN_LAYER_CLUSTERS),
                    circle: vis(PIN_LAYER_UNCLUSTERED),
                    symbol: vis(PIN_LAYER_UNCLUSTERED_SYMBOL)
                }
            };
        },
        __testGetAgencyAreaCount: function () {
            return agencyAreasGeoJson().features.length;
        },
        __testGetAgencyPinCount: function () {
            let n = 0;
            Object.keys(markersById).forEach(function (id) {
                const v = markersById[id].options.jobData || {};
                if (isAgencyPin(v)) {
                    n += 1;
                }
            });
            return n;
        },
        __testAgencyLabel: function () { return AGENCY_LABEL; },
        __testHasAgencyMapTextLabels: function () {
            if (!map) {
                return false;
            }
            const legacyPin = ["jobsy-pins", "agency-label"].join("-");
            const legacyCluster = ["jobsy-pins-cluster", "agency-label"].join("-");
            return !!(map.getLayer(legacyPin) || map.getLayer(legacyCluster));
        },
        __testGetMapCreateCount: function () { return mapCreateCount; },
        __testGetPinsNetworkFetchCount: function () { return pinsNetworkFetchCount; },
        __testViewportQueryString: function () { return viewportQueryString(); },
        __testMarkUserMapGesture: markUserMapGesture,
        __testGetPinsUrl: function () {
            return pinsBaseUrl ? pinsBaseUrl + viewportQueryString() : pinsUrl;
        },
        __testPinsFilterKey: pinsFilterKey,
        __testClearPinsFilterCache: function () {
            Object.keys(pinsByFilterKey).forEach(function (k) { delete pinsByFilterKey[k]; });
            pinsNetworkFetchCount = 0;
            pinsCachedPayload = null;
            pinsEtag = null;
        },
        __testGetSetVacanciesCallCount: function () { return setVacanciesCallCount; },
        __testResetSetVacanciesCallCount: function () { setVacanciesCallCount = 0; },
        __testGetViewportNotifyCount: function () { return viewportNotifyCount; },
        __testResetViewportNotifyCount: function () { viewportNotifyCount = 0; },
        __testGetTravelRingDrawCount: function () { return travelRingDrawCount; },
        __testFitAllPins: function () {
            if (!map) {
                return;
            }
            var points = collectVacancyPoints(Object.keys(markersById).map(function (id) {
                return markersById[id] && markersById[id].vacancy;
            }).filter(Boolean));
            if (!points.length) {
                return;
            }
            fitMapToVacancies(points);
        },
        /** @internal Funda-style cluster zoom for Playwright (fires onClusterClick on densest cluster). */
        __testTapLargestCluster: function () {
            if (!map || typeof map.queryRenderedFeatures !== "function") {
                return false;
            }
            const layers = [PIN_LAYER_CLUSTERS + "-hit", PIN_LAYER_CLUSTERS].filter(function (id) {
                return !!map.getLayer(id);
            });
            const feats = map.queryRenderedFeatures({ layers: layers }) || [];
            if (!feats.length) {
                return false;
            }
            feats.sort(function (a, b) {
                return (Number(b.properties && b.properties.point_count) || 0) -
                    (Number(a.properties && a.properties.point_count) || 0);
            });
            const f = feats[0];
            onClusterClick({
                features: [f],
                point: map.project(f.geometry.coordinates),
                lngLat: { lng: f.geometry.coordinates[0], lat: f.geometry.coordinates[1] },
                originalEvent: { stopPropagation: function () { } }
            });
            return true;
        },
        /** @internal Open the densest visible cluster for v3 popup tests. */
        debugOpenLargestCluster: async function () {
            if (!map || typeof map.queryRenderedFeatures !== "function") {
                return false;
            }
            const layers = [PIN_LAYER_CLUSTERS + "-hit", PIN_LAYER_CLUSTERS].filter(function (id) {
                return !!map.getLayer(id);
            });
            if (!layers.length) {
                return false;
            }
            const feats = map.queryRenderedFeatures({ layers: layers }) || [];
            if (!feats.length) {
                return false;
            }
            feats.sort(function (a, b) {
                return (Number(b.properties && b.properties.point_count) || 0) -
                    (Number(a.properties && a.properties.point_count) || 0);
            });
            const f = feats[0];
            const props = f.properties || {};
            const source = map.getSource(PIN_SOURCE);
            if (!source || typeof source.getClusterLeaves !== "function") {
                return false;
            }
            lastClusterTapAt = Date.now();
            const total = Math.max(Number(props.point_count) || 0, 2);
            const leaves = await source.getClusterLeaves(props.cluster_id, total, 0);
            const childMarkers = (leaves || [])
                .map(function (leaf) { return markerByLeafId(leaf.properties && leaf.properties.id); })
                .filter(Boolean);
            if (!childMarkers.length) {
                return false;
            }
            openClusterList(childMarkers, f.geometry.coordinates);
            return true;
        }
    };
})();
