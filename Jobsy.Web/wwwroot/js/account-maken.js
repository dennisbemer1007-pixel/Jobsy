// Signup shell: reveal + focus e-mail field when arriving with #email (e.g. from gratis-dna CTA).
(function () {
    "use strict";

    function revealEmailForm() {
        var stage = document.querySelector(".pub-signup");
        if (stage) {
            stage.classList.add("pub-signup--email-focus");
        }

        var providers = document.querySelector(".pub-signup__providers");
        if (providers) {
            providers.setAttribute("hidden", "hidden");
        }

        var form = document.querySelector(".pub-signup__form, .pub-signup .login-form");
        if (form) {
            form.removeAttribute("hidden");
            form.setAttribute("aria-hidden", "false");
        }
    }

    function focusEmailIfHash() {
        if (window.location.hash !== "#email") {
            return;
        }

        revealEmailForm();

        var input = document.getElementById("email");
        if (!input) {
            return;
        }

        try {
            input.scrollIntoView({ block: "center", behavior: "smooth" });
        } catch (e) {
            try {
                input.scrollIntoView(true);
            } catch (e2) { }
        }

        if (typeof input.focus === "function") {
            window.setTimeout(function () {
                input.focus({ preventScroll: true });
            }, 120);
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", focusEmailIfHash);
    } else {
        focusEmailIfHash();
    }

    window.addEventListener("hashchange", focusEmailIfHash);
    document.addEventListener("enhancedload", focusEmailIfHash);
})();