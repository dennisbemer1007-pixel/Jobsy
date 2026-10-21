(function () {
    "use strict";

    function mount() {
        if (!window.jobsyBff) {
            return;
        }

        document.querySelectorAll("[data-cand-mail-settings]").forEach(function (root) {
            var url = root.getAttribute("data-bff-url");
            if (!url) {
                return;
            }
            window.jobsyBff.wireAutosave(root, url, 650);
            root.addEventListener("input", function () {
                var status = root.querySelector("[data-mail-save-status]");
                if (status) {
                    status.textContent = "";
                }
            });
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
