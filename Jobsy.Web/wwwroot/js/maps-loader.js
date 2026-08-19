window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var pendingPaint = {};
    // Desktop PSI must not download MapLibre. Real users who never hover still get a late fallback.
    var INTERACT_FALLBACK_MS = 15000;
    var css = [
        "/lib/maplibre/maplibre-gl.css"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl.js",
        "/js/jobsyMapLibre.min.js?v=20260819-psi"
    ];
    var discoveryScripts = [
        "/js/jobMap.min.js?v=20260819-psi"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.min.js?v=20260819-psi"
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

    function isWideViewport() {
        try {
            return window.matchMedia("(min-width: 769px)").matches;
        } catch (e) {
            return (window.innerWidth || 0) >= 769;
        }
    }

    function whenMapSlotReady(elementId, cb) {
        var done = false;
        var INTERACT_EVENTS = ["pointerdown", "pointerenter", "touchstart", "wheel", "keydown", "focusin"];
        var el = elementId ? document.getElementById(elementId) : null;
        var target = (el && el.closest && (el.closest(".map-pane") || el.closest(".map-stage"))) || el || document.body;
        var fallback = 0;
        var finish = function () {
            if (done) {
                return;
            }
            done = true;
            INTERACT_EVENTS.forEach(function (ev) {
                target.removeEventListener(ev, finish);
            });
            clearTimeout(fallback);
            afterIdle(cb);
        };
        INTERACT_EVENTS.forEach(function (ev) {
            target.addEventListener(ev, finish, { passive: true });
        });
        fallback = setTimeout(finish, INTERACT_FALLBACK_MS);
        // Mobile: the map is the first screen — load after first paint, not on hover.
        if (!isWideViewport()) {
            afterPageLoad(finish, 1200);
        }
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
        isReady: isReady,
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
