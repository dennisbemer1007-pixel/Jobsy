/* Auth progressive enhancement: password toggle + session-expired draft clear. No secrets. */
(function () {
  "use strict";

  var STORAGE_PREFIX = "jobsy.draft.";

  function clearDrafts() {
    try {
      var keys = [];
      for (var i = 0; i < sessionStorage.length; i++) {
        var k = sessionStorage.key(i);
        if (k && k.indexOf(STORAGE_PREFIX) === 0) {
          keys.push(k);
        }
      }
      keys.forEach(function (k) {
        sessionStorage.removeItem(k);
      });
    } catch (e) {
      /* ignore */
    }
  }

  function bindPasswordToggles(root) {
    var buttons = (root || document).querySelectorAll("[data-au-password-toggle]");
    buttons.forEach(function (btn) {
      if (btn.getAttribute("data-au-bound") === "1") {
        return;
      }
      btn.setAttribute("data-au-bound", "1");
      btn.hidden = false;
      btn.addEventListener("click", function () {
        var id = btn.getAttribute("aria-controls");
        var input = id ? document.getElementById(id) : null;
        if (!input) {
          return;
        }
        var show = input.getAttribute("type") === "password";
        input.setAttribute("type", show ? "text" : "password");
        btn.setAttribute("aria-pressed", show ? "true" : "false");
        var showLabel = btn.getAttribute("data-label-show") || "Toon";
        var hideLabel = btn.getAttribute("data-label-hide") || "Verberg";
        btn.textContent = show ? hideLabel : showLabel;
      });
    });
  }

  function boot() {
    var main = document.querySelector("main[data-clear-drafts], [data-au-login][data-clear-drafts]");
    if (main) {
      clearDrafts();
      if (window.lobsySessionIdle && typeof window.lobsySessionIdle.clearDrafts === "function") {
        try {
          window.lobsySessionIdle.clearDrafts();
        } catch (e) {
          /* ignore */
        }
      }
    }
    bindPasswordToggles(document);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  window.lobsyAuth = window.lobsyAuth || {};
  window.lobsyAuth.clearDrafts = clearDrafts;
  window.lobsyAuth.bindPasswordToggles = bindPasswordToggles;
})();
