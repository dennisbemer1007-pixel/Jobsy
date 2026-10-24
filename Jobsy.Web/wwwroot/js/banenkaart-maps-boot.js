/* banenkaart-maps-boot.js — MapLibre loader for NoBlazor SSR banenkaart only (not full app-core). */
window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var mapLibreWorker = "/lib/maplibre/maplibre-gl-csp-worker.js?v=20260820-r166";
    var css = ["/lib/maplibre/maplibre-gl.css?v=20260820-r166"];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl-csp.js?v=20260820-r180",
        "/js/jobsyMapLibre.min.js?v=20261023-lagen-compact1"
    ];
    var discoveryScripts = ["/js/jobMap.min.js?v=20261024-listmapresize1"];

    function pathOnly(url) {
        var q = url.indexOf("?");
        var hash = url.indexOf("#");
        var end = url.length;
        if (q !== -1) end = q;
        if (hash !== -1 && hash < end) end = hash;
        return url.slice(0, end);
    }

    function hrefMatches(node, href) {
        var current = pathOnly(node.getAttribute("href") || node.getAttribute("src") || "");
        var want = pathOnly(href);
        return current === want || current === want.replace(/^\//, "") || ("/" + current) === want;
    }

    function loadCss(href) {
        if (document.querySelector('link[data-jobsy-map="' + href + '"]')) {
            return Promise.resolve();
        }
        var links = document.querySelectorAll("link[rel=\"stylesheet\"]");
        for (var i = 0; i < links.length; i++) {
            if (hrefMatches(links[i], href)) {
                links[i].setAttribute("data-jobsy-map", href);
                return Promise.resolve();
            }
        }
        return new Promise(function (resolve, reject) {
            var link = document.createElement("link");
            link.rel = "stylesheet";
            link.href = href;
            link.media = "print";
            link.setAttribute("data-jobsy-map", href);
            link.onload = function () {
                link.media = "all";
                resolve();
            };
            link.onerror = reject;
            document.head.appendChild(link);
        });
    }

    function isMapLibreMain(src) {
        return src.indexOf("maplibre-gl-csp.js") !== -1;
    }

    function configureMapLibreWorker() {
        if (!window.maplibregl) {
            return;
        }
        var url = mapLibreWorker;
        try {
            url = new URL(mapLibreWorker, document.baseURI).href;
        } catch (e) { }
        if (typeof window.maplibregl.setWorkerUrl === "function") {
            window.maplibregl.setWorkerUrl(url);
        } else {
            window.maplibregl.workerUrl = url;
        }
    }

    function preloadScripts(urls) {
        urls.forEach(function (href) {
            if (document.querySelector('link[data-jobsy-map-preload="' + href + '"]')) {
                return;
            }
            var link = document.createElement("link");
            link.rel = "preload";
            link.as = "script";
            link.href = href;
            link.crossOrigin = "anonymous";
            link.setAttribute("data-jobsy-map-preload", href);
            document.head.appendChild(link);
        });
    }

    function loadScript(src) {
        if (document.querySelector('script[data-jobsy-map="' + src + '"]')) {
            if (isMapLibreMain(src)) {
                configureMapLibreWorker();
            }
            return Promise.resolve();
        }
        if (isMapLibreMain(src) && window.maplibregl) {
            configureMapLibreWorker();
            return Promise.resolve();
        }
        if (src.indexOf("jobsyMapLibre") !== -1 && window.jobsyMapLibre) {
            return Promise.resolve();
        }
        if (src.indexOf("jobMap") !== -1 && window.jobMap) {
            return Promise.resolve();
        }
        return new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.defer = true;
            script.crossOrigin = "anonymous";
            script.setAttribute("data-jobsy-map", src);
            if (!isMapLibreMain(src)) {
                script.setAttribute("fetchpriority", "high");
            }
            script.onload = function () {
                if (isMapLibreMain(src)) {
                    configureMapLibreWorker();
                }
                resolve();
            };
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    function loadScriptsParallel(urls) {
        return Promise.all(urls.map(loadScript));
    }

    function isReady() {
        return !!(window.maplibregl && window.jobsyMapLibre && window.jobMap);
    }

    function ensure(kind) {
        kind = kind === "discovery" ? "discovery" : "discovery";
        if (isReady()) {
            return Promise.resolve();
        }
        if (pending[kind]) {
            return pending[kind];
        }
        var urls = mapLibreScripts.concat(discoveryScripts);
        pending[kind] = loadScriptsParallel(urls).then(function () {
            Promise.all(css.map(loadCss)).catch(function () { });
        }).catch(function (err) {
            pending[kind] = null;
            throw err;
        });
        return pending[kind];
    }

    function readBootPayload() {
        try {
            var node = document.getElementById("jobsy-map-boot");
            if (!node || !node.textContent) {
                return null;
            }
            return JSON.parse(node.textContent);
        } catch (e) {
            return null;
        }
    }

    function hasInlinePins(parsed) {
        return !!(parsed && Array.isArray(parsed.pins) && parsed.pins.length > 0);
    }

    /** After FCP: start MapLibre on the next frame (no requestIdleCallback delay). */
    function afterFirstContentfulPaint(cb) {
        var done = false;
        function run() {
            if (done) {
                return;
            }
            done = true;
            try { cb(); } catch (e) { }
        }
        function scheduleSoon() {
            if (typeof requestAnimationFrame === "function") {
                requestAnimationFrame(function () { requestAnimationFrame(run); });
            } else {
                setTimeout(run, 0);
            }
        }
        if (typeof PerformanceObserver !== "undefined") {
            try {
                var po = new PerformanceObserver(function (list) {
                    var fcp = list.getEntriesByName("first-contentful-paint");
                    if (fcp && fcp.length) {
                        po.disconnect();
                        scheduleSoon();
                    }
                });
                po.observe({ type: "paint", buffered: true });
                setTimeout(function () {
                    try { po.disconnect(); } catch (ePo) { }
                    scheduleSoon();
                }, 1200);
                return;
            } catch (eObs) { }
        }
        scheduleSoon();
    }

    function bootDiscoveryMap() {
        if (window.jobMap && typeof window.jobMap.boot === "function") {
            window.jobMap.boot("job-map");
        }
    }

    function deferMapBootWhileListMode() {
        var root = document.querySelector("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return false;
        }
        var listMode = root.getAttribute("data-kb-view") === "list"
            || (root.classList.contains("show-list") && !root.classList.contains("show-map"));
        if (!listMode) {
            return false;
        }
        try {
            return window.matchMedia("(min-width: 900px)").matches;
        } catch (eMq) {
            return false;
        }
    }

    function applyCatalogToMapWhenReady() {
        if (!window.jobsyBanenkaartCatalog || typeof window.jobsyBanenkaartCatalog.applyToMap !== "function") {
            return;
        }
        var filters = typeof window.jobsyBanenkaartCatalog.resolveFilters === "function"
            ? window.jobsyBanenkaartCatalog.resolveFilters()
            : null;
        if (window.jobsyBanenkaartCatalog.isReady && window.jobsyBanenkaartCatalog.isReady()) {
            window.jobsyBanenkaartCatalog.applyToMap(filters);
        }
    }

    function whenMapIdle(cb) {
        if (!window.jobMap || typeof window.jobMap.__testGetMap !== "function") {
            cb();
            return;
        }
        var m = window.jobMap.__testGetMap();
        if (!m || typeof m.once !== "function") {
            cb();
            return;
        }
        if (typeof m.isStyleLoaded === "function" && m.isStyleLoaded()) {
            m.once("idle", cb);
            return;
        }
        m.once("load", function () {
            m.once("idle", cb);
        });
    }

    function startDiscoveryMapAfterVisible() {
        bootDiscoveryMap();
        whenMapIdle(function () {
            if (window.jobMap && typeof window.jobMap.invalidate === "function") {
                window.jobMap.invalidate();
            }
            if (window.jobMap && typeof window.jobMap.fitToOriginRings === "function") {
                window.jobMap.fitToOriginRings(false);
            }
            applyCatalogToMapWhenReady();
        });
    }

    function warmBoot() {
        var parsed = readBootPayload();
        if (!parsed) {
            return;
        }
        var warmUrl = parsed.catalogMode ? parsed.catalogUrl : (parsed.catalogUrl || parsed.pinsUrl);
        if (!hasInlinePins(parsed) && warmUrl && typeof fetch === "function") {
            fetch(String(warmUrl), {
                credentials: "same-origin",
                headers: { Accept: "application/json" }
            }).catch(function () { });
        }
        if (parsed.catalogMode && window.jobsyBanenkaartCatalog && typeof window.jobsyBanenkaartCatalog.prefetch === "function") {
            window.jobsyBanenkaartCatalog.prefetch().catch(function () { });
        }
        var view = parsed.view || (parsed.mapInit && parsed.mapInit.view) || null;
        if (window.jobsyBasemapWarm && typeof window.jobsyBasemapWarm.warm === "function") {
            window.jobsyBasemapWarm.warm(view).catch(function () { });
        }
        function onMapPaneVisible() {
            if (typeof requestAnimationFrame === "function") {
                requestAnimationFrame(function () {
                    requestAnimationFrame(startDiscoveryMapAfterVisible);
                });
            } else {
                startDiscoveryMapAfterVisible();
            }
        }

        if (document.documentElement.dataset.bkMapRevealListen !== "1") {
            document.documentElement.dataset.bkMapRevealListen = "1";
            document.addEventListener("jobsy-bk-map-pane-visible", onMapPaneVisible);
        }

        ensure("discovery").then(function () {
            if (deferMapBootWhileListMode()) {
                var root = document.querySelector("[data-banenkaart-ssr=\"1\"]");
                if (root && root.getAttribute("data-kb-view") === "map") {
                    onMapPaneVisible();
                }
                return;
            }
            startDiscoveryMapAfterVisible();
        }).catch(function () { });
    }

    function scheduleWarmBoot() {
        function run() {
            try {
                warmBoot();
            } catch (eBoot) { /* ssr/map retry */ }
        }
        function afterSsrUi() {
            var started = false;
            function runOnce() {
                if (started) {
                    return;
                }
                started = true;
                requestAnimationFrame(run);
            }
            if (document.querySelector("[data-banenkaart-ssr=\"1\"]")) {
                document.addEventListener("jobsy-bk-ssr-ui-ready", runOnce, { once: true });
                setTimeout(runOnce, 200);
                return;
            }
            runOnce();
        }
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", afterSsrUi);
        } else {
            afterSsrUi();
        }
    }

    scheduleWarmBoot();

    return { ensure: ensure };
})();
