window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var css = [
        "/lib/leaflet/leaflet.css",
        "/lib/leaflet/MarkerCluster.css",
        "/lib/leaflet/MarkerCluster.Default.css"
    ];
    var leafletScripts = [
        "/lib/leaflet/leaflet.min.js",
        "/lib/leaflet/leaflet.markercluster.min.js"
    ];
    var discoveryScripts = [
        "/js/jobMap.js?v=20260816-tbt"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.js?v=20260816-tbt"
    ];

    function loadCss(href) {
        if (document.querySelector('link[data-jobsy-map="' + href + '"]')) {
            return Promise.resolve();
        }
        return new Promise(function (resolve, reject) {
            var link = document.createElement("link");
            link.rel = "stylesheet";
            link.href = href;
            link.setAttribute("data-jobsy-map", href);
            link.onload = function () { resolve(); };
            link.onerror = reject;
            document.head.appendChild(link);
        });
    }

    function loadScript(src) {
        if (document.querySelector('script[data-jobsy-map="' + src + '"]')) {
            return Promise.resolve();
        }
        return new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.setAttribute("data-jobsy-map", src);
            script.onload = function () { resolve(); };
            script.onerror = reject;
            document.body.appendChild(script);
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
        if (!window.L) {
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
        var urls = leafletScripts.slice();
        if (kind !== "detail") {
            urls = urls.concat(discoveryScripts);
        }
        if (kind !== "discovery") {
            urls = urls.concat(detailScripts);
        }
        return urls;
    }

    return {
        ensure: function (kind) {
            kind = normalizeKind(kind);
            if (isReady(kind)) {
                return Promise.resolve();
            }
            if (pending[kind]) {
                return pending[kind];
            }
            pending[kind] = Promise.all(css.map(loadCss)).then(function () {
                return loadScriptsInOrder(scriptsFor(kind), 0);
            }).catch(function (err) {
                pending[kind] = null;
                throw err;
            });
            return pending[kind];
        }
    };
})();
