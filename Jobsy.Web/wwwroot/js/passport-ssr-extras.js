(function () {
  "use strict";

  function bindReadAloud(root) {
    root.querySelectorAll("[data-read-aloud-text]").forEach(function (btn) {
      if (btn.getAttribute("data-read-aloud-bound") === "1") return;
      btn.setAttribute("data-read-aloud-bound", "1");
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var text = btn.getAttribute("data-read-aloud-text") || "";
        if (!text || !window.lobsyReadAloud || typeof window.lobsyReadAloud.speak !== "function") return;
        var lang = document.documentElement.lang || "nl";
        window.lobsyReadAloud.speak(text, lang);
      });
    });
  }

  function bindRefTip(root) {
    root.querySelectorAll("[data-ref-verif-tip-dismiss]").forEach(function (btn) {
      if (btn.getAttribute("data-ref-tip-bound") === "1") return;
      btn.setAttribute("data-ref-tip-bound", "1");
      btn.addEventListener("click", function () {
        var key = btn.getAttribute("data-ref-verif-tip-dismiss") || "";
        if (key && window.jobsyRefVerificationTip) {
          window.jobsyRefVerificationTip.dismiss(key);
        }
        var tip = btn.closest(".ref-verif-tip");
        if (tip) tip.hidden = true;
      });
    });
  }

  function init() {
    var root = document.querySelector("[data-passport-ssr]");
    if (!root) return;
    bindReadAloud(root);
    bindRefTip(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
