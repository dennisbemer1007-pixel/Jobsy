window.jobsyExtras = (function () {
    "use strict";

    var extrasSrc = "/js/app-extras.js?v=20260820-r164";
    var feedbackSrc = "/js/feedback.js?v=20260820-r164";
    var pending = {};

    function loadScript(src, ready) {
        if (ready()) {
            return Promise.resolve();
        }
        if (pending[src]) {
            return pending[src];
        }
        pending[src] = new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.async = true;
            script.onload = function () { resolve(); };
            script.onerror = function () {
                pending[src] = null;
                reject(new Error("Failed to load " + src));
            };
            document.head.appendChild(script);
        });
        return pending[src];
    }

    function ensure() {
        return loadScript(extrasSrc, function () {
            return !!(window.lobsySessionIdle && window.jobsyDownload && window.jobsyRichtext);
        });
    }

    window.lobsyFeedbackEnsure = function () {
        return loadScript(feedbackSrc, function () {
            return !!window.lobsyFeedback;
        });
    };

    return { ensure: ensure };
})();
