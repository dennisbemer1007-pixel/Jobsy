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

    function setFiltersOpen(open) {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        root.classList.toggle("filters-open", open);
        var sheet = qs("[data-testid=kb-filter-sheet]");
        var backdrop = qs("[data-testid=kb-filter-backdrop]");
        qsa("[data-testid=kb-filters-button]").forEach(function (btn) {
            btn.classList.toggle("is-active", open);
            btn.setAttribute("aria-expanded", open ? "true" : "false");
        });
        if (sheet) {
            sheet.setAttribute("aria-hidden", open ? "false" : "true");
            sheet.setAttribute("aria-modal", open ? "true" : "false");
        }
        if (backdrop) {
            backdrop.setAttribute("aria-hidden", open ? "false" : "true");
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
                }
            });
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
            }
            if (action.indexOf("/api/banenkaart-client/reset-filters") !== -1) {
                try { localStorage.removeItem(GUEST_FILTER_KEY); } catch (eReset) { }
            }
        }, true);
    }

    function onReady() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        bindGuestFilterPersistence();
        bindFilters();
        markFilterReady();
        if (root.dataset.banenkaartSsrEnhance === "1") {
            return;
        }
        bindMobileView();
        bindStartPrompt();
        root.dataset.banenkaartSsrEnhance = "1";
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
