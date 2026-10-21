// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    try {
        performance.mark("jobsy-blazor-boot-start");
    } catch (e) { }

    var reconnectAttempts = 0;
    var rejectedAuthCheck = null;
    var silentRetryTimer = null;

    function allowDocumentReload() {
        window.__lobsyAllowDocumentReload = true;
    }

    function installReloadGuard() {
        if (window.__lobsyReloadGuardInstalled) {
            return;
        }
        window.__lobsyReloadGuardInstalled = true;
        window.lobsyAllowDocumentReload = allowDocumentReload;
        try {
            var proto = window.Location && window.Location.prototype;
            if (!proto || !proto.reload) {
                return;
            }
            var nativeReload = proto.reload;
            proto.reload = function () {
                if (!window.__lobsyAllowDocumentReload) {
                    try {
                        console.warn("[lobsy] blocked automatic document.reload()");
                    } catch (e) { }
                    return;
                }
                window.__lobsyAllowDocumentReload = false;
                return nativeReload.apply(this, arguments);
            };
        } catch (e) { }
    }

    installReloadGuard();

    /* Enhanced nav before Blazor.start can patch the URL without swapping the interactive router. */
    (function guardInternalNavUntilCircuitReady() {
        if (window.__lobsyEarlyNavGuard) {
            return;
        }
        window.__lobsyEarlyNavGuard = true;

        function circuitReady() {
            try {
                return document.documentElement.getAttribute("data-lobsy-circuit") === "ready";
            } catch (e) {
                return false;
            }
        }

        function isInternalNavAnchor(anchor) {
            if (!anchor || !anchor.getAttribute) {
                return false;
            }
            if (anchor.hasAttribute("download") || anchor.target === "_blank") {
                return false;
            }
            if (anchor.getAttribute("data-enhance-nav") === "false") {
                return false;
            }
            var href = anchor.getAttribute("href");
            if (!href || href.charAt(0) === "#") {
                return false;
            }
            try {
                var url = new URL(anchor.href, location.href);
                if (url.origin !== location.origin) {
                    return false;
                }
                return url.pathname !== location.pathname || url.search !== location.search || url.hash !== location.hash;
            } catch (e2) {
                return false;
            }
        }

        document.addEventListener(
            "click",
            function (ev) {
                if (circuitReady()) {
                    return;
                }
                var anchor = ev.target && ev.target.closest ? ev.target.closest("a[href]") : null;
                if (!isInternalNavAnchor(anchor)) {
                    return;
                }
                ev.preventDefault();
                ev.stopImmediatePropagation();
                window.location.assign(anchor.href);
            },
            true
        );
    })();

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
                scheduleSilentRetry();
                tryReconnect("rejected-circuit");
                return;
            }
            markSessionExpiredUi(current);
            syncReconnectReserve();
        });
    }

    function modalShowsReconnect() {
        var m = modal();
        if (!m || !m.classList) return false;
        return m.classList.contains("components-reconnect-failed");
    }

    async function tryReconnect(reason) {
        if (!modalShowsReconnect()) return false;
        if (!window.Blazor || typeof Blazor.reconnect !== "function") return false;
        reconnectAttempts += 1;
        try {
            console.info("[lobsy] Blazor.reconnect #" + reconnectAttempts + " (" + reason + ")");
        } catch (e) { }
        try {
            var ok = await Blazor.reconnect();
            if (ok === true) {
                stopSilentRetry();
                syncReconnectReserve();
                return true;
            }
        } catch (e) { /* silent */ }
        return false;
    }

    function scheduleSilentRetry() {
        if (silentRetryTimer) {
            return;
        }
        silentRetryTimer = window.setInterval(function () {
            var m = modal();
            if (!m || !m.classList.contains("components-reconnect-failed")) {
                stopSilentRetry();
                return;
            }
            tryReconnect("silent-interval");
        }, 5000);
    }

    function stopSilentRetry() {
        if (silentRetryTimer) {
            window.clearInterval(silentRetryTimer);
            silentRetryTimer = null;
        }
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
            } else if (m.classList.contains("components-reconnect-failed")) {
                scheduleSilentRetry();
            } else {
                m.classList.remove("reconnect-toast--auth-pending");
                m.classList.remove("reconnect-toast--session-expired");
                stopSilentRetry();
            }
        });
        obs.observe(m, { attributes: true, attributeFilter: ["class"] });
    }

    function onResume() {
        if (document.visibilityState !== "visible") {
            return;
        }
        tryReconnect("visibility");
        var m = modal();
        if (m && m.classList.contains("components-reconnect-rejected")) {
            handleRejectedState();
        }
    }

    async function manualReconnect() {
        var ok = await tryReconnect("manual");
        if (ok) {
            return;
        }
        if (window.Blazor && typeof Blazor.reconnect === "function") {
            await tryReconnect("manual-retry");
        }
    }

    function goLoginAfterSessionExpired() {
        var path = "";
        try {
            path = window.location.pathname + window.location.search;
        } catch (e) { }
        var target = "/login?error=session-expired";
        if (path && path !== "/login") {
            target += "&returnUrl=" + encodeURIComponent(path);
        }
        window.location.assign(target);
    }

    function bindReconnectUi() {
        if (document.__lobsyReconnectUiBound) {
            return;
        }
        document.__lobsyReconnectUiBound = true;
        document.addEventListener("click", function (ev) {
            var t = ev.target;
            if (!t || !t.closest) return;
            if (t.closest("[data-reconnect-try]")) {
                ev.preventDefault();
                manualReconnect();
                return;
            }
            if (t.closest("[data-reconnect-login]")) {
                ev.preventDefault();
                goLoginAfterSessionExpired();
            }
        });
    }

    window.lobsyReconnect = {
        tryReconnect: tryReconnect,
        manualReconnect: manualReconnect,
        allowDocumentReload: allowDocumentReload
    };

    bindReconnectUi();
    watchReconnectModal();

    document.addEventListener("components-reconnect-state-changed", function (ev) {
        var state = ev && ev.detail ? ev.detail.state : "";
        if (state === "rejected") {
            handleRejectedState();
            scheduleSilentRetry();
            tryReconnect("state-rejected");
        } else if (state === "failed" || state === "resume-failed") {
            scheduleSilentRetry();
            tryReconnect("state-failed");
        }
    });

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
        document.addEventListener("visibilitychange", onResume);
        window.addEventListener("online", function () {
            onResume();
        });

    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
