(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-bedrijvenkaart-ssr=\"1\"]");
  }

  function isDesktopSplit() {
    return window.matchMedia("(min-width: 901px)").matches;
  }

  function setMobileView(mode) {
    var el = root();
    if (!el || isDesktopSplit()) {
      return;
    }
    var view = mode === "list" ? "list" : "map";
    el.setAttribute("data-bk-mobile-view", view);
    el.querySelectorAll("[data-bk-view-map]").forEach(function (btn) {
      var on = view === "map";
      btn.classList.toggle("is-active", on);
      btn.setAttribute("aria-checked", on ? "true" : "false");
    });
    el.querySelectorAll("[data-bk-view-list]").forEach(function (btn) {
      var on = view === "list";
      btn.classList.toggle("is-active", on);
      btn.setAttribute("aria-checked", on ? "true" : "false");
    });
  }

  function bind() {
    if (document.documentElement.getAttribute("data-bk-ssr-layout-bound") === "1") {
      return;
    }
    document.documentElement.setAttribute("data-bk-ssr-layout-bound", "1");

    document.body.addEventListener("click", function (ev) {
      var host = ev.target.closest("[data-bedrijvenkaart-ssr=\"1\"]");
      if (!host || isDesktopSplit()) {
        return;
      }
      if (ev.target.closest("[data-bk-view-map]")) {
        ev.preventDefault();
        setMobileView("map");
      } else if (ev.target.closest("[data-bk-view-list]")) {
        ev.preventDefault();
        setMobileView("list");
      }
    });

    window.addEventListener("resize", function () {
      var el = root();
      if (!el) {
        return;
      }
      if (isDesktopSplit()) {
        el.removeAttribute("data-bk-mobile-view");
      } else if (!el.getAttribute("data-bk-mobile-view")) {
        el.setAttribute("data-bk-mobile-view", "map");
      }
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", bind);
  } else {
    bind();
  }
})();
