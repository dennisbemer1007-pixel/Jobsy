window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var pendingPaint = {};
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
        "/js/jobMap.js?v=20260819-fast"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.js?v=20260819-fast"
    ];

    function hrefMatches(node, href) {
        var current = node.getAttribute("href") || node.getAttribute("src") || "";
        return current === href || current === href.replace(/^\//, "") || ("/" + current) === href;
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
        if (src.indexOf("leaflet.min.js") !== -1 && window.L) {
            return Promise.resolve();
        }
        if (src.indexOf("leaflet.markercluster") !== -1 && window.L && typeof window.L.markerClusterGroup === "function") {
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

    function afterNextPaint(cb) {
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(cb);
            });
        } else {
            setTimeout(cb, 0);
        }
    }

    function isVisible(el) {
        if (!el) {
            return false;
        }
        var r = el.getBoundingClientRect();
        var vh = window.innerHeight || 0;
        return r.width > 0 && r.height > 0 && r.bottom > 0 && r.top < vh;
    }

    function whenMapSlotReady(elementId, cb) {
        var el = elementId ? document.getElementById(elementId) : null;
        var done = false;
        var finish = function () {
            if (done) {
                return;
            }
            done = true;
            if (io) {
                io.disconnect();
            }
            clearTimeout(fallback);
            afterNextPaint(cb);
        };
        var io = null;
        if (!el || isVisible(el)) {
            finish();
            return;
        }
        if (typeof IntersectionObserver === "function") {
            io = new IntersectionObserver(function (entries) {
                for (var i = 0; i < entries.length; i++) {
                    if (entries[i].isIntersecting) {
                        finish();
                        return;
                    }
                }
            }, { rootMargin: "80px" });
            io.observe(el);
        }
        var fallback = setTimeout(finish, 400);
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
        ensureAfterPaint: function (kind, elementId) {
            kind = normalizeKind(kind);
            if (isReady(kind) || pending[kind]) {
                return ensure(kind);
            }
            if (pendingPaint[kind]) {
                return pendingPaint[kind];
            }
            pendingPaint[kind] = new Promise(function (resolve, reject) {
                whenMapSlotReady(elementId, function () {
                    ensure(kind).then(resolve, function (err) {
                        pendingPaint[kind] = null;
                        reject(err);
                    });
                });
            });
            return pendingPaint[kind];
        },
        warmDiscovery: function () {
            if (!document.getElementById("job-map")) {
                return;
            }
            ensure("discovery");
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
