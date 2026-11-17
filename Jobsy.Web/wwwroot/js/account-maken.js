// Signup shell: focus e-mail field when arriving with #email (e.g. from gratis-dna CTA).
(function () {
    "use strict";

    function focusEmailIfHash() {
        if (window.location.hash !== "#email") {
            return;
        }

        var input = document.getElementById("email");
        if (input && typeof input.focus === "function") {
            input.focus({ preventScroll: false });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", focusEmailIfHash);
    } else {
        focusEmailIfHash();
    }

    document.addEventListener("enhancedload", focusEmailIfHash);
})();