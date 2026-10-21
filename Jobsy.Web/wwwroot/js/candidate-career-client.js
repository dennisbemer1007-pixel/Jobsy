(function () {
    "use strict";

    function mount() {
        if (!window.jobsyBff) {
            return;
        }

        document.querySelectorAll("[data-career-step-actions]").forEach(function (root) {
            root.querySelectorAll("[data-career-bff-url]").forEach(function (btn) {
                if (btn.__careerBff) {
                    return;
                }
                btn.__careerBff = true;
                btn.addEventListener("click", function () {
                    if (btn.disabled) {
                        return;
                    }
                    btn.disabled = true;
                    var url = btn.getAttribute("data-career-bff-url") || "";
                    var fd = window.jobsyBff.formFromContainer(root);
                    window.jobsyBff.postForm(url, fd, root)
                        .catch(function () { })
                        .finally(function () {
                            btn.disabled = false;
                        });
                });
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
