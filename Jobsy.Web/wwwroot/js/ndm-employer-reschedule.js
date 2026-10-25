(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-employer-reschedule-ssr]");
  }

  function acceptSlot(container, slotId) {
    var appId = container.getAttribute("data-application-id");
    if (!appId || !slotId) return;

    var btn = container.querySelector('[data-ndm-employer-accept-slot][data-slot-id="' + slotId + '"]');
    if (btn) btn.disabled = true;

    fetch(
      "/api/ndm-employer-reschedule-client/accept-slot?applicationId=" + encodeURIComponent(appId),
      {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
        body: JSON.stringify({ slotId: slotId }),
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
        if (btn) btn.disabled = false;
      });
  }

  function bindAcceptButtons(container) {
    container.querySelectorAll("[data-ndm-employer-accept-slot]").forEach(function (btn) {
      if (btn.getAttribute("data-ndm-reschedule-bound") === "1") {
        return;
      }
      btn.setAttribute("data-ndm-reschedule-bound", "1");
      btn.addEventListener("click", function () {
        var slotId = btn.getAttribute("data-slot-id");
        acceptSlot(container, slotId);
      });
    });
  }

  function init() {
    var el = root();
    if (!el) return;
    bindAcceptButtons(el);
  }

  window.NdmEmployerRescheduleInit = init;

  function bindEnhancedNavRecovery() {
    if (window.__ndmEmployerRescheduleEnhancedBound) {
      return;
    }
    window.__ndmEmployerRescheduleEnhancedBound = true;
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
  }

  bindEnhancedNavRecovery();

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
