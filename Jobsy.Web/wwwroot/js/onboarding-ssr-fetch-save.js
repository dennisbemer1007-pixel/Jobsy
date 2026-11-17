(function () {
    "use strict";

    function readAntiforgery(form) {
        var header = form.getAttribute("data-ob-antiforgery-header") || "RequestVerificationToken";
        var token = form.getAttribute("data-ob-antiforgery-token") || "";
        if (!token && window.JobsyBffSecurity && typeof window.JobsyBffSecurity.readAntiforgeryToken === "function") {
            token = window.JobsyBffSecurity.readAntiforgeryToken();
        }
        if (!token) {
            var meta =
                document.querySelector('meta[name="jobsy-request-verification-token"]') ||
                document.querySelector('meta[name="request-verification-token"]');
            if (meta && meta.getAttribute("content")) {
                token = meta.getAttribute("content");
            }
        }
        if (!token) {
            var hidden = document.querySelector('input[name="__RequestVerificationToken"]');
            if (hidden && hidden.value) {
                token = hidden.value;
            }
        }
        return { header: header, token: token };
    }

    function putStep(form, step, body) {
        var af = readAntiforgery(form);
        var headers = { "Content-Type": "application/json" };
        if (af.token) headers[af.header] = af.token;
        if (body.advance !== true) {
            body.advance = false;
        }
        if (body.back !== true) {
            body.back = false;
        }
        if (body.skipOptional !== true) {
            body.skipOptional = false;
        }
        var url = step === 1 ? "/api/onboarding-client/step1" : "/api/onboarding-client/steps/" + step;
        var options = {
            method: "PUT",
            credentials: "same-origin",
            headers: headers,
            body: JSON.stringify(body)
        };
        if (window.JobsyBffSecurity && typeof window.JobsyBffSecurity.fetch === "function") {
            return window.JobsyBffSecurity.fetch(url, options);
        }
        return fetch(url, options);
    }

    window.jobsyOnboardingSsrSave = {
        putStep: putStep
    };
})();
