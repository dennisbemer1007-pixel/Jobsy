(function () {
  "use strict";

  var DESKTOP = window.matchMedia("(min-width: 1024px)");
  var MOBILE_DETAIL = window.matchMedia("(max-width: 900px)");

  function pageRoot() {
    return document.querySelector("[data-testid=\"applications-page-ssr\"]");
  }

  function parseRowId(href) {
    if (!href) {
      return null;
    }
    try {
      var url = new URL(href, window.location.origin);
      return url.searchParams.get("sel");
    } catch (_err) {
      return null;
    }
  }

  function activeSollicPanel() {
    var page = pageRoot();
    if (!page) {
      return null;
    }
    var panels = page.querySelector(".sollicitaties-tab-panels");
    if (!panels || panels.getAttribute("data-sollic-active-tab") !== "sollicitaties") {
      return null;
    }
    return page.querySelector("[data-sollic-tab-panel=\"sollicitaties\"]");
  }

  function setMobileDetailMode(on) {
    var page = pageRoot();
    if (!page) {
      return;
    }
    page.classList.toggle("sollicitaties-page--detail", !!on);
  }

  function historyStateBase() {
    return window.history.state && window.history.state.sollic
      ? Object.assign({}, window.history.state.sollic)
      : { tab: "sollicitaties", filter: "all", sel: null, thread: null };
  }

  function writeHistory(state, method, url) {
    try {
      window.history[method]({ sollic: state }, "", url);
    } catch (_e) {
      /* ignore */
    }
  }

  function selectRow(rowId, options) {
    var opts = options || {};
    if (!rowId) {
      return;
    }

    var panel = activeSollicPanel();
    if (!panel) {
      return;
    }

    panel.querySelectorAll(".sollic-card.sollic-card--app").forEach(function (card) {
      if (card.hidden) {
        return;
      }
      var id = card.getAttribute("data-sollic-row-id");
      card.classList.toggle("is-selected", id === rowId);
    });

    panel.querySelectorAll(".sollic-detail-panel").forEach(function (detailPanel) {
      var id = detailPanel.getAttribute("data-sollic-row-id");
      var card = panel.querySelector(".sollic-card[data-sollic-row-id=\"" + id + "\"]");
      var show = id === rowId && card && !card.hidden;
      detailPanel.hidden = !show;
    });

    if (MOBILE_DETAIL.matches) {
      setMobileDetailMode(true);
    }

    if (opts.updateUrl) {
      var link = panel.querySelector(".sollic-card[data-sollic-row-id=\"" + rowId + "\"] .sollic-card__select");
      var href = link && link.getAttribute("href");
      if (href) {
        var state = historyStateBase();
        state.tab = "sollicitaties";
        state.sel = rowId;
        state.thread = null;
        writeHistory(state, opts.historyMethod || "replaceState", href);
      }
    }
  }

  function closeMobileDetail(options) {
    var opts = options || {};
    setMobileDetailMode(false);
    var panel = activeSollicPanel();
    if (panel) {
      panel.querySelectorAll(".sollic-card.sollic-card--app").forEach(function (card) {
        card.classList.remove("is-selected");
      });
    }
    if (opts.updateUrl !== false) {
      var state = historyStateBase();
      state.tab = "sollicitaties";
      state.sel = null;
      state.thread = null;
      var filter = state.filter || "all";
      var url = "/candidate/applications";
      if (filter !== "all") {
        url += "?filter=" + encodeURIComponent(filter);
      }
      writeHistory(state, opts.historyMethod || "replaceState", url);
    }
  }

  function shouldIntercept(ev) {
    if (ev.defaultPrevented || ev.button !== 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.altKey) {
      return false;
    }
    return true;
  }

  function onClick(ev) {
    var back = ev.target && ev.target.closest && ev.target.closest(".sollic-detail__back.back");
    if (back && MOBILE_DETAIL.matches && shouldIntercept(ev)) {
      ev.preventDefault();
      closeMobileDetail({ updateUrl: true, historyMethod: "pushState" });
      return;
    }

    if (!DESKTOP.matches && !MOBILE_DETAIL.matches) {
      return;
    }

    var panel = activeSollicPanel();
    if (!panel) {
      return;
    }

    var card = ev.target && ev.target.closest && ev.target.closest(".sollic-card.sollic-card--app");
    if (!card || !panel.contains(card) || card.hidden) {
      return;
    }

    if (ev.target.closest("a.sollic-card__ndm-act, a.sollic-card__msg")) {
      return;
    }

    if (!shouldIntercept(ev)) {
      return;
    }

    var selectLink = card.querySelector(".sollic-card__select");
    if (!selectLink) {
      return;
    }

    var rowId = parseRowId(selectLink.getAttribute("href"));
    if (!rowId) {
      return;
    }

    ev.preventDefault();
    selectRow(rowId, {
      updateUrl: true,
      historyMethod: MOBILE_DETAIL.matches ? "pushState" : "replaceState"
    });
  }

  window.__sollicRailSyncFromHistory = function (state) {
    if (!state || state.tab !== "sollicitaties") {
      if (MOBILE_DETAIL.matches) {
        setMobileDetailMode(false);
      }
      return;
    }
    if (state.sel && MOBILE_DETAIL.matches) {
      selectRow(state.sel, { updateUrl: false });
    } else if (MOBILE_DETAIL.matches) {
      closeMobileDetail({ updateUrl: false });
    } else if (state.sel && DESKTOP.matches) {
      selectRow(state.sel, { updateUrl: false });
    }
  };

  function boot() {
    if (!pageRoot()) {
      return;
    }

    document.addEventListener("click", onClick, true);

    var params = new URL(window.location.href).searchParams;
    var sel = params.get("sel");
    var tab = params.get("tab") || "sollicitaties";

    if (tab === "sollicitaties" && sel) {
      if (MOBILE_DETAIL.matches) {
        selectRow(sel, { updateUrl: false });
      } else if (DESKTOP.matches) {
        selectRow(sel, { updateUrl: false });
      }
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
