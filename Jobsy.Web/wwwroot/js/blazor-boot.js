// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    try {
        performance.mark("jobsy-blazor-boot-start");
    } catch (e) { }
    var reconnectAttempts = 0;
    var rejectedAuthCheck = null;

    function modal() {
        return document.getElementById("components-reconnect-modal");
    }

    function modalIsVisible(m) {
        if (!m || !m.classList) return false;
        return (
            m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-retrying")
            || m.classList.contains("components-reconnect-failed")
            || m.classList.contains("components-reconnect-rejected")
            || m.classList.contains("components-reconnect-paused")
        );
    }

    function syncReconnectReserve() {
        var m = modal();
        var root = document.documentElement;
        if (!root || !root.setAttribute) return;
        if (modalIsVisible(m)) {
            root.setAttribute("data-lobsy-reconnect-active", "");
        } else {
            root.removeAttribute("data-lobsy-reconnect-active");
        }
    }

    function verifySession() {
        return fetch("/account/session-activity", {
            method: "GET",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        }).then(function (r) {
            if (r.status === 401 || r.headers.get("X-Jobsy-Session") === "expired") {
                return false;
            }
            return r.ok;
        }).catch(function () {
            // Transient network blip during reconnect — do not claim session expiry.
            return true;
        });
    }

    function demoteRejectedToFailed(m) {
        if (!m || !m.classList) return;
        m.classList.remove("components-reconnect-rejected");
        m.classList.remove("reconnect-toast--session-expired");
        m.classList.add("components-reconnect-failed");
    }

    function markSessionExpiredUi(m) {
        if (!m || !m.classList) return;
        m.classList.add("reconnect-toast--session-expired");
    }

    function handleRejectedState() {
        var m = modal();
        if (!m || !m.classList.contains("components-reconnect-rejected")) {
            return;
        }
        if (rejectedAuthCheck) {
            return;
        }
        m.classList.add("reconnect-toast--auth-pending");
        rejectedAuthCheck = verifySession().then(function (sessionOk) {
            rejectedAuthCheck = null;
            var current = modal();
            if (!current || !current.classList.contains("components-reconnect-rejected")) {
                if (current) {
                    current.classList.remove("reconnect-toast--auth-pending");
                }
                syncReconnectReserve();
                return;
            }
            current.classList.remove("reconnect-toast--auth-pending");
            if (sessionOk) {
                demoteRejectedToFailed(current);
                syncReconnectReserve();
                tryReconnect("rejected-circuit");
                return;
            }
            markSessionExpiredUi(current);
            syncReconnectReserve();
        });
    }

    function watchReconnectModal() {
        var m = modal();
        if (!m || m.__lobsyReconnectObserved) {
            return;
        }
        m.__lobsyReconnectObserved = true;
        syncReconnectReserve();
        if (typeof MutationObserver === "undefined") {
            return;
        }
        var obs = new MutationObserver(function () {
            syncReconnectReserve();
            if (m.classList.contains("components-reconnect-rejected")) {
                handleRejectedState();
            } else {
                m.classList.remove("reconnect-toast--auth-pending");
                m.classList.remove("reconnect-toast--session-expired");
            }
        });
        obs.observe(m, { attributes: true, attributeFilter: ["class"] });
    }

    // Only intervene after Blazor's built-in loop has ended (failed), or after rejected is demoted to failed.
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
            if (ok === true) {
                syncReconnectReserve();
                return;
            }
            // Circuit gone — never auto-reload during use; user uses the toast button.
        } catch (e) { /* silent */ }
    }

    watchReconnectModal();

    if (!window.Blazor || typeof Blazor.start !== "function") {
        try {
            console.error("[lobsy] Blazor is not defined — blazor-boot.js ran before blazor.web.js?");
        } catch (e) { }
        return;
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
        try {
            performance.mark("jobsy-circuit-ready");
            performance.measure("jobsy-circuit", "jobsy-blazor-boot-start", "jobsy-circuit-ready");
        } catch (e) { }
        document.documentElement.setAttribute("data-lobsy-circuit", "ready");
        watchReconnectModal();
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
