window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var pendingPaint = {};
    var DISCOVERY_DELAY_MS = 3000;
    var css = [
        "/lib/maplibre/maplibre-gl.css"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl.js",
        "/js/jobsyMapLibre.js?v=20260819-lcp"
    ];
    var discoveryScripts = [
        "/js/jobMap.js?v=20260819-lcp"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.js?v=20260819-lcp"
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
            script.async = true;
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

    function afterIdle(cb) {
        if (typeof requestIdleCallback === "function") {
            requestIdleCallback(function () { cb(); }, { timeout: 2500 });
        } else {
            setTimeout(cb, 0);
        }
    }

    function afterPageLoad(cb, minDelayMs) {
        // MapLibre (1.1 MB) must not parse during the initial HTML load.
        minDelayMs = minDelayMs || 0;
        function start() {
            var go = function () { afterIdle(cb); };
            if (minDelayMs > 0) {
                setTimeout(go, minDelayMs);
            } else {
                go();
            }
        }
        if (document.readyState === "complete") {
            start();
        } else {
            window.addEventListener("load", start, { once: true });
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
        afterPageLoad(function () {
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
                cb();
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
            var fallback = setTimeout(finish, 8000);
        }, DISCOVERY_DELAY_MS);
    }

    function fetchAssets(kind) {
        return Promise.all([
            Promise.all(css.map(loadCss)),
            loadScriptsInOrder(scriptsFor(kind), 0)
        ]);
    }

    function ensure(kind) {
        kind = normalizeKind(kind);
        if (isReady(kind)) {
            return Promise.resolve();
        }
        if (pending[kind]) {
            return pending[kind];
        }
        pending[kind] = new Promise(function (resolve, reject) {
            afterPageLoad(function () {
                fetchAssets(kind).then(resolve, function (err) {
                    pending[kind] = null;
                    reject(err);
                });
            });
        });
        return pending[kind];
    }

    return {
        ensure: ensure,
        ensureAfterPaint: function (kind, elementId) {
            kind = normalizeKind(kind);
            if (isReady(kind)) {
                return Promise.resolve();
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
            // Intentionally empty: MapLibre must not start on page load.
        }
    };
})();
