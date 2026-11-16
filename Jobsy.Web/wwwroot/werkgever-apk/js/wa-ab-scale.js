(function () {
  "use strict";

  function toggleClass(el, name, on) {
    if (!el || !el.classList) return;
    if (on) el.classList.add(name);
    else el.classList.remove(name);
  }

  function applySelection(form, btn, val) {
    var hidden = form.querySelector('input[name="Value"]');
    if (hidden) hidden.value = val;
    form.querySelectorAll("[data-wa-ab-value]").forEach(function (b) {
      var sel = b === btn;
      toggleClass(b, "sel", sel);
      b.setAttribute("aria-checked", sel ? "true" : "false");
      b.setAttribute("tabindex", sel ? "0" : "-1");
    });
    form.setAttribute("data-wa-ab-selected", "1");
  }

  function values(form) {
    return [].map.call(form.querySelectorAll("[data-wa-ab-value]"), function (b) {
      return parseInt(b.getAttribute("data-wa-ab-value") || "0", 10);
    }).filter(function (n) { return n >= 1 && n <= 5; });
  }

  function onKeyDown(form, e) {
    var vals = values(form);
    if (!vals.length) return;
    var hidden = form.querySelector('input[name="Value"]');
    var current = hidden && hidden.value ? parseInt(hidden.value, 10) : vals[0];
    var index = Math.max(0, vals.indexOf(current));
    var next = null;
    if (e.key === "ArrowRight" || e.key === "ArrowDown") next = vals[Math.min(index + 1, vals.length - 1)];
    else if (e.key === "ArrowLeft" || e.key === "ArrowUp") next = vals[Math.max(index - 1, 0)];
    else if (e.key === "Home") next = vals[0];
    else if (e.key === "End") next = vals[vals.length - 1];
    else if (e.key >= "1" && e.key <= "5") next = parseInt(e.key, 10);
    if (next == null || vals.indexOf(next) < 0) return;
    e.preventDefault();
    var btn = form.querySelector('[data-wa-ab-value="' + next + '"]');
    if (btn) {
      applySelection(form, btn, String(next));
      btn.focus();
    }
  }

  function bindForm(form) {
    if (!form || form.getAttribute("data-wa-ab-bound") === "1") return;
    form.setAttribute("data-wa-ab-bound", "1");
    form.querySelectorAll("[data-wa-ab-value]").forEach(function (btn) {
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        var val = btn.getAttribute("data-wa-ab-value");
        if (!val) return;
        applySelection(form, btn, val);
      });
    });
    form.addEventListener("keydown", function (e) {
      onKeyDown(form, e);
    });
    form.addEventListener("submit", function (e) {
      var hidden = form.querySelector('input[name="Value"]');
      if (!hidden || !hidden.value) {
        e.preventDefault();
      }
    });
  }

  function init() {
    document.querySelectorAll('form[data-wa-ab-scale="1"]').forEach(bindForm);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();