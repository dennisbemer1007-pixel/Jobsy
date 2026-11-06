(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-outcome-ssr]");
  }

  function bindChips(container) {
    var chips = container.querySelectorAll("[data-ndm-outcome-chip]");
    chips.forEach(function (chip) {
      chip.addEventListener("click", function () {
        chip.classList.toggle("on");
      });
    });
  }

  function bindAssistant(container) {
    var btn = container.querySelector("[data-ndm-ask-lobsy]");
    if (!btn) return;
    btn.addEventListener("click", function () {
      var fallback = btn.getAttribute("data-assistant-fallback");
      if (fallback) {
        window.location.href = fallback;
      }
    });
  }

  function init() {
    var el = root();
    if (!el) return;
    bindChips(el);
    bindAssistant(el);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
