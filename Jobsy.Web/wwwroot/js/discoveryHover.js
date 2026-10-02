/* Banenkaart list: map highlight on hover + scroll-rooted load-more (not viewport). */
window.jobsyDiscovery = (function () {
    "use strict";

    var hoverBound = false;
    var observer = null;
    var currentEl = null;

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
            // Prefer the pane (fixed-height scrollport). A growing .vacancy-list is not a
            // scroll root, so the sentinel stays intersecting and would load every card.
            var root = el.closest(".vacancy-pane") || el.closest(".vacancy-list") || null;
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
        }
    };
})();
