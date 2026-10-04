/* Anonymous gratis werk-DNA — localStorage jobsy.gratisDna.v1 (functional, not analytics-gated). */
window.jobsyGratisDna = (function () {
    const STORAGE_KEY = "jobsy.gratisDna.v1";
    const RETENTION_DAYS = 7;
    const SCHEMA_VERSION = 1;

    const ALLOWED = {
        competency: [1, 6, 11, 16, 21],
        career: [1, 6, 14, 18, 22],
        culture: [1, 3, 5, 7, 11],
        values: [1, 6, 11, 16, 21]
    };

    function storage() {
        try {
            return localStorage;
        } catch {
            try {
                return sessionStorage;
            } catch {
                return null;
            }
        }
    }

    function readRaw() {
        const store = storage();
        if (!store) return null;
        try {
            return store.getItem(STORAGE_KEY);
        } catch {
            try {
                return sessionStorage.getItem(STORAGE_KEY);
            } catch {
                return null;
            }
        }
    }

    function writeRaw(json) {
        const store = storage();
        if (!store) return false;
        try {
            store.setItem(STORAGE_KEY, json);
            return true;
        } catch {
            try {
                sessionStorage.setItem(STORAGE_KEY, json);
                return true;
            } catch {
                return false;
            }
        }
    }

    function removeRaw() {
        try {
            localStorage.removeItem(STORAGE_KEY);
        } catch { /* ignore */ }
        try {
            sessionStorage.removeItem(STORAGE_KEY);
        } catch { /* ignore */ }
    }

    function filterAnswers(map, allowed) {
        const out = {};
        if (!map || typeof map !== "object") return out;
        const allowedSet = new Set(allowed);
        for (const key of Object.keys(map)) {
            const id = Number(key);
            const value = Number(map[key]);
            if (!allowedSet.has(id)) continue;
            if (!Number.isFinite(value) || value < 1 || value > 5) continue;
            out[String(id)] = value;
        }
        return out;
    }

    function validate(raw) {
        if (!raw || typeof raw !== "object") return null;
        if (raw.v !== SCHEMA_VERSION) return null;
        const expires = Date.parse(raw.expiresAtUtc || raw.ExpiresAtUtc || "");
        if (!Number.isFinite(expires) || expires <= Date.now()) return null;
        if (raw.ageBand !== "16plus" && raw.AgeBand !== "16plus") return null;
        const consent = raw.consent || raw.Consent;
        if (!consent || !consent.version && !consent.Version) return null;
        const answers = raw.answers || raw.Answers || {};
        return {
            v: SCHEMA_VERSION,
            createdAtUtc: raw.createdAtUtc || raw.CreatedAtUtc,
            expiresAtUtc: raw.expiresAtUtc || raw.ExpiresAtUtc,
            consent: {
                version: consent.version || consent.Version,
                atUtc: consent.atUtc || consent.AtUtc
            },
            ageBand: "16plus",
            answers: {
                competency: filterAnswers(answers.competency || answers.Competency, ALLOWED.competency),
                career: filterAnswers(answers.career || answers.Career, ALLOWED.career),
                culture: filterAnswers(answers.culture || answers.Culture, ALLOWED.culture),
                values: filterAnswers(answers.values || answers.Values, ALLOWED.values)
            }
        };
    }

    function load() {
        const raw = readRaw();
        if (!raw) return null;
        try {
            const parsed = JSON.parse(raw);
            const valid = validate(parsed);
            if (!valid) {
                removeRaw();
                return null;
            }
            return JSON.stringify(valid);
        } catch {
            removeRaw();
            return null;
        }
    }

    function save(json) {
        if (!json) return false;
        let parsed;
        try {
            parsed = typeof json === "string" ? JSON.parse(json) : json;
        } catch {
            return false;
        }
        const valid = validate(parsed);
        if (!valid) return false;
        return writeRaw(JSON.stringify(valid));
    }

    function clear() {
        removeRaw();
        return true;
    }

    function wrapLines(ctx, text, x, y, maxWidth, lineHeight, rtl) {
        if (!text) return y;
        const words = String(text).split(/\s+/);
        let line = "";
        for (let i = 0; i < words.length; i++) {
            const test = line ? line + " " + words[i] : words[i];
            if (ctx.measureText(test).width > maxWidth && line) {
                ctx.fillText(line, x, y);
                line = words[i];
                y += lineHeight;
            } else {
                line = test;
            }
        }
        if (line) {
            ctx.fillText(line, x, y);
            y += lineHeight;
        }
        return y;
    }

    async function loadLogo() {
        return new Promise((resolve) => {
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = () => resolve(img);
            img.onerror = () => resolve(null);
            img.src = "/images/brand/lobsy-128.png?v=20261004-13";
        });
    }

    async function renderCard(model) {
        const width = 1080;
        const height = 1920;
        const canvas = document.createElement("canvas");
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext("2d");
        const rtl = model && model.rtl;

        if (document.fonts && document.fonts.ready) {
            try {
                await document.fonts.ready;
            } catch { /* ignore */ }
        }

        const brand = "#0f2d5c";
        const surface = "#f7f4f0";
        const muted = "#5a6578";
        const accent = "#dce8f5";

        ctx.fillStyle = brand;
        ctx.fillRect(0, 0, width, height);

        const pad = 72;
        let y = pad + 40;

        const logo = await loadLogo();
        if (logo) {
            const logoSize = 120;
            ctx.drawImage(logo, (width - logoSize) / 2, y, logoSize, logoSize);
            y += logoSize + 36;
        }

        ctx.fillStyle = "#ffffff";
        ctx.font = "600 56px Segoe UI, Helvetica Neue, Arial, sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(model.title || "Mijn werk-DNA", width / 2, y);
        y += 48;

        ctx.fillStyle = accent;
        ctx.font = "600 28px Segoe UI, Helvetica Neue, Arial, sans-serif";
        ctx.fillText(model.badge || "", width / 2, y);
        y += 56;

        const tiles = Array.isArray(model.tiles) ? model.tiles : [];
        const tileX = pad;
        const tileW = width - pad * 2;

        for (const tile of tiles) {
            ctx.fillStyle = "rgba(255,255,255,0.12)";
            roundRect(ctx, tileX, y, tileW, 200, 24);
            ctx.fill();

            ctx.fillStyle = "#ffffff";
            ctx.font = "600 32px Segoe UI, Helvetica Neue, Arial, sans-serif";
            ctx.textAlign = rtl ? "right" : "left";
            const textX = rtl ? tileX + tileW - 32 : tileX + 32;
            ctx.fillText(tile.title || "", textX, y + 52);

            ctx.fillStyle = surface;
            ctx.font = "400 30px Segoe UI, Helvetica Neue, Arial, sans-serif";
            wrapLines(ctx, tile.sentence || "", textX, y + 100, tileW - 64, 38, rtl);
            y += 220;
        }

        y = height - 200;
        ctx.fillStyle = "#ffffff";
        ctx.font = "600 34px Segoe UI, Helvetica Neue, Arial, sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(model.footerCta || "Ontdek jouw werk-DNA · lobsy.nl/dna", width / 2, y);

        return new Promise((resolve, reject) => {
            canvas.toBlob((blob) => {
                if (blob) resolve(blob);
                else reject(new Error("canvas blob failed"));
            }, "image/png");
        });
    }

    function roundRect(ctx, x, y, w, h, r) {
        ctx.beginPath();
        ctx.moveTo(x + r, y);
        ctx.lineTo(x + w - r, y);
        ctx.quadraticCurveTo(x + w, y, x + w, y + r);
        ctx.lineTo(x + w, y + h - r);
        ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
        ctx.lineTo(x + r, y + h);
        ctx.quadraticCurveTo(x, y + h, x, y + h - r);
        ctx.lineTo(x, y + r);
        ctx.quadraticCurveTo(x, y, x + r, y);
        ctx.closePath();
    }

    function downloadBlob(blob, filename) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename || "mijn-werk-dna.png";
        a.click();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    }

    async function share(model, channel) {
        const shareUrl = "https://lobsy.nl/dna";
        const text = (model && model.shareText) || "Ontdek jouw werk-DNA";
        try {
            const blob = await renderCard(model || {});
            const file = new File([blob], "mijn-werk-dna.png", { type: "image/png" });

            if (channel === "whatsapp") {
                window.open("https://wa.me/?text=" + encodeURIComponent(text + " " + shareUrl), "_blank", "noopener,noreferrer");
                return { ok: true };
            }

            if (channel === "instagram") {
                downloadBlob(blob);
                return { ok: true, hint: "instagram" };
            }

            if (typeof navigator.share === "function") {
                if (navigator.canShare && navigator.canShare({ files: [file] })) {
                    await navigator.share({ files: [file], text, url: shareUrl });
                    return { ok: true };
                }
                await navigator.share({ text: text + " " + shareUrl, url: shareUrl });
                return { ok: true };
            }

            if (channel === "copy" || channel === "generic") {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    await navigator.clipboard.writeText(shareUrl);
                    return { ok: true, copied: true };
                }
            }

            downloadBlob(blob);
            return { ok: true };
        } catch (err) {
            if (err && err.name === "AbortError") {
                return { ok: false, aborted: true };
            }
            throw err;
        }
    }

    function createEmpty(consentVersion) {
        const now = new Date();
        const expires = new Date(now.getTime() + RETENTION_DAYS * 86400000);
        return {
            v: SCHEMA_VERSION,
            createdAtUtc: now.toISOString(),
            expiresAtUtc: expires.toISOString(),
            consent: { version: consentVersion, atUtc: now.toISOString() },
            ageBand: "16plus",
            answers: { competency: {}, career: {}, culture: {}, values: {} }
        };
    }

    function ensureLoaded() {
        return Promise.resolve();
    }

    var resultDotNet = null;
    var stickyObserver = null;
    var cookieObserver = null;
    var sheetTrapHandler = null;

    function measureCookieBanner() {
        var banner = document.querySelector(".cookie-consent");
        var root = document.documentElement;
        var id = "pub-cookie-banner-height-style";
        var el = document.getElementById(id);
        if (!banner || root.classList.contains("cookie-consent-known")) {
            if (el) el.remove();
            root.style.removeProperty("--pub-cookie-banner-height");
            return;
        }
        if (el) el.remove();
        var h = Math.ceil(banner.getBoundingClientRect().height || 0);
        if (h <= 0) {
            root.style.removeProperty("--pub-cookie-banner-height");
            return;
        }
        root.style.setProperty("--pub-cookie-banner-height", h + "px");
    }

    function bindCookiePadding() {
        measureCookieBanner();
        if (cookieObserver) return;
        cookieObserver = new MutationObserver(function () {
            measureCookieBanner();
        });
        cookieObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ["class"]
        });
        window.addEventListener("resize", measureCookieBanner, { passive: true });
        // Sticky must appear without reload once consent is known (D15).
        document.addEventListener("click", function (ev) {
            var btn = ev.target && ev.target.closest ? ev.target.closest("[data-consent], .cookie-consent .btn-compact") : null;
            if (!btn) return;
            setTimeout(measureCookieBanner, 50);
        }, true);
    }

    function unbindCookiePadding() {
        if (cookieObserver) {
            cookieObserver.disconnect();
            cookieObserver = null;
        }
        window.removeEventListener("resize", measureCookieBanner);
    }

    function bindResultChrome(dotNetRef) {
        resultDotNet = dotNetRef || null;
        bindCookiePadding();
        var card = document.querySelector("[data-gd-signup-inline]");
        var sticky = document.querySelector("[data-gd-sticky]");
        if (stickyObserver) {
            stickyObserver.disconnect();
            stickyObserver = null;
        }
        if (card && sticky && "IntersectionObserver" in window) {
            stickyObserver = new IntersectionObserver(function (entries) {
                var entry = entries && entries[0];
                var visible = !!(entry && entry.isIntersecting && entry.intersectionRatio > 0.35);
                if (sticky.classList) {
                    sticky.classList.toggle("is-card-visible", visible);
                }
                if (resultDotNet && resultDotNet.invokeMethodAsync) {
                    try { resultDotNet.invokeMethodAsync("SetStickyHiddenByCard", visible); } catch (e) { /* ignore */ }
                }
            }, { threshold: [0, 0.35, 0.6, 1] });
            stickyObserver.observe(card);
        }
    }

    function unbindResultChrome() {
        if (stickyObserver) {
            stickyObserver.disconnect();
            stickyObserver = null;
        }
        unbindCookiePadding();
        resultDotNet = null;
    }

    function focusTrap(dialog) {
        var focusables = dialog.querySelectorAll("a[href],button:not([disabled]),input,select,textarea,[tabindex]:not([tabindex='-1'])");
        if (!focusables.length) return;
        var first = focusables[0];
        var last = focusables[focusables.length - 1];
        sheetTrapHandler = function (ev) {
            if (ev.key === "Escape") {
                closeSheet();
                return;
            }
            if (ev.key !== "Tab") return;
            if (ev.shiftKey && document.activeElement === first) {
                ev.preventDefault();
                last.focus();
            } else if (!ev.shiftKey && document.activeElement === last) {
                ev.preventDefault();
                first.focus();
            }
        };
        dialog.addEventListener("keydown", sheetTrapHandler);
        first.focus();
    }

    function openSheet() {
        var dialog = document.querySelector("[data-gd-sheet]");
        if (!dialog) return;
        if (typeof dialog.showModal === "function") {
            if (!dialog.open) dialog.showModal();
        } else {
            dialog.setAttribute("open", "");
        }
        focusTrap(dialog);
    }

    function closeSheet() {
        var dialog = document.querySelector("[data-gd-sheet]");
        if (!dialog) return;
        if (sheetTrapHandler) {
            dialog.removeEventListener("keydown", sheetTrapHandler);
            sheetTrapHandler = null;
        }
        if (typeof dialog.close === "function") {
            dialog.close();
        } else {
            dialog.removeAttribute("open");
        }
    }

    function bindQuestionChrome() {
        bindCookiePadding();
    }

    function unbindQuestionChrome() {
        unbindCookiePadding();
    }

    function focusQuestionHeading() {
        var h = document.getElementById("gd-q-heading");
        if (h && typeof h.focus === "function") {
            try { h.focus({ preventScroll: true }); } catch (e) { h.focus(); }
        }
    }

    function openLangMenu() {
        var details = document.getElementById("pub-lang");
        if (details) {
            details.open = true;
            var summary = details.querySelector("summary");
            if (summary && typeof summary.focus === "function") summary.focus();
        }
    }

    return {
        load,
        save,
        clear,
        renderCard,
        share,
        createEmpty,
        ensureLoaded,
        STORAGE_KEY,
        bindResultChrome,
        unbindResultChrome,
        bindQuestionChrome,
        unbindQuestionChrome,
        focusQuestionHeading,
        openSheet,
        closeSheet,
        openLangMenu,
        measureCookieBanner
    };
})();

window.jobsyEnsureGratisDna = function () {
    if (window.jobsyGratisDna) {
        return window.jobsyGratisDna.ensureLoaded();
    }
    return new Promise(function (resolve, reject) {
        var existing = document.querySelector('script[data-gratis-dna]');
        if (existing) {
            existing.addEventListener("load", function () { resolve(); });
            existing.addEventListener("error", reject);
            return;
        }
        var s = document.createElement("script");
        s.src = "/js/gratis-dna.js?v=20261003-csp2";
        s.defer = true;
        s.dataset.gratisDna = "true";
        s.onload = function () { resolve(); };
        s.onerror = reject;
        document.head.appendChild(s);
    });
};
