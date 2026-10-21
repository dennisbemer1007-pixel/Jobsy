(function () {
    "use strict";

    function mount() {
        if (!window.jobsyBff) {
            return;
        }

        document.querySelectorAll("[data-profile-bff]").forEach(function (root) {
            var url = root.getAttribute("data-bff-url");
            if (!url) {
                return;
            }
            window.jobsyBff.wireAutosave(root, url, 550);
        });
    }

    function scheduleMount() {
        mount();
        var attempts = 0;
        var timer = setInterval(function () {
            mount();
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
