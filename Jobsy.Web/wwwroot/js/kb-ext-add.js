(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-kb-ext-add-ssr]");
  }

  function setError(el, message) {
    if (!el) return;
    if (!message) {
      el.hidden = true;
      el.textContent = "";
      return;
    }
    el.hidden = false;
    el.textContent = message;
  }

  function bind() {
    var page = root();
    if (!page) return;

    var input = page.querySelector("[data-testid='kb-ext-url-input']");
    var btn = page.querySelector("[data-kb-ext-submit]");
    var err = page.querySelector("[data-kb-ext-error]");
    if (!input || !btn) return;

    var working = btn.getAttribute("data-kb-ext-working") || "…";
    var idle = btn.getAttribute("data-kb-ext-idle") || btn.textContent;

    btn.addEventListener("click", function () {
      if (btn.disabled) return;
      setError(err, null);
      var url = (input.value || "").trim();
      if (!url) {
        setError(err, btn.getAttribute("data-kb-ext-empty") || "Vul een link in.");
        return;
      }

      btn.disabled = true;
      btn.textContent = working;

      fetch("/api/kb-ext-client/import", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify({ url: url })
      })
        .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, body: j }; }); })
        .then(function (res) {
          if (res.body && res.body.redirect) {
            window.location.href = res.body.redirect;
            return;
          }
          var msg = (res.body && res.body.error) || btn.getAttribute("data-kb-ext-fail") || "Mislukt.";
          if (msg === "feature_disabled") {
            msg = btn.getAttribute("data-kb-ext-disabled") || msg;
          }
          setError(err, msg);
        })
        .catch(function () {
          setError(err, btn.getAttribute("data-kb-ext-fail") || "Mislukt.");
        })
        .finally(function () {
          btn.disabled = false;
          btn.textContent = idle;
        });
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", bind);
  } else {
    bind();
  }
})();
