(function () {
    "use strict";

    var STORAGE_PREFIX = "lobsy.sessionDraft:";
    var DEFAULT_MINUTES = 30;
    var REFRESH_MS = 60 * 1000;
    var ACTIVITY_THROTTLE_MS = 5 * 1000;

    var timeoutMinutes = DEFAULT_MINUTES;
    var idleTimer = null;
    var refreshTimer = null;
    var lastActivityFlush = 0;
    var started = false;
    var expiring = false;
    var apiBaseUrl = "";

    function draftKey() {
        return STORAGE_PREFIX + (window.location.pathname || "/");
    }

    function saveCriticalDrafts() {
        try {
            var forms = document.querySelectorAll(
                "form[data-session-draft], .vacancy-editor form, form.login-form, .panel-page form.login-form"
            );
            if (!forms.length) {
                return;
            }

            var payload = { savedAt: Date.now(), fields: {} };
            forms.forEach(function (form, formIndex) {
                var fields = form.querySelectorAll("input, textarea, select");
                fields.forEach(function (el, fieldIndex) {
                    if (!el || el.disabled) {
                        return;
                    }
                    var type = (el.type || "").toLowerCase();
                    if (type === "password" || type === "file" || type === "hidden") {
                        return;
                    }
                    if (type === "checkbox" || type === "radio") {
                        if (!el.checked) {
                            return;
                        }
                    }
                    var key = el.name || el.id || ("f" + formIndex + "_" + fieldIndex);
                    if (!key) {
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

            Object.keys(payload.fields).forEach(function (key) {
                var item = payload.fields[key];
                if (!item) {
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
                if (!el) {
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
            // Lightweight beacon so server LastActivity cookie stays fresh during Blazor circuits.
            if (navigator.sendBeacon) {
                navigator.sendBeacon("/account/session-activity");
            } else {
                fetch("/account/session-activity", {
                    method: "POST",
                    credentials: "same-origin",
                    keepalive: true
                }).catch(function () { });
            }
        } catch (e) {
            // Ignore.
        }
    }

    function onIdle() {
        if (expiring) {
            return;
        }
        expiring = true;
        saveCriticalDrafts();
        var target = "/account/logout?reason=session-expired";
        try {
            window.location.href = target;
        } catch (e) {
            window.location.assign(target);
        }
    }

    function resolveTimeoutUrl() {
        var base = (apiBaseUrl || "").replace(/\/?$/, "/");
        if (!base) {
            return "/api/settings/session-security";
        }
        return base + "api/settings/session-security";
    }

    function refreshTimeout() {
        fetch(resolveTimeoutUrl(), { credentials: "omit" })
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
            .catch(function () { });
    }

    function bindActivity() {
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

    window.lobsySessionIdle = {
        start: function (options) {
            options = options || {};
            if (typeof options.timeoutMinutes === "number" && options.timeoutMinutes > 0) {
                timeoutMinutes = options.timeoutMinutes;
            }
            if (options.apiBaseUrl) {
                apiBaseUrl = String(options.apiBaseUrl);
            }
            if (started) {
                scheduleIdle();
                return;
            }
            started = true;
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
        saveDrafts: saveCriticalDrafts,
        restoreDrafts: restoreCriticalDrafts,
        markActivity: onActivity
    };
})();
