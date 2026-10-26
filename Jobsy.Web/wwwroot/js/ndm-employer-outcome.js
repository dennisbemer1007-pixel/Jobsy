(function () {
  "use strict";

  function root() {
    var page = document.querySelector(
      "[data-ndm-employer-outcome-ssr]:not([hidden]), [data-ndm-outcome-panel]:not([hidden])"
    );
    if (page) {
      return page;
    }
    return document.querySelector("[data-ndm-employer-outcome-ssr], [data-ndm-outcome-panel]");
  }

  function selectedOutcome(container) {
    var btn = container.querySelector("[data-ndm-employer-outcome].on");
    return btn ? btn.getAttribute("data-outcome-kind") : null;
  }

  function syncPreview(container) {
    var kind = selectedOutcome(container);
    if (!kind) {
      return;
    }
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

  function syncReasonChips(container, kind) {
    container.querySelectorAll("[data-ndm-outcome-chips]").forEach(function (row) {
      var rowKind = row.getAttribute("data-ndm-outcome-chips");
      var show = rowKind === kind || (kind === "NextRound" && rowKind === "Offer");
      row.style.display = show ? "" : "none";
    });
    var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
    if (reasonEl && kind === "NotNow") {
      /* keep typed reason */
    }
  }

  function bindOutcomeOptions(container) {
    container.querySelectorAll("[data-ndm-employer-outcome]").forEach(function (btn) {
      btn.addEventListener("click", function (ev) {
        ev.preventDefault();
        container.querySelectorAll("[data-ndm-employer-outcome]").forEach(function (other) {
          other.classList.remove("on");
        });
        btn.classList.add("on");
        var kind = btn.getAttribute("data-outcome-kind");
        syncReasonChips(container, kind);
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
    if (container.getAttribute("data-ndm-outcome-submitting") === "1") {
      return;
    }

    var appId = container.getAttribute("data-application-id");
    var outcome = selectedOutcome(container);
    var reasonEl = container.querySelector("[data-ndm-outcome-reason]");
    var reason = reasonEl ? reasonEl.value.trim() : "";
    if (!appId || !outcome || !reason) return;

    var submit = container.querySelector("[data-ndm-outcome-submit]");
    container.setAttribute("data-ndm-outcome-submitting", "1");
    window.__ndmEmployerOutcomeInFlight = appId;
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
        container.removeAttribute("data-ndm-outcome-submitting");
        if (window.__ndmEmployerOutcomeInFlight === appId) {
          window.__ndmEmployerOutcomeInFlight = null;
        }
        if (submit) submit.disabled = false;
      });
  }

  function bindSubmit(container) {
    var submit = container.querySelector("[data-ndm-outcome-submit]");
    if (!submit || submit.getAttribute("data-ndm-outcome-bound") === "1") return;
    submit.setAttribute("data-ndm-outcome-bound", "1");
    submit.addEventListener("click", function (ev) {
      ev.preventDefault();
      ev.stopImmediatePropagation();
      recordOutcome(container);
    });
  }

  function init() {
    var el = root();
    if (!el || el.getAttribute("data-ndm-outcome-init") === "1") return;
    el.setAttribute("data-ndm-outcome-init", "1");
    bindOutcomeOptions(el);
    bindReasonChips(el);
    bindReasonInput(el);
    bindSubmit(el);
    var kind = selectedOutcome(el);
    if (kind) {
      syncReasonChips(el, kind);
      syncPreview(el);
    }
  }

  function bindEnhancedNavRecovery() {
    function onNav() {
      init();
    }
    document.addEventListener("enhancedload", onNav);
    try {
      if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
        window.Blazor.addEventListener("enhancedload", onNav);
      }
    } catch (_e) {
      /* ignore */
    }
    if (window.__ndmEmployerOutcomeCaptureBound) {
      return;
    }
    window.__ndmEmployerOutcomeCaptureBound = true;
    document.addEventListener(
      "click",
      function (ev) {
        var btn = ev.target && ev.target.closest && ev.target.closest("[data-ndm-employer-outcome]");
        if (!btn || !root()) {
          return;
        }
        ev.preventDefault();
        ev.stopImmediatePropagation();
        var container = root();
        container.querySelectorAll("[data-ndm-employer-outcome]").forEach(function (other) {
          other.classList.remove("on");
        });
        btn.classList.add("on");
        var kind = btn.getAttribute("data-outcome-kind");
        syncReasonChips(container, kind);
        syncPreview(container);
      },
      true
    );
  }

  window.NdmEmployerOutcomeInit = init;
  bindEnhancedNavRecovery();

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
