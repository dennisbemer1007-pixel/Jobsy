(function () {
    "use strict";

    function bindStep1Form(form) {
        if (form.getAttribute("data-ob-step1-bound") === "1") return;
        form.setAttribute("data-ob-step1-bound", "1");

        var next = form.querySelector('[data-testid="onboarding-next"]');
        if (next) {
            next.removeAttribute("data-ob-nav-value");
            next.setAttribute("formaction", "/candidate/start/step-1/volgende");
            next.setAttribute("name", "obNav");
            next.setAttribute("value", "next");
            next.setAttribute("data-ob-ssr-nav-next", "1");
        }

        var stepField = form.querySelector('input[name="step"]');
        if (!stepField) {
            stepField = document.createElement("input");
            stepField.type = "hidden";
            stepField.name = "step";
            stepField.value = "1";
            form.appendChild(stepField);
        }

        function syncStep1Fields() {
            var root = form.querySelector('[data-ob-client-step="1"]');
            if (!root) return;
            var workField = root.querySelector("[data-ob-ssr-work-statuses]");
            if (workField) {
                var codes = [];
                root.querySelectorAll(".work-status-chip[data-ob-code]").forEach(function (btn) {
                    if (btn.getAttribute("aria-pressed") === "true") {
                        codes.push(btn.getAttribute("data-ob-code"));
                    }
                });
                workField.value = codes.join(",");
            }
            if (typeof root.__obSyncStep1FormFields === "function") {
                root.__obSyncStep1FormFields();
            }
        }

        form.addEventListener("submit", syncStep1Fields, true);
        var next = form.querySelector('[data-testid="onboarding-next"]');
        if (next) {
            next.addEventListener("click", syncStep1Fields, true);
        }
    }

    function boot() {
        document.querySelectorAll('form[data-ob-ssr-form][data-ob-ssr-step="1"]').forEach(bindStep1Form);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();