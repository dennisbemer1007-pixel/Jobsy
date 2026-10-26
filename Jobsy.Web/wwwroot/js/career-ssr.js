(function () {
  "use strict";

  var TAB_KEYS = ["voor-jou", "omscholen", "kansberoepen"];

  function pageRoot() {
    return document.querySelector("[data-testid=\"career-page-ssr\"]");
  }

  function normalizeTab(raw) {
    if (TAB_KEYS.indexOf(raw) >= 0) {
      return raw;
    }
    return "voor-jou";
  }

  function parseTabFromUrl() {
    try {
      var u = new URL(window.location.href);
      return normalizeTab(u.searchParams.get("tab"));
    } catch (_e) {
      return "voor-jou";
    }
  }

  function buildUrl(tab, meer) {
    var u = new URL(window.location.href);
    if (!tab || tab === "voor-jou") {
      u.searchParams.delete("tab");
    } else {
      u.searchParams.set("tab", tab);
    }
    if (meer && meer.length) {
      u.searchParams.set("meer", meer.join(","));
    } else {
      u.searchParams.delete("meer");
    }
    u.searchParams.delete("saved");
    u.searchParams.delete("error");
    return u.pathname + u.search + u.hash;
  }

  function parseMeerFromUrl() {
    try {
      var u = new URL(window.location.href);
      var raw = u.searchParams.get("meer");
      if (!raw) {
        return [];
      }
      return raw.split(",").map(function (s) {
        return s.trim().toLowerCase();
      }).filter(Boolean);
    } catch (_e2) {
      return [];
    }
  }

  function stripForTab(tab) {
    return tab === "omscholen" ? "omscholen" : "carriere";
  }

  function applyTab(page, tab, push) {
    tab = normalizeTab(tab);
    page.setAttribute("data-career-active-tab", tab);

    page.querySelectorAll("[data-career-tab]").forEach(function (btn) {
      var t = normalizeTab(btn.getAttribute("data-career-tab"));
      var on = t === tab;
      btn.classList.toggle("is-active", on);
      btn.setAttribute("aria-selected", on ? "true" : "false");
    });

    page.querySelectorAll("[data-career-panel]").forEach(function (panel) {
      var t = normalizeTab(panel.getAttribute("data-career-panel"));
      panel.hidden = t !== tab;
    });

    var stripKey = stripForTab(tab);
    page.querySelectorAll("[data-career-strip-wrap]").forEach(function (wrap) {
      var key = wrap.getAttribute("data-career-strip-wrap");
      wrap.hidden = key !== stripKey;
    });

    if (push) {
      var meer = parseMeerFromUrl();
      var url = buildUrl(tab, meer);
      if (url !== window.location.pathname + window.location.search + window.location.hash) {
        window.history.pushState({ careerTab: tab, careerMeer: meer }, "", url);
      }
    }
  }

  function visibleLimit(expanded, groupId) {
    return expanded.indexOf(groupId) >= 0 ? 9999 : 3;
  }

  function applyMeer(page, expanded, push) {
    page.querySelectorAll("[data-career-card-group]").forEach(function (list) {
      var groupId = list.getAttribute("data-career-card-group");
      var limit = visibleLimit(expanded, groupId);
      var idx = 0;
      list.querySelectorAll("[data-career-card-item]").forEach(function (item) {
        idx += 1;
        item.hidden = idx > limit;
      });
      var meerBtn = page.querySelector("[data-career-meer=\"" + groupId + "\"]");
      if (meerBtn) {
        var total = list.querySelectorAll("[data-career-card-item]").length;
        meerBtn.hidden = total <= 3;
        var open = expanded.indexOf(groupId) >= 0;
        meerBtn.setAttribute("aria-expanded", open ? "true" : "false");
        if (meerBtn.dataset.careerMeerLess && meerBtn.dataset.careerMeerMore) {
          meerBtn.textContent = open ? meerBtn.dataset.careerMeerLess : meerBtn.dataset.careerMeerMore;
        }
      }
    });

    if (push) {
      var tab = parseTabFromUrl();
      var url = buildUrl(tab, expanded);
      window.history.pushState({ careerTab: tab, careerMeer: expanded }, "", url);
    }
  }

  function init() {
    var page = pageRoot();
    if (!page) {
      return;
    }

    var tab = parseTabFromUrl();
    var meer = parseMeerFromUrl();
    applyTab(page, tab, false);
    applyMeer(page, meer, false);

    page.querySelectorAll("[data-career-tab]").forEach(function (btn) {
      btn.addEventListener("click", function (ev) {
        ev.preventDefault();
        applyTab(page, btn.getAttribute("data-career-tab"), true);
      });
    });

    page.querySelectorAll("[data-career-meer]").forEach(function (btn) {
      btn.addEventListener("click", function (ev) {
        ev.preventDefault();
        var groupId = btn.getAttribute("data-career-meer");
        var expanded = parseMeerFromUrl().slice();
        var ix = expanded.indexOf(groupId);
        if (ix >= 0) {
          expanded.splice(ix, 1);
        } else {
          expanded.push(groupId);
        }
        applyMeer(page, expanded, true);
      });
    });

    window.addEventListener("popstate", function () {
      applyTab(page, parseTabFromUrl(), false);
      applyMeer(page, parseMeerFromUrl(), false);
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
