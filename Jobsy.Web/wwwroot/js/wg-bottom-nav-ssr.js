(function () {
    "use strict";

    function qs(sel, root) {
        return (root || document).querySelector(sel);
    }

    function setMeerOpen(open) {
        var panel = qs("[data-wg-meer-panel]");
        var backdrop = qs("[data-wg-meer-backdrop]");
        var toggle = qs("[data-wg-meer-toggle]");
        if (!panel) {
            return;
        }

        var on = !!open;
        panel.hidden = !on;
        if (backdrop) {
            backdrop.hidden = !on;
        }

        panel.classList.toggle("is-open", on);
        document.documentElement.classList.toggle("wg-meer-sheet-open", on);
        if (toggle) {
            toggle.classList.toggle("is-active", on);
            toggle.setAttribute("aria-expanded", on ? "true" : "false");
        }
    }

    function bindMeer(root) {
        if (!root || root.getAttribute("data-wg-meer-bound") === "1") {
            return;
        }

        root.setAttribute("data-wg-meer-bound", "1");
    }

    function onClick(ev) {
        if (ev.target.closest("[data-wg-meer-toggle]")) {
            ev.preventDefault();
            setMeerOpen(true);
            return;
        }

        if (ev.target.closest("[data-wg-meer-close]") || ev.target.closest("[data-wg-meer-backdrop]")) {
            ev.preventDefault();
            setMeerOpen(false);
            return;
        }

        if (ev.target.closest("[data-wg-meer-panel] a[href]")) {
            setMeerOpen(false);
            return;
        }

        var panel = qs("[data-wg-meer-panel]");
        if (panel && !panel.hidden
            && !ev.target.closest("[data-wg-meer-panel]")
            && !ev.target.closest("[data-wg-meer-toggle]")) {
            setMeerOpen(false);
        }
    }

    function onKeyDown(ev) {
        if (ev.key === "Escape") {
            setMeerOpen(false);
        }
    }

    function init() {
        var nav = qs(".wg-bottom-nav");
        if (nav) {
            bindMeer(nav);
        }

        if (document.documentElement.getAttribute("data-wg-meer-ssr-bound") !== "1") {
            document.documentElement.setAttribute("data-wg-meer-ssr-bound", "1");
            document.body.addEventListener("click", onClick);
            document.addEventListener("keydown", onKeyDown);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    window.wgBottomNavSsr = { setMeerOpen: setMeerOpen, init: init };
})();
