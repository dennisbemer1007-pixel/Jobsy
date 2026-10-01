// Public / legal pages progressive enhancement (public-pages 02): table-of-contents
// scroll-spy and the print button. The pages work without this file.
(function () {
    "use strict";

    var SPY_LINK = "[data-pp-toc-link]";
    var current = null;

    function markJs() {
        document.documentElement.classList.add("pp-js");
    }

    function links() {
        return Array.prototype.slice.call(document.querySelectorAll(SPY_LINK));
    }

    function setCurrent(id) {
        if (id === current) {
            return;
        }

        current = id;
        links().forEach(function (link) {
            var href = link.getAttribute("href") || "";
            if (href === "#" + id) {
                link.setAttribute("aria-current", "location");
            } else {
                link.removeAttribute("aria-current");
            }
        });
    }

    function startSpy() {
        var all = links();
        if (!all.length || typeof IntersectionObserver !== "function") {
            return;
        }

        var sections = all
            .map(function (link) {
                var href = link.getAttribute("href") || "";
                return href.charAt(0) === "#" ? document.getElementById(href.slice(1)) : null;
            })
            .filter(Boolean);

        if (!sections.length) {
            return;
        }

        var visible = {};
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                visible[entry.target.id] = entry.isIntersecting;
            });

            for (var i = 0; i < sections.length; i++) {
                if (visible[sections[i].id]) {
                    setCurrent(sections[i].id);
                    return;
                }
            }
        }, { rootMargin: "-20% 0px -70% 0px", threshold: 0 });

        sections.forEach(function (section) {
            observer.observe(section);
        });
    }

    function onClick(ev) {
        var target = ev.target;
        if (!target || !target.closest) {
            return;
        }

        if (target.closest("[data-pp-print]")) {
            ev.preventDefault();
            window.print();
        }
    }

    function init() {
        if (!document.querySelector(SPY_LINK) && !document.querySelector("[data-pp-print]")) {
            return;
        }

        markJs();
        startSpy();
    }

    document.addEventListener("click", onClick);

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    // Enhanced navigation swaps the document body; re-run after each load.
    document.addEventListener("enhancedload", init);

    // Mijn gegevens (07): after account deletion the dialog's circuit asks us to submit the
    // hidden POST logout form instead of a GET navigation (keeps the antiforgery POST path).
    window.lobsyPublicPages = window.lobsyPublicPages || {};
    window.lobsyPublicPages.submitForm = function (id) {
        var form = document.getElementById(id);
        if (form && typeof form.submit === "function") {
            form.submit();
        }
    };
})();
