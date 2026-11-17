(function () {
  "use strict";

  function readToken() {
    var meta =
      document.querySelector('meta[name="jobsy-request-verification-token"]') ||
      document.querySelector('meta[name="request-verification-token"]');
    if (meta && meta.getAttribute("content")) {
      return meta.getAttribute("content");
    }

    var input = document.querySelector('input[name="__RequestVerificationToken"]');
    if (input && input.value) {
      return input.value;
    }

    var ob = document.querySelector("[data-ob-antiforgery-token]");
    if (ob && ob.getAttribute("data-ob-antiforgery-token")) {
      return ob.getAttribute("data-ob-antiforgery-token");
    }

    var saved = document.getElementById("saved-ssr-token");
    if (saved && saved.value) {
      return saved.value;
    }

    return "";
  }

  function mergeHeaders(headers) {
    var out = headers ? Object.assign({}, headers) : {};
    var token = readToken();
    if (token) {
      out.RequestVerificationToken = token;
    }
    if (!out["X-Requested-With"]) {
      out["X-Requested-With"] = "XMLHttpRequest";
    }
    return out;
  }

  window.JobsyBffSecurity = {
    readAntiforgeryToken: readToken,
    mergeHeaders: mergeHeaders,
    fetch: function (url, options) {
      options = options || {};
      options.credentials = options.credentials || "same-origin";
      options.headers = mergeHeaders(options.headers);
      return fetch(url, options);
    }
  };
})();