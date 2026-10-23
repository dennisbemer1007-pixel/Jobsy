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
    var pendingReloadReason = null;
    var pendingAssignHref = null;
    var activeReconnectionProcess = null;
    var healthyDismissTimer = null;
    var reconnectFetchBackoffUntil = Object.create(null);
    var reconnectFetchFailStreak = Object.create(null);
    var reconnectInFlight = null;
    var onlineRecoveryTimer = null;

    function isBrowserOnline() {
        try {
            return typeof navigator === "undefined" || navigator.onLine !== false;
        } catch (e) {
            return true;
        }
    }

    function reconnectPendingOffline() {
        try {
            if (document.documentElement.hasAttribute("data-lobsy-reconnect-active")) {
                return true;
            }
            if (reconnectModalVisible()) {
                return true;
            }
        } catch (e) { }
        return false;
    }

    function shouldDeferDocumentNavigation() {
        return !isBrowserOnline() || reconnectPendingOffline();
    }

    function deferReloadUntilOnline(reason) {
        pendingReloadReason = reason || pendingReloadReason || "offline";
        try {
            console.info("[lobsy] reload deferred until online (" + pendingReloadReason + ")");
        } catch (e) { }
        return false;
    }

    function deferAssignUntilOnline(href, reason) {
        pendingAssignHref = href;
        try {
            console.info(
                "[lobsy] navigation deferred until online (" + (reason || "offline") + "): " + href
            );
        } catch (e) { }
        return false;
    }

    function assignWithServerRetry(href, attempt) {
        var maxAttempts = 3;
        var tryAssign = function () {
            allowDocumentReload();
            window.location.assign(href);
        };
        if (attempt >= maxAttempts) {
            tryAssign();
            return;
        }
        try {
            fetch(href, { method: "HEAD", credentials: "same-origin", cache: "no-store" })
                .then(function (r) {
                    if (r.status >= 500 && r.status <= 599 && attempt + 1 < maxAttempts) {
                        window.setTimeout(function () {
                            assignWithServerRetry(href, attempt + 1);
                        }, 400 * Math.pow(2, attempt));
                        return;
                    }
                    tryAssign();
                })
                .catch(function () {
                    if (attempt + 1 < maxAttempts) {
                        window.setTimeout(function () {
                            assignWithServerRetry(href, attempt + 1);
                        }, 400 * Math.pow(2, attempt));
                        return;
                    }
                    tryAssign();
                });
        } catch (e) {
            tryAssign();
        }
    }

    function safeLocationAssign(href, reason) {
        if (shouldDeferDocumentNavigation()) {
            return deferAssignUntilOnline(href, reason);
        }
        assignWithServerRetry(href, 0);
        return true;
    }

    function flushPendingAfterOnline() {
        if (shouldDeferDocumentNavigation()) {
            return;
        }
        var pendingHref = pendingAssignHref;
        var pendingReload = pendingReloadReason;
        pendingAssignHref = null;
        pendingReloadReason = null;
        if (pendingHref) {
            assignWithServerRetry(pendingHref, 0);
            return;
        }
        if (pendingReload) {
            reloadPageThrottled(pendingReload);
        }
    }

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
    installReconnectFetchBackoff();

    function installReconnectFetchBackoff() {
        if (window.__lobsyReconnectFetchBackoff) {
            return;
        }
        window.__lobsyReconnectFetchBackoff = true;
        var nativeFetch = window.fetch.bind(window);
        window.fetch = function (input, init) {
            var url = "";
            try {
                url = String(typeof input === "string" ? input : (input && input.url) || "");
            } catch (e) { }
            var key = null;
            if (url.indexOf("/_blazor/negotiate") !== -1) {
                key = "negotiate";
            } else if (url.indexOf("session-security") !== -1) {
                key = "session-security";
            } else if (url.indexOf("session-activity") !== -1) {
                key = "session-activity";
            }
            if (key === "session-activity" || key === "session-security") {
                if (reconnectPendingOffline()) {
                    return new Promise(function (resolve) {
                        var deadline = Date.now() + 20000;
                        (function waitForCircuit() {
                            if (!reconnectPendingOffline() || Date.now() >= deadline) {
                                resolve(nativeFetch(input, init));
                                return;
                            }
                            window.setTimeout(waitForCircuit, 250);
                        })();
                    });
                }
            }
            if (!key) {
                return nativeFetch(input, init);
            }
            var now = Date.now();
            var waitUntil = reconnectFetchBackoffUntil[key] || 0;
            var run = function () {
                return nativeFetch(input, init).then(function (r) {
                    if (r.status >= 500 && r.status <= 599) {
                        var streak = (reconnectFetchFailStreak[key] || 0) + 1;
                        reconnectFetchFailStreak[key] = streak;
                        reconnectFetchBackoffUntil[key] = Date.now() + Math.min(8000, 750 * streak);
                    } else if (r.ok) {
                        reconnectFetchFailStreak[key] = 0;
                    }
                    return r;
                });
            };
            if (now < waitUntil) {
                return new Promise(function (resolve) {
                    window.setTimeout(function () {
                        resolve(run());
                    }, waitUntil - now);
                });
            }
            return run();
        };
    }

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
        if (shouldDeferDocumentNavigation()) {
            return deferReloadUntilOnline(reason);
        }
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
        if (!circuitInteractive() && isBrowserOnline() && !reconnectPendingOffline()) {
            reloadPageThrottled("mobile-suspend-" + source);
            return;
        }
        if (isBrowserOnline() && !circuitInteractive()) {
            tryReconnect("suspend-" + source);
        }
    }

    function syncAppHeaderHeightPx() {
        try {
            var header = document.querySelector(".app-header");
            if (!header) {
                return;
            }
            var px = Math.ceil(header.getBoundingClientRect().height);
            if (px > 0) {
                document.documentElement.style.setProperty("--app-header-height", px + "px");
            }
        } catch (e) { }
    }

    window.lobsyPaspoortLayout = window.lobsyPaspoortLayout || {
        syncSidebarSticky: function () {
            try {
                var sidebar = document.querySelector(
                    ".passport-page--illustrated .passport-layout__sidebar"
                );
                if (!sidebar) {
                    return;
                }
                if (window.innerWidth < 1024) {
                    sidebar.classList.remove("passport-layout__sidebar--sticky-when-fits");
                    return;
                }
                syncAppHeaderHeightPx();
                var header = document.querySelector(".app-header");
                var headerBottom = header ? header.getBoundingClientRect().bottom : 68;
                var available = window.innerHeight - headerBottom - 16;
                var fits = sidebar.scrollHeight <= available + 1;
                sidebar.classList.toggle("passport-layout__sidebar--sticky-when-fits", fits);
            } catch (e) { }
        },
    };

    window.lobsyCandidateNav = window.lobsyCandidateNav || {
        markInteractive: function () {
            try {
                document.documentElement.setAttribute("data-lobsy-candidate-nav-interactive", "true");
                syncAppHeaderHeightPx();
            } catch (e) { }
        },
        syncAppHeaderHeight: syncAppHeaderHeightPx,
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
                ["/candidate/ontdekkingsreis", [".journey-overview", "[data-discovery-ssr=\"1\"]"]],
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
        syncActiveClassesFromLocation: function (path) {
            var p = this.normalizePath(path || location.pathname);
            var links = document.querySelectorAll('[data-lobsy-candidate-nav="true"]');
            for (var i = 0; i < links.length; i++) {
                var el = links[i];
                var match = this.normalizePath(el.getAttribute("href") || "") === p;
                el.classList.toggle("is-active", match);
                if (match) {
                    el.setAttribute("aria-current", "page");
                } else {
                    el.removeAttribute("aria-current");
                }
            }
        },
        navActiveMatchesPath: function (path) {
            var p = this.normalizePath(path);
            var actives = [];
            var links = document.querySelectorAll('[data-lobsy-candidate-nav="true"]');
            for (var i = 0; i < links.length; i++) {
                var el = links[i];
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
                try {
                    var url = typeof href === "string" && href.indexOf("http") === 0
                        ? href
                        : expected + location.search + location.hash;
                    safeLocationAssign(url, "nav-url-lag");
                } catch (e2) {
                    safeLocationAssign(expected, "nav-url-lag");
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
                        safeLocationAssign(
                            expected + location.search + location.hash,
                            "nav-url-stuck"
                        );
                    } else {
                        requestAnimationFrame(tick);
                    }
                    return;
                }
                if (self.inSync(expected)) {
                    self.applyDocumentTitleForPath(expected);
                    return;
                }
                if (Date.now() >= deadline) {
                    if (shouldDeferDocumentNavigation()) {
                        deferReloadUntilOnline("nav-desync-" + expected);
                        return;
                    }
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
                safeLocationAssign(anchor.href, "pre-circuit-nav");
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
                if (navInteractive) {
                    return;
                }
                ev.preventDefault();
                ev.stopImmediatePropagation();
                safeLocationAssign(href, "nav-not-interactive");
            },
            true
        );

        document.addEventListener("enhancedload", function () {
            if (!circuitReady()) {
                return;
            }
            try {
                window.lobsyCandidateNav.syncActiveClassesFromLocation(location.pathname);
                window.lobsyCandidateNav.applyDocumentTitleForPath(location.pathname);
                window.lobsyCandidateNav.scheduleSyncCheck(location.pathname);
                window.lobsyPaspoortLayout.syncSidebarSticky();
                syncAppHeaderHeightPx();
            } catch (eLoad) { }
        });

        window.addEventListener("resize", function () {
            try {
                syncAppHeaderHeightPx();
                window.lobsyPaspoortLayout.syncSidebarSticky();
                window.lobsyCandidateNav.syncActiveClassesFromLocation(location.pathname);
            } catch (eResize) { }
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

    function dismissReconnectToast(reason) {
        var m = modal();
        if (!m || !m.classList) {
            syncReconnectReserve();
            return;
        }
        m.classList.remove(
            "components-reconnect-show",
            "components-reconnect-retrying",
            "components-reconnect-failed",
            "components-reconnect-rejected",
            "components-reconnect-paused"
        );
        m.classList.add("components-reconnect-hide");
        m.classList.remove("reconnect-toast--auth-pending");
        m.classList.remove("reconnect-toast--session-expired");
        stopSilentRetry();
        tryingSinceMs = 0;
        syncReconnectReserve();
        try {
            if (reason) {
                console.info("[lobsy] reconnect toast dismissed (" + reason + ")");
            }
        } catch (e) { }
    }

    function onCircuitHealthyAfterReconnect() {
        dismissReconnectToast("circuit-healthy");
        if (healthyDismissTimer) {
            window.clearInterval(healthyDismissTimer);
            healthyDismissTimer = null;
        }
        try {
            if (document.querySelector('[data-testid="candidate-header-nav"]')) {
                window.lobsyCandidateNav.markInteractive();
            }
            window.lobsyCandidateNav.syncActiveClassesFromLocation(location.pathname);
        } catch (e) { }
    }

    function scheduleHealthyDismissPoll() {
        if (healthyDismissTimer) {
            return;
        }
        var started = Date.now();
        healthyDismissTimer = window.setInterval(function () {
            if (Date.now() - started > 5000) {
                window.clearInterval(healthyDismissTimer);
                healthyDismissTimer = null;
                return;
            }
            if (isBrowserOnline() && circuitInteractive()) {
                onCircuitHealthyAfterReconnect();
            }
        }, 250);
    }

    window.lobsyIsReconnectActive = function () {
        return modalIsVisible(modal());
    };

    function dispatchReconnectState(dialogEl, state, detail) {
        if (!dialogEl || !dialogEl.dispatchEvent) {
            return;
        }
        dialogEl.dispatchEvent(new CustomEvent("components-reconnect-state-changed", {
            detail: detail || { state: state }
        }));
    }

    function createReconnectDisplay(dialogEl) {
        function clearStates() {
            dialogEl.classList.remove(
                "components-reconnect-show",
                "components-reconnect-hide",
                "components-reconnect-retrying",
                "components-reconnect-failed",
                "components-reconnect-rejected",
                "components-reconnect-paused");
        }

        return {
            show: function (state) {
                clearStates();
                dialogEl.classList.add("components-reconnect-show");
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "show", state);
                scheduleSilentRetry();
            },
            update: function (state) {
                if (state && state.type === "pause") {
                    clearStates();
                    dialogEl.classList.add("components-reconnect-paused");
                } else {
                    dialogEl.classList.add("components-reconnect-show");
                    if (state && state.currentAttempt > 1 && state.secondsToNextAttempt > 0) {
                        dialogEl.classList.add("components-reconnect-retrying");
                    }
                }
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "retrying", state);
            },
            hide: function () {
                clearStates();
                dialogEl.classList.add("components-reconnect-hide");
                syncReconnectReserve();
                stopSilentRetry();
                dispatchReconnectState(dialogEl, "hide");
                onCircuitHealthyAfterReconnect();
            },
            failed: function () {
                clearStates();
                dialogEl.classList.add("components-reconnect-failed");
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "failed");
                scheduleSilentRetry();
            },
            rejected: function () {
                // Never full-page reload — keep silent retry instead of framework hard refresh.
                clearStates();
                dialogEl.classList.add("components-reconnect-failed");
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "rejected");
                scheduleSilentRetry();
            }
        };
    }

    function runReconnectTimer(totalMs, display, attempt) {
        return new Promise(function (resolve) {
            if (!totalMs || totalMs <= 0) {
                resolve();
                return;
            }
            var step = 1000;
            var remaining = totalMs;
            var tick = function () {
                display.update({
                    type: "reconnect",
                    currentAttempt: attempt + 1,
                    secondsToNextAttempt: Math.max(0, Math.ceil(remaining / 1000))
                });
                remaining -= step;
                if (remaining <= 0) {
                    resolve();
                    return;
                }
                window.setTimeout(tick, step);
            };
            tick();
        });
    }

    function startReconnectionLoop(options, display, graceful, remote) {
        var disposed = false;
        var maxRetries = options.maxRetries != null ? options.maxRetries : 60;

        display.show({ type: graceful ? "pause" : "reconnect", remote: remote });
        if (graceful) {
            display.update({ type: "pause", remote: remote });
            return {
                cancel: function () {
                    disposed = true;
                }
            };
        }

        (async function loop() {
            for (var attempt = 0; !disposed && attempt < maxRetries; attempt++) {
                var delayMs = 500;
                if (typeof options.retryIntervalMilliseconds === "function") {
                    delayMs = options.retryIntervalMilliseconds(attempt);
                    if (delayMs == null) {
                        break;
                    }
                } else if (typeof options.retryIntervalMilliseconds === "number") {
                    delayMs = options.retryIntervalMilliseconds;
                } else {
                    delayMs = attempt < 3 ? 500 : attempt < 10 ? 2000 : 5000;
                }

                await runReconnectTimer(delayMs, display, attempt);
                if (disposed) {
                    return;
                }

                if (await tryReconnect("loop-" + (attempt + 1))) {
                    display.hide();
                    return;
                }
            }
            if (!disposed) {
                display.failed();
            }
        })();

        return {
            cancel: function () {
                disposed = true;
            }
        };
    }

    function createLobsyReconnectionHandler() {
        var display = null;
        return {
            onConnectionDown: function (options, error, graceful, remote) {
                var dialogEl = document.getElementById(options.dialogId || "components-reconnect-modal");
                if (!dialogEl) {
                    return;
                }
                if (!display) {
                    display = createReconnectDisplay(dialogEl);
                }
                if (activeReconnectionProcess) {
                    return;
                }
                activeReconnectionProcess = startReconnectionLoop(options, display, !!graceful, !!remote);
            },
            onConnectionUp: function () {
                if (activeReconnectionProcess) {
                    activeReconnectionProcess.cancel();
                    activeReconnectionProcess = null;
                }
                if (display) {
                    display.hide();
                } else {
                    onCircuitHealthyAfterReconnect();
                }
                scheduleHealthyDismissPoll();
            }
        };
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
        if (!window.Blazor || typeof Blazor.reconnect !== "function") {
            return false;
        }
        if (reconnectInFlight) {
            return reconnectInFlight;
        }
        reconnectInFlight = (async function () {
            reconnectAttempts += 1;
            var quiet = String(reason || "").indexOf("silent") !== -1 || String(reason || "").indexOf("loop-") === 0;
            if (!quiet) {
                try {
                    console.info("[lobsy] Blazor.reconnect #" + reconnectAttempts + " (" + reason + ")");
                } catch (e) { }
            }
            try {
                var ok = await Blazor.reconnect();
                if (ok === true) {
                    silentRetryCount = 0;
                    stopSilentRetry();
                    onCircuitHealthyAfterReconnect();
                    return true;
                }
                if (typeof Blazor.resumeCircuit === "function") {
                    var resumed = await Blazor.resumeCircuit();
                    if (resumed === true) {
                        silentRetryCount = 0;
                        stopSilentRetry();
                        onCircuitHealthyAfterReconnect();
                        return true;
                    }
                }
            } catch (e) { /* silent */ }
            return false;
        })();
        try {
            return await reconnectInFlight;
        } finally {
            reconnectInFlight = null;
        }
    }

    function scheduleSilentRetry() {
        if (silentRetryTimer) {
            return;
        }
        silentRetryTimer = window.setInterval(function () {
            if (isBrowserOnline() && circuitInteractive()) {
                onCircuitHealthyAfterReconnect();
                stopSilentRetry();
                return;
            }
            var m = modal();
            if (!m || !m.classList.contains("components-reconnect-failed")) {
                if (!m || !m.classList.contains("components-reconnect-rejected")) {
                    stopSilentRetry();
                    return;
                }
            }
            if (!isBrowserOnline()) {
                return;
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
                if (circuitInteractive() && !modalNeedsRecovery(modal())) {
                    onCircuitHealthyAfterReconnect();
                }
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
        safeLocationAssign(target, "session-expired-login");
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
        allowDocumentReload: allowDocumentReload,
        isActive: window.lobsyIsReconnectActive
    };

    bindReconnectUi();
    watchReconnectModal();

    document.addEventListener("components-reconnect-state-changed", function (ev) {
        var state = ev && ev.detail ? ev.detail.state : "";
        if (state === "connected" || state === "connected-successfully") {
            onCircuitHealthyAfterReconnect();
            return;
        }
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
            reconnectionHandler: createLobsyReconnectionHandler(),
            reconnectionOptions: {
                dialogId: "components-reconnect-modal",
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
        try {
            syncAppHeaderHeightPx();
            if (document.querySelector('[data-testid="candidate-header-nav"]')) {
                window.lobsyCandidateNav.markInteractive();
            }
            window.lobsyPaspoortLayout.syncSidebarSticky();
        } catch (eReady) { }
        watchReconnectModal();
        document.addEventListener("visibilitychange", onResume);
        window.addEventListener("pageshow", onPageShow);
        window.addEventListener("online", function () {
            if (onlineRecoveryTimer) {
                window.clearTimeout(onlineRecoveryTimer);
            }
            var jitterMs = 200 + Math.floor(Math.random() * 400);
            onlineRecoveryTimer = window.setTimeout(function () {
                onlineRecoveryTimer = null;
                onResume();
                scheduleHealthyDismissPoll();
                tryReconnect("online").finally(function () {
                    flushPendingAfterOnline();
                    scheduleHealthyDismissPoll();
                });
            }, jitterMs);
        });
        window.setInterval(function () {
            noteTryingState();
            revealHiddenReconnectModal();
        }, 2000);

        try {
            if (window.Blazor._internal && window.Blazor._internal.navigationManager
                && typeof window.Blazor._internal.navigationManager.refresh === "function") {
                var nav = window.Blazor._internal.navigationManager;
                var nativeRefresh = nav.refresh.bind(nav);
                nav.refresh = function (force) {
                    if (!window.__lobsyAllowDocumentReload) {
                        try {
                            console.warn("[lobsy] blocked unprompted navigation refresh");
                        } catch (e) { }
                        return;
                    }
                    return nativeRefresh(force);
                };
            }
        } catch (eNavRefresh) { }

    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
