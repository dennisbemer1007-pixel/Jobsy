(function () {
    "use strict";

    var KEY = "Jobsy.CookieConsent";

    function applyKnownClass() {
        try {
            var known = !!(localStorage.getItem(KEY) || "");
            document.documentElement.classList.toggle("cookie-consent-known", known);
        } catch (e) {
            // private mode / blocked storage — show the banner
        }
    }

    window.jobsyCookieConsent = {
        get: function () {
            try {
                return localStorage.getItem(KEY) || "";
            } catch (e) {
                return "";
            }
        },
        set: function (value) {
            try {
                localStorage.setItem(KEY, value || "necessary");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            return (window.jobsyCookieConsent.get() || "").toLowerCase() === "analytics";
        }
    };

    window.jobsyViewport = {
        isWide: function () {
            return window.matchMedia("(min-width: 769px)").matches;
        }
    };

    applyKnownClass();
})();
