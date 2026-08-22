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
                var maxAge = 60 * 60 * 24 * 365;
                document.cookie = KEY + "=" + encodeURIComponent(value || "necessary")
                    + "; Path=/; SameSite=Lax; Max-Age=" + maxAge
                    + (location.protocol === "https:" ? "; Secure" : "");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            var value = (window.jobsyCookieConsent.get() || "").toLowerCase();
            return value === "analytics" || value.indexOf("analytics.") === 0;
        }
    };

    window.jobsyViewport = {
        isWide: function () {
            return window.matchMedia("(min-width: 769px)").matches;
        }
    };

    applyKnownClass();
})();
