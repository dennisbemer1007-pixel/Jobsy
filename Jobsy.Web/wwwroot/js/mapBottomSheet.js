/**
 * Funda-style map bottom sheet: translate3d snaps, history back, reduced-motion.
 */
window.mapBottomSheet = (function () {
    "use strict";

    const sheets = Object.create(null);

    function prefersReducedMotion() {
        return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    }

    function panelEl(id) {
        const root = document.getElementById(id);
        return root ? root.querySelector(".map-sheet__panel") : null;
    }

    function setTranslate(id, y, animate) {
        const panel = panelEl(id);
        if (!panel) {
            return;
        }
        const motion = animate && !prefersReducedMotion();
        panel.style.transition = motion
            ? "transform 280ms cubic-bezier(0.22, 1, 0.36, 1)"
            : "none";
        panel.style.transform = "translate3d(0, " + Math.round(y) + "px, 0)";
        const root = document.getElementById(id);
        if (root) {
            root.style.setProperty("--map-sheet-y", Math.round(y) + "px");
            root.dataset.sheetHeight = String(Math.max(0, window.innerHeight - y));
        }
    }

    function viewportHeight() {
        return window.innerHeight || document.documentElement.clientHeight || 844;
    }

    function onPopState(ev) {
        const state = ev.state;
        if (!state || !state.mapSheetId) {
            return;
        }
        const entry = sheets[state.mapSheetId];
        if (entry && entry.dotNet) {
            try {
                entry.dotNet.invokeMethodAsync("OnBrowserBack");
            } catch (e) { }
        }
    }

    function onResize() {
        Object.keys(sheets).forEach(function (id) {
            const entry = sheets[id];
            if (entry && entry.dotNet) {
                try {
                    entry.dotNet.invokeMethodAsync("OnViewportResize", viewportHeight());
                } catch (e) { }
            }
        });
    }

    function bind(id, dotNetRef) {
        sheets[id] = { dotNet: dotNetRef, history: false };
        if (!window._mapSheetPopBound) {
            window._mapSheetPopBound = true;
            window.addEventListener("popstate", onPopState);
            window.addEventListener("resize", onResize);
        }
    }

    function unbind(id) {
        delete sheets[id];
    }

    function pushHistory(id) {
        const entry = sheets[id];
        if (!entry || entry.history) {
            return;
        }
        try {
            history.pushState({ mapSheetId: id }, "");
            entry.history = true;
        } catch (e) { }
    }

    function clearHistory(id) {
        const entry = sheets[id];
        if (!entry) {
            return;
        }
        entry.history = false;
    }

    function listScrollTop(id) {
        const el = document.getElementById(id);
        return el ? Math.round(el.scrollTop || 0) : 0;
    }

    return {
        bind: bind,
        unbind: unbind,
        setTranslate: setTranslate,
        viewportHeight: viewportHeight,
        pushHistory: pushHistory,
        clearHistory: clearHistory,
        prefersReducedMotion: prefersReducedMotion,
        listScrollTop: listScrollTop
    };
})();
