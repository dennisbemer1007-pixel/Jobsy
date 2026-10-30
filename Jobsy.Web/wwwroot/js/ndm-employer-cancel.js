(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-employer-cancel-ssr]");
  }

  function cancelScope() {
    return document.querySelector("[data-ndm-cancel-panel]") || root();
  }

  function init() {
    var page = root();
    if (!page) return;

    var scope = cancelScope();
    var appId = page.getAttribute("data-application-id");
    var textarea = scope.querySelector("[data-ndm-cancel-reason]");
    var submit = scope.querySelector("[data-ndm-employer-cancel-submit]");

    scope.querySelectorAll("[data-ndm-reason-chip]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        scope.querySelectorAll("[data-ndm-reason-chip]").forEach(function (c) {
          c.classList.remove("on");
        });
        chip.classList.add("on");
        if (textarea) {
          textarea.value = chip.getAttribute("data-ndm-reason-chip") || chip.textContent || "";
          textarea.focus();
        }
      });
    });

    if (!submit || !appId) return;

    submit.addEventListener("click", function () {
      var reason = textarea ? (textarea.value || "").trim() : "";
      if (!reason) {
        if (textarea) textarea.focus();
        return;
      }

      submit.disabled = true;
      fetch(
        "/api/ndm-employer-cancel-client/cancel?applicationId=" + encodeURIComponent(appId),
        {
          method: "POST",
          credentials: "same-origin",
          headers: {
            "Content-Type": "application/json",
            "X-Requested-With": "XMLHttpRequest",
          },
          body: JSON.stringify({ reason: reason }),
        }
      )
        .then(function (res) {
          if (!res.ok) throw new Error("fail");
          return res.json();
        })
        .then(function (data) {
          window.location.href =
            (data && data.redirectUrl) ||
            page.getAttribute("data-back-href") ||
            window.location.pathname;
        })
        .catch(function () {
          submit.disabled = false;
        });
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
