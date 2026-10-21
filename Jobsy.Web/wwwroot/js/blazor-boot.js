// Manual Blazor.start after blazor.web.js (autostart=false).
// Must load with defer directly AFTER blazor.web.js so execution order is guaranteed.
(function () {
    try {
        performance.mark("jobsy-blazor-boot-start");
    } catch (e) { }

    var reconnectAttempts = 0;
    var rejectedAuthCheck = null;
    var silentRetryTimer = null;
    var activeReconnectionProcess = null;

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

    window.lobsyIsReconnectActive = function () {
        return modalIsVisible(modal());
    };

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

    async function tryReconnect(reason) {
        var m = modal();
        if (!m || !modalIsVisible(m)) return false;
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
            if (typeof Blazor.resumeCircuit === "function") {
                var resumed = await Blazor.resumeCircuit();
                if (resumed === true) {
                    stopSilentRetry();
                    syncReconnectReserve();
                    return true;
                }
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
            if (!m || !modalIsVisible(m)) {
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
                tryReconnect("display-show");
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
            },
            failed: function () {
                clearStates();
                dialogEl.classList.add("components-reconnect-failed");
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "failed");
                scheduleSilentRetry();
                tryReconnect("display-failed");
            },
            rejected: function () {
                // Never full-page reload — keep silent retry instead of framework hard refresh.
                clearStates();
                dialogEl.classList.add("components-reconnect-failed");
                syncReconnectReserve();
                dispatchReconnectState(dialogEl, "rejected");
                scheduleSilentRetry();
                tryReconnect("display-rejected");
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
                    delayMs = attempt === 0 && options.retryIntervalMilliseconds > 3000
                        ? 3000
                        : options.retryIntervalMilliseconds;
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
                    dialogEl = document.createElement("div");
                    dialogEl.id = options.dialogId || "components-reconnect-modal";
                    dialogEl.className = "reconnect-toast";
                    document.body.appendChild(dialogEl);
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
                }
            }
        };
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
            } else if (modalIsVisible(m)) {
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
        await tryReconnect("manual-retry");
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
        allowDocumentReload: allowDocumentReload,
        isActive: window.lobsyIsReconnectActive
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
        } else if (state === "show" || state === "retrying") {
            scheduleSilentRetry();
            tryReconnect("state-" + state);
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
        watchReconnectModal();
        document.addEventListener("visibilitychange", onResume);
        window.addEventListener("pageshow", function (ev) {
            if (ev && ev.persisted) {
                onResume();
            }
        });
        window.addEventListener("online", function () {
            onResume();
        });

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
        } catch (e) { }
    }).catch(function (err) {
        try { console.warn("[lobsy] Blazor.start failed", err); } catch (e) { }
    });
})();
