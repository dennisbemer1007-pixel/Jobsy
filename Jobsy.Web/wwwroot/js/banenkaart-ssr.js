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
        qsa("[data-testid=kb-filters-button]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var root = qs("[data-banenkaart-ssr=\"1\"]");
                var open = root && !root.classList.contains("filters-open");
                setFiltersOpen(!!open);
            });
        });
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

    function enhanceMap() {
        var boot = readBootNode();
        if (!boot || !window.jobsyMaps) {
            return;
        }
        window.jobsyMaps.ensure("discovery").then(function () {
            if (!window.jobMap || typeof window.jobMap.init !== "function") {
                return;
            }
            var pins = Array.isArray(boot.pins) ? boot.pins : [];
            var opts = boot.mapInit && typeof boot.mapInit === "object" ? boot.mapInit : {};
            opts.pinsUrl = boot.pinsUrl || opts.pinsUrl;
            opts.view = boot.view || opts.view;
            opts.preferFilledLocation = boot.preferFilledLocation !== false;
            opts.illustratedPopup = true;
            opts.syncViewportList = false;
            try {
                window.jobMap.init("job-map", pins, opts);
            } catch (e) { }
            if (boot.origin && typeof window.jobMap.setOrigin === "function") {
                try {
                    window.jobMap.setOrigin(boot.origin.lat, boot.origin.lng, opts.travel || {});
                } catch (e2) { }
            }
            if (boot.zoomToPinsOutsideTravel && typeof window.jobMap.__testFitAllPins === "function") {
                try {
                    window.jobMap.__testFitAllPins();
                } catch (e3) { }
            }
        }).catch(function () { });
    }

    function onReady() {
        var root = qs("[data-banenkaart-ssr=\"1\"]");
        if (!root) {
            return;
        }
        root.dataset.banenkaartSsrBound = "1";
        bindFilters();
        bindMobileView();
        bindStartPrompt();
        enhanceMap();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", onReady);
    } else {
        onReady();
    }
})();
