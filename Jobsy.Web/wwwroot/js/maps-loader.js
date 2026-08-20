window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var css = [
        "/lib/maplibre/maplibre-gl.css?v=20260820-r163"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl.js?v=20260820-r163",
        "/js/jobsyMapLibre.js?v=20260820-r161"
    ];
    var discoveryScripts = [
        "/js/jobMap.js?v=20260820-r161"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.js?v=20260820-r161"
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

    function isHighPriorityScript(src) {
        return src.indexOf("maplibre-gl.js") !== -1 || src.indexOf("jobsyMapLibre.js") !== -1;
    }

    function loadScript(src) {
        if (document.querySelector('script[data-jobsy-map="' + src + '"]')) {
            return Promise.resolve();
        }
        if (src.indexOf("maplibre-gl.js") !== -1 && window.maplibregl) {
            return Promise.resolve();
        }
        if (src.indexOf("jobsyMapLibre.js") !== -1 && window.jobsyMapLibre) {
            return Promise.resolve();
        }
        if (src.indexOf("jobMap.js") !== -1 && window.jobMap) {
            return Promise.resolve();
        }
        if (src.indexOf("vacancyDetailMap.js") !== -1 && window.vacancyDetailMap) {
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
            script.onload = function () { resolve(); };
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
            this.ensure("discovery");
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
