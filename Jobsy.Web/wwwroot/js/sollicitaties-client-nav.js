(function () {
  "use strict";

  var BASE = "/candidate/applications";
  var TAB_SOL = "sollicitaties";
  var TAB_BER = "berichten";
  var TAB_ACT = "activiteiten";

  function parseStateFromUrl(url) {
    var tab = TAB_SOL;
    var filter = "all";
    var sel = null;
    var thread = null;
    try {
      var u = typeof url === "string" ? new URL(url, window.location.origin) : url;
      tab = u.searchParams.get("tab") || TAB_SOL;
      if (tab !== TAB_BER && tab !== TAB_ACT) {
        tab = TAB_SOL;
      }
      filter = u.searchParams.get("filter") || "all";
      sel = u.searchParams.get("sel");
      thread = u.searchParams.get("thread");
    } catch (_e) {
      /* keep defaults */
    }
    return { tab: tab, filter: filter, sel: sel, thread: thread };
  }

  function hrefForState(state) {
    var q = [];
    if (state.tab && state.tab !== TAB_SOL) {
      q.push("tab=" + encodeURIComponent(state.tab));
    }
    if (state.tab === TAB_SOL && state.filter && state.filter !== "all") {
      q.push("filter=" + encodeURIComponent(state.filter));
    }
    if (state.tab === TAB_SOL && state.sel) {
      q.push("sel=" + encodeURIComponent(state.sel));
    }
    if (state.tab === TAB_BER && state.thread) {
      q.push("thread=" + encodeURIComponent(state.thread));
    }
    return q.length ? BASE + "?" + q.join("&") : BASE;
  }

  function rootPage() {
    return document.querySelector("[data-testid=\"applications-page-ssr\"]");
  }

  function panelsRoot() {
    return document.querySelector(".sollicitaties-tab-panels");
  }

  function rowMatchesFilter(el, filter) {
    var keys = (el.getAttribute("data-sollic-filters") || "").split(/\s+/);
    return filter === "all" || keys.indexOf(filter) >= 0;
  }

  function applyFilter(filter, options) {
    var opts = options || {};
    var page = rootPage();
    if (!page) {
      return;
    }

    var panel = page.querySelector("[data-sollic-tab-panel=\"" + TAB_SOL + "\"]");
    if (!panel) {
      return;
    }

    panel.querySelectorAll(".sollicitaties-chip").forEach(function (chip) {
      var f = chip.getAttribute("data-sollic-filter") || "all";
      chip.classList.toggle("is-selected", f === filter);
    });

    var visibleIds = [];
    panel.querySelectorAll(".sollic-card.sollic-card--app").forEach(function (card) {
      var show = rowMatchesFilter(card, filter);
      card.hidden = !show;
      if (show) {
        visibleIds.push(card.getAttribute("data-sollic-row-id"));
      }
    });

    panel.querySelectorAll(".sollic-detail-panel").forEach(function (detail) {
      detail.hidden = true;
    });

    var layout = panel.querySelector("[data-sollic-layout]");
    var empty = panel.querySelector("[data-sollic-empty]");
    var hasVisible = visibleIds.length > 0;
    if (layout) {
      layout.hidden = !hasVisible;
    }
    if (empty) {
      empty.hidden = hasVisible;
    }

    var sel = opts.sel;
    if (!sel || visibleIds.indexOf(sel) < 0) {
      sel = visibleIds[0] || null;
    }

    if (sel && window.matchMedia("(min-width: 1024px)").matches) {
      panel.querySelectorAll(".sollic-card.sollic-card--app").forEach(function (card) {
        var id = card.getAttribute("data-sollic-row-id");
        card.classList.toggle("is-selected", id === sel);
      });
      panel.querySelectorAll(".sollic-detail-panel").forEach(function (detail) {
        var id = detail.getAttribute("data-sollic-row-id");
        var show = id === sel && rowMatchesFilter(detail, filter);
        detail.hidden = !show;
      });
    }

    var panels = panelsRoot();
    if (panels) {
      panels.setAttribute("data-sollic-active-filter", filter);
    }

    if (opts.updateUrl !== false) {
      var state = parseStateFromUrl(window.location.href);
      state.filter = filter;
      state.tab = TAB_SOL;
      state.sel = sel;
      state.thread = null;
      try {
        window.history.replaceState({ sollic: state }, "", hrefForState(state));
      } catch (_e2) {
        /* ignore */
      }
    }

    scrollSelectedFilterChip();
  }

  function applyTab(tab, options) {
    var opts = options || {};
    var page = rootPage();
    if (!page) {
      return;
    }

    page.querySelectorAll("[data-sollic-tab-panel]").forEach(function (panel) {
      var id = panel.getAttribute("data-sollic-tab-panel");
      panel.hidden = id !== tab;
    });

    page.querySelectorAll(".sollicitaties-segment__tab[data-sollic-tab]").forEach(function (link) {
      var id = link.getAttribute("data-sollic-tab");
      var active = id === tab;
      link.classList.toggle("is-active", active);
      if (active) {
        link.setAttribute("aria-current", "page");
      } else {
        link.removeAttribute("aria-current");
      }
    });

    var panels = panelsRoot();
    if (panels) {
      panels.setAttribute("data-sollic-active-tab", tab);
    }

    if (tab === TAB_SOL) {
      var filter =
        (panels && panels.getAttribute("data-sollic-active-filter")) ||
        parseStateFromUrl(window.location.href).filter;
      applyFilter(filter, { updateUrl: false, sel: opts.sel });
    }

    if (tab === TAB_BER && opts.thread && typeof window.__sollicBerichtenSelectThread === "function") {
      window.__sollicBerichtenSelectThread(opts.thread, false);
    }

    if (opts.updateUrl !== false) {
      var state = {
        tab: tab,
        filter: "all",
        sel: null,
        thread: null
      };
      if (tab === TAB_SOL) {
        state.filter =
          (panels && panels.getAttribute("data-sollic-active-filter")) ||
          parseStateFromUrl(window.location.href).filter ||
          "all";
        var selectedCard = page.querySelector(
          ".sollic-card.sollic-card--app.is-selected:not([hidden])"
        );
        state.sel = (selectedCard && selectedCard.getAttribute("data-sollic-row-id")) || opts.sel || null;
      } else if (tab === TAB_BER) {
        state.thread = opts.thread || parseStateFromUrl(window.location.href).thread;
      }
      try {
        var method = opts.history === "replace" ? "replaceState" : "pushState";
        window.history[method]({ sollic: state }, "", hrefForState(state));
      } catch (_e) {
        /* ignore */
      }
    }
  }

  function scrollSelectedFilterChip() {
    if (!window.matchMedia("(max-width: 639px)").matches) {
      return;
    }
    var row = document.querySelector(".sollicitaties-toolbar__filters");
    if (!row) {
      return;
    }
    var selected = row.querySelector(".sollicitaties-chip.is-selected");
    if (!selected) {
      return;
    }
    try {
      selected.scrollIntoView({ inline: "center", block: "nearest", behavior: "instant" });
    } catch (_err) {
      selected.scrollIntoView(true);
    }
  }

  function shouldIntercept(ev, link) {
    if (!link || ev.defaultPrevented) {
      return false;
    }
    if (ev.button !== 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.altKey) {
      return false;
    }
    if (link.target && link.target !== "_self") {
      return false;
    }
    return true;
  }

  function onClick(ev) {
    var chip = ev.target && ev.target.closest && ev.target.closest(".sollicitaties-chip[data-sollic-filter]");
    if (chip && shouldIntercept(ev, chip)) {
      var page = rootPage();
      var panels = panelsRoot();
      var activeTab = panels && panels.getAttribute("data-sollic-active-tab");
      if (page && activeTab === TAB_SOL) {
        ev.preventDefault();
        applyFilter(chip.getAttribute("data-sollic-filter") || "all", { updateUrl: true });
      }
      return;
    }

    var tabLink = ev.target && ev.target.closest && ev.target.closest(".sollicitaties-segment__tab[data-sollic-tab]");
    if (tabLink && shouldIntercept(ev, tabLink)) {
      ev.preventDefault();
      var tab = tabLink.getAttribute("data-sollic-tab") || TAB_SOL;
      var parsed = parseStateFromUrl(tabLink.getAttribute("href") || window.location.href);
      applyTab(tab, { thread: parsed.thread, updateUrl: true, history: "push" });
    }
  }

  function onPopState(ev) {
    var state = (ev.state && ev.state.sollic) || parseStateFromUrl(window.location.href);
    applyTab(state.tab, { thread: state.thread, sel: state.sel, updateUrl: false });
    if (state.tab === TAB_SOL) {
      applyFilter(state.filter, { sel: state.sel, updateUrl: false });
    }
  }

  function boot() {
    if (!rootPage() || !panelsRoot()) {
      return;
    }

    document.addEventListener("click", onClick, true);
    window.addEventListener("popstate", onPopState);

    var initial = parseStateFromUrl(window.location.href);
    try {
      window.history.replaceState({ sollic: initial }, "", window.location.href);
    } catch (_e) {
      /* ignore */
    }

    scrollSelectedFilterChip();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
