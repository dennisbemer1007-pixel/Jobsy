(function () {
    "use strict";

    function dismissOnboardingCoach() {
        document.querySelectorAll(".lobsy-coach-dock--onboarding-b, .lobsy-coach-dock--variant-b").forEach(function (dock) {
            dock.classList.add("lobsy-coach-dock--bubble-dismissed");
        });
        var shell = document.getElementById("lobsy-assistant-ssr-shell");
        if (shell) shell.hidden = true;
        var panel = document.getElementById("lobsy-assistant-panel");
        if (panel) panel.hidden = true;
    }

    function syncStep7ConsentPayload(form) {
        var step = parseInt(form.getAttribute("data-ob-ssr-step") || "0", 10);
        if (step !== 7) return;
        var hidden = form.querySelector("[data-ob-ssr-payload]");
        if (!hidden) return;
        var box = form.querySelector('input[name="consentRequested"]');
        var payload = { consentRequested: !!(box && box.checked), answers: {} };
        try {
            if (hidden.value) {
                var prev = JSON.parse(hidden.value);
                if (prev && typeof prev === "object" && prev.answers) {
                    payload.answers = prev.answers;
                }
            }
        } catch (e) { /* ignore */ }
        hidden.value = JSON.stringify(payload);
    }

    function bindForm(form) {
        if (form.getAttribute("data-ob-v3-footer-bound") === "1") return;
        form.setAttribute("data-ob-v3-footer-bound", "1");

        form.addEventListener(
            "pointerdown",
            function (ev) {
                if (ev.button !== 0) return;
                var btn = ev.target.closest(
                    ".ob-v3-dfoot [data-testid=onboarding-back], .ob-v3-dfoot [data-testid=onboarding-next]"
                );
                if (!btn || !form.contains(btn)) return;
                dismissOnboardingCoach();
                if (
                    btn.getAttribute("data-testid") === "onboarding-next"
                    && String(btn.getAttribute("value") || btn.value || "").toLowerCase() === "next"
                ) {
                    syncStep7ConsentPayload(form);
                }
            },
            true
        );

        form.addEventListener(
            "submit",
            function (ev) {
                var submitter = ev.submitter;
                var isNext =
                    submitter &&
                    submitter.getAttribute("name") === "obNav" &&
                    String(submitter.getAttribute("value") || "").toLowerCase() === "next";
                if (isNext) {
                    syncStep7ConsentPayload(form);
                }
                dismissOnboardingCoach();
            },
            true
        );
    }

    function boot() {
        document.querySelectorAll("form[data-ob-ssr-form].ob-wizard--v3").forEach(bindForm);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }

    if (!window.__obV3FooterObs) {
        window.__obV3FooterObs = true;
        new MutationObserver(boot).observe(document.documentElement, { childList: true, subtree: true });
    }
})();