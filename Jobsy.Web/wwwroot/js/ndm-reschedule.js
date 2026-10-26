(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-reschedule-ssr]");
  }

  function setVisible(el, visible) {
    if (!el) return;
    el.hidden = !visible;
    el.classList.toggle("ndm-reschedule-is-hidden", !visible);
    el.setAttribute("aria-hidden", visible ? "false" : "true");
  }

  function setMode(container, mode) {
    var next = mode === "cancel" ? "cancel" : "reschedule";
    container.setAttribute("data-active-mode", next);

    container.querySelectorAll("[data-ndm-mode-chip]").forEach(function (chip) {
      var on = chip.getAttribute("data-ndm-mode-chip") === next;
      chip.classList.toggle("on", on);
      chip.setAttribute("aria-pressed", on ? "true" : "false");
    });

    setVisible(container.querySelector("[data-ndm-when-block]"), next !== "cancel");
    setVisible(container.querySelector("[data-ndm-submit-reschedule]"), next === "reschedule");
    setVisible(container.querySelector("[data-ndm-submit-cancel]"), next === "cancel");
    setVisible(container.querySelector("[data-ndm-side-title-reschedule]"), next === "reschedule");
    setVisible(container.querySelector("[data-ndm-side-title-cancel]"), next === "cancel");

    try {
      var url = new URL(window.location.href);
      url.searchParams.set("panel", next);
      window.history.replaceState({}, "", url.pathname + url.search + url.hash);
    } catch (_e) {
      /* ignore */
    }
  }

  function navigateBack(container) {
    var href = container.getAttribute("data-back-href");
    if (href) {
      window.location.href = href;
    }
  }

  function postJson(path, appId, reason) {
    return fetch(path + "?applicationId=" + encodeURIComponent(appId), {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
      },
      body: JSON.stringify({ reason: reason }),
    });
  }

  function bindReasonChips(container) {
    var textarea = container.querySelector("[data-ndm-reason]");
    if (!textarea) return;
    container.querySelectorAll("[data-ndm-reason-chip]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        container.querySelectorAll("[data-ndm-reason-chip]").forEach(function (c) {
          c.classList.remove("on");
        });
        chip.classList.add("on");
        var text = chip.getAttribute("data-ndm-reason-chip") || chip.textContent || "";
        textarea.value = text.trim();
        textarea.focus();
      });
    });
  }

  function bindSubmit(container) {
    var appId = container.getAttribute("data-application-id");
    if (!appId) return;
    var textarea = container.querySelector("[data-ndm-reason]");
    var backHref = container.getAttribute("data-back-href");
    var applicationsHref = container.getAttribute("data-applications-href");

    function reasonOrAlert() {
      if (!textarea) return "";
      var value = (textarea.value || "").trim();
      if (!value) {
        textarea.focus();
      }
      return value;
    }

    var rescheduleBtn = container.querySelector("[data-ndm-submit-reschedule]");
    if (rescheduleBtn) {
      rescheduleBtn.addEventListener("click", function () {
        var reason = reasonOrAlert();
        if (!reason) return;
        rescheduleBtn.disabled = true;
        postJson("/api/ndm-reschedule-client/reschedule", appId, reason)
          .then(function (res) {
            if (!res.ok) throw new Error("fail");
            window.location.href = backHref || applicationsHref || window.location.pathname;
          })
          .catch(function () {
            rescheduleBtn.disabled = false;
          });
      });
    }

    var cancelBtn = container.querySelector("[data-ndm-submit-cancel]");
    if (cancelBtn) {
      cancelBtn.addEventListener("click", function () {
        var reason = reasonOrAlert();
        if (!reason) return;
        cancelBtn.disabled = true;
        postJson("/api/ndm-reschedule-client/cancel", appId, reason)
          .then(function (res) {
            if (!res.ok) throw new Error("fail");
            return res.json();
          })
          .then(function (data) {
            window.location.href =
              (data && data.redirectUrl) ||
              applicationsHref ||
              backHref ||
              window.location.pathname;
          })
          .catch(function () {
            cancelBtn.disabled = false;
          });
      });
    }
  }

  function init() {
    var container = root();
    if (!container || container.getAttribute("data-ndm-reschedule-bound") === "1") return;
    container.setAttribute("data-ndm-reschedule-bound", "1");

    var initial = container.getAttribute("data-initial-mode") || "reschedule";
    setMode(container, initial);
    bindReasonChips(container);
    bindSubmit(container);

    container.querySelectorAll("[data-ndm-mode-chip]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        setMode(container, chip.getAttribute("data-ndm-mode-chip"));
      });
    });

    var closeBtn = container.querySelector("[data-ndm-reschedule-close]");
    if (closeBtn) {
      closeBtn.addEventListener("click", function () {
        navigateBack(container);
      });
    }

    var never = container.querySelector("[data-ndm-reschedule-nevermind]");
    if (never) {
      never.addEventListener("click", function () {
        navigateBack(container);
      });
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
