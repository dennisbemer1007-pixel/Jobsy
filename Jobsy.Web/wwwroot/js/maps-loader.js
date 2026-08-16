window.jobsyMaps = (function () {
    "use strict";

    var pending = null;
    var css = [
        "/lib/leaflet/leaflet.css",
        "/lib/leaflet/MarkerCluster.css",
        "/lib/leaflet/MarkerCluster.Default.css"
    ];
    var scripts = [
        "/lib/leaflet/leaflet.min.js",
        "/lib/leaflet/leaflet.markercluster.min.js",
        "/js/jobMap.js?v=20260816-perf",
        "/js/vacancyDetailMap.js?v=20260816-perf"
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

    return {
        ensure: function () {
            if (window.L && window.jobMap && window.vacancyDetailMap) {
                return Promise.resolve();
            }
            if (pending) {
                return pending;
            }
            pending = Promise.all(css.map(loadCss)).then(function () {
                return loadScriptsInOrder(scripts, 0);
            }).catch(function (err) {
                pending = null;
                throw err;
            });
            return pending;
        }
    };
})();
