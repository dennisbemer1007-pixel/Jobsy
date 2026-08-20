window.jobsyMaps = (function () {
    "use strict";

    // MapLibre CSS/JS are preloaded from Home <head> (script, not the worker).
    // The worker is preloaded as fetch so it is not unused main-thread JS.
    // Injected here as soon as #job-map exists (or Blazor calls ensure).
    var pending = {};
    var mapLibreWorker = "/lib/maplibre/maplibre-gl-csp-worker.js?v=20260820-r166";
    var css = [
        "/lib/maplibre/maplibre-gl.css?v=20260820-r166"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl-csp.js?v=20260820-r180",
        "/js/jobsyMapLibre.min.js?v=20260820-r172"
    ];
    var discoveryScripts = [
        "/js/jobMap.min.js?v=20260820-r181"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.min.js?v=20260820-r166"
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
            link.setAttribute("fetchpriority", "high");
            link.setAttribute("data-jobsy-map", href);
            link.onload = function () { resolve(); };
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

    function isHighPriorityScript(src) {
        return isMapLibreMain(src) || src.indexOf("jobsyMapLibre") !== -1;
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
            script.async = false;
            script.setAttribute("data-jobsy-map", src);
            if (isHighPriorityScript(src)) {
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

    function loadScriptsInOrder(urls, index) {
        if (index >= urls.length) {
            return Promise.resolve();
        }
        return loadScript(urls[index]).then(function () {
            return loadScriptsInOrder(urls, index + 1);
        });
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
        kind = normalizeKind(kind);
        if (isReady(kind)) {
            return Promise.resolve();
        }
        if (pending[kind]) {
            return pending[kind];
        }
        pending[kind] = Promise.all([
            Promise.all(css.map(loadCss)),
            loadScriptsInOrder(scriptsFor(kind), 0)
        ]).catch(function (err) {
            pending[kind] = null;
            throw err;
        });
        return pending[kind];
    }

    return {
        ensure: ensure,
        warmDiscovery: function () {
            if (!document.getElementById("job-map")) {
                return;
            }
            this.ensure("discovery").then(function () {
                try {
                    if (window.jobMap && typeof window.jobMap.boot === "function") {
                        window.jobMap.boot("job-map");
                    }
                } catch (e) { }
            });
        }
    };
})();

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
        window.jobsyMaps.warmDiscovery();
    });
} else {
    window.jobsyMaps.warmDiscovery();
}
