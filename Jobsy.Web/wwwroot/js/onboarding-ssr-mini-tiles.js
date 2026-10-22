(function () {
    "use strict";

    function ensureHiddenInput(card, qid, score) {
        var name = "miniAnswers[" + qid + "]";
        var input = card.querySelector('input[name="' + name + '"]');
        if (!input) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = name;
            card.appendChild(input);
        }
        input.value = String(score);
    }

    function selectLikertTile(btn) {
        var card = btn.closest(".ob-client-mini-q");
        if (!card) return;
        var qid = parseInt(btn.getAttribute("data-ob-mini-qid") || card.getAttribute("data-ob-qid"), 10);
        var score = parseInt(btn.getAttribute("data-ob-mini-score"), 10);
        if (!qid || !(score >= 1 && score <= 5)) return;

        card.querySelectorAll("[data-ob-mini-score]").forEach(function (tile) {
            var on = tile === btn;
            tile.classList.toggle("is-selected", on);
            tile.setAttribute("aria-pressed", on ? "true" : "false");
        });
        ensureHiddenInput(card, qid, score);
        return { qid: qid, score: score };
    }

    function collectMiniAnswers(form) {
        var answers = {};
        form.querySelectorAll('input[name^="miniAnswers["]').forEach(function (input) {
            var name = input.getAttribute("name") || "";
            var m = name.match(/^miniAnswers\[(\d+)\]$/);
            if (!m) return;
            var qid = parseInt(m[1], 10);
            var val = parseInt(input.value, 10);
            if (qid && val >= 1 && val <= 5) answers[qid] = val;
        });
        return answers;
    }

    function collectStep7Payload(form) {
        var payload = { answers: collectMiniAnswers(form) };
        var consentBox = form.querySelector('input[name="consentRequested"]');
        if (consentBox) payload.consentRequested = !!consentBox.checked;
        return payload;
    }

    function bindMiniForm(form) {
        if (form.getAttribute("data-ob-ssr-mini-bound") === "1") return;
        form.setAttribute("data-ob-ssr-mini-bound", "1");
        var step = parseInt(form.getAttribute("data-ob-ssr-step") || "0", 10);
        if (!step) return;

        form.querySelectorAll("[data-ob-ssr-mini]").forEach(function (miniRoot) {
            miniRoot.addEventListener("click", function (ev) {
                var btn = ev.target.closest("[data-ob-mini-score]");
                if (!btn || !miniRoot.contains(btn)) return;
                ev.preventDefault();
                selectLikertTile(btn);
                if (!window.jobsyOnboardingSsrSave) return;
                var payload = step === 7 ? collectStep7Payload(form) : { answers: collectMiniAnswers(form) };
                window.jobsyOnboardingSsrSave.putStep(form, step, payload).catch(function () { });
            });
        });

        var root = form.querySelector('[data-ob-client-step="' + step + '"]');
        if (root) root.setAttribute("data-ob-client-mounted", "1");
    }

    function boot() {
        document.querySelectorAll('form[data-ob-ssr-form][data-ob-ssr-step="7"], form[data-ob-ssr-form][data-ob-ssr-step="8"]').forEach(bindMiniForm);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
