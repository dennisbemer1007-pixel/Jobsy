// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    if (!window.Blazor || typeof Blazor.start !== "function") {
        try {
            console.error("[lobsy] Blazor is not defined — blazor-boot.js ran before blazor.web.js?");
        } catch (e) { }
        return;
    }

    var reloadKey = "lobsy-circuit-reload";
    var reconnectAttempts = 0;

    function modal() {
        return document.getElementById("components-reconnect-modal");
    }

    // Only intervene after Blazor's built-in loop has ended (failed).
    // Never call Blazor.reconnect() while "components-reconnect-show" is set —
    // Blazor already retries immediately on visibilitychange, and a parallel
    // reconnect() would dispose the loop and leave a false "failed" toast.
    function modalShowsReconnect() {
        var m = modal();
        if (!m || !m.classList) return false;
        return m.classList.contains("components-reconnect-failed");
    }

    async function tryReconnect(reason) {
        if (!modalShowsReconnect()) return;
        if (!window.Blazor || typeof Blazor.reconnect !== "function") return;
        reconnectAttempts += 1;
        try {
            console.info("[lobsy] Blazor.reconnect #" + reconnectAttempts + " (" + reason + ")");
        } catch (e) { }
        try {
            var ok = await Blazor.reconnect();
            if (ok === true) return; // Blazor hides the toast
            // Circuit gone — reload once, guarded against loops.
            try {
                if (sessionStorage.getItem(reloadKey) === "1") return;
                sessionStorage.setItem(reloadKey, "1");
            } catch (e) { }
            location.reload();
        } catch (e) { /* silent */ }
    }

    // Public shells (landing, legal, status) have no circuit draft worth keeping.
    // A filled text field means the visitor typed something — leave the reload button.
    function hasDraftInput() {
        var nodes = document.querySelectorAll("input, textarea");
        for (var i = 0; i < nodes.length; i++) {
            var el = nodes[i];
            if (!el || el.disabled) continue;
            var type = (el.type || "text").toLowerCase();
            if (type === "hidden" || type === "checkbox" || type === "radio"
                || type === "submit" || type === "button" || type === "search") continue;
            if ((el.value || "").trim().length > 0) return true;
        }
        return false;
    }

    function safePublicPage() {
        if (hasDraftInput()) return false;
        if (document.querySelector(".pub-theme, .err-layout")) return true;
        var path = (location.pathname || "/").toLowerCase();
        if (path.length > 1 && path.charAt(path.length - 1) === "/") path = path.slice(0, -1);
        return path === "/"
            || path === "/banenkaart"
            || path === "/ontdek"
            || path === "/login"
            || path === "/account-maken"
            || path === "/hoe-werkt-lobsy"
            || path === "/privacy"
            || path === "/algemene-voorwaarden"
            || path === "/gebruiksvoorwaarden"
            || path === "/wie-zijn-wij"
            || path === "/partner"
            || path === "/register"
            || path.indexOf("/status/") === 0;
    }

    // Rejected = the server dropped the circuit (deploy, retention). Reload is the
    // only recovery. Public pages may try again after 8s so a deploy burst does not
    // stick on "Je sessie is verlopen."; other pages reload once until the circuit
    // is healthy again.
    function onRejectedAutoReload() {
        var m = modal();
        if (!m || !m.classList || !m.classList.contains("components-reconnect-rejected")) return;
        if (hasDraftInput()) return;
        var now = Date.now();
        var safe = safePublicPage();
        try {
            var raw = sessionStorage.getItem(reloadKey);
            if (raw) {
                var at = parseInt(raw, 10);
                if (!safe || raw === "1" || isNaN(at) || now - at < 8000) return;
            }
            sessionStorage.setItem(reloadKey, String(now));
        } catch (e) { }
        location.reload();
    }

    function clearReloadGuardIfHealthy() {
        var m = modal();
        if (!m || !m.classList) return;
        if (m.classList.contains("components-reconnect-hide")
            || (!m.classList.contains("components-reconnect-show")
                && !m.classList.contains("components-reconnect-retrying")
                && !m.classList.contains("components-reconnect-failed")
                && !m.classList.contains("components-reconnect-rejected")
                && !m.classList.contains("components-reconnect-paused"))) {
            try { sessionStorage.removeItem(reloadKey); } catch (e) { }
        }
    }

    Blazor.start({
        circuit: {
            reconnectionOptions: {
                maxRetries: 60,
                retryIntervalMilliseconds: function (n) {
                    return n < 3 ? 500 : n < 10 ? 2000 : 5000;
                }
            }
        }
    }).then(function () {
        document.documentElement.setAttribute("data-lobsy-circuit", "ready");
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState !== "visible") return;
            onRejectedAutoReload();
            tryReconnect("visibility");
        });
        window.addEventListener("online", function () {
            onRejectedAutoReload();
            tryReconnect("online");
        });

        var m = modal();
        if (m) {
            // .NET 10 ReconnectModal dispatches this in addition to toggling the class.
            m.addEventListener("components-reconnect-state-changed", function () {
                onRejectedAutoReload();
                clearReloadGuardIfHealthy();
            });
            if (typeof MutationObserver === "function") {
                new MutationObserver(function () {
                    onRejectedAutoReload();
                    clearReloadGuardIfHealthy();
                }).observe(m, { attributes: true, attributeFilter: ["class"] });
            }
        }
    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
