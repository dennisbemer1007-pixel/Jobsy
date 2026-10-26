(function () {
  if (window.__sollicActiviteitenBound) {
    return;
  }
  window.__sollicActiviteitenBound = true;

  var root = document.querySelector("[data-testid=\"sollic-activiteiten-tab\"]");
  if (!root) {
    return;
  }

  function msg(attr, fallback) {
    return root.getAttribute(attr) || fallback;
  }

  function showFormError(message) {
    var el = root.querySelector(".sollic-activiteiten-form-error");
    if (!el) {
      return;
    }
    el.textContent = message || "";
    el.hidden = !message;
  }

  function showAgreementError(message) {
    var el = root.querySelector(".sollic-activiteiten-agreement-error");
    if (!el) {
      return;
    }
    el.textContent = message || "";
    el.hidden = !message;
  }

  root.addEventListener("click", function (ev) {
    var chip = ev.target.closest("[data-sollic-activ-filter-chip]");
    if (chip) {
      ev.preventDefault();
      var key = chip.getAttribute("data-sollic-activ-filter-chip");
      root.querySelectorAll("[data-sollic-activ-filter-chip]").forEach(function (c) {
        c.classList.toggle("is-active", c === chip);
      });
      root.querySelectorAll("[data-sollic-activ-filter]").forEach(function (item) {
        var f = item.getAttribute("data-sollic-activ-filter");
        var show = key === "all" || f === key;
        item.hidden = !show;
      });
      return;
    }

    var kindBtn = ev.target.closest("[data-sollic-activ-kind]");
    if (kindBtn) {
      ev.preventDefault();
      var kind = kindBtn.getAttribute("data-sollic-activ-kind");
      root.querySelectorAll("[data-sollic-activ-kind]").forEach(function (b) {
        b.classList.toggle("is-selected", b === kindBtn);
      });
      var input = root.querySelector("[data-sollic-activ-kind-input]");
      if (input) {
        input.value = kind;
      }
      return;
    }

    if (ev.target.closest("[data-sollic-activ-pdf]")) {
      ev.preventDefault();
      window.location.assign("/api/sollicitaties-client/activiteiten/period.pdf");
      return;
    }

    if (ev.target.closest("[data-sollic-activ-adjust-toggle]")) {
      ev.preventDefault();
      var panel = root.querySelector("[data-sollic-activ-agreement-panel]");
      if (panel) {
        panel.hidden = !panel.hidden;
        showAgreementError("");
      }
      return;
    }

    if (ev.target.closest("[data-sollic-activ-agreement-cancel]")) {
      ev.preventDefault();
      var panelCancel = root.querySelector("[data-sollic-activ-agreement-panel]");
      if (panelCancel) {
        panelCancel.hidden = true;
      }
      showAgreementError("");
      return;
    }

    if (ev.target.closest("[data-sollic-activ-agreement-save]")) {
      ev.preventDefault();
      var inputEl = root.querySelector("[data-sollic-activ-agreement-input]");
      var n = inputEl ? parseInt(inputEl.value, 10) : NaN;
      if (!n || n < 1 || n > 20) {
        showAgreementError(msg("data-msg-agreement-invalid", "Choose a number between 1 and 20."));
        return;
      }
      fetch("/api/sollicitaties-client/activiteiten/agreement", {
        method: "PUT",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
        body: JSON.stringify({ activitiesPerPeriod: n })
      }).then(function (res) {
        if (!res.ok) {
          throw new Error("agreement");
        }
        var url = new URL(window.location.href);
        url.searchParams.set("tab", "activiteiten");
        window.location.assign(url.toString());
      }).catch(function () {
        showAgreementError(msg("data-msg-agreement-failed", "Updating failed."));
      });
    }
  });

  var form = root.querySelector("[data-sollic-activ-form]");
  if (form) {
    form.addEventListener("submit", function (ev) {
      ev.preventDefault();
      showFormError("");
      var fd = new FormData(form);
      var kind = fd.get("kind");
      var dateStr = fd.get("activityDate");
      var employer = (fd.get("employer") || "").toString().trim();
      var functionTitle = (fd.get("functionTitle") || "").toString().trim();
      if (!dateStr || !employer || !functionTitle) {
        showFormError(msg("data-msg-form-required", "Fill in date, employer and role."));
        return;
      }
      fetch("/api/sollicitaties-client/activiteiten", {
        method: "POST",
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        body: fd
      }).then(function (res) {
        if (!res.ok) {
          throw new Error("save");
        }
        var url = new URL(window.location.href);
        url.searchParams.set("tab", "activiteiten");
        window.location.assign(url.toString());
      }).catch(function () {
        showFormError(msg("data-msg-save-failed", "Saving failed."));
      });
    });
  }
})();
