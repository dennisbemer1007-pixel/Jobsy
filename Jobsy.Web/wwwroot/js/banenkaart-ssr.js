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

    var BK_UI_KEY = "jobsy.bk.ui.v1";
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
        var mapBtn = qs("[data-testid=kb-view-kaart]");
        var listBtn = qs("[data-testid=kb-view-lijst]");
        var fab = qs("[data-testid=kb-map-fab]");
        function setMap(on) {
            root.classList.toggle("show-map", on);
            root.classList.toggle("show-list", !on);
            writeUiState({ showMap: !!on });
            ensureBodyScrollUnlocked();
            if (on && window.jobMap && typeof window.jobMap.invalidate === "function") {
                requestAnimationFrame(function () {
                    window.jobMap.invalidate();
                });
            }
            if (mapBtn) {
                mapBtn.classList.toggle("is-active", on);
                mapBtn.setAttribute("aria-checked", on ? "true" : "false");
            }
            if (listBtn) {
                listBtn.classList.toggle("is-active", !on);
                listBtn.setAttribute("aria-checked", on ? "true" : "false");
            }
        }
        if (mapBtn) {
            mapBtn.addEventListener("click", function () { setMap(true); });
        }
        if (listBtn) {
            listBtn.addEventListener("click", function () { setMap(false); });
        }
        if (fab) {
            fab.addEventListener("click", function () { setMap(true); });
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
    }

    function whenMapIdle(cb) {
        if (!window.jobMap || typeof window.jobMap.__testGetMap !== "function") {
            cb();
            return;
        }
        var m = window.jobMap.__testGetMap();
        if (!m || typeof m.once !== "function") {
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
            if (!form || form.id !== "kb-filter-form") {
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

    /** Fresh GET / hard reload: filters closed; desktop always shows map + list rail. */
    function applyInitialUiState() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        setFiltersOpen(false);
        writeUiState({ filtersOpen: false });
        if (isDesktopBanenkaart()) {
            root.classList.add("show-map");
            root.classList.remove("show-list");
            var mapBtn = qs("[data-testid=kb-view-kaart]");
            var listBtn = qs("[data-testid=kb-view-lijst]");
            if (mapBtn) {
                mapBtn.classList.add("is-active");
                mapBtn.setAttribute("aria-checked", "true");
            }
            if (listBtn) {
                listBtn.classList.remove("is-active");
                listBtn.setAttribute("aria-checked", "false");
            }
            return;
        }
        var state = readUiState();
        if (state.showMap === false) {
            root.classList.remove("show-map");
            root.classList.add("show-list");
            var mapBtnM = qs("[data-testid=kb-view-kaart]");
            var listBtnM = qs("[data-testid=kb-view-lijst]");
            if (mapBtnM) {
                mapBtnM.classList.remove("is-active");
                mapBtnM.setAttribute("aria-checked", "false");
            }
            if (listBtnM) {
                listBtnM.classList.add("is-active");
                listBtnM.setAttribute("aria-checked", "true");
            }
        }
        ensureBodyScrollUnlocked();
    }

    function listScroller() {
        return qs(".jobsy-discovery[data-banenkaart-ssr=\"1\"] .vacancy-list")
            || qs(".jobsy-discovery[data-banenkaart-ssr=\"1\"] .vacancy-pane");
    }

    function captureBanenkaartViewState() {
        var state = { savedAt: Date.now() };
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
            var scroller = listScroller();
            if (scroller && typeof state.listScrollTop === "number") {
                scroller.scrollTop = state.listScrollTop;
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
        if (labelEl && label) {
            labelEl.value = label;
        }
        qsa("[data-kb-address]").forEach(function (input) {
            if (label) {
                input.value = label;
            }
        });
    }

    function applyOriginFromGeocode(label, lat, lng) {
        syncOriginFields(lat, lng, label);
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

    function bindAddressGeocode() {
        if (document.documentElement.dataset.banenkaartAddressGeocode === "1") {
            return;
        }
        document.documentElement.dataset.banenkaartAddressGeocode = "1";
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
                var hit = rows[0];
                var lat = Number(hit.latitude != null ? hit.latitude : hit.Latitude);
                var lng = Number(hit.longitude != null ? hit.longitude : hit.Longitude);
                var label = hit.label || hit.Label || q;
                if (!isFinite(lat) || !isFinite(lng)) {
                    return;
                }
                applyOriginFromGeocode(label, lat, lng);
            }).catch(function () { });
        }, true);
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
        document.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }
            var link = target.closest("a[href*=\"/vacancies/\"]");
            if (!link || !qs("[data-banenkaart-ssr=\"1\"]")) {
                return;
            }
            captureBanenkaartViewState();
        }, true);
    }

    function isBackForwardNavigation() {
        try {
            var nav = performance.getEntriesByType("navigation")[0];
            return !!(nav && nav.type === "back_forward");
        } catch (eNav) {
            return false;
        }
    }

    function restoreUiStateFromSession() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        setFiltersOpen(false);
        if (isDesktopBanenkaart()) {
            root.classList.add("show-map");
            root.classList.remove("show-list");
            if (window.jobMap && typeof window.jobMap.invalidate === "function") {
                window.jobMap.invalidate();
            }
        }
    }

    function resumeMapLayout() {
        ensureBodyScrollUnlocked();
        if (window.jobMap && typeof window.jobMap.invalidate === "function") {
            window.jobMap.invalidate();
        }
    }

    function syncLayoutForViewport() {
        applyInitialUiState();
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
            var mq = window.matchMedia("(min-width: 900px)");
            if (typeof mq.addEventListener === "function") {
                mq.addEventListener("change", schedule);
            } else if (typeof mq.addListener === "function") {
                mq.addListener(schedule);
            }
        } catch (eMq) { }
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
            if (ev.persisted || isBackForwardNavigation()) {
                restoreBanenkaartViewState();
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
        bindKeywordApplyEnter();
        bindKeywordFields();
        bindListActions();
        bindViewportLayout();
        applyInitialUiState();
        bindGuestFilterPersistence();
        bindClientFilterApply();
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

    bindFilters();
    markFilterReady();
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", onReady);
    } else {
        onReady();
    }
})();
