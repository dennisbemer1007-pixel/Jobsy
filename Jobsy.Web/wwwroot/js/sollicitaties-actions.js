(function () {
  function showError(root, message) {
    var existing = root.querySelector(".sollicitaties-inline-error");
    if (!existing) {
      existing = document.createElement("p");
      existing.className = "state-message error sollicitaties-inline-error";
      existing.setAttribute("role", "alert");
      root.insertBefore(existing, root.firstChild);
    }
    existing.textContent = message || "Er ging iets mis. Probeer het opnieuw.";
    existing.hidden = !message;
  }

  document.addEventListener("click", function (ev) {
    var btn = ev.target && ev.target.closest && ev.target.closest(".sollic-detail__withdraw");
    if (!btn) {
      return;
    }
    ev.preventDefault();
    var appId = btn.getAttribute("data-application-id");
    if (!appId) {
      return;
    }
    if (!window.confirm("Sollicitatie intrekken?")) {
      return;
    }
    var page = document.querySelector(".sollicitaties-page") || document.body;
    btn.disabled = true;
    fetch("/api/applications/" + encodeURIComponent(appId) + "/withdraw", {
      method: "POST",
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" }
    })
      .then(function (res) {
        if (!res.ok) {
          return res.text().then(function (t) {
            throw new Error(t || "withdraw failed");
          });
        }
        window.location.assign("/candidate/applications");
      })
      .catch(function () {
        btn.disabled = false;
        showError(page, "Intrekken is niet gelukt. Probeer het opnieuw.");
      });
  });
})();
