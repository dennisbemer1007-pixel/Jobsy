(function () {
  "use strict";

  function bindAskLobsy(root) {
    var ask = root.querySelector("[data-ndm-ask-lobsy]");
    if (!ask) return;
    ask.addEventListener("click", function () {
      var coach = document.querySelector(".lobsy-coach-cta, .lobsy-coach-dock button, #lobsy-coach-btn");
      if (coach) {
        coach.click();
        return;
      }
      var fallback = ask.getAttribute("data-assistant-fallback");
      if (fallback) window.location.href = fallback;
    });
  }

  function boot() {
    document.querySelectorAll("[data-ndm-bevestiging-ssr], [data-ndm-herinneringen-ssr]").forEach(bindAskLobsy);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
