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

  function selectRow(rowId, replaceUrl) {
    if (!rowId) {
      return;
    }

    var page = document.querySelector("[data-testid=\"applications-page-ssr\"]");
    if (!page) {
      return;
    }

    page.querySelectorAll(".sollic-card.sollic-card--app").forEach(function (card) {
      var id = card.getAttribute("data-sollic-row-id");
      card.classList.toggle("is-selected", id === rowId);
    });

    page.querySelectorAll(".sollic-detail-panel").forEach(function (panel) {
      var id = panel.getAttribute("data-sollic-row-id");
      var show = id === rowId;
      panel.hidden = !show;
    });

    if (replaceUrl) {
      var link = page.querySelector(".sollic-card[data-sollic-row-id=\"" + rowId + "\"] .sollic-card__select");
      var href = link && link.getAttribute("href");
      if (href) {
        try {
          window.history.replaceState(window.history.state, "", href);
        } catch (_e) {
          /* ignore */
        }
      }
    }
  }

  function onClick(ev) {
    if (!DESKTOP.matches) {
      return;
    }

    var link = ev.target && ev.target.closest && ev.target.closest(".sollic-card__select");
    if (!link) {
      return;
    }

    var rowId = parseRowId(link.getAttribute("href"));
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
      var page = document.querySelector("[data-testid=\"applications-page-ssr\"]");
      var selected = page && page.querySelector(".sollic-card.sollic-card--app.is-selected");
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
