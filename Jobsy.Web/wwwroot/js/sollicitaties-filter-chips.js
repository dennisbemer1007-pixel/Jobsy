(function () {
    "use strict";

    function scrollSelectedFilterChip() {
        if (!window.matchMedia("(max-width: 639px)").matches) {
            return;
        }

        var row = document.querySelector(".sollicitaties-toolbar__filters");
        if (!row) {
            return;
        }

        var selected = row.querySelector(".sollicitaties-chip.is-selected");
        if (!selected) {
            return;
        }

        try {
            selected.scrollIntoView({ inline: "center", block: "nearest", behavior: "instant" });
        } catch (_err) {
            selected.scrollIntoView(true);
        }
    }

    function scheduleScroll() {
        requestAnimationFrame(scrollSelectedFilterChip);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", scheduleScroll);
    } else {
        scheduleScroll();
    }
})();
