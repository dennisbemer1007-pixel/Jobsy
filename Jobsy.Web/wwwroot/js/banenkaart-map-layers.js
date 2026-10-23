(function () {
    "use strict";

    var HINT_KEY = "jobsy.bk.cluster-hint.v1";
    var icons = function () {
        return window.jobsyBanenkaartMapIcons || null;
    };

    function isMobile() {
        return !!(window.matchMedia && window.matchMedia("(max-width: 699px)").matches);
    }

    function legendHtml(expanded) {
        var ic = icons();
        var pinW = ic ? ic.pinW(0.72) : "";
        var pinI = ic ? ic.pinI(0.72, false, true) : "";
        var tap = isMobile() ? "Tik" : "Klik";
        var rings = expanded
            ? "<div class=\"lg\"><span class=\"sym\"><i class=\"ln\"></i></span><div>10 min fietsen</div></div>"
            + "<div class=\"lg\"><span class=\"sym\"><i class=\"ln d\"></i></span><div>20 min fietsen</div></div>"
            + "<div class=\"lg\"><span class=\"sym\"><i class=\"ln dt\"></i></span><div>30 min fietsen</div></div>"
            : "<h4>Wat zie je op de kaart?</h4>"
            + "<div class=\"lgr\"><span><i class=\"ln\"></i>10</span><span><i class=\"ln d\"></i>20</span>"
            + "<span><i class=\"ln dt\"></i>30 min fietsen</span></div>";
        return rings
            + "<div class=\"lg\"><span class=\"sym\"><span class=\"cl cl--flat\">5</span></span>"
            + "<div>Groep vacatures<small>" + tap + " om in te zoomen</small></div></div>"
            + "<div class=\"lg\"><span class=\"sym\">" + pinW + "</span><div>Vacature bij werkgever</div></div>"
            + "<div class=\"lg\"><span class=\"sym\">" + pinI + "</span>"
            + "<div>Vacature via intermediair<small>zoals een uitzendbureau</small></div></div>";
    }

    function renderDesktopLegend() {
        var host = document.querySelector("[data-testid=kb-map-legend-root]");
        if (!host || isMobile()) {
            if (host) {
                host.innerHTML = "";
            }
            return;
        }
        host.innerHTML = "<aside class=\"legend\" data-testid=\"kb-map-legend\" aria-label=\"Wat zie je op de kaart?\">"
            + legendHtml(false) + "</aside>";
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
                + "<h2 id=\"kb-legend-sheet-title\">Uitleg kaart</h2>" + legendHtml(true)
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
        if (isMobile()) {
            hint.style.left = "12px";
            hint.style.bottom = "96px";
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
