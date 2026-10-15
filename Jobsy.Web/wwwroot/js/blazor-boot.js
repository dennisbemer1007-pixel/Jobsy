// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    if (!window.Blazor || typeof Blazor.start !== "function") {
        try {
            console.error("[lobsy] Blazor is not defined — blazor-boot.js ran before blazor.web.js?");
        } catch (e) { }
        return;
    }

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
            // Circuit gone — never auto-reload during use; user uses the toast button.
        } catch (e) { /* silent */ }
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
            tryReconnect("visibility");
        });
        window.addEventListener("online", function () {
            tryReconnect("online");
        });

    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
