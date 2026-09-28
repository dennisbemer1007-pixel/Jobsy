window.jobsyMaps = (function () {
    "use strict";

    // MapLibre is not linked from every document. On the banenkaart we preload +
    // load the three map scripts in parallel so first pins do not wait on the Blazor circuit.
    var pending = {};
    var mapLibreWorker = "/lib/maplibre/maplibre-gl-csp-worker.js?v=20260820-r166";
    var css = [
        "/lib/maplibre/maplibre-gl.css?v=20260820-r166"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl-csp.js?v=20260820-r180",
        "/js/jobsyMapLibre.min.js?v=20260926-mapfix9"
    ];
    var discoveryScripts = [
        "/js/jobMap.min.js?v=20260928-perf"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.min.js?v=20260928-perf"
    ];

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
        if (src.indexOf("vacancyDetailMap") !== -1 && window.vacancyDetailMap) {
            return Promise.resolve();
        }
        return new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.async = true;
            script.setAttribute("data-jobsy-map", src);
            script.setAttribute("fetchpriority", "high");
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

    /** Load all map scripts in parallel (maplibre / jobsyMapLibre / jobMap). */
    function loadScriptsParallel(urls) {
        return Promise.all(urls.map(loadScript));
    }

    function normalizeKind(kind) {
        return kind === "discovery" || kind === "detail" ? kind : "all";
    }

    function isReady(kind) {
        if (!window.maplibregl || !window.jobsyMapLibre) {
            return false;
        }
        if (kind === "detail") {
            return !!window.vacancyDetailMap;
        }
        if (kind === "discovery") {
            return !!window.jobMap;
        }
        return !!(window.jobMap && window.vacancyDetailMap);
    }

    function scriptsFor(kind) {
        var urls = mapLibreScripts.slice();
        if (kind !== "detail") {
            urls = urls.concat(discoveryScripts);
        }
        if (kind !== "discovery") {
            urls = urls.concat(detailScripts);
        }
        return urls;
    }

    function ensure(kind) {
        // Never pull MapLibre / jobMap onto auth/login surfaces (keeps /login ~65 KB).
        try {
            var path = (window.location && window.location.pathname) || "";
            if (path === "/login" || path.indexOf("/login?") === 0
                || path.indexOf("/account/") === 0
                || path.indexOf("/register") === 0) {
                return Promise.resolve();
            }
        } catch (e) { }
        kind = normalizeKind(kind);
        if (isReady(kind)) {
            return Promise.resolve();
        }
        if (pending[kind]) {
            return pending[kind];
        }
        var urls = scriptsFor(kind);
        preloadScripts(urls);
        pending[kind] = Promise.all([
            Promise.all(css.map(loadCss)),
            loadScriptsParallel(urls)
        ]).catch(function (err) {
            pending[kind] = null;
            throw err;
        });
        return pending[kind];
    }

    /** Start pins HTTP before MapLibre finishes loading (first pins &lt; 1.5s target). */
    function prefetchPins(url) {
        if (!url || typeof fetch !== "function") {
            return Promise.resolve(null);
        }
        if (window.__jobsyPinsPrefetch && window.__jobsyPinsPrefetchUrl === url) {
            return window.__jobsyPinsPrefetch;
        }
        window.__jobsyPinsPrefetchUrl = url;
        window.__jobsyPinsPrefetch = fetch(url, {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("pins " + res.status);
                }
                var etag = res.headers.get("ETag");
                if (etag) {
                    window.__jobsyPinsEtag = etag;
                }
                return res.json();
            })
            .catch(function () {
                window.__jobsyPinsPrefetch = null;
                return null;
            });
        return window.__jobsyPinsPrefetch;
    }

    function isDiscoveryPath() {
        try {
            var path = (window.location && window.location.pathname) || "";
            return path === "/" || path === "";
        } catch (e) {
            return false;
        }
    }

    // Kick off pins fetch + map scripts as soon as boot JSON is in the DOM (no circuit).
    function warmBootPins() {
        try {
            var node = document.getElementById("jobsy-map-boot");
            if (!node || !node.textContent) {
                return;
            }
            var parsed = JSON.parse(node.textContent);
            if (parsed && parsed.pinsUrl) {
                prefetchPins(String(parsed.pinsUrl));
            }
            if (isDiscoveryPath()) {
                ensure("discovery").then(function () {
                    if (window.jobMap && typeof window.jobMap.boot === "function") {
                        window.jobMap.boot("job-map");
                    }
                }).catch(function () { });
            }
        } catch (e) { }
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", warmBootPins);
    } else {
        warmBootPins();
    }

    return {
        ensure: ensure,
        prefetchPins: prefetchPins
    };
})();
