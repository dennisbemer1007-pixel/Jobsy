(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-passport-proof]");
  }

  function sheetEls(r) {
    return {
      backdrop: r.querySelector("[data-passport-proof-backdrop]"),
      sheet: r.querySelector("[data-passport-proof-sheet]"),
      title: r.querySelector("#passport-proof-sheet-title"),
      panels: r.querySelectorAll("[data-passport-proof-sheet-panel]")
    };
  }

  function openSheet(r, kind) {
    var els = sheetEls(r);
    if (!els.sheet) return;
    els.panels.forEach(function (panel) {
      var on = panel.getAttribute("data-passport-proof-sheet-panel") === kind;
      panel.hidden = !on;
    });
    if (els.title) {
      var btn = r.querySelector('[data-passport-proof-open="' + kind + '"]');
      var label = btn ? btn.getAttribute("data-sheet-title") : "";
      if (label) els.title.textContent = label;
    }
    els.backdrop.hidden = false;
    els.sheet.hidden = false;
    try {
      if (window.jobsyDialog && typeof window.jobsyDialog.trap === "function") {
        window.jobsyDialog.trap(els.sheet);
      }
    } catch (e) {}
    els.sheet.focus();
  }

  function closeSheet(r) {
    var els = sheetEls(r);
    if (!els.sheet || els.sheet.hidden) return;
    els.backdrop.hidden = true;
    els.sheet.hidden = true;
    try {
      if (window.jobsyDialog && typeof window.jobsyDialog.release === "function") {
        window.jobsyDialog.release();
      }
    } catch (e) {}
  }

  function bind(r) {
    r.querySelectorAll("[data-passport-proof-open]").forEach(function (btn) {
      if (btn.getAttribute("data-passport-proof-bound") === "1") return;
      btn.setAttribute("data-passport-proof-bound", "1");
      btn.addEventListener("click", function () {
        openSheet(r, btn.getAttribute("data-passport-proof-open") || "");
      });
    });
    var els = sheetEls(r);
    if (els.backdrop && els.backdrop.getAttribute("data-passport-proof-bound") !== "1") {
      els.backdrop.setAttribute("data-passport-proof-bound", "1");
      els.backdrop.addEventListener("click", function () {
        closeSheet(r);
      });
    }
    r.querySelectorAll("[data-passport-proof-close]").forEach(function (btn) {
      if (btn.getAttribute("data-passport-proof-bound") === "1") return;
      btn.setAttribute("data-passport-proof-bound", "1");
      btn.addEventListener("click", function () {
        closeSheet(r);
      });
    });
    if (els.sheet && els.sheet.getAttribute("data-passport-proof-bound") !== "1") {
      els.sheet.setAttribute("data-passport-proof-bound", "1");
      els.sheet.addEventListener("keydown", function (e) {
        if (e.key === "Escape") closeSheet(r);
      });
    }
    var initial = r.getAttribute("data-open-proof-item");
    if (initial) {
      var kind = initial.toLowerCase();
      if (kind === "experience" || kind === "werkervaring") openSheet(r, "experience");
      else if (kind === "education" || kind === "certificates" || kind === "opleiding") openSheet(r, "certificates");
      else if (kind === "references" || kind === "referenties") openSheet(r, "references");
      else if (kind === "cv") openSheet(r, "cv");
    }
  }

  function init() {
    var r = root();
    if (!r) return;
    bind(r);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
