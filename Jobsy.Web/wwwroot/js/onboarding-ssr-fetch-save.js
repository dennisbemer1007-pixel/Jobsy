(function () {
    "use strict";

    function readAntiforgery(form) {
        return {
            header: form.getAttribute("data-ob-antiforgery-header") || "RequestVerificationToken",
            token: form.getAttribute("data-ob-antiforgery-token") || ""
        };
    }

    function putStep(form, step, body) {
        var af = readAntiforgery(form);
        var headers = { "Content-Type": "application/json" };
        if (af.token) headers[af.header] = af.token;
        body.advance = false;
        body.back = false;
        body.skipOptional = false;
        var url = step === 1 ? "/api/onboarding-client/step1" : "/api/onboarding-client/steps/" + step;
        return fetch(url, {
            method: "PUT",
            credentials: "same-origin",
            headers: headers,
            body: JSON.stringify(body)
        });
    }

    window.jobsyOnboardingSsrSave = {
        putStep: putStep
    };
})();
