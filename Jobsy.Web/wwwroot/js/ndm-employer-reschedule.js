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

  function init() {
    var el = root();
    if (!el) return;

    el.querySelectorAll("[data-ndm-employer-accept-slot]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var slotId = btn.getAttribute("data-slot-id");
        acceptSlot(el, slotId);
      });
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
