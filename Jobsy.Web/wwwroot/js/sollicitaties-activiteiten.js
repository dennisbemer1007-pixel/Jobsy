(function () {
  if (window.__sollicActiviteitenBound) {
    return;
  }
  window.__sollicActiviteitenBound = true;

  var root = document.querySelector("[data-testid=\"sollic-activiteiten-tab\"]");
  if (!root) {
    return;
  }

  function showFormError(message) {
    var el = root.querySelector(".sollic-activiteiten-form-error");
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

    if (ev.target.closest("[data-sollic-activ-adjust]")) {
      ev.preventDefault();
      var current = root.querySelector("[data-sollic-activ-target]");
      var def = current ? current.getAttribute("data-sollic-activ-target") : "4";
      var raw = window.prompt("Hoeveel activiteiten per 4 weken?", def);
      if (raw === null) {
        return;
      }
      var n = parseInt(raw, 10);
      if (!n || n < 1 || n > 20) {
        window.alert("Kies een getal tussen 1 en 20.");
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
        window.alert("Aanpassen is niet gelukt. Probeer het opnieuw.");
      });
    }
  });

  var form = root.querySelector("[data-sollic-activ-form]");
  if (form) {
    form.addEventListener("submit", function (ev) {
      ev.preventDefault();
      showFormError("");
      var fd = new FormData(form);
      var kind = parseInt(fd.get("kind") || "1", 10);
      var dateStr = fd.get("activityDate");
      var employer = (fd.get("employer") || "").toString().trim();
      var functionTitle = (fd.get("functionTitle") || "").toString().trim();
      var result = (fd.get("result") || "").toString().trim();
      if (!dateStr || !employer || !functionTitle) {
        showFormError("Vul datum, werkgever en functie in.");
        return;
      }
      var payload = {
        kind: kind,
        activityDate: dateStr,
        employer: employer,
        functionTitle: functionTitle,
        result: result || null
      };
      fetch("/api/sollicitaties-client/activiteiten", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
        body: JSON.stringify(payload)
      }).then(function (res) {
        if (!res.ok) {
          throw new Error("save");
        }
        var url = new URL(window.location.href);
        url.searchParams.set("tab", "activiteiten");
        window.location.assign(url.toString());
      }).catch(function () {
        showFormError("Opslaan is niet gelukt. Probeer het opnieuw.");
      });
    });
  }
})();
