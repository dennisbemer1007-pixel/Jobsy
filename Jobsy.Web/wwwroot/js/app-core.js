/* app-core.js — concatenated geo + culture + sessionIdle + cookieConsent + download + richtext + maps-loader. */

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

/* === sessionIdle.js === */
(function () {
    "use strict";

    var STORAGE_PREFIX = "lobsy.sessionDraft:";
    var DEFAULT_MINUTES = 30;
    var REFRESH_MS = 60 * 1000;
    var ACTIVITY_THROTTLE_MS = 5 * 1000;
    var SENSITIVE_RE = /(iban|password|passwd|secret|token|cvv|ssn|bsn|vat|btw|credit.?card|card.?number)/i;

    var timeoutMinutes = DEFAULT_MINUTES;
    var idleTimer = null;
    var refreshTimer = null;
    var lastActivityFlush = 0;
    var started = false;
    var expiring = false;
    var apiBaseUrl = "";
    var userKey = "anon";

    function draftKey() {
        return STORAGE_PREFIX + userKey + ":" + (window.location.pathname || "/");
    }

    function isSensitiveField(el) {
        var type = (el.type || "").toLowerCase();
        if (type === "password" || type === "file" || type === "hidden") {
            return true;
        }
        var identity = [el.name, el.id, el.getAttribute("autocomplete"), el.getAttribute("aria-label")]
            .filter(Boolean)
            .join(" ");
        return SENSITIVE_RE.test(identity);
    }

    function saveCriticalDrafts() {
        try {
            // Opt-in only — never scrape arbitrary login-form PII (IBAN, registration, etc.).
            var forms = document.querySelectorAll('form[data-session-draft="true"]');
            if (!forms.length) {
                return;
            }

            var payload = { savedAt: Date.now(), userKey: userKey, fields: {} };
            forms.forEach(function (form, formIndex) {
                var fields = form.querySelectorAll("input, textarea, select");
                fields.forEach(function (el, fieldIndex) {
                    if (!el || el.disabled || isSensitiveField(el)) {
                        return;
                    }
                    var type = (el.type || "").toLowerCase();
                    if (type === "checkbox" || type === "radio") {
                        if (!el.checked) {
                            return;
                        }
                    }
                    var key = el.name || el.id || ("f" + formIndex + "_" + fieldIndex);
                    if (!key || SENSITIVE_RE.test(key)) {
                        return;
                    }
                    payload.fields[key] = {
                        value: el.value,
                        type: type,
                        tag: (el.tagName || "").toLowerCase(),
                        name: el.name || "",
                        id: el.id || ""
                    };
                });
            });

            if (Object.keys(payload.fields).length > 0) {
                sessionStorage.setItem(draftKey(), JSON.stringify(payload));
            }
        } catch (e) {
            // Draft save must never block logout.
        }
    }

    function restoreCriticalDrafts() {
        try {
            var raw = sessionStorage.getItem(draftKey());
            if (!raw) {
                return;
            }
            var payload = JSON.parse(raw);
            if (!payload || !payload.fields) {
                return;
            }
            if (payload.userKey && payload.userKey !== userKey) {
                sessionStorage.removeItem(draftKey());
                return;
            }

            Object.keys(payload.fields).forEach(function (key) {
                var item = payload.fields[key];
                if (!item || SENSITIVE_RE.test(key) || SENSITIVE_RE.test(item.name || "") || SENSITIVE_RE.test(item.id || "")) {
                    return;
                }
                var el = null;
                if (item.id) {
                    el = document.getElementById(item.id);
                }
                if (!el && item.name) {
                    el = document.querySelector(
                        '[name="' + item.name.replace(/"/g, '\\"') + '"]'
                    );
                }
                if (!el || isSensitiveField(el)) {
                    return;
                }
                var type = (item.type || el.type || "").toLowerCase();
                if (type === "checkbox" || type === "radio") {
                    el.checked = true;
                } else {
                    el.value = item.value || "";
                    el.dispatchEvent(new Event("input", { bubbles: true }));
                    el.dispatchEvent(new Event("change", { bubbles: true }));
                }
            });
        } catch (e) {
            // Ignore restore failures.
        }
    }

    function clearDrafts() {
        try {
            var keys = [];
            for (var i = 0; i < sessionStorage.length; i++) {
                var k = sessionStorage.key(i);
                if (k && k.indexOf(STORAGE_PREFIX) === 0) {
                    keys.push(k);
                }
            }
            keys.forEach(function (k) {
                sessionStorage.removeItem(k);
            });
        } catch (e) {
            // Ignore.
        }
    }

    function clearIdleTimer() {
        if (idleTimer) {
            clearTimeout(idleTimer);
            idleTimer = null;
        }
    }

    function scheduleIdle() {
        clearIdleTimer();
        var ms = Math.max(1, timeoutMinutes) * 60 * 1000;
        idleTimer = setTimeout(onIdle, ms);
    }

    function forceSessionExpiredLogout() {
        if (expiring) {
            return;
        }
        // Already on the login screen — do not bounce through logout again (login loop).
        try {
            var path = window.location.pathname || "";
            if (/^\/login\/?$/i.test(path)) {
                return;
            }
        } catch (e) { }
        expiring = true;
        saveCriticalDrafts();
        var target = "/account/logout?reason=session-expired";
        try {
            window.location.href = target;
        } catch (e) {
            window.location.assign(target);
        }
    }

    function onActivity() {
        if (!started || expiring) {
            return;
        }
        var now = Date.now();
        if (now - lastActivityFlush < ACTIVITY_THROTTLE_MS) {
            scheduleIdle();
            return;
        }
        lastActivityFlush = now;
        scheduleIdle();
        try {
            fetch("/account/session-activity", {
                method: "POST",
                credentials: "same-origin",
                headers: { "Accept": "application/json" },
                keepalive: true
            }).then(function (r) {
                if (r.status === 401 || r.headers.get("X-Jobsy-Session") === "expired") {
                    forceSessionExpiredLogout();
                }
            }).catch(function () { });
        } catch (e) {
            // Ignore.
        }
    }

    function onIdle() {
        forceSessionExpiredLogout();
    }

    function refreshTimeout() {
        var urls = ["/account/session-security"];
        if (apiBaseUrl) {
            urls.push(String(apiBaseUrl).replace(/\/?$/, "/") + "api/settings/session-security");
        }

        function tryNext(index) {
            if (index >= urls.length) {
                return;
            }
            fetch(urls[index], { credentials: "same-origin" })
                .then(function (r) {
                    if (!r.ok) {
                        throw new Error("timeout fetch failed");
                    }
                    return r.json();
                })
                .then(function (data) {
                    var minutes = Number(data && data.inactivityTimeoutMinutes);
                    if (!Number.isFinite(minutes) || minutes < 5) {
                        minutes = DEFAULT_MINUTES;
                    }
                    if (minutes > 480) {
                        minutes = 480;
                    }
                    if (minutes !== timeoutMinutes) {
                        timeoutMinutes = minutes;
                        scheduleIdle();
                    }
                })
                .catch(function () {
                    tryNext(index + 1);
                });
        }

        tryNext(0);
    }

    function bindActivity() {
        if (bindActivity._bound) {
            return;
        }
        bindActivity._bound = true;
        ["click", "mousedown", "keydown", "touchstart", "scroll", "mousemove"].forEach(function (evt) {
            document.addEventListener(evt, onActivity, { passive: true, capture: true });
        });
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "visible") {
                onActivity();
            }
        });
        window.addEventListener("lobsy:navigation", onActivity);
    }

    // Clear drafts when landing on login after expiry (shared-browser hygiene).
    try {
        if (/[?&]error=session-expired\b/.test(window.location.search || "")) {
            // Keep opt-in vacancy drafts for the same browser user; drop nothing globally here —
            // identity-bound keys already prevent cross-user restore. Still scrub if anon.
            if (userKey === "anon") {
                clearDrafts();
            }
        }
    } catch (e) { }

    window.lobsySessionIdle = {
        start: function (options) {
            options = options || {};
            if (typeof options.timeoutMinutes === "number" && options.timeoutMinutes > 0) {
                timeoutMinutes = options.timeoutMinutes;
            }
            if (options.apiBaseUrl) {
                apiBaseUrl = String(options.apiBaseUrl);
            }
            if (options.userKey) {
                userKey = String(options.userKey);
            }
            if (started) {
                scheduleIdle();
                return;
            }
            started = true;
            expiring = false;
            bindActivity();
            restoreCriticalDrafts();
            scheduleIdle();
            refreshTimeout();
            refreshTimer = setInterval(refreshTimeout, REFRESH_MS);
        },
        stop: function () {
            started = false;
            expiring = false;
            clearIdleTimer();
            if (refreshTimer) {
                clearInterval(refreshTimer);
                refreshTimer = null;
            }
        },
        setTimeoutMinutes: function (minutes) {
            var n = Number(minutes);
            if (!Number.isFinite(n) || n < 5) {
                n = DEFAULT_MINUTES;
            }
            timeoutMinutes = Math.min(480, n);
            if (started) {
                scheduleIdle();
            }
        },
        setUserKey: function (key) {
            userKey = key ? String(key) : "anon";
        },
        saveDrafts: saveCriticalDrafts,
        restoreDrafts: restoreCriticalDrafts,
        clearDrafts: clearDrafts,
        markActivity: onActivity,
        expireNow: forceSessionExpiredLogout,
        checkSession: function () {
            return fetch("/account/session-activity", {
                method: "GET",
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            }).then(function (r) {
                if (r.status === 401 || r.headers.get("X-Jobsy-Session") === "expired") {
                    forceSessionExpiredLogout();
                    return false;
                }
                return true;
            }).catch(function () {
                return true;
            });
        }
    };
})();

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

/* === download.js === */
window.jobsyDownload = {
  text: function (filename, content, mimeType) {
    const blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename || "download.txt";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },
  bytes: function (filename, base64, mimeType) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: mimeType || "application/octet-stream" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename || "download.bin";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }
};

/* === richtext.js === */
window.jobsyRichtext = {
    /**
     * Wrap the current selection in a textarea with before/after markup.
     * If nothing is selected, wraps the whole value (or inserts a placeholder).
     * Returns the new textarea value.
     */
    wrap: function (textarea, before, after, placeholder) {
        if (!textarea) {
            return null;
        }

        var start = textarea.selectionStart ?? 0;
        var end = textarea.selectionEnd ?? 0;
        var value = textarea.value ?? "";
        var selected = value.substring(start, end);

        if (!selected) {
            selected = placeholder || "";
        }

        var next = value.substring(0, start) + before + selected + after + value.substring(end);
        textarea.value = next;

        var cursor = start + before.length + selected.length;
        textarea.focus();
        textarea.setSelectionRange(cursor, cursor);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));

        return next;
    },

    /**
     * Prompt for a URL and wrap the selection in an <a> tag.
     */
    insertLink: function (textarea) {
        if (!textarea) {
            return null;
        }

        var url = window.prompt("Link-URL (https://…)", "https://");
        if (!url || !url.trim()) {
            return null;
        }

        url = url.trim();
        var start = textarea.selectionStart ?? 0;
        var end = textarea.selectionEnd ?? 0;
        var value = textarea.value ?? "";
        var selected = value.substring(start, end) || url;

        var before = '<a href="' + url.replace(/"/g, "&quot;") + '" target="_blank" rel="noopener">';
        var after = "</a>";
        var next = value.substring(0, start) + before + selected + after + value.substring(end);
        textarea.value = next;

        var cursor = start + before.length + selected.length;
        textarea.focus();
        textarea.setSelectionRange(cursor, cursor);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));

        return next;
    }
};

/* === maps-loader.js === */
window.jobsyMaps = (function () {
    "use strict";

    var pending = {};
    var pendingPaint = {};
    var css = [
        "/lib/maplibre/maplibre-gl.css"
    ];
    var mapLibreScripts = [
        "/lib/maplibre/maplibre-gl.js",
        "/js/jobsyMapLibre.js?v=20260819-mapcls"
    ];
    var discoveryScripts = [
        "/js/jobMap.js?v=20260819-mapcls"
    ];
    var detailScripts = [
        "/js/vacancyDetailMap.js?v=20260819-mapcls"
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

    function afterNextPaint(cb) {
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(cb);
            });
        } else {
            setTimeout(cb, 0);
        }
    }

    function afterIdle(cb) {
        var run = function () {
            afterNextPaint(cb);
        };
        if (typeof requestIdleCallback === "function") {
            requestIdleCallback(function () { run(); }, { timeout: 1800 });
        } else {
            setTimeout(run, 250);
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
            afterIdle(cb);
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
            }, { rootMargin: "160px" });
            io.observe(el);
        }
        var fallback = setTimeout(finish, 2200);
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
            this.ensureAfterPaint("discovery", "job-map");
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
