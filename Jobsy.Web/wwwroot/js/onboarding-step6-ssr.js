(function () {
    "use strict";

    function bindStep6Form(form) {
        if (form.getAttribute("data-ob-step6-bound") === "1") return;
        form.setAttribute("data-ob-step6-bound", "1");

        var input = form.querySelector('input[name="dream"]');
        var grid = form.querySelector("[data-ob-ssr-dream-grid]");

        function deselectTiles() {
            if (!grid) return;
            grid.querySelectorAll(".ob-rustig__dream, .ob-wizard__dream-grid button").forEach(function (btn) {
                btn.classList.remove("is-on", "is-selected");
                btn.setAttribute("aria-pressed", "false");
            });
        }

        function selectDreamTile(btn) {
            var title = btn.getAttribute("data-ob-dream-title") || "";
            if (input) input.value = title;
            deselectTiles();
            btn.classList.add("is-on", "is-selected");
            btn.setAttribute("aria-pressed", "true");
            return title.trim();
        }

        function saveDream() {
            if (!window.jobsyOnboardingSsrSave || !input) return;
            var val = input.value.trim();
            window.jobsyOnboardingSsrSave.putStep(form, 6, { dream: val, dreamSearch: val }).catch(function () { });
        }

        form.addEventListener(
            "submit",
            function () {
                form.setAttribute("data-ob-step6-submitting", "1");
            },
            true
        );

        if (grid) {
            grid.addEventListener("click", function (ev) {
                var btn = ev.target.closest(".ob-rustig__dream[data-ob-dream-title], .ob-wizard__dream-grid button[data-ob-dream-title]");
                if (!btn || !grid.contains(btn)) return;
                ev.preventDefault();
                selectDreamTile(btn);
                saveDream();
            });
        }

        if (input) {
            input.addEventListener("input", deselectTiles);
            input.addEventListener("blur", function () {
                if (form.getAttribute("data-ob-step6-submitting") === "1") return;
                saveDream();
            });
        }

        var root = form.querySelector('[data-ob-client-step="6"]');
        if (root) root.setAttribute("data-ob-client-mounted", "1");

        var next = form.querySelector('[data-testid="onboarding-next"]');
        if (next) {
            next.removeAttribute("data-ob-nav-value");
            next.setAttribute("formaction", "/candidate/start/step-6/volgende");
            next.setAttribute("name", "obNav");
            next.setAttribute("value", "next");
            next.setAttribute("data-ob-ssr-nav-next", "1");
        }

        function pinDreamUnknownNav() {
            form.querySelectorAll(
                '[data-testid="onboarding-dream-unknown"], [data-testid="onboarding-dream-unknown-mobile"]'
            ).forEach(function (btn) {
                btn.setAttribute("type", "submit");
                btn.setAttribute("name", "obNav");
                btn.setAttribute("value", "dreamUnknown");
                btn.setAttribute("formaction", "/candidate/start/step-6/volgende");
                btn.setAttribute("formnovalidate", "formnovalidate");
            });
        }

        function bindDreamUnknownSubmit() {
            form.querySelectorAll(
                '[data-testid="onboarding-dream-unknown"], [data-testid="onboarding-dream-unknown-mobile"]'
            ).forEach(function (btn) {
                if (btn.getAttribute("data-ob-dream-unknown-bound") === "1") return;
                btn.setAttribute("data-ob-dream-unknown-bound", "1");
                btn.addEventListener(
                    "click",
                    function (ev) {
                        ev.preventDefault();
                        ev.stopPropagation();
                        var tokenEl = form.querySelector('input[name="__RequestVerificationToken"]');
                        var token = tokenEl ? tokenEl.value : "";
                        if (!token) return;
                        var body = new URLSearchParams();
                        body.set("__RequestVerificationToken", token);
                        body.set("obNav", "dreamUnknown");
                        fetch("/candidate/start/step-6/volgende", {
                            method: "POST",
                            credentials: "same-origin",
                            headers: { "Content-Type": "application/x-www-form-urlencoded" },
                            body: body.toString(),
                            redirect: "follow",
                        })
                            .then(function (res) {
                                var target = res.url || "/candidate/start?step=7";
                                if (target.indexOf("step=7") >= 0) window.location.assign(target);
                                else window.location.reload();
                            })
                            .catch(function () {
                                window.location.assign("/candidate/start?step=6&obError="
                                    + encodeURIComponent("Probeer het nog eens"));
                            });
                    },
                    true
                );
            });
        }

        pinDreamUnknownNav();
        bindDreamUnknownSubmit();
        requestAnimationFrame(function () {
            pinDreamUnknownNav();
            bindDreamUnknownSubmit();
        });
        setTimeout(function () {
            pinDreamUnknownNav();
            bindDreamUnknownSubmit();
        }, 0);
        setTimeout(function () {
            pinDreamUnknownNav();
            bindDreamUnknownSubmit();
        }, 150);

        var stepField = form.querySelector('input[name="step"]');
        if (!stepField) {
            stepField = document.createElement("input");
            stepField.type = "hidden";
            stepField.name = "step";
            stepField.value = "6";
            form.appendChild(stepField);
        }
    }

    function boot() {
        document.querySelectorAll('form[data-ob-ssr-form][data-ob-ssr-step="6"]').forEach(bindStep6Form);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
