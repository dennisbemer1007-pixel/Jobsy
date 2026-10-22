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

    /**
     * Sync URL query params without Blazor navigation (no LocationChanged / rerender flash).
     * @param {Record<string, string|null|undefined>} updates
     */
    function replaceQuery(updates) {
        if (!updates || typeof updates !== "object") {
            return;
        }
        try {
            const url = new URL(window.location.href);
            Object.keys(updates).forEach(function (key) {
                const v = updates[key];
                if (v === null || v === undefined || v === "") {
                    url.searchParams.delete(key);
                } else {
                    url.searchParams.set(key, String(v));
                }
            });
            const next = url.pathname + url.search + url.hash;
            const cur = window.location.pathname + window.location.search + window.location.hash;
            if (next !== cur) {
                history.replaceState(history.state, "", next);
            }
        } catch {
            // ignore
        }
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
                    const accuracyMeters = typeof pos.coords.accuracy === "number" ? pos.coords.accuracy : null;
                    setStoredOrigin(lat, lng);
                    resolve({ lat: lat, lng: lng, accuracyMeters: accuracyMeters, Lat: lat, Lng: lng, AccuracyMeters: accuracyMeters });
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
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
        });
    }

    function focusElement(id) {
        const el = document.getElementById(id);
        if (el && typeof el.focus === "function") {
            el.focus();
            if (typeof el.select === "function") {
                try {
                    el.select();
                } catch {
                    // read-only inputs
                }
            }
        }
    }

    function blurElement(id) {
        const el = document.getElementById(id);
        if (el && typeof el.blur === "function") {
            el.blur();
        }
    }

    /** Resolves when jobsyGeo.requestLocation is callable (app-core loaded). */
    function whenReady(timeoutMs) {
        var limit = typeof timeoutMs === "number" && timeoutMs > 0 ? timeoutMs : 8000;
        return new Promise(function (resolve, reject) {
            if (typeof requestLocation === "function") {
                resolve(true);
                return;
            }
            var started = Date.now();
            var timer = setInterval(function () {
                if (typeof requestLocation === "function") {
                    clearInterval(timer);
                    resolve(true);
                    return;
                }
                if (Date.now() - started >= limit) {
                    clearInterval(timer);
                    reject(new Error("Locatie is nog niet beschikbaar. Probeer het zo opnieuw."));
                }
            }, 40);
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
        replaceQuery,
        getOrCreateAnonymousKey,
        tryClaimClick,
        tryClaimSiteVisit,
        requestLocation,
        focusElement,
        blurElement,
        whenReady,
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

    function readCookieValue() {
        var escaped = KEY.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var match = document.cookie.match(new RegExp("(?:^|; )" + escaped + "=([^;]*)"));
        if (!match || !match[1]) {
            return "";
        }
        try {
            return decodeURIComponent(match[1]);
        } catch (e2) {
            return match[1];
        }
    }

    function consentStored() {
        try {
            if (localStorage.getItem(KEY)) {
                return true;
            }
        } catch (e) { }
        return !!readCookieValue();
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
                var ls = localStorage.getItem(KEY);
                if (ls) {
                    return ls;
                }
            } catch (e) { }
            return readCookieValue();
        },
        set: function (value) {
            try {
                localStorage.setItem(KEY, value || "necessary");
                var maxAge = 60 * 60 * 24 * 365;
                document.cookie = KEY + "=" + encodeURIComponent(value || "necessary")
                    + "; Path=/; Max-Age=" + maxAge
                    + "; SameSite=Lax"
                    + (location.protocol === "https:" ? "; Secure" : "");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        clear: function () {
            try {
                localStorage.removeItem(KEY);
            } catch (e) { }
            try {
                document.cookie = KEY + "=; Path=/; Max-Age=0; SameSite=Lax"
                    + (location.protocol === "https:" ? "; Secure" : "");
            } catch (e2) { }
            applyKnownClass();
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
        "/js/jobMap.min.js?v=20261019-uwv5"
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
            u.searchParams.delete("minLat");
            u.searchParams.delete("minLng");
            u.searchParams.delete("maxLat");
            u.searchParams.delete("maxLng");
            u.searchParams.delete("zoom");
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
            return path === "/banenkaart" || path === "/banen";
        } catch (e) {
            return false;
        }
    }

    function whenAfterFirstPaint(cb) {
        var done = false;
        function run() {
            if (done) {
                return;
            }
            done = true;
            try { cb(); } catch (e) { }
        }
        function scheduleIdle() {
            if (typeof requestIdleCallback === "function") {
                requestIdleCallback(run, { timeout: 1200 });
            } else if (typeof requestAnimationFrame === "function") {
                requestAnimationFrame(function () { requestAnimationFrame(run); });
            } else {
                setTimeout(run, 0);
            }
        }
        if (typeof PerformanceObserver !== "undefined") {
            try {
                var po = new PerformanceObserver(function (list) {
                    var fcp = list.getEntriesByName("first-contentful-paint");
                    if (fcp && fcp.length) {
                        po.disconnect();
                        scheduleIdle();
                    }
                });
                po.observe({ type: "paint", buffered: true });
                setTimeout(function () {
                    try { po.disconnect(); } catch (ePo) { }
                    scheduleIdle();
                }, 2800);
                return;
            } catch (eObs) { }
        }
        scheduleIdle();
    }

    function whenCircuitReady(cb) {
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
        setTimeout(function () {
            clearInterval(iv);
            run();
        }, 8000);
    }

    function bootDiscoveryMap() {
        if (window.jobMap && typeof window.jobMap.boot === "function") {
            window.jobMap.boot("job-map");
            return;
        }
        ensure("discovery").then(function () {
            if (window.jobMap && typeof window.jobMap.boot === "function") {
                window.jobMap.boot("job-map");
            }
        }).catch(function () { });
    }

    // VacancyDiscovery transport toggles call jobsyGeo.replaceQuery on the query string.
    // Kick off pins fetch immediately; MapLibre after FCP/idle; boot when circuit is ready.
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
                whenAfterFirstPaint(function () {
                    ensure("discovery").then(function () {
                        bootDiscoveryMap();
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

    var extrasSrc = "/js/app-extras.js?v=20261022-onboarding-bff";
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

    function openDatePicker(el) {
        if (!el) {
            return;
        }
        if (typeof el.showPicker === "function") {
            try {
                el.showPicker();
                return;
            } catch (e) {
                // fall through
            }
        }
        el.focus();
        el.click();
    }

    return { ensure: ensure, openDatePicker: openDatePicker };
})();

window.jobsyPageVisible = function () {
    return typeof document === "undefined" || document.visibilityState !== "hidden";
};

window.jobsyDom = {
    scrollRoot: function () {
        var richtingA = document.querySelector(".ob-wizard--richting-a");
        var main = document.querySelector(".app-shell:has(.ob-wizard--richting-a) .app-main")
            || document.querySelector(".app-shell--questionnaire .app-main")
            || document.querySelector(".app-main");
        if (richtingA && main) {
            return main;
        }
        var wizardContent = document.querySelector(".app-shell--questionnaire .ob-wizard__content");
        if (wizardContent && wizardContent.scrollHeight > wizardContent.clientHeight + 1) {
            return wizardContent;
        }
        if (main && main.scrollHeight > main.clientHeight + 1) {
            return main;
        }
        return document.scrollingElement || document.documentElement;
    },
    scrollToTop: function () {
        var root = window.jobsyDom.scrollRoot();
        root.scrollTop = 0;
        try {
            window.scrollTo(0, 0);
        } catch (e) { }
    },
    scrollToId: function (id) {
        var el = document.getElementById(id);
        if (!el) {
            return;
        }
        try {
            el.scrollIntoView({ behavior: "smooth", block: "start" });
        } catch (e) {
            try {
                el.scrollIntoView(true);
            } catch (e2) { }
        }
    },
    focusWizardStepHeading: function () {
        var scope = document.querySelector(".ob-wizard__content")
            || document.querySelector(".ob-wizard");
        if (!scope) {
            return;
        }
        var h = scope.querySelector("h1[tabindex], h1");
        if (!h || typeof h.focus !== "function") {
            return;
        }
        try {
            h.focus({ preventScroll: true });
        } catch (e) {
            try {
                h.focus();
            } catch (e2) { }
        }
    },
    resetWizardStepScroll: function () {
        window.jobsyDom.scrollToTop();
        window.jobsyDom.focusWizardStepHeading();
    },
    readInputValue: function (el) {
        if (!el || typeof el.value === "undefined" || el.value === null) {
            return "";
        }
        return String(el.value);
    },
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

    function portalPaspoortDangerDialog(el) {
        if (!el || !el.classList) {
            return;
        }
        if (!el.classList.contains("profile-reset-modal") && !el.classList.contains("unsubscribe-modal")) {
            return;
        }
        var backdrop = el.previousElementSibling;
        if (!backdrop || !backdrop.classList.contains("share-modal-backdrop")) {
            return;
        }
        if (el.parentElement === document.body) {
            return;
        }
        document.body.appendChild(backdrop);
        document.body.appendChild(el);
    }

    function trap(el) {
        release();
        if (!el) {
            return;
        }
        portalPaspoortDangerDialog(el);
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
        ? "/service-worker.published.js?v=20261021-01"
        : "/service-worker.js?v=20261021-01";
    var isWebKit = /AppleWebKit/i.test(navigator.userAgent || "")
        && !/Chrome|Chromium|CriOS|EdgiOS|FxiOS/i.test(navigator.userAgent || "");
    var userRequestedReload = false;
    var updateBannerVisible = false;
    var pageLoadedAt = Date.now();
    var registrationWired = false;
    var updateDismissed = false;
    /** Grace after navigation so a stale waiting worker from the previous deploy is not shown immediately. */
    var UPDATE_BANNER_GRACE_MS = 12000;

    function circuitReady() {
        try {
            return document.documentElement.getAttribute("data-lobsy-circuit") === "ready";
        } catch (e) {
            return false;
        }
    }

    function ensureUpdateBanner() {
        var el = document.getElementById("lobsy-app-update");
        if (el) {
            return el;
        }
        el = document.createElement("div");
        el.id = "lobsy-app-update";
        el.className = "lobsy-app-update";
        el.setAttribute("role", "status");
        el.setAttribute("aria-live", "polite");
        el.innerHTML =
            '<div class="lobsy-app-update__panel">'
            + '<span class="lobsy-app-update__text">Nieuwe versie beschikbaar.</span>'
            + '<button type="button" class="lobsy-app-update__action" data-lobsy-app-update>Vernieuwen</button>'
            + '<button type="button" class="lobsy-app-update__dismiss" data-lobsy-app-update-dismiss aria-label="Sluiten">&times;</button>'
            + "</div>";
        el.addEventListener("click", function (ev) {
            var dismiss = ev.target && ev.target.closest ? ev.target.closest("[data-lobsy-app-update-dismiss]") : null;
            if (dismiss) {
                ev.preventDefault();
                updateDismissed = true;
                el.classList.remove("is-visible");
                updateBannerVisible = false;
                return;
            }
            var btn = ev.target && ev.target.closest ? ev.target.closest("[data-lobsy-app-update]") : null;
            if (!btn) {
                return;
            }
            ev.preventDefault();
            userRequestedReload = true;
            try {
                console.info("[lobsy] manual reload (app update)");
            } catch (e) { }
            if (window.lobsyAllowDocumentReload) {
                window.lobsyAllowDocumentReload();
            }
            navigator.serviceWorker.getRegistration().then(function (reg) {
                if (reg && reg.waiting) {
                    reg.waiting.postMessage({ type: "SKIP_WAITING" });
                } else {
                    window.location.reload();
                }
            }).catch(function () {
                window.location.reload();
            });
        });
        document.body.appendChild(el);
        return el;
    }

    function mayShowUpdateBanner() {
        if (updateDismissed || updateBannerVisible) {
            return false;
        }
        if (!navigator.serviceWorker.controller) {
            return false;
        }
        if (Date.now() - pageLoadedAt < UPDATE_BANNER_GRACE_MS) {
            return false;
        }
        return true;
    }

    function showUpdateBanner() {
        if (!mayShowUpdateBanner()) {
            return;
        }
        updateBannerVisible = true;
        var el = ensureUpdateBanner();
        el.classList.add("is-visible");
    }

    function scheduleUpdateBannerCheck() {
        var wait = UPDATE_BANNER_GRACE_MS - (Date.now() - pageLoadedAt);
        if (wait <= 0) {
            showUpdateBanner();
            return;
        }
        window.setTimeout(function () {
            showUpdateBanner();
        }, wait + 50);
    }

    function wireWaitingWorker(reg, worker) {
        if (!worker) {
            return;
        }
        worker.addEventListener("statechange", function () {
            if (worker.state === "installed" && navigator.serviceWorker.controller) {
                scheduleUpdateBannerCheck();
            }
        });
        if (worker.state === "installed" && navigator.serviceWorker.controller) {
            scheduleUpdateBannerCheck();
        }
    }

    function wireRegistration(reg) {
        if (!reg) {
            return;
        }
        if (registrationWired) {
            return;
        }
        registrationWired = true;
        /* Do not surface reg.waiting from before this page load — only updates found after register. */
        reg.addEventListener("updatefound", function () {
            wireWaitingWorker(reg, reg.installing);
        });
    }

    navigator.serviceWorker.addEventListener("controllerchange", function () {
        if (userRequestedReload) {
            if (window.lobsyAllowDocumentReload) {
                window.lobsyAllowDocumentReload();
            }
            window.location.reload();
        }
    });

    function register() {
        navigator.serviceWorker.register(swUrl, { scope: "/", updateViaCache: "none" })
            .then(function (reg) {
                wireRegistration(reg);
                return reg.update().catch(function () { });
            })
            .catch(function () { });
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

    document.addEventListener("visibilitychange", function () {
        if (document.visibilityState !== "visible") {
            return;
        }
        navigator.serviceWorker.getRegistration().then(function (reg) {
            if (reg) {
                return reg.update().catch(function () { });
            }
        }).catch(function () { });
    });

    window.addEventListener("load", function () {
        window.setTimeout(scheduleRegister, isWebKit ? 1200 : 0);
    });
})();

/* === lobsyPwaInstall === */
window.lobsyPwaInstall = (function () {
  var KEY_VISITS = "Jobsy.PwaVisits";
  var KEY_DISMISS = "Jobsy.PwaInstallDismissed";
  var KEY_ENTRY_DISMISS = "Jobsy.PwaEntryDismissedUntil";
  var KEY_POST_APPLY = "Jobsy.PwaPostApplyCardVisible";
  var KEY_SHARE_HINT = "Jobsy.PwaShareHintSeen";
  var DISMISS_MS = 30 * 24 * 60 * 60 * 1000;
  var deferred = null;
  var relatedInstalled = false;
  var bannerRef = null;
  var entryRefs = [];
  var ua = navigator.userAgent || "";
  var isIos = /iphone|ipad|ipod/i.test(ua)
    || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
  var isSamsungBrowser = /SamsungBrowser/i.test(ua);
  var isAndroid = /Android/i.test(ua);

  function readStandaloneMedia() {
    return window.matchMedia("(display-mode: standalone)").matches
      || window.matchMedia("(display-mode: fullscreen)").matches
      || window.matchMedia("(display-mode: minimal-ui)").matches
      || window.navigator.standalone === true;
  }

  function readAndroidAppReferrer() {
    try {
      return (document.referrer || "").indexOf("android-app://") === 0;
    } catch (e) {
      return false;
    }
  }

  function readPwaStartUrl() {
    try {
      var params = new URLSearchParams(window.location.search);
      return params.get("utm_source") === "pwa";
    } catch (e) {
      return false;
    }
  }

  function isInstalledApp() {
    return readStandaloneMedia() || readAndroidAppReferrer() || readPwaStartUrl() || relatedInstalled;
  }

  function readStandalone() {
    return isInstalledApp();
  }

  function entryDismissedUntil() {
    try {
      var raw = localStorage.getItem(KEY_ENTRY_DISMISS);
      if (!raw) return null;
      var n = parseInt(raw, 10);
      return isNaN(n) ? null : n;
    } catch (e) {
      return null;
    }
  }

  function wasEntryDismissed() {
    var until = entryDismissedUntil();
    return until !== null && Date.now() < until;
  }

  function dismissEntryFor30Days() {
    try {
      localStorage.setItem(KEY_ENTRY_DISMISS, String(Date.now() + DISMISS_MS));
    } catch (e) { }
    notifyEntry();
  }

  function markInstalled() {
    relatedInstalled = true;
    try {
      localStorage.setItem(KEY_DISMISS, "1");
      localStorage.removeItem(KEY_POST_APPLY);
    } catch (e) { }
    deferred = null;
    notifyBanner();
    notifyEntry();
    stabilizeStandaloneTitle();
  }

  function stabilizeStandaloneTitle() {
    try {
      var bad = /installatie|uninstall|ongedaan/i.test(document.title || "");
      if (bad) {
        document.title = "Lobsy";
        return;
      }
      if (readStandaloneMedia() && !(document.title || "").trim()) {
        document.title = "Lobsy";
      }
    } catch (e) { }
  }

  function notifyBanner() {
    if (!bannerRef || typeof bannerRef.invokeMethodAsync !== "function") return;
    var show = shouldShowBanner();
    bannerRef.invokeMethodAsync("SetPwaBannerVisible", show).catch(function () { });
  }

  function notifyEntry() {
    entryRefs.forEach(function (ref) {
      if (!ref || typeof ref.invokeMethodAsync !== "function") return;
      ref.invokeMethodAsync("OnPwaInstallSignalsChanged").catch(function () { });
    });
  }

  function shouldShowBanner() {
    if (isInstalledApp() || wasDismissed()) return false;
    if (isIos) return visits() >= 2;
    return visits() >= 2 && !!deferred;
  }

  try {
    window.addEventListener("beforeinstallprompt", function (e) {
      e.preventDefault();
      deferred = e;
      window.dispatchEvent(new CustomEvent("lobsy-pwa-prompt-ready"));
      notifyBanner();
      notifyEntry();
    });
    window.addEventListener("appinstalled", function () {
      markInstalled();
      window.dispatchEvent(new CustomEvent("lobsy-pwa-installed"));
    });
  } catch (err) { }

  if (navigator.getInstalledRelatedApps) {
    navigator.getInstalledRelatedApps().then(function (apps) {
      if (apps && apps.length) {
        markInstalled();
      }
    }).catch(function () { });
  }

  stabilizeStandaloneTitle();
  if (readStandaloneMedia()) {
    try {
      var titleObs = new MutationObserver(function () { stabilizeStandaloneTitle(); });
      titleObs.observe(document.querySelector("title") || document.head, { subtree: true, childList: true, characterData: true });
    } catch (e) { }
  }

  function visits() {
    try {
      var n = parseInt(localStorage.getItem(KEY_VISITS) || "0", 10);
      return isNaN(n) ? 0 : n;
    } catch (e) {
      return 0;
    }
  }

  function bumpVisit() {
    if (isInstalledApp()) return visits();
    try {
      var n = visits() + 1;
      localStorage.setItem(KEY_VISITS, String(n));
      return n;
    } catch (e) {
      return 0;
    }
  }

  function wasDismissed() {
    try {
      var legacy = localStorage.getItem(KEY_DISMISS) === "1";
      if (legacy) return true;
      return wasEntryDismissed();
    } catch (e) {
      return false;
    }
  }

  function buildChromeIntentUrl() {
    var path = window.location.pathname + window.location.search + window.location.hash;
    return "intent://" + window.location.host + path + "#Intent;scheme=https;package=com.android.chrome;end";
  }

  function getSignals() {
    return {
      displayStandalone: window.matchMedia("(display-mode: standalone)").matches,
      displayMinimalUi: window.matchMedia("(display-mode: minimal-ui)").matches,
      displayFullscreen: window.matchMedia("(display-mode: fullscreen)").matches,
      navigatorStandalone: window.navigator.standalone === true,
      androidAppReferrer: readAndroidAppReferrer(),
      pwaStartUrl: readPwaStartUrl(),
      relatedInstalled: relatedInstalled,
      isIos: isIos,
      isSamsungBrowser: isSamsungBrowser,
      isAndroid: isAndroid,
      canDeferredPrompt: !!deferred && !isInstalledApp(),
      entryDismissedUntilUnixMs: entryDismissedUntil(),
      postApplyCardPending: (function () {
        try { return localStorage.getItem(KEY_POST_APPLY) === "1"; } catch (e) { return false; }
      })(),
      shareHintSeen: (function () {
        try { return localStorage.getItem(KEY_SHARE_HINT) === "1"; } catch (e) { return false; }
      })()
    };
  }

  return {
    bumpVisit: bumpVisit,
    isIos: function () { return isIos; },
    isStandalone: function () { return isInstalledApp(); },
    canPrompt: function () { return !!deferred && !isInstalledApp(); },
    wasDismissed: wasDismissed,
    dismiss: function () {
      dismissEntryFor30Days();
      try { localStorage.setItem(KEY_DISMISS, "1"); } catch (e) { }
      deferred = null;
      notifyBanner();
    },
    dismissEntry: dismissEntryFor30Days,
    shouldShowBanner: shouldShowBanner,
    watchBanner: function (dotNetRef) {
      bannerRef = dotNetRef;
      function tick() { notifyBanner(); }
      window.addEventListener("lobsy-pwa-prompt-ready", tick);
      window.addEventListener("lobsy-pwa-installed", tick);
      tick();
    },
    unwatchBanner: function () {
      bannerRef = null;
    },
    watchEntry: function (dotNetRef) {
      if (dotNetRef && entryRefs.indexOf(dotNetRef) < 0) {
        entryRefs.push(dotNetRef);
      }
      function tick() { notifyEntry(); }
      window.addEventListener("lobsy-pwa-prompt-ready", tick);
      window.addEventListener("lobsy-pwa-installed", tick);
      tick();
    },
    unwatchEntry: function (dotNetRef) {
      entryRefs = entryRefs.filter(function (r) { return r !== dotNetRef; });
    },
    getSignals: getSignals,
    buildChromeIntentUrl: buildChromeIntentUrl,
    notifyApplicationSubmitted: function () {
      if (isInstalledApp()) return;
      try {
        localStorage.setItem(KEY_POST_APPLY, "1");
      } catch (e) { }
      notifyEntry();
    },
    dismissPostApplyCard: function () {
      try { localStorage.removeItem(KEY_POST_APPLY); } catch (e) { }
      dismissEntryFor30Days();
      notifyEntry();
    },
    markShareHintSeen: function () {
      try { localStorage.setItem(KEY_SHARE_HINT, "1"); } catch (e) { }
      notifyEntry();
    },
    copyPageLink: async function () {
      var url = window.location.href;
      try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
          await navigator.clipboard.writeText(url);
          return { ok: true };
        }
      } catch (e) { }
      return { ok: false, url: url };
    },
    prompt: async function () {
      if (!deferred) return { ok: false, reason: "unavailable" };
      try {
        deferred.prompt();
        var choice = await deferred.userChoice;
        var outcome = choice && choice.outcome;
        deferred = null;
        if (outcome === "accepted") {
          markInstalled();
        } else {
          dismissEntryFor30Days();
        }
        notifyBanner();
        notifyEntry();
        return { ok: true, outcome: outcome };
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

window.jobsyProfileEncourage = window.jobsyProfileEncourage || {
  isDismissed: function (key) {
    try { return localStorage.getItem("lobsy-encourage:" + key) === "1"; } catch (e) { return false; }
  },
  dismiss: function (key) {
    try { localStorage.setItem("lobsy-encourage:" + key, "1"); } catch (e) { }
  }
};

window.jobsyRefVerificationTip = window.jobsyRefVerificationTip || {
  isDismissed: function (key) {
    try { return localStorage.getItem("lobsy-ref-verification:" + key) === "1"; } catch (e) { return false; }
  },
  dismiss: function (key) {
    try { localStorage.setItem("lobsy-ref-verification:" + key, "1"); } catch (e) { }
  }
};

function jobsyCoachDockElement(dock) {
  if (!dock) return null;
  if (dock.nodeType === 1 && dock.classList) return dock;
  return null;
}

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
    var el = jobsyCoachDockElement(dock) || document.querySelector(".lobsy-coach-dock[data-lobsy-coach]");
    if (!el || !el.classList) return;
    dock = el;
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
    dock = jobsyCoachDockElement(dock) || document.querySelector(".lobsy-coach-dock[data-lobsy-coach]");
    if (!dock || !dock.getBoundingClientRect) return;
    var btn = dock.querySelector("#lobsy-coach-btn") || dock;
    var box = btn.getBoundingClientRect();
    if (!box || box.width < 8) return;
    var gap = 12;
    var clear = Math.ceil(window.innerWidth - box.left + gap);
    if (clear < 72) clear = 72;
    document.documentElement.style.setProperty("--lobsy-coach-clear-inline", clear + "px");
  },
  syncStickyFooters: function (cssVar) {
    cssVar = cssVar || "--sticky-footer-h";
    var root = document.documentElement;
    var roMap = window.jobsyCoachDock._stickyRoMap = window.jobsyCoachDock._stickyRoMap || new WeakMap();

    function measure() {
      var max = 0;
      document.querySelectorAll("[data-sticky-footer]").forEach(function (el) {
        if (!el || !el.isConnected) return;
        var style = window.getComputedStyle(el);
        if (style.display === "none" || style.visibility === "hidden") return;
        var h = Math.ceil(el.getBoundingClientRect().height);
        if (h > max) max = h;
      });
      root.style.setProperty(cssVar, max + "px");
    }

    function observeEl(el) {
      if (!el || roMap.has(el)) return;
      if (typeof ResizeObserver === "function") {
        var ro = new ResizeObserver(measure);
        ro.observe(el);
        roMap.set(el, ro);
      }
    }

    document.querySelectorAll("[data-sticky-footer]").forEach(observeEl);
    measure();
    document.querySelectorAll("[data-lobsy-coach], .lobsy-coach-dock").forEach(function (dock) {
      window.jobsyCoachDock.syncClearance(dock);
    });

    if (!window.jobsyCoachDock._stickyMo) {
      window.jobsyCoachDock._stickyMo = new MutationObserver(function () {
        document.querySelectorAll("[data-sticky-footer]").forEach(observeEl);
        measure();
      });
      window.jobsyCoachDock._stickyMo.observe(document.body, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ["class", "style", "hidden"]
      });
    }

  },
  bindKbExtCoachGuard: function () {
    if (!document.querySelector(".kb-ext[data-testid='kb-ext-detail']")) {
      return;
    }
    var state = window.jobsyCoachDock._kbExtGuard = window.jobsyCoachDock._kbExtGuard || {};

    function mobileKbExt() {
      return window.matchMedia("(max-width: 639px)").matches
        && !!document.querySelector(".kb-ext[data-testid='kb-ext-detail']");
    }

    function dockEl() {
      return document.querySelector(".lobsy-coach-dock[data-lobsy-coach]");
    }

    function actionsEl() {
      return document.querySelector("[data-testid='kb-ext-detail-actions']");
    }

    function setSuppressed(on) {
      var dock = dockEl();
      if (!dock || !dock.classList) return;
      dock.classList.toggle("lobsy-coach-dock--suppressed", !!on);
      if (document.documentElement && document.documentElement.classList) {
        document.documentElement.classList.toggle("kb-ext-coach-suppressed", !!on);
      }
    }

    function actionsVisible() {
      var actions = actionsEl();
      if (!actions) return false;
      var r = actions.getBoundingClientRect();
      var vh = window.innerHeight || document.documentElement.clientHeight || 0;
      return r.height > 0 && r.bottom > 0 && r.top < vh;
    }

    function coachOverlapsMain() {
      var dock = dockEl();
      var coach = dock ? dock.querySelector("#lobsy-coach-btn") : null;
      if (!coach || !coach.getBoundingClientRect) return false;
      var c = coach.getBoundingClientRect();
      if (c.width < 8 || c.height < 8) return false;
      var vh = window.innerHeight || document.documentElement.clientHeight || 0;
      var nodes = document.querySelectorAll(
        "main a, main button, main p, main td, main th, main h1, main h2, main h3, main li"
      );
      for (var i = 0; i < nodes.length; i++) {
        var node = nodes[i];
        if (!node || !dock || dock.contains(node)) continue;
        var style = window.getComputedStyle(node);
        if (style.display === "none" || style.visibility === "hidden") continue;
        var box = node.getBoundingClientRect();
        if (box.width < 4 || box.height < 4) continue;
        if (box.bottom <= 0 || box.top >= vh) continue;
        if (
          c.left < box.right - 1 &&
          c.right > box.left + 1 &&
          c.top < box.bottom - 1 &&
          c.bottom > box.top + 1
        ) {
          return true;
        }
      }
      return false;
    }

    function refresh() {
      if (!mobileKbExt()) {
        setSuppressed(false);
        return;
      }
      var suppress = actionsVisible();
      if (!suppress) {
        suppress = coachOverlapsMain();
      }
      setSuppressed(suppress);
    }

    function teardownIo() {
      if (state.io) {
        state.io.disconnect();
        state.io = null;
      }
    }

    function setupIo() {
      teardownIo();
      if (!mobileKbExt()) return;
      var actions = actionsEl();
      if (!actions || typeof IntersectionObserver !== "function") {
        refresh();
        return;
      }
      state.io = new IntersectionObserver(function () {
        refresh();
      }, { threshold: [0, 0.02, 0.08, 0.2] });
      state.io.observe(actions);
      refresh();
    }

    if (!state.bound) {
      state.bound = true;
      window.addEventListener("scroll", refresh, { passive: true });
      window.addEventListener("resize", function () {
        setupIo();
      }, { passive: true });
      if (!state.mq) {
        state.mq = window.matchMedia("(max-width: 639px)");
        state.mq.addEventListener("change", setupIo);
      }
      if (!state.mo) {
        state.mo = new MutationObserver(function () {
          if (document.querySelector(".kb-ext[data-testid='kb-ext-detail']")) {
            setupIo();
          }
        });
        state.mo.observe(document.body, { childList: true, subtree: true });
      }
      document.addEventListener("enhancedload", setupIo);
      try {
        if (window.Blazor && typeof Blazor.addEventListener === "function") {
          Blazor.addEventListener("enhancedload", setupIo);
        }
      } catch (e) { }
    }

    setupIo();
  }
};

(function jobsyCoachKbExtBoot() {
  function boot() {
    if (!document.querySelector(".kb-ext[data-testid='kb-ext-detail']")) {
      return;
    }
    if (window.jobsyCoachDock && typeof window.jobsyCoachDock.bindKbExtCoachGuard === "function") {
      window.jobsyCoachDock.bindKbExtCoachGuard();
    }
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
  document.addEventListener("enhancedload", boot);
  try {
    if (window.Blazor && typeof Blazor.addEventListener === "function") {
      Blazor.addEventListener("enhancedload", boot);
    }
  } catch (e) { }
})();
