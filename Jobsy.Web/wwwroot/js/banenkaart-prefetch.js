(function () {
    "use strict";

    var STYLE_URL = "/map/basemap/liberty/style.json";
    var CATALOG_URL = "/api/banenkaart-client/all";
    var MAP_LIBRE_CSS = "/lib/maplibre/maplibre-gl.css?v=20260820-r166";
    var MAP_LIBRE_JS = "/lib/maplibre/maplibre-gl-csp.js?v=20260820-r180";
    var JOBSY_MAP_JS = "/js/jobsyMapLibre.min.js?v=20261027-54";

    function saveDataEnabled() {
        try {
            if (navigator.connection && navigator.connection.saveData) {
                return false;
            }
            var eff = navigator.connection && navigator.connection.effectiveType;
            if (eff === "2g" || eff === "slow-2g") {
                return false;
            }
        } catch (e) { }
        return true;
    }

    function idle(cb) {
        if (typeof requestIdleCallback === "function") {
            requestIdleCallback(function () { cb(); }, { timeout: 2500 });
        } else {
            setTimeout(cb, 1200);
        }
    }

    function prefetchLink(href, as, crossOrigin) {
        if (!href || document.querySelector('link[data-jobsy-prefetch="' + href + '"]')) {
            return;
        }
        var link = document.createElement("link");
        link.rel = "prefetch";
        link.href = href;
        link.setAttribute("data-jobsy-prefetch", href);
        if (as) {
            link.as = as;
        }
        if (crossOrigin) {
            link.crossOrigin = crossOrigin;
        }
        document.head.appendChild(link);
    }

    function warmMapAssets() {
        prefetchLink(MAP_LIBRE_CSS, "style", "anonymous");
        prefetchLink(MAP_LIBRE_JS, "script", "anonymous");
        prefetchLink(JOBSY_MAP_JS, "script", "anonymous");
        prefetchLink(STYLE_URL, "fetch", "anonymous");
        prefetchLink("/banenkaart", "document");
    }

    function readPrefetchOrigin() {
        var node = document.getElementById("jobsy-bk-prefetch");
        if (!node || !node.textContent) {
            return null;
        }
        try {
            return JSON.parse(node.textContent);
        } catch (e) {
            return null;
        }
    }

    function warmIsochrones(origin) {
        if (!origin || origin.lat == null || origin.lng == null) {
            return;
        }
        var mode = origin.transport || "Fiets";
        var mins = origin.maxMinutes || 20;
        if (String(mode).toLowerCase().indexOf("ov") !== -1) {
            return;
        }
        var url = "/api/travel/isochrones?lat=" + encodeURIComponent(origin.lat)
            + "&lng=" + encodeURIComponent(origin.lng)
            + "&mode=" + encodeURIComponent(mode)
            + "&minutes=" + encodeURIComponent(String(mins));
        fetch(url, { credentials: "same-origin", headers: { Accept: "application/json" } }).catch(function () { });
    }

    function warmCatalog() {
        if (window.jobsyBanenkaartCatalog && typeof window.jobsyBanenkaartCatalog.prefetch === "function") {
            window.jobsyBanenkaartCatalog.prefetch().catch(function () { });
            return;
        }
        fetch(CATALOG_URL, {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        }).then(function (res) {
            if (!res.ok) {
                return null;
            }
            return res.text().then(function (body) {
                try {
                    sessionStorage.setItem("jobsy.bk.catalog.body.v1", body);
                    var etag = res.headers.get("ETag");
                    if (etag) {
                        sessionStorage.setItem("jobsy.bk.catalog.etag.v1", etag);
                    }
                    sessionStorage.setItem("jobsy.bk.catalog.at.v1", String(Date.now()));
                } catch (e) { }
                if (window.jobsyBanenkaartCatalog && typeof window.jobsyBanenkaartCatalog.ingestRaw === "function") {
                    window.jobsyBanenkaartCatalog.ingestRaw(body, res.headers.get("ETag"));
                }
            });
        }).catch(function () { });
    }

    function onLoad() {
        if (!saveDataEnabled()) {
            return;
        }
        idle(function () {
            warmMapAssets();
            warmCatalog();
            var origin = readPrefetchOrigin();
            var onBanenkaart = /^\/banenkaart(\/|$)/.test(window.location.pathname || "");
            if (onBanenkaart) {
                warmIsochrones(origin);
            }
            if (window.jobsyBasemapWarm && typeof window.jobsyBasemapWarm.warm === "function") {
                var view = origin && origin.lat != null && origin.lng != null
                    ? { lat: origin.lat, lng: origin.lng, zoom: 11 }
                    : { lat: 52.15, lng: 5.3, zoom: 10.5 };
                window.jobsyBasemapWarm.warm(view).catch(function () { });
            }
        });
    }

    if (document.readyState === "complete") {
        onLoad();
    } else {
        window.addEventListener("load", onLoad, { once: true });
    }
})();
