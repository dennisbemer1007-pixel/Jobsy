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

    function getOrCreateAnonymousKey() {
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
