/**
 * Funda-style map bottom sheet: translate3d snaps, pointer drag on JS thread,
 * history back, reduced-motion.
 */
window.mapBottomSheet = (function () {
    "use strict";

    const sheets = Object.create(null);
    const COLLAPSED = 64;
    const CARD = 160;

    function prefersReducedMotion() {
        return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    }

    function viewportHeight() {
        return window.innerHeight || document.documentElement.clientHeight || 844;
    }

    function rootEl(id) {
        return document.getElementById(id);
    }

    function panelEl(id) {
        const root = rootEl(id);
        return root ? root.querySelector(".map-sheet__panel") : null;
    }

    function heightFor(snap, vh) {
        if (snap === "card") return CARD;
        if (snap === "half") return Math.max(CARD, vh * 0.5);
        if (snap === "full") return Math.max(CARD, vh * 0.92);
        return COLLAPSED;
    }

    function nearestSnap(h, vh) {
        const candidates = [
            ["collapsed", COLLAPSED],
            ["card", CARD],
            ["half", Math.max(CARD, vh * 0.5)],
            ["full", Math.max(CARD, vh * 0.92)]
        ];
        let best = candidates[0];
        let bestD = Math.abs(candidates[0][1] - h);
        for (let i = 1; i < candidates.length; i++) {
            const d = Math.abs(candidates[i][1] - h);
            if (d < bestD) {
                best = candidates[i];
                bestD = d;
            }
        }
        return best[0];
    }

    function setTranslate(id, y, animate) {
        const panel = panelEl(id);
        const root = rootEl(id);
        if (!panel) {
            return;
        }
        const motion = animate && !prefersReducedMotion();
        panel.style.transition = motion
            ? "transform 280ms cubic-bezier(0.22, 1, 0.36, 1)"
            : "none";
        const yy = Math.round(y);
        panel.style.transform = "translate3d(0, " + yy + "px, 0)";
        if (root) {
            root.style.setProperty("--map-sheet-y", yy + "px");
            root.dataset.sheetHeight = String(Math.max(0, viewportHeight() - yy));
        }
        const entry = sheets[id];
        if (entry) {
            entry.y = yy;
        }
    }

    function applySnap(id, snap, animate) {
        const vh = viewportHeight();
        const h = heightFor(snap, vh);
        const y = Math.max(0, vh - h);
        const root = rootEl(id);
        if (root) {
            root.dataset.snap = snap;
        }
        setTranslate(id, y, animate !== false);
        if (typeof window.jobMap !== "undefined" && window.jobMap && typeof window.jobMap.setSheetPadding === "function") {
            try { window.jobMap.setSheetPadding(h); } catch (e) { }
        }
        return h;
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
            if (!entry) return;
            const root = rootEl(id);
            const snap = (root && root.dataset.snap) || "collapsed";
            applySnap(id, snap, false);
            if (entry.dotNet) {
                try {
                    entry.dotNet.invokeMethodAsync("OnViewportResize", viewportHeight());
                } catch (e) { }
            }
        });
    }

    function bindPointer(id) {
        const panel = panelEl(id);
        const entry = sheets[id];
        if (!panel || !entry || entry.pointerBound) {
            return;
        }
        entry.pointerBound = true;
        let dragging = false;
        let startY = 0;
        let startTranslate = 0;
        let pointerId = null;

        panel.addEventListener("pointerdown", function (ev) {
            if (ev.pointerType === "mouse" && ev.button !== 0) {
                return;
            }
            // Let buttons/links/selects work; drag from handle/body chrome.
            const t = ev.target;
            if (t && t.closest && t.closest("a, button, input, select, textarea, .vac-compact, .map-sheet__cluster-row, .map-sheet__list-scroll")) {
                return;
            }
            dragging = true;
            pointerId = ev.pointerId;
            startY = ev.clientY;
            startTranslate = entry.y != null ? entry.y : (viewportHeight() - COLLAPSED);
            panel.style.transition = "none";
            try { panel.setPointerCapture(ev.pointerId); } catch (e) { }
        });

        panel.addEventListener("pointermove", function (ev) {
            if (!dragging || (pointerId != null && ev.pointerId !== pointerId)) {
                return;
            }
            const vh = viewportHeight();
            const dy = ev.clientY - startY;
            const minY = vh - heightFor("full", vh);
            const maxY = vh - COLLAPSED;
            const next = Math.min(maxY, Math.max(minY, startTranslate + dy));
            setTranslate(id, next, false);
            if (typeof window.jobMap !== "undefined" && window.jobMap && typeof window.jobMap.setSheetPadding === "function") {
                try { window.jobMap.setSheetPadding(vh - next); } catch (e) { }
            }
        });

        function endDrag(ev) {
            if (!dragging) {
                return;
            }
            if (pointerId != null && ev && ev.pointerId !== pointerId) {
                return;
            }
            dragging = false;
            pointerId = null;
            const vh = viewportHeight();
            const h = vh - (entry.y != null ? entry.y : vh - COLLAPSED);
            const snap = nearestSnap(h, vh);
            applySnap(id, snap, true);
            if (entry.dotNet) {
                try {
                    entry.dotNet.invokeMethodAsync("OnJsSnapChanged", snap);
                } catch (e) { }
            }
        }

        panel.addEventListener("pointerup", endDrag);
        panel.addEventListener("pointercancel", endDrag);
    }

    function bind(id, dotNetRef) {
        sheets[id] = sheets[id] || {};
        sheets[id].dotNet = dotNetRef;
        sheets[id].history = !!sheets[id].history;
        if (!window._mapSheetPopBound) {
            window._mapSheetPopBound = true;
            window.addEventListener("popstate", onPopState);
            window.addEventListener("resize", onResize);
        }
        // Start collapsed immediately (no flash over the map).
        applySnap(id, "collapsed", false);
        bindPointer(id);
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

    function listScrollTop(elId) {
        const el = document.getElementById(elId);
        return el ? Math.round(el.scrollTop || 0) : 0;
    }

    function setSnap(id, snap, animate) {
        return applySnap(id, snap || "collapsed", animate !== false);
    }

    return {
        bind: bind,
        unbind: unbind,
        setTranslate: setTranslate,
        setSnap: setSnap,
        applySnap: applySnap,
        viewportHeight: viewportHeight,
        pushHistory: pushHistory,
        clearHistory: clearHistory,
        prefersReducedMotion: prefersReducedMotion,
        listScrollTop: listScrollTop
    };
})();
