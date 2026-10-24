(function () {
    "use strict";

    var HINT_KEY = "jobsy.bk.cluster-hint.v1";
    var icons = function () {
        return window.jobsyBanenkaartMapIcons || null;
    };

    function isMobile() {
        return !!(window.matchMedia && window.matchMedia("(max-width: 699px)").matches);
    }

    function transportLegendVerb(mode) {
        var t = String(mode || "Fiets");
        if (t === "Auto") {
            return "rijden";
        }
        if (t === "OV") {
            return "ov";
        }
        if (t === "Lopend") {
            return "lopen";
        }
        return "fietsen";
    }

    function ringLineClass(minutes) {
        var m = Number(minutes) || 0;
        if (m <= 10) {
            return "ln";
        }
        if (m <= 20) {
            return "ln d";
        }
        return "ln dt";
    }

    function legendRingsHtml(expanded, filters) {
        filters = filters || {};
        var hasOrigin = filters.originLat != null && filters.originLng != null;
        if (!hasOrigin) {
            return "";
        }
        var verb = transportLegendVerb(filters.transport);
        var mins = filters.maxMinutes;
        if (mins == null) {
            if (expanded) {
                return "<div class=\"lg\"><span class=\"sym\"><i class=\"ln\"></i></span><div>10 min " + verb + "</div></div>"
                    + "<div class=\"lg\"><span class=\"sym\"><i class=\"ln d\"></i></span><div>20 min " + verb + "</div></div>"
                    + "<div class=\"lg\"><span class=\"sym\"><i class=\"ln dt\"></i></span><div>30 min " + verb + "</div></div>";
            }
            return "<h4>Wat zie je op de kaart?</h4>"
                + "<div class=\"lgr\"><span><i class=\"ln\"></i>10</span><span><i class=\"ln d\"></i>20</span>"
                + "<span><i class=\"ln dt\"></i>30 min " + verb + "</span></div>";
        }
        var cls = ringLineClass(mins);
        if (expanded) {
            return "<div class=\"lg\"><span class=\"sym\"><i class=\"" + cls + "\"></i></span><div>"
                + mins + " min " + verb + "</div></div>";
        }
        return "<h4>Wat zie je op de kaart?</h4>"
            + "<div class=\"lgr\"><span><i class=\"" + cls + "\"></i>" + mins + " min " + verb + "</span></div>";
    }

    function legendHtml(expanded, filters) {
        var ic = icons();
        var pinW = ic ? ic.pinW(0.72) : "";
        var pinI = ic ? ic.pinI(0.72, false, true) : "";
        var tap = isMobile() ? "Tik" : "Klik";
        var rings = legendRingsHtml(expanded, filters);
        return rings
            + "<div class=\"lg\"><span class=\"sym\"><span class=\"cl cl--flat\">5</span></span>"
            + "<div>Groep vacatures<small>" + tap + " om in te zoomen</small></div></div>"
            + "<div class=\"lg\"><span class=\"sym\">" + pinW + "</span><div>Vacature bij werkgever</div></div>"
            + "<div class=\"lg\"><span class=\"sym\">" + pinI + "</span>"
            + "<div>Vacature via intermediair<small>zoals een uitzendbureau</small></div></div>";
    }

    var lastLegendFilters = null;

    function renderDesktopLegend(filters) {
        var host = document.querySelector("[data-testid=kb-map-legend-root]");
        if (!host || isMobile()) {
            if (host) {
                host.innerHTML = "";
            }
            return;
        }
        if (filters) {
            lastLegendFilters = filters;
        } else if (window.jobsyBanenkaartCatalog
            && typeof window.jobsyBanenkaartCatalog.resolveFilters === "function") {
            lastLegendFilters = window.jobsyBanenkaartCatalog.resolveFilters();
        }
        host.innerHTML = "<aside class=\"legend\" data-testid=\"kb-map-legend\" aria-label=\"Wat zie je op de kaart?\">"
            + legendHtml(false, lastLegendFilters) + "</aside>";
    }

    function bindMobileLegend() {
        var btn = document.querySelector("[data-testid=kb-map-legbtn]");
        var sheet = document.querySelector("[data-testid=kb-map-legend-sheet]");
        var scrim = document.querySelector("[data-testid=kb-map-legend-scrim]");
        if (!btn || !sheet) {
            return;
        }
        function close() {
            sheet.hidden = true;
            sheet.setAttribute("aria-hidden", "true");
            if (scrim) {
                scrim.hidden = true;
            }
            document.body.classList.remove("s-legend");
        }
        function open() {
            sheet.innerHTML = "<div class=\"grab\" aria-hidden=\"true\"></div>"
                + "<button type=\"button\" class=\"x\" data-testid=\"kb-map-legend-close\" aria-label=\"Sluiten\">×</button>"
                + "<h2 id=\"kb-legend-sheet-title\">Uitleg kaart</h2>" + legendHtml(true, lastLegendFilters)
                + "<button type=\"button\" class=\"close\" data-testid=\"kb-map-legend-done\">Sluiten</button>";
            sheet.hidden = false;
            sheet.setAttribute("aria-hidden", "false");
            if (scrim) {
                scrim.hidden = false;
            }
            document.body.classList.add("s-legend");
            sheet.querySelector("[data-testid=kb-map-legend-close]")?.addEventListener("click", close);
            sheet.querySelector("[data-testid=kb-map-legend-done]")?.addEventListener("click", close);
        }
        btn.addEventListener("click", function () {
            if (sheet.hidden) {
                open();
            } else {
                close();
            }
        });
        if (scrim) {
            scrim.addEventListener("click", close);
        }
    }

    function maybeShowClusterHint() {
        try {
            if (localStorage.getItem(HINT_KEY) === "1") {
                return;
            }
        } catch (e) { }
        var map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
        if (!map) {
            return;
        }
        var feats = [];
        try {
            feats = map.queryRenderedFeatures({ layers: ["jobsy-pins-clusters"] }) || [];
        } catch (eFeat) { }
        if (!feats.length) {
            return;
        }
        var best = feats[0];
        for (var i = 1; i < feats.length; i++) {
            var a = best.properties && best.properties.point_count;
            var b = feats[i].properties && feats[i].properties.point_count;
            if ((b || 0) > (a || 0)) {
                best = feats[i];
            }
        }
        var coords = best.geometry && best.geometry.coordinates;
        if (!coords) {
            return;
        }
        var pt = map.project(coords);
        var hint = document.createElement("div");
        hint.className = "hint" + (isMobile() ? " below" : "");
        var ic = icons();
        hint.innerHTML = (ic ? ic.bikeSvg : "") + (isMobile() ? "Tik" : "Klik") + " om in te zoomen";
        var wrap = document.querySelector(".map-stage") || document.querySelector("#job-map");
        if (!wrap) {
            return;
        }
        wrap.appendChild(hint);
        var wrapRect = wrap.getBoundingClientRect();
        var hintW = 220;
        if (isMobile()) {
            var left = Math.max(8, Math.min(pt.x - 24, wrapRect.width - hintW - 8));
            var top = Math.max(72, pt.y - 48);
            hint.style.left = left + "px";
            hint.style.top = top + "px";
            hint.style.bottom = "auto";
        } else {
            hint.style.left = Math.max(12, pt.x + 40) + "px";
            hint.style.top = Math.max(80, pt.y - 21) + "px";
        }
        setTimeout(function () {
            try { hint.remove(); } catch (e2) { }
        }, 8000);
        try {
            localStorage.setItem(HINT_KEY, "1");
        } catch (e3) { }
    }

    function boot() {
        renderDesktopLegend();
        bindMobileLegend();
        document.addEventListener("jobsy-bk-catalog-ready", function () {
            renderDesktopLegend();
        });
        document.addEventListener("jobsy-bk-filters-applied", function (ev) {
            var filters = ev && ev.detail ? ev.detail.filters : null;
            renderDesktopLegend(filters);
        });
        document.addEventListener("jobsy-bk-map-ready", function () {
            setTimeout(maybeShowClusterHint, 1200);
        });
        window.addEventListener("resize", function () {
            renderDesktopLegend();
        });
    }

    if (document.querySelector("[data-banenkaart-ssr=\"1\"]")) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", boot);
        } else {
            boot();
        }
    }

    window.jobsyBanenkaartMapLayers = {
        renderDesktopLegend: renderDesktopLegend,
        legendHtml: legendHtml
    };
})();
