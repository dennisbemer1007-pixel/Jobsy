(function () {
    "use strict";

    var KEY = "Jobsy.CookieConsent";

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
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            return (window.jobsyCookieConsent.get() || "").toLowerCase() === "analytics";
        }
    };
})();
