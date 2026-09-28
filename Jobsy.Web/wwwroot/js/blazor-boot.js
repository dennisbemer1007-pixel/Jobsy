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

    function modalShowsReconnect() {
        var m = modal();
        if (!m || !m.classList) return false;
        return m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-failed");
    }

    function tryReconnect(reason) {
        if (!modalShowsReconnect()) return;
        if (!window.Blazor || typeof Blazor.reconnect !== "function") return;
        reconnectAttempts += 1;
        try {
            console.info("[lobsy] Blazor.reconnect #" + reconnectAttempts + " (" + reason + ")");
        } catch (e) { }
        Blazor.reconnect();
    }

    function onRejectedAutoReload() {
        var m = modal();
        if (!m || !m.classList || !m.classList.contains("components-reconnect-rejected")) return;
        try {
            if (sessionStorage.getItem(reloadKey) === "1") return;
            sessionStorage.setItem(reloadKey, "1");
        } catch (e) { }
        location.reload();
    }

    function clearReloadGuardIfHealthy() {
        var m = modal();
        if (!m || !m.classList) return;
        if (m.classList.contains("components-reconnect-hide")
            || (!m.classList.contains("components-reconnect-show")
                && !m.classList.contains("components-reconnect-failed")
                && !m.classList.contains("components-reconnect-rejected"))) {
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
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "visible") tryReconnect("visibility");
        });
        window.addEventListener("online", function () { tryReconnect("online"); });

        var m = modal();
        if (m && typeof MutationObserver === "function") {
            new MutationObserver(function () {
                onRejectedAutoReload();
                clearReloadGuardIfHealthy();
            }).observe(m, { attributes: true, attributeFilter: ["class"] });
        }
    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
