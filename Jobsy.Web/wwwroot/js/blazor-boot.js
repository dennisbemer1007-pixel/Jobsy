// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    try {
        performance.mark("jobsy-blazor-boot-start");
    } catch (e) { }

    var reconnectAttempts = 0;
    var rejectedAuthCheck = null;
    var silentRetryTimer = null;
    var silentRetryCount = 0;
    var RECONNECT_RELOAD_AFTER = 6;
    var RELOAD_THROTTLE_MS = 30000;
    var tryingSinceMs = 0;
    var reconnectMutating = false;
    var reconnectObsFrame = 0;

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

    function reconnectModalVisible() {
        return modalIsVisible(modal());
    }

    window.lobsyCircuit = {
        isReadyForAction: function () {
            try {
                var html = document.documentElement;
                if (!html || html.getAttribute("data-lobsy-circuit") !== "ready") {
                    return false;
                }
                if (html.hasAttribute("data-lobsy-reconnect-active")) {
                    return false;
                }
                if (reconnectModalVisible()) {
                    return false;
                }
                return true;
            } catch (e) {
                return false;
            }
        },
        reloadPageThrottled: reloadPageThrottled,
        reloadPageOnce: function (reason) {
            reloadPageThrottled(reason || "default");
        },
        recoverAfterSuspend: function (source) {
            return recoverAfterSuspend(source || "api");
        },
        demoteStuckTrying: demoteInvisibleTryingToFailed
    };

    function reloadPageThrottled(reason) {
        var key = "lobsy:reload-ts";
        try {
            var now = Date.now();
            var last = parseInt(sessionStorage.getItem(key) || "0", 10);
            if (last && now - last < RELOAD_THROTTLE_MS) {
                try {
                    console.info("[lobsy] reload throttled (" + reason + ")");
                } catch (e) { }
                return false;
            }
            sessionStorage.setItem(key, String(now));
        } catch (e2) { }
        try {
            console.info("[lobsy] reload (" + reason + ")");
        } catch (e3) { }
        allowDocumentReload();
        window.location.reload();
        return true;
    }

    function modalNeedsRecovery(m) {
        if (!m || !m.classList) {
            return false;
        }
        return (
            m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-retrying")
            || m.classList.contains("components-reconnect-failed")
            || m.classList.contains("components-reconnect-rejected")
            || m.classList.contains("components-reconnect-paused")
        );
    }

    function demoteInvisibleTryingToFailed() {
        var m = modal();
        if (!m || !m.classList) {
            return;
        }
        if (
            m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-retrying")
        ) {
            m.classList.remove("components-reconnect-show", "components-reconnect-retrying");
            m.classList.add("components-reconnect-failed");
            syncReconnectReserve();
        }
    }

    function noteTryingState() {
        var m = modal();
        if (!m || !m.classList) {
            tryingSinceMs = 0;
            return;
        }
        if (
            m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-retrying")
        ) {
            if (!tryingSinceMs) {
                tryingSinceMs = Date.now();
            }
            if (Date.now() - tryingSinceMs > 8000) {
                demoteInvisibleTryingToFailed();
                tryingSinceMs = 0;
            }
            return;
        }
        tryingSinceMs = 0;
    }

    function circuitInteractive() {
        try {
            return document.documentElement.getAttribute("data-lobsy-circuit") === "ready"
                && !document.documentElement.hasAttribute("data-lobsy-reconnect-active");
        } catch (e) {
            return false;
        }
    }

    async function recoverAfterSuspend(source) {
        if (document.visibilityState !== "visible") {
            return;
        }
        noteTryingState();
        var m = modal();
        if (m && modalNeedsRecovery(m)) {
            var ok = await tryReconnect("suspend-" + source);
            if (ok) {
                return;
            }
        }
        if (m && m.classList.contains("components-reconnect-rejected")) {
            handleRejectedState();
            return;
        }
        if (!circuitInteractive()) {
            reloadPageThrottled("mobile-suspend-" + source);
        }
    }

    window.lobsyCandidateNav = window.lobsyCandidateNav || {
        markInteractive: function () {
            try {
                document.documentElement.setAttribute("data-lobsy-candidate-nav-interactive", "true");
            } catch (e) { }
        },
        applyDocumentTitle: function (title) {
            if (!title) {
                return;
            }
            try {
                document.title = title;
                var el = document.querySelector("title");
                if (el) {
                    el.textContent = title;
                }
            } catch (e) { }
        },
        applyDocumentTitleFromAnchor: function (anchor) {
            if (!anchor || !anchor.getAttribute) {
                return;
            }
            this.applyDocumentTitle(anchor.getAttribute("data-lobsy-document-title"));
        },
        applyDocumentTitleForPath: function (path) {
            var target = this.normalizePath(path || location.pathname);
            var links = this.visibleNavLinks();
            for (var i = 0; i < links.length; i++) {
                if (this.normalizePath(links[i].getAttribute("href") || "") === target) {
                    this.applyDocumentTitleFromAnchor(links[i]);
                    return;
                }
            }
        },
        normalizePath: function (urlOrPath) {
            try {
                var u =
                    typeof urlOrPath === "string" && urlOrPath.indexOf("http") === 0
                        ? new URL(urlOrPath)
                        : new URL(String(urlOrPath || ""), location.href);
                var path = u.pathname || "/";
                return path.length > 1 && path.charAt(path.length - 1) === "/"
                    ? path.slice(0, -1)
                    : path;
            } catch (e) {
                var s = String(urlOrPath || "/").split("?")[0].split("#")[0];
                if (s.length > 1 && s.charAt(s.length - 1) === "/") {
                    s = s.slice(0, -1);
                }
                return s || "/";
            }
        },
        contentMatchesPath: function (path) {
            var p = this.normalizePath(path);
            var routes = [
                ["/candidate/ontdekkingsreis", [".journey-overview"]],
                ["/candidate/paspoort", [".passport-page"]],
                ["/banenkaart", [".jobsy-discovery", ".bk-page"]],
                ["/candidate/applications", [".apps-page", ".kb-apps"]],
                ["/carriere", [".career-page", ".career-card"]],
            ];
            for (var i = 0; i < routes.length; i++) {
                if (this.normalizePath(routes[i][0]) !== p) {
                    continue;
                }
                var probes = routes[i][1];
                for (var j = 0; j < probes.length; j++) {
                    if (document.querySelector(probes[j])) {
                        return true;
                    }
                }
                return false;
            }
            return true;
        },
        visibleNavLinks: function () {
            var links = document.querySelectorAll('[data-lobsy-candidate-nav="true"]');
            var visible = [];
            for (var i = 0; i < links.length; i++) {
                var el = links[i];
                if (el.offsetParent !== null || el.getClientRects().length > 0) {
                    visible.push(el);
                }
            }
            return visible;
        },
        navActiveMatchesPath: function (path) {
            var p = this.normalizePath(path);
            var actives = [];
            var visible = this.visibleNavLinks();
            for (var i = 0; i < visible.length; i++) {
                var el = visible[i];
                if (
                    el.classList.contains("is-active")
                    || el.getAttribute("aria-current") === "page"
                ) {
                    actives.push(el);
                }
            }
            if (actives.length !== 1) {
                return false;
            }
            return this.normalizePath(actives[0].getAttribute("href") || "") === p;
        },
        inSync: function (path) {
            var target = path || location.pathname;
            return this.navActiveMatchesPath(target) && this.contentMatchesPath(target);
        },
        commitPrimaryNav: function (href, title) {
            var self = this;
            var expected = self.normalizePath(href);
            if (title) {
                self.applyDocumentTitle(title);
            }
            function ensureHistory() {
                if (self.normalizePath(location.pathname) === expected) {
                    self.scheduleSyncCheck(href);
                    return;
                }
                try {
                    console.warn("[lobsy] nav URL lag — assigning", expected);
                } catch (e) { }
                allowDocumentReload();
                try {
                    var url = typeof href === "string" && href.indexOf("http") === 0
                        ? href
                        : expected + location.search + location.hash;
                    window.location.assign(url);
                } catch (e2) {
                    window.location.assign(expected);
                }
            }
            requestAnimationFrame(function () {
                requestAnimationFrame(ensureHistory);
            });
        },
        scheduleSyncCheck: function (expectedHref) {
            var self = this;
            var expected = self.normalizePath(expectedHref);
            var deadline = Date.now() + 1500;
            function tick() {
                var path = self.normalizePath(location.pathname);
                if (path !== expected) {
                    if (Date.now() >= deadline) {
                        try {
                            console.warn("[lobsy] candidate nav URL stuck — assigning", expected);
                        } catch (e) { }
                        allowDocumentReload();
                        window.location.assign(expected + location.search + location.hash);
                    } else {
                        requestAnimationFrame(tick);
                    }
                    return;
                }
                if (self.inSync(expected)) {
                    return;
                }
                if (Date.now() >= deadline) {
                    try {
                        console.warn("[lobsy] candidate nav desync — reloading", expected);
                    } catch (e) { }
                    allowDocumentReload();
                    location.reload();
                    return;
                }
                requestAnimationFrame(tick);
            }
            requestAnimationFrame(tick);
        },
    };

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

        /* Primary candidate nav: no enhanced-nav URL patch without interactive @onclick. */
        document.addEventListener(
            "click",
            function (ev) {
                if (!circuitReady()) {
                    return;
                }
                var anchor = ev.target && ev.target.closest
                    ? ev.target.closest("[data-lobsy-candidate-nav=\"true\"]")
                    : null;
                if (!anchor) {
                    return;
                }
                var href = anchor.href;
                var navInteractive =
                    document.documentElement.getAttribute("data-lobsy-candidate-nav-interactive") ===
                    "true";
                if (!navInteractive) {
                    ev.preventDefault();
                    ev.stopImmediatePropagation();
                    allowDocumentReload();
                    window.location.assign(href);
                    return;
                }
                ev.preventDefault();
                try {
                    var title = anchor.getAttribute("data-lobsy-document-title");
                    window.lobsyCandidateNav.commitPrimaryNav(href, title);
                } catch (eNav) { }
            },
            true
        );

        document.addEventListener("enhancedload", function () {
            if (!circuitReady()) {
                return;
            }
            try {
                window.lobsyCandidateNav.applyDocumentTitleForPath(location.pathname);
                window.lobsyCandidateNav.scheduleSyncCheck(location.pathname);
            } catch (eLoad) { }
        });
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
        return modalNeedsRecovery(modal());
    }

    async function tryReconnect(reason) {
        if (!window.Blazor || typeof Blazor.reconnect !== "function") return false;
        reconnectAttempts += 1;
        try {
            console.info("[lobsy] Blazor.reconnect #" + reconnectAttempts + " (" + reason + ")");
        } catch (e) { }
        try {
            var ok = await Blazor.reconnect();
            if (ok === true) {
                silentRetryCount = 0;
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
                if (!m || !m.classList.contains("components-reconnect-rejected")) {
                    stopSilentRetry();
                    return;
                }
            }
            silentRetryCount += 1;
            if (silentRetryCount >= RECONNECT_RELOAD_AFTER) {
                stopSilentRetry();
                try {
                    console.info("[lobsy] reconnect exhausted — reload once");
                } catch (e) { }
                window.lobsyCircuit.reloadPageOnce("reconnect-exhausted");
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

    function revealHiddenReconnectModal() {
        var m = modal();
        if (!m || !m.classList) {
            return;
        }
        if (m.classList.contains("components-reconnect-hide")) {
            m.classList.remove("components-reconnect-hide");
        }
    }

    function handleReconnectModalState() {
        var m = modal();
        if (!m || !m.classList) {
            return;
        }
        revealHiddenReconnectModal();
        if (m.classList.contains("components-reconnect-rejected")) {
            handleRejectedState();
        } else if (m.classList.contains("components-reconnect-failed")) {
            scheduleSilentRetry();
        } else if (
            m.classList.contains("components-reconnect-show")
            || m.classList.contains("components-reconnect-retrying")
        ) {
            scheduleSilentRetry();
        } else {
            m.classList.remove("reconnect-toast--auth-pending");
            m.classList.remove("reconnect-toast--session-expired");
            stopSilentRetry();
        }
    }

    function scheduleReconnectModalState() {
        if (reconnectObsFrame) {
            return;
        }
        reconnectObsFrame = requestAnimationFrame(function () {
            reconnectObsFrame = 0;
            if (reconnectMutating) {
                scheduleReconnectModalState();
                return;
            }
            reconnectMutating = true;
            try {
                noteTryingState();
                syncReconnectReserve();
                handleReconnectModalState();
            } finally {
                reconnectMutating = false;
            }
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
            if (reconnectMutating) {
                return;
            }
            scheduleReconnectModalState();
        });
        obs.observe(m, { attributes: true, attributeFilter: ["class"] });
    }

    function onResume() {
        recoverAfterSuspend("visibility");
    }

    function onPageShow(ev) {
        if (ev && ev.persisted) {
            recoverAfterSuspend("bfcache");
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
                return;
            }
            if (t.closest("[data-reconnect-reload]")) {
                ev.preventDefault();
                stopSilentRetry();
                window.lobsyCircuit.reloadPageOnce("reconnect-manual");
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
        window.addEventListener("pageshow", onPageShow);
        window.addEventListener("online", function () {
            onResume();
        });
        window.setInterval(function () {
            noteTryingState();
            revealHiddenReconnectModal();
        }, 2000);

    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
