(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-employer-ssr]");
  }

  function init() {
    var el = root();
    if (!el) return;
    el.querySelectorAll("a[href*='panel=']").forEach(function (link) {
      link.setAttribute("data-enhance-nav", "false");
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
