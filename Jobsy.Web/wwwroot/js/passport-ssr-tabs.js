(function () {
  "use strict";

  var TAB_ONLY_PARAMS = ["check", "section", "item", "saved", "error"];

  function normalizeTab(raw) {
    if (!raw) return "dna";
    var t = String(raw).trim().toLowerCase();
    if (t === "wie-ben-ik" || t === "profile" || t === "profiel") return "dna";
    if (t === "functiefit" || t === "match") return "fit";
    if (t === "carriere" || t === "carrière" || t === "loopbaan") return "career";
    if (t === "bewijzen" || t === "bewijs") return "proof";
    if (t === "gegevens") return "data";
    return t;
  }

  function stripPassportQueryNoise(u, tab) {
    TAB_ONLY_PARAMS.forEach(function (key) {
      u.searchParams.delete(key);
    });
    if (tab !== "fit") {
      u.searchParams.delete("check");
    }
    return u;
  }

  function buildUrl(tab) {
    var u = new URL(window.location.href);
    tab = normalizeTab(tab);
    stripPassportQueryNoise(u, tab);
    if (!tab || tab === "dna") {
      u.searchParams.delete("tab");
    } else {
      u.searchParams.set("tab", tab);
    }
    return u.pathname + u.search + u.hash;
  }

  function tabFromPaspoortHref(href) {
    if (!href) {
      return null;
    }
    try {
      var u = new URL(href, window.location.href);
      var path = u.pathname.replace(/\/+$/, "");
      if (path !== "/candidate/paspoort") {
        return null;
      }
      return normalizeTab(u.searchParams.get("tab"));
    } catch (_e) {
      return null;
    }
  }

  function setTab(root, tab, push) {
    tab = normalizeTab(tab);
    var tabs = root.querySelectorAll("[data-passport-tab]");
    var panels = root.querySelectorAll("[data-passport-panel]");
    tabs.forEach(function (btn) {
      var id = btn.getAttribute("data-passport-tab");
      var on = id === tab;
      btn.classList.toggle("passport-tab--active", on);
      btn.setAttribute("aria-selected", on ? "true" : "false");
      btn.tabIndex = on ? 0 : -1;
    });
    panels.forEach(function (panel) {
      var id = panel.getAttribute("data-passport-panel");
      panel.hidden = id !== tab;
    });
    var next = buildUrl(tab);
    if (push && window.history && window.history.pushState) {
      window.history.pushState({ passportTab: tab }, "", next);
    } else if (window.history && window.history.replaceState) {
      window.history.replaceState({ passportTab: tab }, "", next);
    }
    if (window.lobsyPaspoortLayout && window.lobsyPaspoortLayout.syncSidebarSticky) {
      window.lobsyPaspoortLayout.syncSidebarSticky();
    }
  }

  function tabOrder(root) {
    var list = root.querySelector("[data-passport-tab-order]");
    if (!list) return [];
    var raw = list.getAttribute("data-passport-tab-order") || "";
    return raw
      .split(",")
      .map(function (t) {
        return normalizeTab(t);
      })
      .filter(Boolean);
  }

  function neighborTab(root, current, delta) {
    var order = tabOrder(root);
    if (order.length === 0) return current;
    var idx = order.indexOf(normalizeTab(current));
    if (idx < 0) idx = 0;
    var next = (idx + delta + order.length) % order.length;
    return order[next];
  }

  function bindTabListKeys(root) {
    var tablist = root.querySelector("[data-passport-tab-order]");
    if (!tablist || tablist.getAttribute("data-passport-tab-keys") === "1") return;
    tablist.setAttribute("data-passport-tab-keys", "1");
    tablist.addEventListener("keydown", function (e) {
      var key = e.key;
      if (key !== "ArrowLeft" && key !== "ArrowRight" && key !== "Home" && key !== "End") return;
      var active = root.querySelector("[data-passport-tab][aria-selected=\"true\"]");
      if (!active || !tablist.contains(document.activeElement)) return;
      var current = active.getAttribute("data-passport-tab");
      var target = current;
      if (key === "ArrowLeft") target = neighborTab(root, current, -1);
      else if (key === "ArrowRight") target = neighborTab(root, current, 1);
      else if (key === "Home") {
        var order = tabOrder(root);
        target = order[0] || current;
      } else if (key === "End") {
        var endOrder = tabOrder(root);
        target = endOrder[endOrder.length - 1] || current;
      }
      if (target && target !== current) {
        e.preventDefault();
        setTab(root, target, true);
        var btn = root.querySelector('[data-passport-tab="' + target + '"]');
        if (btn) btn.focus();
      }
    });
  }

  function bindStoryToggle(root) {
    root.querySelectorAll("[data-passport-story-toggle]").forEach(function (btn) {
      if (btn.getAttribute("data-passport-story-bound") === "1") return;
      btn.setAttribute("data-passport-story-bound", "1");
      btn.addEventListener("click", function () {
        var block = root.querySelector("[data-passport-story]");
        if (!block) return;
        var expanded = block.classList.toggle("is-expanded");
        block.classList.toggle("is-clamped", !expanded);
        btn.textContent = expanded
          ? btn.getAttribute("data-label-less") || btn.textContent
          : btn.getAttribute("data-label-more") || btn.textContent;
      });
    });
  }

  function bindPaspoortTabLinks(root) {
    root.addEventListener(
      "click",
      function (e) {
        var link = e.target.closest("a[href]");
        if (!link || !root.contains(link)) {
          return;
        }
        if (link.getAttribute("data-passport-tab-link")) {
          return;
        }
        var tab = tabFromPaspoortHref(link.getAttribute("href"));
        if (!tab) {
          return;
        }
        e.preventDefault();
        e.stopPropagation();
        setTab(root, tab, true);
      },
      true
    );
  }

  function init() {
    var root = document.querySelector("[data-passport-ssr]");
    if (!root) return;

    var initial = normalizeTab(new URL(window.location.href).searchParams.get("tab"));
    setTab(root, initial, false);
    bindStoryToggle(root);
    bindTabListKeys(root);
    bindPaspoortTabLinks(root);

    root.querySelectorAll("[data-passport-tab]").forEach(function (btn) {
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        setTab(root, btn.getAttribute("data-passport-tab"), true);
      });
    });

    root.querySelectorAll("[data-passport-tab-link]").forEach(function (link) {
      link.addEventListener("click", function (e) {
        var tab = link.getAttribute("data-passport-tab-link");
        if (!tab) return;
        e.preventDefault();
        e.stopPropagation();
        setTab(root, tab, true);
      });
    });

    window.addEventListener("popstate", function () {
      var tab = normalizeTab(new URL(window.location.href).searchParams.get("tab"));
      setTab(root, tab, false);
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
