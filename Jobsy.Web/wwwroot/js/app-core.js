/* app-core.js — concatenated geo + culture + cookieConsent + maps-loader + extras-loader. */

/* === geo.js === */
window.jobsyGeo = (function () {
    const STORAGE_KEY = "jobsy.origin";
    const ANON_KEY = "jobsy.anonymousKey";
    const CLICKED_KEY = "jobsy.clickedVacancies";
    const SITE_VISIT_KEY = "jobsy.siteVisitClaimed";
    const AGE_KEY = "jobsy.discoveryAge";
    const PROMPT_KEY = "jobsy.locationPrompted";

    function getStoredOrigin() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return null;
            const parsed = JSON.parse(raw);
            const lat = Number(parsed.lat);
            const lng = Number(parsed.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
            const label = typeof parsed.label === "string" && parsed.label.trim()
                ? parsed.label.trim()
                : null;
            return { lat: lat, lng: lng, label: label };
        } catch {
            return null;
        }
    }

    function setStoredOrigin(lat, lng, label) {
        const payload = {
            lat: Number(lat),
            lng: Number(lng),
            at: new Date().toISOString()
        };
        if (typeof label === "string" && label.trim()) {
            payload.label = label.trim();
        }
        localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
    }

    function clearStoredOrigin() {
        localStorage.removeItem(STORAGE_KEY);
    }

    function wasLocationPrompted() {
        try {
            return sessionStorage.getItem(PROMPT_KEY) === "1";
        } catch {
            return false;
        }
    }

    function markLocationPrompted() {
        try {
            sessionStorage.setItem(PROMPT_KEY, "1");
        } catch {
            // ignore
        }
    }

    function getStoredAge() {
        try {
            const raw = sessionStorage.getItem(AGE_KEY);
            if (raw == null || raw === "") return null;
            const age = Number(raw);
            if (!Number.isFinite(age) || age < 15 || age > 67) return null;
            return Math.round(age);
        } catch {
            return null;
        }
    }

    function setStoredAge(age) {
        if (age == null || age === "") {
            sessionStorage.removeItem(AGE_KEY);
            return;
        }
        const n = Number(age);
        if (!Number.isFinite(n) || n < 15 || n > 67) {
            sessionStorage.removeItem(AGE_KEY);
            return;
        }
        sessionStorage.setItem(AGE_KEY, String(Math.round(n)));
    }

    function clearStoredAge() {
        sessionStorage.removeItem(AGE_KEY);
    }

    function analyticsAllowed() {
        try {
            if (window.jobsyCookieConsent && typeof window.jobsyCookieConsent.allowsAnalytics === "function") {
                return !!window.jobsyCookieConsent.allowsAnalytics();
            }
            return (localStorage.getItem("Jobsy.CookieConsent") || "").toLowerCase() === "analytics";
        } catch {
            return false;
        }
    }

    function getOrCreateAnonymousKey() {
        // Do not create/persist engagement keys before analytics consent (ePrivacy).
        if (!analyticsAllowed()) {
            return null;
        }

        let key = null;
        try {
            key = localStorage.getItem(ANON_KEY) || sessionStorage.getItem(ANON_KEY);
        } catch {
            key = sessionStorage.getItem(ANON_KEY);
        }
        if (!key) {
            key = "anon-" + crypto.randomUUID();
        }
        try {
            localStorage.setItem(ANON_KEY, key);
        } catch {
            // ignore
        }
        try {
            sessionStorage.setItem(ANON_KEY, key);
        } catch {
            // ignore
        }
        return key;
    }

    function readClickedSet() {
        try {
            const raw = sessionStorage.getItem(CLICKED_KEY);
            const parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed.map(String) : [];
        } catch {
            return [];
        }
    }

    /** Returns true once per vacancy per browser tab session. */
    function tryClaimClick(vacancyId) {
        const id = String(vacancyId || "");
        if (!id) return false;
        const set = readClickedSet();
        if (set.includes(id)) return false;
        set.push(id);
        sessionStorage.setItem(CLICKED_KEY, JSON.stringify(set));
        return true;
    }

    /** Returns true once per browser tab session for site-visit analytics. */
    function tryClaimSiteVisit() {
        try {
            if (sessionStorage.getItem(SITE_VISIT_KEY) === "1") {
                return false;
            }
            sessionStorage.setItem(SITE_VISIT_KEY, "1");
            return true;
        } catch {
            return true;
        }
    }

    function requestLocation() {
        return new Promise(function (resolve, reject) {
            if (!window.isSecureContext && location.hostname !== "localhost" && location.hostname !== "127.0.0.1") {
                reject(new Error("Locatie delen vereist een beveiligde verbinding (HTTPS)."));
                return;
            }

            if (!navigator.geolocation) {
                reject(new Error("Geolocation niet beschikbaar in deze browser."));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                function (pos) {
                    const lat = pos.coords.latitude;
                    const lng = pos.coords.longitude;
                    setStoredOrigin(lat, lng);
                    resolve({ lat: lat, lng: lng });
                },
                function (err) {
                    let message = "Locatie geweigerd.";
                    if (err) {
                        if (err.code === 1) message = "Locatietoegang geweigerd. Sta locatie toe in je browser.";
                        else if (err.code === 2) message = "Locatie kon niet worden bepaald.";
                        else if (err.code === 3) message = "Locatie ophalen duurde te lang.";
                        else if (err.message) message = err.message;
                    }
                    reject(new Error(message));
                },
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 30000 }
            );
        });
    }

    /**
     * Returns stored origin only. Geolocation requires explicit user action ("Mijn locatie").
     */
    async function ensureLocationOnLaunch() {
        return getStoredOrigin();
    }

    function scrollToId(id) {
        const el = document.getElementById(id);
        if (el) {
            el.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    /**
     * Opens http(s)/mailto in a new tab; custom schemes navigate in-place.
     * Returns { opened: bool, usedNewTab: bool }.
     */
    function openShare(url) {
        if (!url) return { opened: false, usedNewTab: false };
        const isWeb = /^(https?:|mailto:)/i.test(url);
        if (isWeb) {
            const win = window.open(url, "_blank", "noopener,noreferrer");
            return { opened: !!win, usedNewTab: true };
        }

        window.location.href = url;
        return { opened: true, usedNewTab: false };
    }

    async function copyText(text) {
        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch {
            // fall through
        }
        return false;
    }

    const HIGHLIGHT_SEED_KEY = "jobsy.highlightShuffleSeed";

    /// Stable per browser-tab session seed for randomizing featured vacancy order.
    function getOrCreateHighlightSeed() {
        try {
            const raw = sessionStorage.getItem(HIGHLIGHT_SEED_KEY);
            if (raw != null && raw !== "") {
                const n = Number.parseInt(raw, 10);
                if (Number.isFinite(n)) {
                    return n >>> 0;
                }
            }
            const seed = (Math.random() * 0xffffffff) >>> 0;
            sessionStorage.setItem(HIGHLIGHT_SEED_KEY, String(seed));
            return seed;
        } catch {
            return (Math.random() * 0xffffffff) >>> 0;
        }
    }

    return {
        getStoredOrigin,
        setStoredOrigin,
        clearStoredOrigin,
        getStoredAge,
        setStoredAge,
        clearStoredAge,
        wasLocationPrompted,
        markLocationPrompted,
        ensureLocationOnLaunch,
        getOrCreateAnonymousKey,
        tryClaimClick,
        tryClaimSiteVisit,
        requestLocation,
        scrollToId,
        openShare,
        copyText,
        getOrCreateHighlightSeed
    };
})();

/* === culture.js === */
window.jobsyCulture = {
  cookieName: "Jobsy.Culture",
  get: function () {
    var match = document.cookie.match(new RegExp("(?:^|; )" + this.cookieName + "=([^;]*)"));
    return match ? decodeURIComponent(match[1]) : null;
  },
  set: function (code) {
    var maxAge = 60 * 60 * 24 * 365;
    document.cookie =
      this.cookieName +
      "=" +
      encodeURIComponent(code) +
      "; path=/; max-age=" +
      maxAge +
      "; SameSite=Lax" +
      (location.protocol === "https:" ? "; Secure" : "");
  },
  applyDocument: function (code, rtl) {
    document.documentElement.lang = code || "nl";
    document.documentElement.dir = rtl ? "rtl" : "ltr";
  }
};

/* === cookieConsent.js === */
(function () {
    "use strict";

    var KEY = "Jobsy.CookieConsent";

    function applyKnownClass() {
        try {
            var known = !!(localStorage.getItem(KEY) || "");
            document.documentElement.classList.toggle("cookie-consent-known", known);
        } catch (e) {
            // private mode / blocked storage — show the banner
        }
    }

    window.jobsyCookieConsent = {
        get: function () {
            try {
                return localStorage.getItem(KEY) || "";
            } catch (e) {
                return "";
            }
        },
        set: function (value) {
            try {
                localStorage.setItem(KEY, value || "necessary");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            return (window.jobsyCookieConsent.get() || "").toLowerCase() === "analytics";
        }
    };

    window.jobsyViewport = {
        isWide: function () {
            return window.matchMedia("(min-width: 769px)").matches;
        }
    };

    applyKnownClass();
})();

/* === maps-loader.js === */
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
        "/js/jobsyMapLibre.min.js?v=20260821-r190"
    ];
    var discoveryScripts = [
        "/js/jobMap.min.js?v=20260821-r192"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.min.js?v=20260821-r190"
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
            // Preload MapLibre + jobMap only. Creating the map here is discarded
            // when the Blazor circuit attaches, which loaded tiles twice.
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

/* === extras-loader.js === */
window.jobsyExtras = (function () {
    "use strict";

    var extrasSrc = "/js/app-extras.js?v=20260820-r164";
    var feedbackSrc = "/js/feedback.js?v=20260820-r164";
    var pending = {};

    function loadScript(src, ready) {
        if (ready()) {
            return Promise.resolve();
        }
        if (pending[src]) {
            return pending[src];
        }
        pending[src] = new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.async = true;
            script.onload = function () { resolve(); };
            script.onerror = function () {
                pending[src] = null;
                reject(new Error("Failed to load " + src));
            };
            document.head.appendChild(script);
        });
        return pending[src];
    }

    function ensure() {
        return loadScript(extrasSrc, function () {
            return !!(window.lobsySessionIdle && window.jobsyDownload && window.jobsyRichtext);
        });
    }

    window.lobsyFeedbackEnsure = function () {
        return loadScript(feedbackSrc, function () {
            return !!window.lobsyFeedback;
        });
    };

    return { ensure: ensure };
})();
