(function () {
    "use strict";

    function bindConsentForm(form) {
        if (form.getAttribute("data-ob-step7-consent-bound") === "1") return;
        form.setAttribute("data-ob-step7-consent-bound", "1");

        var next = form.querySelector('[data-testid="onboarding-next"]');
        if (next) {
            next.removeAttribute("data-ob-nav-value");
            next.setAttribute("formaction", "/candidate/start/step-7/consent-volgende");
            next.setAttribute("name", "obNav");
            next.setAttribute("value", "next");
            next.setAttribute("data-ob-ssr-nav-next", "1");
        }

        var stepField = form.querySelector('input[name="step"]');
        if (!stepField) {
            stepField = document.createElement("input");
            stepField.type = "hidden";
            stepField.name = "step";
            stepField.value = "7";
            form.appendChild(stepField);
        }
    }

    function boot() {
        document.querySelectorAll('form[data-ob-ssr-form][data-ob-ssr-step="7"]').forEach(function (form) {
            var consent = form.querySelector("[data-testid=test-journey-consent], .ob-v3-consent");
            if (!consent) return;
            bindConsentForm(form);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();