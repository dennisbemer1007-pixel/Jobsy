/* app-core.js — concatenated geo + culture + cookieConsent + maps-loader + extras-loader. */

/* === geo.js === */
window.jobsyGeo = (function () {
    const STORAGE_KEY = "jobsy.origin";
    const SESSION_ORIGIN_KEY = "jobsy.kb.origin";
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

    function getSessionOrigin() {
        try {
            const raw = sessionStorage.getItem(SESSION_ORIGIN_KEY);
            if (!raw) return null;
            const parsed = JSON.parse(raw);
            const lat = Number(parsed.lat);
            const lng = Number(parsed.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
            const label = typeof parsed.label === "string" && parsed.label.trim()
                ? parsed.label.trim()
                : null;
            const transport = typeof parsed.transport === "string" && parsed.transport.trim()
                ? parsed.transport.trim()
                : null;
            const maxMinutes = Number(parsed.maxMinutes);
            return {
                lat: lat,
                lng: lng,
                label: label,
                transport: transport,
                maxMinutes: Number.isFinite(maxMinutes) ? maxMinutes : null
            };
        } catch {
            return null;
        }
    }

    function setSessionOrigin(lat, lng, label, transport, maxMinutes) {
        try {
            const payload = {
                lat: Number(lat),
                lng: Number(lng),
                at: new Date().toISOString()
            };
            if (typeof label === "string" && label.trim()) {
                payload.label = label.trim();
            }
            if (typeof transport === "string" && transport.trim()) {
                payload.transport = transport.trim();
            }
            if (maxMinutes != null && Number.isFinite(Number(maxMinutes))) {
                payload.maxMinutes = Math.round(Number(maxMinutes));
            }
            sessionStorage.setItem(SESSION_ORIGIN_KEY, JSON.stringify(payload));
        } catch {
            // ignore
        }
    }

    function clearSessionOrigin() {
        try {
            sessionStorage.removeItem(SESSION_ORIGIN_KEY);
        } catch {
            // ignore
        }
    }

    const WEERGAVE_KEY = "jobsy.kb.weergave";

    function getWeergave() {
        try {
            var raw = sessionStorage.getItem(WEERGAVE_KEY);
            if (raw === "lijst" || raw === "kaart") {
                return raw;
            }
            return null;
        } catch {
            return null;
        }
    }

    function setWeergave(value) {
        try {
            if (value === "lijst" || value === "kaart") {
                sessionStorage.setItem(WEERGAVE_KEY, value);
            } else {
                sessionStorage.removeItem(WEERGAVE_KEY);
            }
        } catch {
            // ignore
        }
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
        getSessionOrigin,
        setSessionOrigin,
        clearSessionOrigin,
        getWeergave,
        setWeergave,
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
    var name = String(this.cookieName).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    var match = document.cookie.match(new RegExp("(?:^|; )" + name + "=([^;]*)"));
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

    function consentStored() {
        try {
            if (localStorage.getItem(KEY)) {
                return true;
            }
        } catch (e) { }
        var escaped = KEY.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var match = document.cookie.match(new RegExp("(?:^|; )" + escaped + "=([^;]*)"));
        return !!(match && match[1]);
    }

    function applyKnownClass() {
        try {
            document.documentElement.classList.toggle("cookie-consent-known", consentStored());
        } catch (e) {
            // private mode / blocked storage — show the banner
        }
    }

    function onEnhancedNav() {
        applyKnownClass();
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
            // Design-system breakpoint (file 03): split view from 900px.
            return window.matchMedia("(min-width: 900px)").matches;
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
    document.addEventListener("enhancedload", onEnhancedNav);
    try {
        if (window.Blazor && typeof Blazor.addEventListener === "function") {
            Blazor.addEventListener("enhancedload", onEnhancedNav);
        }
    } catch (e) { }
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
        "/js/jobsyMapLibre.min.js?v=20261002-ch01"
    ];
    var discoveryScripts = [
        "/js/jobMap.min.js?v=20261010-ag02"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.min.js?v=20261004-13"
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
    var feedbackSrc = "/js/feedback.js?v=20261002-ch10";
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

window.jobsyDom = {
    matchesMedia: function (query) {
        try {
            return !!(window.matchMedia && window.matchMedia(query).matches);
        } catch (e) {
            return false;
        }
    },
    sessionGet: function (key) {
        try { return sessionStorage.getItem(key); } catch (e) { return null; }
    },
    sessionSet: function (key, value) {
        try { sessionStorage.setItem(key, value); } catch (e) { }
    },
    sessionRemove: function (key) {
        try { sessionStorage.removeItem(key); } catch (e) { }
    },
    shareOrCopy: function (url) {
        if (navigator.share) {
            return navigator.share({ url: url });
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(url);
        }
        return Promise.reject(new Error("share-unavailable"));
    },
    bindKeyFocus: function (selector) {
        if (window.__jobsyKeyFocusBound) {
            return;
        }
        window.__jobsyKeyFocusBound = true;
        document.addEventListener("keydown", function (e) {
            if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) {
                var el = document.querySelector(selector);
                if (!el) {
                    return;
                }
                e.preventDefault();
                el.focus();
            }
        });
    },
    ensureScript: function (src, globalName) {
        window.__jobsyScriptPromises = window.__jobsyScriptPromises || {};
        if (globalName && window[globalName]) {
            return Promise.resolve();
        }
        if (window.__jobsyScriptPromises[src]) {
            return window.__jobsyScriptPromises[src];
        }
        window.__jobsyScriptPromises[src] = new Promise(function (resolve, reject) {
            var s = document.createElement("script");
            s.src = src;
            s.onload = function () { resolve(); };
            s.onerror = function () {
                window.__jobsyScriptPromises[src] = null;
                reject(new Error("Failed to load " + src));
            };
            document.head.appendChild(s);
        });
        return window.__jobsyScriptPromises[src];
    }
};

document.addEventListener("change", function (ev) {
    var target = ev.target;
    if (!target || !target.matches || !target.matches("[data-jobsy-submit-form]")) {
        return;
    }
    if (target.form) {
        target.form.submit();
    }
});

window.jobsyMedia = {
    matches: function (query) {
        try {
            return !!(query && window.matchMedia && window.matchMedia(query).matches);
        } catch (e) {
            return false;
        }
    }
};

window.jobsySessionFlag = {
    get: function (key) {
        try { return sessionStorage.getItem(key) === "1"; } catch (e) { return false; }
    },
    set: function (key) {
        try { sessionStorage.setItem(key, "1"); } catch (e) { }
    },
    remove: function (key) {
        try { sessionStorage.removeItem(key); } catch (e) { }
    }
};

window.jobsySaveFile = function (filename, base64, mimeType) {
    try {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        var blob = new Blob([bytes], { type: mimeType || "application/octet-stream" });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = filename || "download.bin";
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    } catch (e) { }
};

window.jobsyBindSearchShortcut = function () {
    if (window.__jobsySalesSearchBound) return;
    window.__jobsySalesSearchBound = true;
    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) {
            e.preventDefault();
            var el = document.querySelector(".sp-search__field input");
            if (el) el.focus();
        }
    });
};

window.jobsyShareUrl = function (url) {
    if (navigator.share) return navigator.share({ url: url });
    if (navigator.clipboard && navigator.clipboard.writeText) return navigator.clipboard.writeText(url);
};

window.jobsyComposeEnter = function (id) {
    var el = typeof id === "string" ? document.getElementById(id) : id;
    if (!el || el.dataset.enterBound) return;
    el.dataset.enterBound = "1";
    el.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" || e.shiftKey) return;
        e.preventDefault();
        var form = el.closest("form");
        if (form && typeof form.requestSubmit === "function") form.requestSubmit();
    });
};

window.jobsyEnsureInsightsMap = function () {
    window.__jobsyInsightsMapReady = window.__jobsyInsightsMapReady || new Promise(function (resolve, reject) {
        if (window.JobsyCandidateInsightsMap) { resolve(); return; }
        var s = document.createElement("script");
        s.src = "js/features/kandidaatinzichten-map.js?v=20261004-14";
        s.onload = function () { resolve(); };
        s.onerror = reject;
        document.head.appendChild(s);
    });
    return window.__jobsyInsightsMapReady;
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

/** Focus the checked (or first) radio inside a radiogroup by element id. */
window.jobsyFocusRadioInGroup = function (groupId) {
    if (!groupId) {
        return;
    }
    var group = document.getElementById(groupId);
    if (!group) {
        return;
    }
    var radios = group.querySelectorAll('input[type="radio"]');
    if (!radios.length) {
        return;
    }
    var target = null;
    for (var i = 0; i < radios.length; i++) {
        if (radios[i].checked) {
            target = radios[i];
            break;
        }
    }
    if (!target) {
        target = radios[0];
    }
    try {
        target.focus({ preventScroll: false });
    } catch (e) {
        try { target.focus(); } catch (e2) { }
    }
};

(function registerKeyboardFocus() {
    function setKeyboard(on) {
        var root = document.documentElement;
        if (!root) {
            return;
        }
        if (on) {
            root.setAttribute("data-lobsy-keyboard", "1");
        } else {
            root.removeAttribute("data-lobsy-keyboard");
        }
    }

    document.addEventListener("keydown", function (ev) {
        if (ev.key === "Tab" || ev.key === "ArrowUp" || ev.key === "ArrowDown" || ev.key === "ArrowLeft" || ev.key === "ArrowRight") {
            setKeyboard(true);
        }
    }, true);
    document.addEventListener("pointerdown", function () {
        setKeyboard(false);
    }, true);
    document.addEventListener("mousedown", function () {
        setKeyboard(false);
    }, true);
})();

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
        if (!active) {
            return;
        }
        if (ev.key === "Escape") {
            var closer = active.querySelector("[data-jobsy-dialog-close]");
            if (closer) {
                ev.preventDefault();
                closer.click();
            }
            return;
        }
        if (ev.key !== "Tab") {
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

    function firstFocusable(el) {
        var list = focusables(el);
        for (var i = 0; i < list.length; i++) {
            var node = list[i];
            // A clipped close control is the first button in row menus. Focusing it
            // does not stick in headless Chrome, so focus falls to the body and the
            // menu treats that as "focus left" and closes itself.
            if (node.classList && node.classList.contains("visually-hidden")) {
                continue;
            }
            return node;
        }
        return list.length ? list[0] : el;
    }

    function trap(el) {
        release();
        if (!el) {
            return;
        }
        previouslyFocused = document.activeElement;
        active = el;
        document.addEventListener("keydown", onKeyDown, true);
        var target = firstFocusable(el);
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

    function focusLeft(el) {
        if (!el || typeof el.contains !== "function") {
            return false;
        }
        var active = document.activeElement;
        if (!active) {
            return true;
        }
        return !el.contains(active);
    }

    return { trap: trap, release: release, focusLeft: focusLeft };
})();

(function registerLobsyServiceWorker() {
    if (!("serviceWorker" in navigator)) {
        return;
    }
    var isPublished = location.hostname !== "localhost" && location.hostname !== "127.0.0.1";
    var swUrl = isPublished
        ? "/service-worker.published.js?v=20261010-02"
        : "/service-worker.js?v=20261010-02";
    var isWebKit = /AppleWebKit/i.test(navigator.userAgent || "")
        && !/Chrome|Chromium|CriOS|EdgiOS|FxiOS/i.test(navigator.userAgent || "");

    function circuitReady() {
        try {
            return document.documentElement.getAttribute("data-lobsy-circuit") === "ready";
        } catch (e) {
            return false;
        }
    }

    function register() {
        navigator.serviceWorker.register(swUrl, { scope: "/", updateViaCache: "none" }).catch(function () { });
    }

    function scheduleRegister() {
        var started = Date.now();
        function tick() {
            if (circuitReady() || Date.now() - started > 12000) {
                register();
                return;
            }
            window.setTimeout(tick, isWebKit ? 400 : 200);
        }
        tick();
    }

    window.addEventListener("load", function () {
        window.setTimeout(scheduleRegister, isWebKit ? 1200 : 0);
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

/* Header menus: one open at a time, even when the Blazor circuit is down.
   html[data-header-menu] is the only visibility source. */
(function () {
  if (window.__jobsyHeaderMenus) return;
  window.__jobsyHeaderMenus = true;

  function syncAria() {
    var open = document.documentElement.getAttribute("data-header-menu");
    document.querySelectorAll("[data-menu-id]").forEach(function (root) {
      var id = root.getAttribute("data-menu-id");
      var on = open === id;
      var trigger = root.querySelector("[data-menu-trigger]");
      if (trigger) trigger.setAttribute("aria-expanded", on ? "true" : "false");
      root.classList.toggle("is-open", on);
      root.classList.toggle("is-closed", !on);
    });
  }

  function setOpen(id) {
    if (id) document.documentElement.setAttribute("data-header-menu", id);
    else document.documentElement.removeAttribute("data-header-menu");
    syncAria();
  }

  document.addEventListener("click", function (event) {
    if (event.target.closest("[data-menu-close]")) {
      setOpen(null);
      return;
    }
    var trigger = event.target.closest("[data-menu-trigger]");
    if (trigger) {
      var root = trigger.closest("[data-menu-id]");
      if (!root) return;
      var id = root.getAttribute("data-menu-id");
      var current = document.documentElement.getAttribute("data-header-menu");
      setOpen(current === id ? null : id);
      return;
    }
    if (event.target.closest(".header-dropdown-backdrop")) {
      setOpen(null);
      return;
    }
    if (!event.target.closest("[data-menu-id]")) {
      if (document.documentElement.hasAttribute("data-header-menu")) setOpen(null);
    }
  }, true);

  document.addEventListener("keydown", function (event) {
    if (event.key !== "Escape") return;
    if (!document.documentElement.hasAttribute("data-header-menu")) return;
    setOpen(null);
  });

  document.addEventListener("click", function (event) {
    document.querySelectorAll("details.pub-lang[open], details.pub-menu[open]").forEach(function (el) {
      if (!el.contains(event.target)) el.removeAttribute("open");
    });
  });

  document.addEventListener("keydown", function (event) {
    if (event.key !== "Escape") return;
    document.querySelectorAll("details.pub-lang[open], details.pub-menu[open]").forEach(function (el) {
      el.removeAttribute("open");
    });
  });

  document.addEventListener("focusout", function (event) {
    var open = document.documentElement.getAttribute("data-header-menu");
    if (!open) return;
    var next = event.relatedTarget;
    // A click with no next focus is handled by the click listener (toggle or switch).
    if (!next) return;
    var root = document.querySelector('[data-menu-id="' + open + '"]');
    if (root && root.contains(next)) return;
    if (next.closest && next.closest("[data-menu-trigger]")) return;
    setOpen(null);
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Tab") document.documentElement.classList.add("using-keyboard");
  }, true);
  document.addEventListener("pointerdown", function () {
    document.documentElement.classList.remove("using-keyboard");
  }, true);
})();

(function () {
  if (window.__jobsyInputMode) return;
  window.__jobsyInputMode = true;
  document.addEventListener("keydown", function (e) {
    if (e.key === "Tab") document.documentElement.setAttribute("data-input", "keyboard");
  }, true);
  document.addEventListener("pointerdown", function () {
    document.documentElement.setAttribute("data-input", "pointer");
  }, true);
})();

window.jobsyCoachDock = window.jobsyCoachDock || {
  bind: function (el, cssVar) {
    if (!el || !cssVar) return;
    var root = document.documentElement;
    var apply = function () {
      var h = 0;
      if (el && el.isConnected) {
        var style = window.getComputedStyle(el);
        var hidden = style.display === "none" || style.visibility === "hidden";
        h = hidden ? 0 : Math.ceil(el.getBoundingClientRect().height);
      }
      root.style.setProperty(cssVar, h + "px");
    };
    apply();
    if (typeof ResizeObserver === "function") {
      var ro = new ResizeObserver(apply);
      ro.observe(el);
    }
  },
  clear: function (cssVar) {
    document.documentElement.style.setProperty(cssVar, "0px");
  },
  focus: function (id) {
    var el = document.getElementById(id);
    if (el && el.focus) el.focus();
  },
  tipDismissed: function (key) {
    try { return localStorage.getItem("lobsy-coach-tip:" + key) === "1"; } catch (e) { return false; }
  },
  dismissTip: function (key) {
    try { localStorage.setItem("lobsy-coach-tip:" + key, "1"); } catch (e) { }
  },
  placeTip: function (dock) {
    if (!dock) return;
    var tip = dock.querySelector(".lobsy-coach-dock__tip");
    dock.classList.add("lobsy-coach-dock--tip-above");
    dock.classList.remove("lobsy-coach-dock--tip-aside");
    if (!tip) return;
    tip.hidden = false;

    function overlapsControls() {
      var tipBox = tip.getBoundingClientRect();
      if (tipBox.width < 2 || tipBox.height < 2) return false;
      var nodes = document.querySelectorAll("main a, main button, main input, main textarea, main select, main summary, main [role='tab'], main [role='button']");
      for (var i = 0; i < nodes.length; i++) {
        var el = nodes[i];
        if (!el || dock.contains(el)) continue;
        var style = window.getComputedStyle(el);
        if (style.display === "none" || style.visibility === "hidden") continue;
        var box = el.getBoundingClientRect();
        if (box.width < 8 || box.height < 8) continue;
        var visible = box.bottom > 0 && box.right > 0 && box.top < window.innerHeight && box.left < window.innerWidth;
        if (!visible) continue;
        if (tipBox.left < box.right - 1 && tipBox.right > box.left + 1 && tipBox.top < box.bottom - 1 && tipBox.bottom > box.top + 1) {
          return true;
        }
      }
      return false;
    }

    if (overlapsControls()) {
      tip.hidden = true;
      return;
    }

    if (window.innerWidth <= 480) {
      dock.classList.remove("lobsy-coach-dock--tip-aside");
      return;
    }

    dock.classList.remove("lobsy-coach-dock--tip-aside");
  },
  bindScrollDismiss: function (dock) {
    if (!dock || dock.dataset.scrollDismiss === "1") return;
    dock.dataset.scrollDismiss = "1";
    var last = window.scrollY || 0;
    window.addEventListener("scroll", function () {
      var y = window.scrollY || 0;
      if (Math.abs(y - last) < 12) return;
      last = y;
      window.jobsyCoachDock.placeTip(dock);
    }, { passive: true });
  },
  syncClearance: function (dock) {
    if (!dock || !dock.getBoundingClientRect) return;
    var btn = dock.querySelector("#lobsy-coach-btn") || dock;
    var box = btn.getBoundingClientRect();
    if (!box || box.width < 8) return;
    var gap = 12;
    var clear = Math.ceil(window.innerWidth - box.left + gap);
    if (clear < 72) clear = 72;
    document.documentElement.style.setProperty("--lobsy-coach-clear-inline", clear + "px");
  }
};
