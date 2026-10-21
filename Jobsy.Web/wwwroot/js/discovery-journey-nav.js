(function () {
    "use strict";

    function collectClient(step) {
        if (step === 1 && window.jobsyOnboardingClient) {
            return window.jobsyOnboardingClient.collect(1);
        }
        if (step >= 2 && step <= 6 && window.jobsyDiscoveryClient) {
            return window.jobsyDiscoveryClient.collect(step);
        }
        return "{}";
    }

    function mount() {
        if (document.__discoveryNextSubmitMounted) {
            return;
        }

        document.__discoveryNextSubmitMounted = true;
        document.addEventListener("submit", function (ev) {
            var form = ev.target;
            if (!form || !form.matches || !form.matches("[data-discovery-next-form]")) {
                return;
            }

            var stepInput = form.querySelector("input[name='step']");
            var jsonInput = form.querySelector("[data-discovery-client-json]");
            if (!stepInput || !jsonInput) {
                return;
            }

            var step = parseInt(stepInput.value || "1", 10);
            jsonInput.value = collectClient(step);
        }, true);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", mount);
    } else {
        mount();
    }
})();
