/* app-extras.js — concatenated sessionIdle + download + richtext. Loaded on demand. */

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

    function sessionReturnUrl() {
        try {
            var path = window.location.pathname || "/";
            if (/^\/login\/?$/i.test(path)) {
                return "";
            }
            return path + (window.location.search || "");
        } catch (e) {
            return "";
        }
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
        var here = sessionReturnUrl();
        var target = "/account/logout?reason=session-expired";
        if (here) {
            target += "&returnUrl=" + encodeURIComponent(here);
        }
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
