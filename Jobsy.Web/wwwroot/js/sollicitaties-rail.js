(function () {
  "use strict";

  var DESKTOP = window.matchMedia("(min-width: 1024px)");

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
    var page = document.querySelector("[data-testid=\"applications-page-ssr\"]");
    if (!page) {
      return null;
    }
    var panels = page.querySelector(".sollicitaties-tab-panels");
    if (!panels || panels.getAttribute("data-sollic-active-tab") !== "sollicitaties") {
      return null;
    }
    return page.querySelector("[data-sollic-tab-panel=\"sollicitaties\"]");
  }

  function selectRow(rowId, replaceUrl) {
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

    if (replaceUrl) {
      var link = panel.querySelector(".sollic-card[data-sollic-row-id=\"" + rowId + "\"] .sollic-card__select");
      var href = link && link.getAttribute("href");
      if (href) {
        try {
          var state = window.history.state && window.history.state.sollic
            ? Object.assign({}, window.history.state.sollic)
            : { tab: "sollicitaties", filter: "all", sel: null, thread: null };
          state.tab = "sollicitaties";
          state.sel = rowId;
          state.thread = null;
          window.history.replaceState({ sollic: state }, "", href);
        } catch (_e) {
          /* ignore */
        }
      }
    }
  }

  function isNestedCardAction(target) {
    return (
      target &&
      target.closest &&
      (target.closest("a.sollic-card__ndm-act") ||
        target.closest("a.sollic-card__msg") ||
        target.closest("a.sollic-card__select") === null &&
          target.closest("a[href]"))
    );
  }

  function onClick(ev) {
    if (!DESKTOP.matches) {
      return;
    }

    if (ev.button !== 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.altKey) {
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

    var selectLink = card.querySelector(".sollic-card__select");
    if (!selectLink) {
      return;
    }

    var rowId = parseRowId(selectLink.getAttribute("href"));
    if (!rowId) {
      return;
    }

    ev.preventDefault();
    selectRow(rowId, true);
  }

  function boot() {
    if (!document.querySelector("[data-testid=\"applications-page-ssr\"]")) {
      return;
    }

    document.addEventListener("click", onClick, true);

    if (DESKTOP.matches) {
      var panel = activeSollicPanel();
      var selected = panel && panel.querySelector(".sollic-card.sollic-card--app.is-selected:not([hidden])");
      var id = selected && selected.getAttribute("data-sollic-row-id");
      if (id) {
        selectRow(id, false);
      }
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
