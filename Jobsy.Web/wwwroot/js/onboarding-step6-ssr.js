(function () {
    "use strict";

    function bindStep6Form(form) {
        if (form.getAttribute("data-ob-step6-bound") === "1") return;
        form.setAttribute("data-ob-step6-bound", "1");

        var input = form.querySelector('input[name="dream"]');
        var grid = form.querySelector("[data-ob-ssr-dream-grid]");
        var saveBtn = form.querySelector("[data-ob-step6-save]");

        function deselectTiles() {
            if (!grid) return;
            grid.querySelectorAll(".ob-rustig__dream").forEach(function (btn) {
                btn.classList.remove("is-on", "is-selected");
                btn.setAttribute("aria-pressed", "false");
            });
        }

        form.addEventListener(
            "submit",
            function () {
                form.setAttribute("data-ob-step6-submitting", "1");
            },
            true
        );

        if (input) {
            input.addEventListener("input", deselectTiles);

            function requestSave() {
                if (form.getAttribute("data-ob-step6-submitting") === "1") return;
                if (!saveBtn) return;
                saveBtn.click();
            }

            input.addEventListener("blur", function () {
                if (form.getAttribute("data-ob-step6-submitting") === "1") return;
                requestSave();
            });
        }

        var root = form.querySelector('[data-ob-client-step="6"]');
        if (root) {
            root.setAttribute("data-ob-client-mounted", "1");
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
