/* Banenkaart list: map highlight on hover + scroll-rooted load-more (not viewport). */
window.jobsyDiscovery = (function () {
    "use strict";

    var hoverBound = false;
    var observer = null;
    var currentEl = null;
    var chipFadeBound = typeof WeakSet === "function" ? new WeakSet() : null;

    function listIsVisible(el) {
        if (!el) return false;
        var discovery = el.closest(".jobsy-discovery, .funda-layout");
        if (!discovery) return true;
        // Mobile map mode hides the vacancy pane — do not auto-load cards then.
        if (discovery.classList.contains("show-map")) {
            try {
                if (window.matchMedia && window.matchMedia("(max-width: 768px)").matches) {
                    return false;
                }
            } catch (e) { /* ignore */ }
        }
        var pane = el.closest(".vacancy-pane");
        if (pane) {
            var style = window.getComputedStyle(pane);
            if (style.visibility === "hidden" || style.display === "none" || style.pointerEvents === "none") {
                return false;
            }
        }
        return true;
    }

    function highlight(id) {
        try {
            if (window.jobMap && typeof window.jobMap.highlight === "function") {
                window.jobMap.highlight(id);
            }
        } catch (e) { /* ignore */ }
    }

    function onEnter(e) {
        var card = e.target && e.target.closest ? e.target.closest(".job-card[data-vacancy-id]") : null;
        if (!card) return;
        var id = card.getAttribute("data-vacancy-id");
        if (id) highlight(id);
    }

    function onLeave(e) {
        var card = e.target && e.target.closest ? e.target.closest(".job-card[data-vacancy-id]") : null;
        if (!card) return;
        var related = e.relatedTarget;
        if (related && card.contains(related)) return;
        highlight(null);
    }

    function updateChipFade(el) {
        if (!el) return;
        var maxScroll = el.scrollWidth - el.clientWidth - 1;
        var more = maxScroll > 0 && el.scrollLeft < maxScroll - 2;
        if (document.documentElement && document.documentElement.getAttribute("dir") === "rtl") {
            // In RTL, scrollLeft can be 0 at the start or negative depending on the engine.
            more = maxScroll > 0 && Math.abs(el.scrollLeft) < maxScroll - 2;
        }
        el.classList.toggle("has-more-end", more);
    }

    function bindOneChipRow(el) {
        if (!el) return;
        if (chipFadeBound) {
            if (chipFadeBound.has(el)) return;
            chipFadeBound.add(el);
        } else if (el.dataset && el.dataset.chipFadeBound === "1") {
            return;
        } else if (el.dataset) {
            el.dataset.chipFadeBound = "1";
        }
        var onScroll = function () { updateChipFade(el); };
        el.addEventListener("scroll", onScroll, { passive: true });
        if (typeof ResizeObserver === "function") {
            var ro = new ResizeObserver(onScroll);
            ro.observe(el);
        }
        window.addEventListener("resize", onScroll);
        // Content can change after Blazor render; refresh a few times.
        updateChipFade(el);
        setTimeout(onScroll, 50);
        setTimeout(onScroll, 250);
    }

    return {
        bindHover: function (root) {
            if (hoverBound) return;
            hoverBound = true;
            var target = root || document;
            // mouseenter/leave do not bubble — use capture.
            target.addEventListener("mouseenter", onEnter, true);
            target.addEventListener("mouseleave", onLeave, true);
        },

        /**
         * Observe the load-more sentinel with the list scroll container as root
         * so cards load while scrolling, not all at once via the viewport.
         */
        observeMore: function (el, dotNetRef, methodName) {
            if (!el || !dotNetRef || !methodName || typeof IntersectionObserver !== "function") {
                return;
            }
            if (observer) {
                try { observer.disconnect(); } catch (e) { /* ignore */ }
                observer = null;
            }
            currentEl = el;
            if (!listIsVisible(el)) {
                return;
            }
            var root = el.closest(".vacancy-list") || null;
            observer = new IntersectionObserver(function (entries) {
                if (!listIsVisible(el)) {
                    return;
                }
                for (var i = 0; i < entries.length; i++) {
                    if (entries[i].isIntersecting) {
                        try {
                            dotNetRef.invokeMethodAsync(methodName);
                        } catch (e) { /* ignore */ }
                        break;
                    }
                }
            }, { root: root, rootMargin: "120px 0px", threshold: 0.01 });
            observer.observe(el);
        },

        disconnect: function () {
            if (observer) {
                try { observer.disconnect(); } catch (e) { /* ignore */ }
                observer = null;
            }
            currentEl = null;
        },

        /**
         * Toggle .has-more-end on .kb-filter-chips while horizontal overflow remains,
         * so the end-edge fade only shows when more chips can be scrolled into view.
         */
        bindFilterChipFade: function (root) {
            var scope = root || document;
            var rows = scope.querySelectorAll
                ? scope.querySelectorAll(".kb-filter-chips:not(.kb-filter-chips--desktop)")
                : [];
            for (var i = 0; i < rows.length; i++) {
                bindOneChipRow(rows[i]);
            }
        }
    };
})();
