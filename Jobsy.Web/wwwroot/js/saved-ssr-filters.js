(function () {
  "use strict";

  function rootPage() {
    return document.querySelector("[data-testid=\"saved-page-ssr\"]");
  }

  function normalizeFilter(raw) {
    if (raw === "open" || raw === "closed") {
      return raw;
    }
    return "all";
  }

  function parseFilterFromUrl() {
    try {
      var u = new URL(window.location.href);
      return normalizeFilter(u.searchParams.get("filter"));
    } catch (_e) {
      return "all";
    }
  }

  function buildUrl(filter, sort) {
    var u = new URL(window.location.href);
    if (!filter || filter === "all") {
      u.searchParams.delete("filter");
    } else {
      u.searchParams.set("filter", filter);
    }
    if (sort) {
      u.searchParams.set("sort", sort);
    }
    u.searchParams.delete("saved");
    u.searchParams.delete("error");
    return u.pathname + u.search + u.hash;
  }

  function itemClosed(el) {
    return el.getAttribute("data-saved-closed") === "1";
  }

  function matchesFilter(el, filter) {
    if (filter === "open") {
      return !itemClosed(el);
    }
    if (filter === "closed") {
      return itemClosed(el);
    }
    return true;
  }

  function applyFilter(page, filter, push) {
    filter = normalizeFilter(filter);
    page.querySelectorAll("[data-saved-filter]").forEach(function (chip) {
      var f = normalizeFilter(chip.getAttribute("data-saved-filter"));
      chip.classList.toggle("is-selected", f === filter);
      chip.setAttribute("aria-pressed", f === filter ? "true" : "false");
    });

    var visible = 0;
    var counted = {};
    page.querySelectorAll("[data-saved-item]").forEach(function (el) {
      var show = matchesFilter(el, filter);
      el.hidden = !show;
      if (show) {
        var vid = el.getAttribute("data-saved-vacancy-id");
        if (vid && !counted[vid]) {
          counted[vid] = true;
          visible += 1;
        }
      }
    });

    var countEl = page.querySelector("[data-saved-filter-count]");
    if (countEl && countEl.dataset.savedCountTemplate) {
      try {
        countEl.textContent = countEl.dataset.savedCountTemplate.replace("{0}", String(visible));
      } catch (_e2) {
        /* keep server text */
      }
    }

    var emptyMsg = page.querySelector("[data-saved-filter-empty]");
    if (emptyMsg) {
      emptyMsg.hidden = visible > 0;
    }

    var sort = page.querySelector("[data-saved-sort]");
    var sortVal = sort ? sort.value : null;
    var next = buildUrl(filter, sortVal);
    if (push && window.history && window.history.pushState) {
      window.history.pushState({ savedFilter: filter }, "", next);
    } else if (window.history && window.history.replaceState) {
      window.history.replaceState({ savedFilter: filter }, "", next);
    }
  }

  function bindSort(page) {
    var select = page.querySelector("[data-saved-sort]");
    if (!select || select.getAttribute("data-saved-sort-bound") === "1") {
      return;
    }
    select.setAttribute("data-saved-sort-bound", "1");
    select.addEventListener("change", function () {
      var filter = parseFilterFromUrl();
      window.location.href = buildUrl(filter, select.value);
    });
  }

  function bindFilters(page) {
    page.querySelectorAll("[data-saved-filter]").forEach(function (chip) {
      if (chip.getAttribute("data-saved-filter-bound") === "1") {
        return;
      }
      chip.setAttribute("data-saved-filter-bound", "1");
      chip.addEventListener("click", function (ev) {
        ev.preventDefault();
        var filter = chip.getAttribute("data-saved-filter") || "all";
        applyFilter(page, filter, true);
      });
    });
  }

  function init() {
    var page = rootPage();
    if (!page) {
      return;
    }
    bindSort(page);
    bindFilters(page);
    applyFilter(page, parseFilterFromUrl(), false);
    window.addEventListener("popstate", function () {
      applyFilter(page, parseFilterFromUrl(), false);
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
