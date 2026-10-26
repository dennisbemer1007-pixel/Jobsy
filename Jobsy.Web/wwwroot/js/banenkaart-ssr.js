(function () {
    "use strict";

    function qs(sel, root) {
        return (root || document).querySelector(sel);
    }

    function qsa(sel, root) {
        return Array.prototype.slice.call((root || document).querySelectorAll(sel));
    }

    function readBootNode() {
        var node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return null;
        }
        try {
            return JSON.parse(node.textContent);
        } catch (e) {
            return null;
        }
    }

    var BK_UI_KEY = "jobsy.bk.ui.v2";
    var BK_UI_KEY_LEGACY = "jobsy.bk.ui.v1";
    var BK_VIEW_KEY = "jobsy.bk.view.v1";

    function readUiState() {
        try {
            var raw = sessionStorage.getItem(BK_UI_KEY);
            return raw ? JSON.parse(raw) : {};
        } catch (eState) {
            return {};
        }
    }

    function writeUiState(patch) {
        try {
            var next = readUiState();
            Object.keys(patch || {}).forEach(function (k) {
                next[k] = patch[k];
            });
            sessionStorage.setItem(BK_UI_KEY, JSON.stringify(next));
        } catch (eWrite) { }
    }

    function ensureBodyScrollUnlocked() {
        try {
            document.documentElement.classList.remove("pwa-sheet-open");
            document.body.style.removeProperty("overflow");
            document.body.style.removeProperty("position");
            document.body.style.removeProperty("top");
            document.body.style.removeProperty("width");
        } catch (eUnlock) { }
    }

    function syncTravelPresetFromSlider() {
        var slider = qs("[data-testid=kb-sheet-travel-slider]");
        if (!slider) {
            return;
        }
        var val = parseInt(slider.value, 10);
        qsa("[data-kb-travel-preset]").forEach(function (btn) {
            var preset = parseInt(btn.getAttribute("data-kb-travel-preset"), 10);
            btn.classList.toggle("is-selected", preset === val);
        });
    }

    function setFiltersOpen(open) {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        root.classList.toggle("filters-open", open);
        if (open) {
            syncTravelPresetFromSlider();
        }
        if (!open) {
            ensureBodyScrollUnlocked();
        }
        writeUiState({ filtersOpen: !!open });
        var sheet = qs("[data-testid=kb-filter-sheet]");
        var backdrop = qs("[data-testid=kb-filter-backdrop]");
        qsa("[data-testid=kb-filters-button]").forEach(function (btn) {
            btn.classList.toggle("is-active", open);
            btn.setAttribute("aria-expanded", open ? "true" : "false");
        });
        if (sheet) {
            sheet.setAttribute("aria-modal", open ? "true" : "false");
            if (open) {
                sheet.removeAttribute("inert");
                sheet.setAttribute("aria-hidden", "false");
            } else {
                sheet.setAttribute("inert", "");
                sheet.setAttribute("aria-hidden", "true");
            }
        }
        if (backdrop) {
            if (open) {
                backdrop.removeAttribute("inert");
                backdrop.setAttribute("aria-hidden", "false");
            } else {
                backdrop.setAttribute("inert", "");
                backdrop.setAttribute("aria-hidden", "true");
            }
        }
    }

    function bindFilters() {
        if (document.documentElement.dataset.banenkaartFilterDelegation === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartFilterDelegation = "1";
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            var btn = target.closest("[data-testid=kb-filters-button]");
            if (!btn) {
                return;
            }
            var root = qs("[data-banenkaart-ssr=\"1\"]");
            if (!root) {
                return;
            }
            ev.preventDefault();
            var open = !root.classList.contains("filters-open");
            setFiltersOpen(open);
        }, true);
        var close = qs("[data-testid=kb-filter-close]");
        if (close) {
            close.addEventListener("click", function () { setFiltersOpen(false); });
        }
        var backdrop = qs("[data-testid=kb-filter-backdrop]");
        if (backdrop) {
            backdrop.addEventListener("click", function () { setFiltersOpen(false); });
        }
        document.addEventListener("keydown", function (ev) {
            if (ev.key === "Escape") {
                setFiltersOpen(false);
            }
        });
        qsa("[data-kb-transport]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var mode = btn.getAttribute("data-kb-transport");
                var hidden = qs("#kb-filter-transport");
                if (hidden && mode) {
                    hidden.value = mode;
                }
                qsa("[data-kb-transport]").forEach(function (b) {
                    b.classList.toggle("is-selected", b === btn);
                });
            });
        });
        qsa("[data-kb-travel-preset]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var slider = qs("[data-testid=kb-sheet-travel-slider]");
                var val = btn.getAttribute("data-kb-travel-preset");
                if (slider && val) {
                    slider.value = val;
                    syncTravelPresetFromSlider();
                }
            });
        });
        var travelSlider = qs("[data-testid=kb-sheet-travel-slider]");
        if (travelSlider) {
            travelSlider.addEventListener("input", syncTravelPresetFromSlider);
            travelSlider.addEventListener("change", syncTravelPresetFromSlider);
            syncTravelPresetFromSlider();
        }
    }

    function bindKeywordFields() {
        if (document.documentElement.dataset.banenkaartKeywordSync === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartKeywordSync = "1";
        var inputs = qsa("[data-banenkaart-ssr=\"1\"] input[name=\"q\"][form=\"kb-filter-form\"]");
        function syncFrom(source) {
            var val = source.value;
            inputs.forEach(function (el) {
                if (el !== source) {
                    el.value = val;
                }
            });
        }
        inputs.forEach(function (el) {
            el.addEventListener("input", function () { syncFrom(el); });
        });
    }

    function bindListActions() {
        if (document.documentElement.dataset.banenkaartListActions === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartListActions = "1";
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            if (target.closest("[data-testid=kb-load-more]")) {
                ev.preventDefault();
                if (window.jobsyBanenkaartCatalog
                    && typeof window.jobsyBanenkaartCatalog.loadMoreListRows === "function") {
                    window.jobsyBanenkaartCatalog.loadMoreListRows();
                }
                return;
            }
            if (target.closest("[data-testid=kb-widen-filters]")) {
                ev.preventDefault();
                setFiltersOpen(true);
            }
        });
    }

    function bindMobileView() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        var fab = qs("[data-testid=kb-map-fab]");
        function setViewMode(onMap) {
            var listMode = !onMap;
            applyBanenkaartViewMode(root, listMode ? "list" : "map");
            writeUiState({ showMap: !!onMap, desktopList: isDesktopBanenkaart() && listMode });
            ensureBodyScrollUnlocked();
            if (onMap && window.jobMap && typeof window.jobMap.invalidate === "function") {
                requestAnimationFrame(function () {
                    window.jobMap.invalidate();
                });
            }
        }
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            if (!target.closest("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            if (isDesktopSplitBanenkaart()) {
                return;
            }
            if (target.closest("[data-testid=kb-view-kaart]")) {
                ev.preventDefault();
                setViewMode(true);
            } else if (target.closest("[data-testid=kb-view-lijst]")) {
                ev.preventDefault();
                setViewMode(false);
            }
        });
        if (fab) {
            fab.addEventListener("click", function () { setViewMode(true); });
        }
    }

    function bindStartPrompt() {
        var later = qs("[data-testid=kb-start-later]");
        if (later) {
            later.addEventListener("click", function () {
                var prompt = qs(".kb-start-prompt");
                if (prompt) {
                    prompt.remove();
                }
                var root = qs("[data-banenkaart-ssr=\"1\"]");
                if (root) {
                    root.classList.remove("kb-start-prompt-open");
                }
            });
        }
        var locate = qs("[data-testid=kb-start-locate]");
        if (locate && window.jobsyGeo && typeof window.jobsyGeo.requestLocation === "function") {
            locate.addEventListener("click", function () {
                window.jobsyGeo.requestLocation().catch(function () { });
            });
        }
    }

    function markMapReady() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (root) {
            root.dataset.banenkaartMapReady = "1";
        }
        if (window.jobMap && typeof window.jobMap.tryOpenVacancyFromQuery === "function") {
            window.jobMap.tryOpenVacancyFromQuery();
        }
    }

    function whenMapIdle(cb) {
        if (!window.jobMap || typeof window.jobMap.__testGetMap !== "function") {
            cb();
            return;
        }
        var waits = 0;
        function waitForInstance() {
            var m = window.jobMap.__testGetMap();
            if (!m || typeof m.once !== "function") {
                if (waits++ < 160) {
                    setTimeout(waitForInstance, 50);
                    return;
                }
                cb();
                return;
            }
            if (typeof m.isStyleLoaded === "function" && m.isStyleLoaded()) {
                m.once("idle", cb);
                return;
            }
            m.once("load", function () {
                m.once("idle", cb);
            });
        }
        waitForInstance();
    }

    function enhanceMap() {
        function watchMapReady() {
            if (window.jobMap && typeof window.jobMap.__testGetMap === "function") {
                whenMapIdle(markMapReady);
                return true;
            }
            return false;
        }
        if (!watchMapReady()) {
            var attempts = 0;
            var iv = setInterval(function () {
                if (watchMapReady() || ++attempts > 240) {
                    clearInterval(iv);
                }
            }, 50);
        }
    }

    function markFilterReady() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (root) {
            root.dataset.banenkaartSsrBound = "1";
        }
    }

    var GUEST_FILTER_KEY = "jobsy.bk.filters.guest.v1";

    function persistGuestFilters(form) {
        if (!form || !form.elements) {
            return;
        }
        try {
            var data = {};
            Array.prototype.forEach.call(form.elements, function (el) {
                if (!el.name || el.disabled) {
                    return;
                }
                if (el.type === "checkbox" || el.type === "radio") {
                    if (el.checked) {
                        data[el.name] = data[el.name] || [];
                        data[el.name].push(el.value);
                    }
                    return;
                }
                if (el.type === "submit" || el.type === "button") {
                    return;
                }
                data[el.name] = el.value;
            });
            localStorage.setItem(GUEST_FILTER_KEY, JSON.stringify(data));
        } catch (eGuest) { }
    }

    function bindGuestFilterPersistence() {
        document.addEventListener("submit", function (ev) {
            var form = ev.target;
            if (!form || !form.getAttribute) {
                return;
            }
            var action = form.getAttribute("action") || "";
            if (action.indexOf("/api/banenkaart-client/apply-filters") !== -1) {
                persistGuestFilters(form);
                writeUiState({ filtersOpen: false });
            }
            if (action.indexOf("/api/banenkaart-client/reset-filters") !== -1) {
                try { localStorage.removeItem(GUEST_FILTER_KEY); } catch (eReset) { }
            }
        }, true);
    }

    function bindClientFilterApply() {
        if (document.documentElement.dataset.banenkaartClientFilters === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartClientFilters = "1";
        document.addEventListener("submit", function (ev) {
            var form = ev.target;
            if (!form || !form.getAttribute) {
                return;
            }
            var action = form.getAttribute("action") || "";
            if (action.indexOf("/api/banenkaart-client/reset-filters") !== -1) {
                var rootReset = qs("[data-banenkaart-ssr=\"1\"]");
                if (!rootReset || !window.jobsyBanenkaartCatalog
                    || typeof window.jobsyBanenkaartCatalog.resetClientFilters !== "function") {
                    return;
                }
                ev.preventDefault();
                try { localStorage.removeItem(GUEST_FILTER_KEY); } catch (eResetStore) { }
                window.jobsyBanenkaartCatalog.resetClientFilters();
                setFiltersOpen(false);
                ensureBodyScrollUnlocked();
                return;
            }
            if (form.id !== "kb-filter-form") {
                return;
            }
            var root = qs("[data-banenkaart-ssr=\"1\"]");
            if (!root || !window.jobsyBanenkaartCatalog
                || typeof window.jobsyBanenkaartCatalog.applyClientFilters !== "function") {
                return;
            }
            ev.preventDefault();
            persistGuestFilters(form);
            var filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
            window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            setFiltersOpen(false);
            ensureBodyScrollUnlocked();
        }, true);
    }

    function isDesktopBanenkaart() {
        return !!(window.jobsyViewport && typeof window.jobsyViewport.isWide === "function"
            && window.jobsyViewport.isWide());
    }

    /** Desktop split: list rail + map (no Kaart/Lijst toggle), from 701px. */
    function isDesktopSplitBanenkaart() {
        try {
            return window.matchMedia("(min-width: 701px)").matches;
        } catch (eSplit) {
            return false;
        }
    }

    function listModeFromUrl() {
        if (isDesktopSplitBanenkaart()) {
            return false;
        }
        try {
            return new URLSearchParams(window.location.search).get("weergave") === "lijst";
        } catch (eUrl) {
            return false;
        }
    }

    function syncViewQueryParam(listMode) {
        try {
            var params = new URLSearchParams(window.location.search);
            if (listMode && !isDesktopSplitBanenkaart()) {
                params.set("weergave", "lijst");
            } else {
                params.delete("weergave");
            }
            var qs = params.toString();
            history.replaceState(null, "", qs ? "/banenkaart?" + qs : "/banenkaart");
        } catch (eSync) { }
    }

    var BK_MAP_VIEW_KEY = "jobsy.bk.mapView.v1";

    function clearMapPopupRestoreSession() {
        try {
            sessionStorage.removeItem(BK_MAP_VIEW_KEY);
        } catch (eClear) { }
    }

    function dismissMapPopupsForPageEntry() {
        if (window.jobMap && typeof window.jobMap.dismissPopups === "function") {
            window.jobMap.dismissPopups();
        }
    }

    /** SSOT: same classes + data-kb-view as SSR markup (show-list/show-map; desktop split is always map). */
    function applyBanenkaartViewMode(root, mode, options) {
        if (!root || (mode !== "list" && mode !== "map")) {
            return;
        }
        var opts = options || {};
        var listMode = mode === "list";
        if (isDesktopSplitBanenkaart()) {
            listMode = false;
        }
        root.classList.remove("show-map", "show-list", "jobsy-discovery--list");
        if (listMode) {
            root.classList.add("show-list");
            root.setAttribute("data-kb-view", "list");
        } else {
            root.classList.add("show-map");
            root.setAttribute("data-kb-view", "map");
        }
        qsa("[data-testid=kb-view-kaart]").forEach(function (btn) {
            btn.classList.toggle("is-active", !listMode);
            btn.setAttribute("aria-checked", listMode ? "false" : "true");
        });
        qsa("[data-testid=kb-view-lijst]").forEach(function (btn) {
            btn.classList.toggle("is-active", listMode);
            btn.setAttribute("aria-checked", listMode ? "true" : "false");
        });
        if (opts.updateUrl !== false) {
            syncViewQueryParam(listMode);
        }
        if (listMode) {
            saveListScrollToSession();
        } else if (opts.skipMapSync !== true) {
            revealMapPaneAndSync();
        }
    }

    window.applyBanenkaartViewMode = applyBanenkaartViewMode;

    function isNavigationReload() {
        try {
            var nav = performance.getEntriesByType("navigation")[0];
            return !!(nav && nav.type === "reload");
        } catch (eNav) {
            return false;
        }
    }

    function clearLegacyUiSession() {
        try {
            sessionStorage.removeItem(BK_UI_KEY_LEGACY);
        } catch (eLegacy) { }
    }

    function saveListScrollToSession() {
        var scroller = listScroller();
        if (!scroller) {
            return;
        }
        var raw;
        try {
            raw = sessionStorage.getItem(BK_VIEW_KEY);
        } catch (eRead) {
            raw = null;
        }
        var state = {};
        if (raw) {
            try {
                state = JSON.parse(raw) || {};
            } catch (eParse) {
                state = {};
            }
        }
        state.listScrollTop = scroller.scrollTop;
        state.savedAt = Date.now();
        try {
            sessionStorage.setItem(BK_VIEW_KEY, JSON.stringify(state));
        } catch (eStore) { }
    }

    function restoreListScrollWithRetry(scrollTop) {
        if (typeof scrollTop !== "number" || scrollTop < 0) {
            return;
        }
        function apply() {
            var root = qs("[data-banenkaart-ssr=\"1\"]");
            if (!root) {
                return;
            }
            var list = qs(".vacancy-list", root);
            var pane = qs(".vacancy-pane", root);
            [list, pane].forEach(function (el) {
                if (el) {
                    el.scrollTop = scrollTop;
                }
            });
        }
        apply();
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(apply);
            });
        }
        [50, 150, 350, 700, 1200, 2000].forEach(function (ms) {
            setTimeout(apply, ms);
        });
    }

    function restoreListScrollFromSession() {
        var raw;
        try {
            raw = sessionStorage.getItem(BK_VIEW_KEY);
        } catch (eRead) {
            return;
        }
        if (!raw) {
            return;
        }
        var state;
        try {
            state = JSON.parse(raw);
        } catch (eParse) {
            return;
        }
        if (!state || typeof state.listScrollTop !== "number") {
            return;
        }
        restoreListScrollWithRetry(state.listScrollTop);
    }

    function syncMapAfterPaneVisible() {
        function pass() {
            var pane = qs(".jobsy-discovery[data-banenkaart-ssr=\"1\"] .map-pane");
            if (pane) {
                try {
                    var st = window.getComputedStyle(pane);
                    if (st.display === "none" || pane.getClientRects().length === 0) {
                        return;
                    }
                } catch (ePane) { }
            }
            if (window.jobsyMaps && typeof window.jobsyMaps.ensure === "function") {
                window.jobsyMaps.ensure("discovery").then(function () {
                    if (window.jobMap && typeof window.jobMap.boot === "function") {
                        window.jobMap.boot("job-map");
                    }
                    whenMapIdle(function () {
                        resumeMapLayout();
                        try {
                            var map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                            if (map && typeof map.resize === "function") {
                                map.resize();
                            }
                        } catch (eResize) { }
                        if (window.jobMap && typeof window.jobMap.fitToOriginRings === "function") {
                            window.jobMap.fitToOriginRings(false);
                        }
                        if (window.jobsyBanenkaartCatalog
                            && typeof window.jobsyBanenkaartCatalog.applyToMap === "function") {
                            var filters = typeof window.jobsyBanenkaartCatalog.resolveFilters === "function"
                                ? window.jobsyBanenkaartCatalog.resolveFilters()
                                : null;
                            window.jobsyBanenkaartCatalog.applyToMap(filters);
                        }
                        if (window.jobMap && typeof window.jobMap.reloadPins === "function") {
                            window.jobMap.reloadPins().catch(function () { });
                        }
                        markMapReady();
                    });
                }).catch(function () {
                    resumeMapLayout();
                });
                return;
            }
            resumeMapLayout();
        }
        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(pass);
            });
        } else {
            pass();
        }
        [80, 250, 600, 1200].forEach(function (ms) {
            setTimeout(pass, ms);
        });
    }

    function revealMapPaneAndSync() {
        try {
            document.dispatchEvent(new Event("jobsy-bk-map-pane-visible"));
        } catch (eEvt) { }
        syncMapAfterPaneVisible();
    }

    function scheduleMapRelayout() {
        revealMapPaneAndSync();
    }

    function setDefaultBanenkaartView(root) {
        applyBanenkaartViewMode(root, "map", { updateUrl: false });
        writeUiState({ showMap: true, desktopList: false, filtersOpen: false });
    }

    /** Fresh GET / hard reload: filters closed; desktop split always shows map + list rail. */
    function applyInitialUiState(skipSessionRestore) {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        setFiltersOpen(false);
        writeUiState({ filtersOpen: false });
        if (skipSessionRestore) {
            ensureBodyScrollUnlocked();
            return;
        }
        clearLegacyUiSession();
        var reload = isNavigationReload();
        if (isDesktopSplitBanenkaart()) {
            applyBanenkaartViewMode(root, "map", { updateUrl: true });
            writeUiState({ showMap: true, desktopList: false, filtersOpen: false });
            if (!isBackForwardNavigation()) {
                clearMapPopupRestoreSession();
                dismissMapPopupsForPageEntry();
            }
            ensureBodyScrollUnlocked();
            return;
        }
        if (isDesktopBanenkaart()) {
            var deskState = reload ? {} : readUiState();
            var listMode = listModeFromUrl()
                || (!reload && deskState.desktopList === true);
            if (reload && !listMode) {
                setDefaultBanenkaartView(root);
                return;
            }
            if (listMode) {
                applyBanenkaartViewMode(root, "list", { updateUrl: false });
                writeUiState({ showMap: false, desktopList: true, filtersOpen: false });
            } else if (!reload) {
                applyBanenkaartViewMode(root, "map", { updateUrl: false });
            }
            return;
        }
        if (reload) {
            if (listModeFromUrl()) {
                applyBanenkaartViewMode(root, "list", { updateUrl: false });
                writeUiState({ showMap: false, filtersOpen: false });
            } else {
                setDefaultBanenkaartView(root);
            }
            ensureBodyScrollUnlocked();
            return;
        }
        var state = readUiState();
        if (listModeFromUrl() || state.showMap === false) {
            applyBanenkaartViewMode(root, "list", { updateUrl: false });
        }
        ensureBodyScrollUnlocked();
    }

    function listScroller() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return null;
        }
        var list = qs(".vacancy-list", root) || qs("[data-testid=vacancy-list]", root);
        var pane = qs(".vacancy-pane", root);
        var best = null;
        var bestMax = 0;
        [list, pane].forEach(function (el) {
            if (!el) {
                return;
            }
            var max = el.scrollHeight - el.clientHeight;
            if (max > bestMax) {
                bestMax = max;
                best = el;
            }
        });
        return best || list || pane;
    }

    function captureBanenkaartViewState(fromListVacancy) {
        var state = { savedAt: Date.now() };
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (root) {
            state.showMap = root.classList.contains("show-map") || isDesktopSplitBanenkaart();
            state.desktopList = !isDesktopSplitBanenkaart()
                && root.classList.contains("jobsy-discovery--list");
        }
        if (fromListVacancy) {
            state.skipMapPopupRestore = true;
            state.pendingListScrollRestore = true;
            clearMapPopupRestoreSession();
        }
        try {
            var map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
            if (map && typeof map.getCenter === "function") {
                var c = map.getCenter();
                state.zoom = map.getZoom();
                state.center = [c.lng, c.lat];
            }
        } catch (eMap) { }
        var scroller = listScroller();
        if (scroller) {
            state.listScrollTop = scroller.scrollTop;
        }
        try {
            sessionStorage.setItem(BK_VIEW_KEY, JSON.stringify(state));
        } catch (eStore) { }
    }

    function restoreBanenkaartViewState() {
        var raw;
        try {
            raw = sessionStorage.getItem(BK_VIEW_KEY);
        } catch (eRead) {
            return;
        }
        if (!raw) {
            return;
        }
        var state;
        try {
            state = JSON.parse(raw);
        } catch (eParse) {
            return;
        }
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (root) {
            if (isDesktopSplitBanenkaart()) {
                if (!root.classList.contains("show-map") || root.classList.contains("show-list")) {
                    applyBanenkaartViewMode(root, "map", { updateUrl: true, skipMapSync: true });
                } else {
                    syncViewQueryParam(false);
                }
            } else if (typeof state.showMap === "boolean") {
                if (isDesktopBanenkaart()) {
                    var listMode = state.desktopList === true || state.showMap === false;
                    applyBanenkaartViewMode(root, listMode ? "list" : "map", { updateUrl: false });
                } else {
                    applyBanenkaartViewMode(root, state.showMap ? "map" : "list", { updateUrl: false });
                }
            }
        }
        if (state && state.skipMapPopupRestore) {
            clearMapPopupRestoreSession();
            dismissMapPopupsForPageEntry();
        }
        if (state && typeof state.listScrollTop === "number") {
            restoreListScrollWithRetry(state.listScrollTop);
            if (state.pendingListScrollRestore) {
                schedulePendingListScrollRestorePasses();
            }
        }
        if (!state || !state.center || state.zoom == null) {
            return;
        }
        window.__jobsyBkRestoringView = true;
        whenMapIdle(function () {
            try {
                var map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                if (map && typeof map.jumpTo === "function") {
                    map.jumpTo({
                        center: state.center,
                        zoom: state.zoom,
                        duration: 0
                    });
                }
            } catch (eJump) { }
            if (typeof state.listScrollTop === "number") {
                restoreListScrollWithRetry(state.listScrollTop);
            }
            if (window.jobMap && typeof window.jobMap.invalidate === "function") {
                window.jobMap.invalidate();
            }
            setTimeout(function () {
                window.__jobsyBkRestoringView = false;
            }, 800);
        });
    }

    function syncOriginFields(lat, lng, label) {
        var latEl = qs("#kb-filter-origin-lat");
        var lngEl = qs("#kb-filter-origin-lng");
        var labelEl = qs("#kb-filter-origin-label");
        if (latEl) {
            latEl.value = String(lat);
        }
        if (lngEl) {
            lngEl.value = String(lng);
        }
        if (labelEl) {
            labelEl.value = label || "";
        }
        qsa("[data-kb-address]").forEach(function (input) {
            input.value = label || "";
        });
    }

    function syncAddressUiFromFilters(filters) {
        if (!filters) {
            return;
        }
        var hasOrigin = filters.originLat != null && filters.originLng != null;
        if (hasOrigin) {
            syncOriginFields(filters.originLat, filters.originLng, filters.originLabel || "");
            toggleAddressClearButtons(true);
            return;
        }
        syncOriginFields("", "", "");
        toggleAddressClearButtons(false);
    }

    function toggleAddressClearButtons(show) {
        qsa("[data-banenkaart-ssr=\"1\"] .kb-address").forEach(function (wrap) {
            wrap.classList.toggle("kb-address--with-clear", !!show);
            var clearBtn = wrap.querySelector("[data-testid=kb-address-clear]");
            if (show && !clearBtn) {
                var field = wrap.querySelector(".kb-address__field");
                if (field) {
                    field.insertAdjacentHTML(
                        "beforeend",
                        "<button type=\"button\" class=\"kb-address__clear\" data-testid=\"kb-address-clear\" aria-label=\"Wissen\">"
                        + "<svg class=\"kb-address__clear-icon\" viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\">"
                        + "<path d=\"M18 6 6 18\" /><path d=\"m6 6 12 12\" /></svg></button>"
                    );
                }
            } else if (!show && clearBtn) {
                clearBtn.remove();
            }
        });
    }

    function clearOrigin() {
        var latEl = qs("#kb-filter-origin-lat");
        var lngEl = qs("#kb-filter-origin-lng");
        var labelEl = qs("#kb-filter-origin-label");
        if (latEl) {
            latEl.value = "";
        }
        if (lngEl) {
            lngEl.value = "";
        }
        if (labelEl) {
            labelEl.value = "";
        }
        qsa("[data-kb-address]").forEach(function (input) {
            input.value = "";
        });
        toggleAddressClearButtons(false);
        if (window.jobMap && typeof window.jobMap.clearOrigin === "function") {
            window.jobMap.clearOrigin();
        }
        var form = qs("#kb-filter-form");
        if (form && window.jobsyBanenkaartCatalog
            && typeof window.jobsyBanenkaartCatalog.applyClientFilters === "function") {
            var filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
            filters.originLat = null;
            filters.originLng = null;
            filters.originLabel = null;
            window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            return;
        }
        try {
            history.replaceState(null, "", "/banenkaart");
        } catch (eClear) { }
    }

    function applyOriginFromGeocode(label, lat, lng) {
        syncOriginFields(lat, lng, label);
        toggleAddressClearButtons(true);
        if (window.jobMap && typeof window.jobMap.setOrigin === "function") {
            window.jobMap.setOrigin(lat, lng);
        }
        var form = qs("#kb-filter-form");
        if (form && window.jobsyBanenkaartCatalog
            && typeof window.jobsyBanenkaartCatalog.applyClientFilters === "function") {
            var filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
            filters.originLat = lat;
            filters.originLng = lng;
            filters.originLabel = label;
            window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            return;
        }
        try {
            var url = window.jobsyBanenkaartCatalog
                && typeof window.jobsyBanenkaartCatalog.buildBanenkaartUrl === "function"
                ? window.jobsyBanenkaartCatalog.buildBanenkaartUrl({
                    originLat: lat,
                    originLng: lng,
                    originLabel: label
                })
                : "/banenkaart?originLat=" + lat + "&originLng=" + lng
                    + "&originLabel=" + encodeURIComponent(label || "");
            history.replaceState(null, "", url);
        } catch (eUrl) { }
    }

    function addressWrapForInput(input) {
        return input && input.closest ? input.closest(".kb-address") : null;
    }

    function hideAddressSuggest(wrap) {
        if (!wrap) {
            qsa("[data-kb-address-suggest]").forEach(function (list) {
                list.hidden = true;
            });
            return;
        }
        var list = wrap.querySelector("[data-kb-address-suggest]");
        if (list) {
            list.hidden = true;
        }
        var input = wrap.querySelector("[data-kb-address]");
        if (input) {
            input.setAttribute("aria-expanded", "false");
        }
    }

    function pickSuggestion(row, fallback) {
        var lat = Number(row.latitude != null ? row.latitude : row.Latitude);
        var lng = Number(row.longitude != null ? row.longitude : row.Longitude);
        var label = row.label || row.Label || fallback;
        if (!isFinite(lat) || !isFinite(lng)) {
            return null;
        }
        return { lat: lat, lng: lng, label: label };
    }

    function showAddressSuggest(wrap, html) {
        var list = wrap && wrap.querySelector("[data-kb-address-suggest]");
        var input = wrap && wrap.querySelector("[data-kb-address]");
        if (!list || !input) {
            return;
        }
        list.innerHTML = html;
        list.hidden = !html;
        input.setAttribute("aria-expanded", html ? "true" : "false");
        list.querySelectorAll(".address-suggestions__item").forEach(function (btn) {
            btn.addEventListener("mousedown", function (ev) {
                ev.preventDefault();
            });
            btn.addEventListener("click", function () {
                var idx = parseInt(btn.getAttribute("data-idx"), 10);
                var hit = btn.__suggestion || null;
                if (!hit && btn.dataset.lat) {
                    hit = {
                        lat: parseFloat(btn.dataset.lat),
                        lng: parseFloat(btn.dataset.lng),
                        label: btn.textContent || ""
                    };
                }
                if (hit) {
                    applyOriginFromGeocode(hit.label, hit.lat, hit.lng);
                }
                hideAddressSuggest(wrap);
            });
        });
    }

    function fetchAddressSuggestions(query, wrap) {
        if (!query || query.length < 3) {
            hideAddressSuggest(wrap);
            return;
        }
        showAddressSuggest(wrap, "<li class=\"address-suggestions__status\" role=\"option\">…</li>");
        fetch("/api/banenkaart-client/address-suggest?q=" + encodeURIComponent(query), {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        }).then(function (res) {
            return res.ok ? res.json() : [];
        }).then(function (rows) {
            if (!Array.isArray(rows) || !rows.length) {
                showAddressSuggest(wrap, "<li class=\"address-suggestions__status\" role=\"option\">Geen adressen gevonden</li>");
                return;
            }
            var html = rows.slice(0, 8).map(function (row, i) {
                var hit = pickSuggestion(row, query);
                if (!hit) {
                    return "";
                }
                return "<li role=\"option\"><button type=\"button\" class=\"address-suggestions__item\" data-idx=\"" + i
                    + "\" data-lat=\"" + hit.lat + "\" data-lng=\"" + hit.lng + "\">"
                    + String(hit.label).replace(/&/g, "&amp;").replace(/</g, "&lt;") + "</button></li>";
            }).join("");
            showAddressSuggest(wrap, html || "<li class=\"address-suggestions__status\" role=\"option\">Geen adressen gevonden</li>");
            var buttons = wrap.querySelectorAll(".address-suggestions__item");
            rows.slice(0, buttons.length).forEach(function (row, i) {
                var hit = pickSuggestion(row, query);
                if (hit && buttons[i]) {
                    buttons[i].__suggestion = hit;
                }
            });
        }).catch(function () {
            hideAddressSuggest(wrap);
        });
    }

    var addressSuggestTimer = null;

    function bindAddressGeocode() {
        if (document.documentElement.dataset.banenkaartAddressGeocode === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartAddressGeocode = "1";
        document.addEventListener("input", function (ev) {
            var input = ev.target && ev.target.closest ? ev.target.closest("[data-kb-address]") : null;
            if (!input || !qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            var wrap = addressWrapForInput(input);
            if (addressSuggestTimer) {
                clearTimeout(addressSuggestTimer);
            }
            addressSuggestTimer = setTimeout(function () {
                fetchAddressSuggestions(String(input.value || "").trim(), wrap);
            }, 220);
        }, true);
        document.addEventListener("focusin", function (ev) {
            var input = ev.target && ev.target.closest ? ev.target.closest("[data-kb-address]") : null;
            if (!input) {
                return;
            }
            var q = String(input.value || "").trim();
            if (q.length >= 3) {
                fetchAddressSuggestions(q, addressWrapForInput(input));
            }
        });
        document.addEventListener("focusout", function (ev) {
            var input = ev.target && ev.target.closest ? ev.target.closest("[data-kb-address]") : null;
            if (!input) {
                return;
            }
            setTimeout(function () {
                hideAddressSuggest(addressWrapForInput(input));
            }, 150);
        });
        document.addEventListener("keydown", function (ev) {
            if (ev.key !== "Enter") {
                return;
            }
            var input = ev.target && ev.target.closest
                ? ev.target.closest("[data-kb-address]")
                : null;
            if (!input || !qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            ev.preventDefault();
            var q = String(input.value || "").trim();
            if (q.length < 3) {
                return;
            }
            fetch("/api/banenkaart-client/address-suggest?q=" + encodeURIComponent(q), {
                credentials: "same-origin",
                headers: { Accept: "application/json" }
            }).then(function (res) {
                return res.ok ? res.json() : [];
            }).then(function (rows) {
                if (!Array.isArray(rows) || !rows.length) {
                    return;
                }
                var hit = pickSuggestion(rows[0], q);
                if (!hit) {
                    return;
                }
                applyOriginFromGeocode(hit.label, hit.lat, hit.lng);
                hideAddressSuggest(addressWrapForInput(input));
            }).catch(function () { });
        }, true);
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            if (target.closest("[data-testid=kb-address-locate]")) {
                ev.preventDefault();
                if (!window.jobsyGeo || typeof window.jobsyGeo.requestLocation !== "function") {
                    return;
                }
                window.jobsyGeo.requestLocation().then(function (pos) {
                    var lat = Number(pos.lat != null ? pos.lat : pos.Lat);
                    var lng = Number(pos.lng != null ? pos.lng : pos.Lng);
                    if (!isFinite(lat) || !isFinite(lng)) {
                        return;
                    }
                    applyOriginFromGeocode("Huidige locatie", lat, lng);
                }).catch(function () { });
                return;
            }
            if (target.closest("[data-testid=kb-address-clear]")) {
                ev.preventDefault();
                clearOrigin();
                return;
            }
            if (target.closest("[data-testid=kb-start-locate]")) {
                ev.preventDefault();
                if (window.jobsyGeo && typeof window.jobsyGeo.requestLocation === "function") {
                    window.jobsyGeo.requestLocation().then(function (pos) {
                        var lat = Number(pos.lat != null ? pos.lat : pos.Lat);
                        var lng = Number(pos.lng != null ? pos.lng : pos.Lng);
                        if (isFinite(lat) && isFinite(lng)) {
                            applyOriginFromGeocode("Huidige locatie", lat, lng);
                        }
                    }).catch(function () { });
                }
            }
        }, true);
    }

    function bindActiveFiltersBar() {
        if (document.documentElement.dataset.banenkaartActiveFilters === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartActiveFilters = "1";
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            if (!target.closest("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            if (target.closest("[data-testid=kb-active-filters-clear]")) {
                ev.preventDefault();
                if (window.jobsyBanenkaartCatalog
                    && typeof window.jobsyBanenkaartCatalog.resetClientFilters === "function") {
                    try { localStorage.removeItem(GUEST_FILTER_KEY); } catch (eClr) { }
                    window.jobsyBanenkaartCatalog.resetClientFilters();
                }
                return;
            }
            if (target.closest("[data-testid=kb-active-tag-travel]")) {
                ev.preventDefault();
                if (window.jobsyBanenkaartCatalog
                    && typeof window.jobsyBanenkaartCatalog.resetTravelKeepOrigin === "function") {
                    window.jobsyBanenkaartCatalog.resetTravelKeepOrigin();
                }
            }
        }, true);
    }

    function bindFilterUrlSync() {
        if (document.documentElement.dataset.banenkaartFilterUrlSync === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartFilterUrlSync = "1";
        window.addEventListener("popstate", function () {
            var root = qs("[data-banenkaart-ssr=\"1\"]");
            if (root && isDesktopBanenkaart()) {
                applyBanenkaartViewMode(root, listModeFromUrl() ? "list" : "map", { updateUrl: false });
            }
            if (!window.jobsyBanenkaartCatalog
                || typeof window.jobsyBanenkaartCatalog.resolveFilters !== "function"
                || typeof window.jobsyBanenkaartCatalog.applyClientFilters !== "function") {
                return;
            }
            window.jobsyBanenkaartCatalog.applyClientFilters(window.jobsyBanenkaartCatalog.resolveFilters());
        });
        document.addEventListener("jobsy-bk-filters-applied", function (ev) {
            var filters = ev && ev.detail ? ev.detail.filters : null;
            if (filters) {
                syncAddressUiFromFilters(filters);
            }
            if (window.jobMap && typeof window.jobMap.invalidate === "function") {
                requestAnimationFrame(function () {
                    window.jobMap.invalidate();
                });
            }
        });
    }

    function bindListSort() {
        if (document.documentElement.dataset.banenkaartListSort === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartListSort = "1";
        document.addEventListener("change", function (ev) {
            var sel = ev.target;
            if (!sel || sel.getAttribute("data-testid") !== "kb-list-sort") {
                return;
            }
            var form = qs("#kb-filter-form");
            if (!form || !window.jobsyBanenkaartCatalog
                || typeof window.jobsyBanenkaartCatalog.applyClientFilters !== "function") {
                return;
            }
            var filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
            filters.sort = sel.value;
            window.jobsyBanenkaartCatalog.applyClientFilters(filters);
        });
    }

    function bindKeywordApplyEnter() {
        if (document.documentElement.dataset.banenkaartKeywordEnter === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartKeywordEnter = "1";
        document.addEventListener("keydown", function (ev) {
            if (ev.key !== "Enter") {
                return;
            }
            var input = ev.target;
            if (!input || input.name !== "q" || input.getAttribute("form") !== "kb-filter-form") {
                return;
            }
            if (!qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            ev.preventDefault();
            var form = qs("#kb-filter-form");
            if (!form || !window.jobsyBanenkaartCatalog
                || typeof window.jobsyBanenkaartCatalog.applyClientFilters !== "function") {
                return;
            }
            var filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
            window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            setFiltersOpen(false);
        }, true);
    }

    function bindVacancyBackRestore() {
        if (document.documentElement.dataset.banenkaartVacancyNav === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartVacancyNav = "1";
        function captureFromVacancyLink(link) {
            if (!link) {
                return;
            }
            var fromList = !!(link.closest(".vacancy-list, .list.vacancy-list, [data-testid=kb-list-row], .vc, .kb-list-row"));
            captureBanenkaartViewState(fromList);
        }
        document.addEventListener("pointerdown", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            var link = target.closest("a[href*=\"/vacancies/\"]");
            if (!link || !qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            saveListScrollToSession();
        }, true);
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            var link = target.closest("a[href*=\"/vacancies/\"]");
            if (!link || !qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            captureFromVacancyLink(link);
        }, true);
        window.addEventListener("pagehide", function () {
            if (!qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            saveListScrollToSession();
        });
    }

    function isBackForwardNavigation() {
        try {
            var nav = performance.getEntriesByType("navigation")[0];
            return !!(nav && nav.type === "back_forward");
        } catch (eNav) {
            return false;
        }
    }

    function cameFromVacancyDetail() {
        try {
            return /\/vacancies\//.test(document.referrer || "");
        } catch (eRef) {
            return false;
        }
    }

    function restoreUiStateFromSession() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        setFiltersOpen(false);
        if (listModeFromUrl()) {
            applyBanenkaartViewMode(root, "list", { updateUrl: false });
            restoreListScrollFromSession();
        }
    }

    function resumeMapLayout() {
        ensureBodyScrollUnlocked();
        if (window.jobMap && typeof window.jobMap.invalidate === "function") {
            window.jobMap.invalidate();
        }
    }

    function syncLayoutForViewport() {
        applyInitialUiState(true);
        resumeMapLayout();
        try {
            var map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
            if (map && typeof map.resize === "function") {
                map.resize();
            }
        } catch (eResize) { }
    }

    function bindViewportLayout() {
        if (document.documentElement.dataset.banenkaartViewportLayout === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartViewportLayout = "1";
        var resizeTimer = null;
        function schedule() {
            if (resizeTimer) {
                clearTimeout(resizeTimer);
            }
            resizeTimer = setTimeout(syncLayoutForViewport, 80);
        }
        window.addEventListener("resize", schedule);
        try {
            var mq = window.matchMedia("(min-width: 701px)");
            if (typeof mq.addEventListener === "function") {
                mq.addEventListener("change", schedule);
            } else if (typeof mq.addListener === "function") {
                mq.addListener(schedule);
            }
        } catch (eMq) { }
    }

    function schedulePendingListScrollRestorePasses() {
        restorePendingListScrollAfterCatalog();
        [120, 350, 800, 1500, 2500, 4000].forEach(function (ms) {
            setTimeout(restorePendingListScrollAfterCatalog, ms);
        });
    }

    function restorePendingListScrollAfterCatalog() {
        var raw;
        try {
            raw = sessionStorage.getItem(BK_VIEW_KEY);
        } catch (eRead) {
            return;
        }
        if (!raw) {
            return;
        }
        var state;
        try {
            state = JSON.parse(raw);
        } catch (eParse) {
            return;
        }
        if (!state || !state.pendingListScrollRestore || typeof state.listScrollTop !== "number") {
            return;
        }
        restoreListScrollWithRetry(state.listScrollTop);
        var scroller = listScroller();
        if (scroller && Math.abs(scroller.scrollTop - state.listScrollTop) <= 80) {
            delete state.pendingListScrollRestore;
            try {
                sessionStorage.setItem(BK_VIEW_KEY, JSON.stringify(state));
            } catch (eWrite) { }
        }
    }

    function bindLifecycleResume() {
        if (document.documentElement.dataset.banenkaartLifecycle === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartLifecycle = "1";
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState !== "visible") {
                return;
            }
            restoreUiStateFromSession();
            resumeMapLayout();
        });
        window.addEventListener("pageshow", function (ev) {
            restoreUiStateFromSession();
            resumeMapLayout();
            if (ev.persisted || isBackForwardNavigation() || cameFromVacancyDetail()) {
                restoreBanenkaartViewState();
            } else {
                clearMapPopupRestoreSession();
                restoreListScrollFromSession();
                dismissMapPopupsForPageEntry();
            }
        });
    }

    function onReady() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        bindLifecycleResume();
        bindVacancyBackRestore();
        bindAddressGeocode();
        bindActiveFiltersBar();
        bindListSort();
        bindKeywordApplyEnter();
        bindKeywordFields();
        bindListActions();
        bindViewportLayout();
        applyInitialUiState();
        bindGuestFilterPersistence();
        bindClientFilterApply();
        bindFilterUrlSync();
        bindFilters();
        markFilterReady();
        if (root.dataset.banenkaartSsrEnhance === "1") {
            document.dispatchEvent(new Event("jobsy-bk-ssr-ui-ready"));
            return;
        }
        bindMobileView();
        bindStartPrompt();
        root.dataset.banenkaartSsrEnhance = "1";
        document.dispatchEvent(new Event("jobsy-bk-ssr-ui-ready"));
        enhanceMap();
    }

    window.__jobsyBkRestoreViewStateForTests = restoreBanenkaartViewState;

    if (document.documentElement.dataset.banenkaartCatalogScrollRestore !== "1") {
        document.documentElement.dataset.banenkaartCatalogScrollRestore = "1";
        document.addEventListener("jobsy-bk-catalog-ready", restorePendingListScrollAfterCatalog);
        document.addEventListener("jobsy-bk-list-dom-updated", restorePendingListScrollAfterCatalog);
    }

    bindFilters();
    markFilterReady();
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", onReady);
    } else {
        onReady();
    }
})();
