(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-employer-outcome-ssr]");
  }

  function selectedOutcome(container) {
    var btn = container.querySelector("[data-ndm-employer-outcome].on");
    return btn ? btn.getAttribute("data-outcome-kind") : null;
  }

  function syncPreview(container) {
    var kind = selectedOutcome(container) || "NotNow";
    var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
    var reason = reasonEl ? reasonEl.value.trim() : "";

    container.querySelectorAll("[data-ndm-preview-pane]").forEach(function (pane) {
      var paneKind = pane.getAttribute("data-ndm-preview-pane");
      if (paneKind === kind) {
        pane.removeAttribute("hidden");
      } else {
        pane.setAttribute("hidden", "hidden");
      }
    });

    var quote = container.querySelector("[data-ndm-preview-quote-" + kind.toLowerCase() + "]");
    if (!quote) {
      quote = container.querySelector("[data-ndm-preview-quote-notnow]");
    }
    if (quote) {
      quote.textContent = reason ? '"' + reason + '"' : '""';
    }
  }

  function bindOutcomeOptions(container) {
    container.querySelectorAll("[data-ndm-employer-outcome]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        container.querySelectorAll("[data-ndm-employer-outcome]").forEach(function (other) {
          other.classList.remove("on");
        });
        btn.classList.add("on");
        syncPreview(container);
      });
    });
  }

  function bindReasonChips(container) {
    container.querySelectorAll("[data-ndm-outcome-chip]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        container.querySelectorAll("[data-ndm-outcome-chip]").forEach(function (other) {
          other.classList.remove("on");
        });
        chip.classList.add("on");
        var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
        if (reasonEl) {
          reasonEl.value = chip.textContent ? chip.textContent.trim() : "";
          syncPreview(container);
        }
      });
    });
  }

  function bindReasonInput(container) {
    var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
    if (!reasonEl) return;
    reasonEl.addEventListener("input", function () {
      syncPreview(container);
    });
  }

  function recordOutcome(container) {
    var appId = container.getAttribute("data-application-id");
    var outcome = selectedOutcome(container);
    var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
    var reason = reasonEl ? reasonEl.value.trim() : "";
    if (!appId || !outcome || !reason) return;

    var submit = container.querySelector("[data-ndm-outcome-submit]");
    if (submit) submit.disabled = true;

    fetch(
      "/api/ndm-employer-outcome-client/record-outcome?applicationId=" + encodeURIComponent(appId),
      {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
        body: JSON.stringify({ outcome: outcome, reason: reason }),
      }
    )
      .then(function (res) {
        if (!res.ok) throw new Error("fail");
        return res.json();
      })
      .then(function (data) {
        if (data && data.redirectUrl) {
          window.location.href = data.redirectUrl;
        } else {
          window.location.reload();
        }
      })
      .catch(function () {
        if (submit) submit.disabled = false;
      });
  }

  function bindSubmit(container) {
    var submit = container.querySelector("[data-ndm-outcome-submit]");
    if (!submit) return;
    submit.addEventListener("click", function () {
      recordOutcome(container);
    });
  }

  function init() {
    var el = root();
    if (!el) return;
    bindOutcomeOptions(el);
    bindReasonChips(el);
    bindReasonInput(el);
    bindSubmit(el);
    syncPreview(el);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
