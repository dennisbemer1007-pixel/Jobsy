/* app-core.js — concatenated geo + culture + cookieConsent + maps-loader + extras-loader. */

/* === geo.js === */
window.jobsyGeo = (function () {
    const STORAGE_KEY = "jobsy.origin";
    const ANON_KEY = "jobsy.anonymousKey";
    const CLICKED_KEY = "jobsy.clickedVacancies";
    const SITE_VISIT_KEY = "jobsy.siteVisitClaimed";
    const AGE_KEY = "jobsy.discoveryAge";
    const PROMPT_KEY = "jobsy.locationPrompted";
    const RECENT_KEY = "jobsy.recentlyViewedVacancies";
    const RECENT_MAX = 20;
    const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

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
            return (localStorage.getItem("Jobsy.CookieConsent") || "").toLowerCase().indexOf("analytics") === 0;
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
     * Returns stored origin only. Geolocation requires an explicit "Mijn locatie" tap.
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

    function recentStorageKey(userId) {
        var id = String(userId || "");
        if (UUID_RE.test(id)) {
            return RECENT_KEY + ":" + id.toLowerCase();
        }
        return RECENT_KEY;
    }

    function listRecentlyViewed(userId) {
        try {
            const raw = localStorage.getItem(recentStorageKey(userId));
            const parsed = raw ? JSON.parse(raw) : [];
            if (!Array.isArray(parsed)) {
                return [];
            }
            const ids = [];
            const seen = {};
            for (var i = 0; i < parsed.length; i++) {
                var item = parsed[i];
                var id = typeof item === "string" ? item : (item && item.id);
                id = String(id || "").toLowerCase();
                if (!UUID_RE.test(id) || seen[id]) {
                    continue;
                }
                seen[id] = true;
                ids.push(id);
            }
            return ids;
        } catch {
            return [];
        }
    }

    function rememberViewedVacancy(vacancyId, userId) {
        const id = String(vacancyId || "").toLowerCase();
        if (!UUID_RE.test(id)) {
            return;
        }
        const rest = listRecentlyViewed(userId).filter(function (x) { return x !== id; });
        rest.unshift(id);
        const payload = rest.slice(0, RECENT_MAX).map(function (x) {
            return { id: x, at: new Date().toISOString() };
        });
        try {
            localStorage.setItem(recentStorageKey(userId), JSON.stringify(payload));
        } catch {
            // quota / private mode
        }
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
        getOrCreateHighlightSeed,
        listRecentlyViewed,
        rememberViewedVacancy
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
                var maxAge = 60 * 60 * 24 * 365;
                document.cookie = KEY + "=" + encodeURIComponent(value || "necessary")
                    + "; Path=/; SameSite=Lax; Max-Age=" + maxAge
                    + (location.protocol === "https:" ? "; Secure" : "");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            var value = (window.jobsyCookieConsent.get() || "").toLowerCase();
            return value === "analytics" || value.indexOf("analytics.") === 0;
        }
    };

    window.jobsyViewport = {
        isWide: function () {
            return window.matchMedia("(min-width: 769px)").matches;
        },
        isKompasWide: function () {
            return window.matchMedia("(min-width: 1024px)").matches;
        },
        /** Subscribe to ≥1024px changes; returns { dispose() } for Blazor interop. */
        watchKompasWide: function (dotNetRef, methodName) {
            if (!dotNetRef || !methodName || !window.matchMedia) {
                return { dispose: function () { } };
            }
            var mql = window.matchMedia("(min-width: 1024px)");
            var handler = function (ev) {
                try {
                    dotNetRef.invokeMethodAsync(methodName, !!(ev && typeof ev.matches === "boolean" ? ev.matches : mql.matches));
                } catch (e) { }
            };
            if (typeof mql.addEventListener === "function") {
                mql.addEventListener("change", handler);
            } else if (typeof mql.addListener === "function") {
                mql.addListener(handler);
            }
            try {
                dotNetRef.invokeMethodAsync(methodName, !!mql.matches);
            } catch (e) { }
            return {
                dispose: function () {
                    if (typeof mql.removeEventListener === "function") {
                        mql.removeEventListener("change", handler);
                    } else if (typeof mql.removeListener === "function") {
                        mql.removeListener(handler);
                    }
                }
            };
        }
    };

    // Infinite-scroll sentinel for vacancy card lists (Zoeken).
    window.jobsyList = (function () {
        var observer = null;
        var currentEl = null;
        return {
            observeMore: function (el, dotNetRef, methodName) {
                if (!el || !dotNetRef || !methodName || typeof IntersectionObserver !== "function") {
                    return;
                }
                if (observer) {
                    try { observer.disconnect(); } catch (e) { }
                    observer = null;
                }
                currentEl = el;
                observer = new IntersectionObserver(function (entries) {
                    for (var i = 0; i < entries.length; i++) {
                        if (entries[i].isIntersecting) {
                            try {
                                dotNetRef.invokeMethodAsync(methodName);
                            } catch (e) { }
                            break;
                        }
                    }
                }, { root: null, rootMargin: "240px 0px", threshold: 0.01 });
                observer.observe(el);
            },
            disconnect: function () {
                if (observer) {
                    try { observer.disconnect(); } catch (e) { }
                    observer = null;
                }
                currentEl = null;
            }
        };
    })();

    // Static cookie banner (PublicLayout SSR): buttons use data-consent, no Blazor circuit.
    document.addEventListener("click", function (ev) {
        var btn = ev.target && ev.target.closest ? ev.target.closest("[data-consent]") : null;
        if (!btn || !btn.getAttribute) return;
        var choice = (btn.getAttribute("data-consent") || "").toLowerCase();
        if (choice !== "necessary" && choice !== "analytics") return;
        ev.preventDefault();

        function finish(value) {
            window.jobsyCookieConsent.set(value);
            var banner = btn.closest(".cookie-consent");
            if (banner && banner.parentNode) {
                banner.parentNode.removeChild(banner);
            }
        }

        if (choice === "necessary") {
            finish("necessary");
            return;
        }

        fetch("/account/cookie-consent/analytics-token", {
            method: "POST",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        })
            .then(function (res) {
                if (!res.ok) throw new Error("token");
                return res.json();
            })
            .then(function (body) {
                finish((body && body.token) || "analytics");
            })
            .catch(function () {
                finish("analytics");
            });
    });

    applyKnownClass();
})();

/* === maps-loader.js === */
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
        "/js/jobMap.min.js?v=20260928-mapperf"
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
            script.defer = true;
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

    function pinsFilterKey(url) {
        if (!url) {
            return "";
        }
        try {
            var u = new URL(url, window.location.origin);
            u.searchParams.delete("transport");
            var keys = Array.prototype.slice.call(u.searchParams.keys()).sort();
            var parts = [];
            for (var i = 0; i < keys.length; i++) {
                var k = keys[i];
                var vals = u.searchParams.getAll(k).slice().sort();
                for (var j = 0; j < vals.length; j++) {
                    parts.push(k + "=" + vals[j]);
                }
            }
            return u.pathname + "?" + parts.join("&");
        } catch (e) {
            return String(url).replace(/([?&])transport=[^&]*/gi, "$1").replace(/[?&]$/, "");
        }
    }

    /** Start pins HTTP before MapLibre finishes loading (first pins &lt; 1.5s target). */
    function prefetchPins(url) {
        if (!url || typeof fetch !== "function") {
            return Promise.resolve(null);
        }
        var key = pinsFilterKey(url);
        if (window.__jobsyPinsPrefetch
            && pinsFilterKey(window.__jobsyPinsPrefetchUrl) === key) {
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
            return path === "/" || path === "" || path === "/banenkaart";
        } catch (e) {
            return false;
        }
    }

    function whenCircuitOrIdle(cb) {
        var done = false;
        function run() {
            if (done) {
                return;
            }
            done = true;
            try { cb(); } catch (e) { }
        }
        function circuitReady() {
            try {
                return !!(window.Blazor && window.Blazor._internal);
            } catch (e) {
                return false;
            }
        }
        if (circuitReady()) {
            run();
            return;
        }
        var iv = setInterval(function () {
            if (circuitReady()) {
                clearInterval(iv);
                run();
            }
        }, 50);
        if (typeof requestIdleCallback === "function") {
            requestIdleCallback(function () {
                clearInterval(iv);
                run();
            }, { timeout: 1500 });
        } else {
            setTimeout(function () {
                clearInterval(iv);
                run();
            }, 1500);
        }
    }

    // Kick off pins fetch immediately; defer MapLibre + boot to idle/circuit.
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
                whenCircuitOrIdle(function () {
                    ensure("discovery").then(function () {
                        if (window.jobMap && typeof window.jobMap.boot === "function") {
                            window.jobMap.boot("job-map");
                        }
                    }).catch(function () { });
                });
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


/* === extras-loader.js === */
window.jobsyExtras = (function () {
    "use strict";

    var extrasSrc = "/js/app-extras.js?v=20260902-bw1";
    var feedbackSrc = "/js/feedback.js?v=20260831-ux2";
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

window.jobsyPageVisible = function () {
    return typeof document === "undefined" || document.visibilityState !== "hidden";
};

window.jobsyQuestionnaire = {
    scrollToQuestion: function (id, smooth) {
        if (!id) {
            return;
        }
        var el = document.getElementById(id);
        if (!el) {
            return;
        }
        var reduce = false;
        try {
            reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        } catch (e) { }
        var behavior = (smooth && !reduce) ? "smooth" : "auto";
        try {
            el.scrollIntoView({ behavior: behavior, block: "center" });
        } catch (e2) {
            el.scrollIntoView(true);
        }
    }
};

window.jobsyDialog = (function () {
    var active = null;
    var previouslyFocused = null;

    function focusables(root) {
        if (!root || !root.querySelectorAll) {
            return [];
        }
        var nodes = root.querySelectorAll(
            'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
        );
        return Array.prototype.filter.call(nodes, function (el) {
            return el.offsetParent !== null || el === document.activeElement;
        });
    }

    function onKeyDown(ev) {
        if (!active || ev.key !== "Tab") {
            return;
        }
        var list = focusables(active);
        if (list.length === 0) {
            ev.preventDefault();
            return;
        }
        var first = list[0];
        var last = list[list.length - 1];
        if (ev.shiftKey && document.activeElement === first) {
            ev.preventDefault();
            last.focus();
        } else if (!ev.shiftKey && document.activeElement === last) {
            ev.preventDefault();
            first.focus();
        }
    }

    function trap(el) {
        release();
        if (!el) {
            return;
        }
        previouslyFocused = document.activeElement;
        active = el;
        document.addEventListener("keydown", onKeyDown, true);
        var list = focusables(el);
        var target = list[0] || el;
        try {
            target.focus();
        } catch (e) { }
        try {
            if (document.body) {
                document.body.setAttribute("data-jobsy-dialog-open", "1");
            }
        } catch (e2) { }
    }

    function release() {
        if (!active) {
            return;
        }
        document.removeEventListener("keydown", onKeyDown, true);
        active = null;
        try {
            if (document.body) {
                document.body.removeAttribute("data-jobsy-dialog-open");
            }
        } catch (e) { }
        if (previouslyFocused && typeof previouslyFocused.focus === "function") {
            try {
                previouslyFocused.focus();
            } catch (e2) { }
        }
        previouslyFocused = null;
    }

    return { trap: trap, release: release };
})();

(function registerLobsyServiceWorker() {
    if (!("serviceWorker" in navigator)) {
        return;
    }
    window.addEventListener("load", function () {
        var isPublished = location.hostname !== "localhost" && location.hostname !== "127.0.0.1";
        var swUrl = isPublished
            ? "/service-worker.published.js?v=20260928-perf"
            : "/service-worker.js?v=20260928-perf";
        navigator.serviceWorker.register(swUrl, { scope: "/" }).catch(function () { });
    });
})();

/* === lobsyPwaInstall === */
window.lobsyPwaInstall = (function () {
  var KEY_VISITS = "Jobsy.PwaVisits";
  var KEY_DISMISS = "Jobsy.PwaInstallDismissed";
  var deferred = null;
  var isIos = /iphone|ipad|ipod/i.test(navigator.userAgent || "")
    || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
  var isStandalone = window.matchMedia("(display-mode: standalone)").matches
    || window.navigator.standalone === true;

  try {
    window.addEventListener("beforeinstallprompt", function (e) {
      e.preventDefault();
      deferred = e;
      window.dispatchEvent(new CustomEvent("lobsy-pwa-prompt-ready"));
    });
  } catch (err) { }

  function visits() {
    try {
      var n = parseInt(localStorage.getItem(KEY_VISITS) || "0", 10);
      return isNaN(n) ? 0 : n;
    } catch (e) {
      return 0;
    }
  }

  function bumpVisit() {
    if (isStandalone) return visits();
    try {
      var n = visits() + 1;
      localStorage.setItem(KEY_VISITS, String(n));
      return n;
    } catch (e) {
      return 0;
    }
  }

  return {
    bumpVisit: bumpVisit,
    isIos: function () { return isIos; },
    isStandalone: function () { return isStandalone; },
    canPrompt: function () { return !!deferred && !isStandalone; },
    wasDismissed: function () {
      try { return localStorage.getItem(KEY_DISMISS) === "1"; } catch (e) { return false; }
    },
    dismiss: function () {
      try { localStorage.setItem(KEY_DISMISS, "1"); } catch (e) { }
      deferred = null;
    },
    shouldShowBanner: function () {
      if (isStandalone || this.wasDismissed()) return false;
      if (isIos) return visits() >= 2;
      return visits() >= 2 && !!deferred;
    },
    prompt: async function () {
      if (!deferred) return { ok: false, reason: "unavailable" };
      try {
        deferred.prompt();
        var choice = await deferred.userChoice;
        deferred = null;
        try { localStorage.setItem(KEY_DISMISS, "1"); } catch (e) { }
        return { ok: true, outcome: choice && choice.outcome };
      } catch (e) {
        return { ok: false, reason: "error" };
      }
    }
  };
})();
