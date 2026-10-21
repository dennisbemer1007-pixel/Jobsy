(function () {
    "use strict";

    function syncLayout() {
        try {
            if (window.lobsyPaspoortLayout?.syncSidebarSticky) window.lobsyPaspoortLayout.syncSidebarSticky();
            if (window.lobsyCandidateNav?.syncAppHeaderHeight) window.lobsyCandidateNav.syncAppHeaderHeight();
        } catch (e) {}
    }

    function mountMotivation() {
        if (!window.jobsyBff) {
            return;
        }

        document.querySelectorAll("[data-passport-motivation-bff]").forEach(function (root) {
            var url = root.getAttribute("data-bff-url");
            if (url) {
                window.jobsyBff.wireAutosave(root, url, 700);
            }
        });
    }

    function scheduleMount() {
        syncLayout();
        mountMotivation();
        var attempts = 0;
        var timer = setInterval(function () {
            syncLayout();
            mountMotivation();
            attempts += 1;
            if (attempts >= 60) {
                clearInterval(timer);
            }
        }, 500);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", scheduleMount);
    } else {
        scheduleMount();
    }
})();
