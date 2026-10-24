(function () {
  "use strict";

  var DESKTOP = window.matchMedia("(min-width: 901px)");

  function parseThreadId(href) {
    if (!href) {
      return null;
    }
    try {
      var url = new URL(href, window.location.origin);
      return url.searchParams.get("thread");
    } catch (_err) {
      return null;
    }
  }

  function selectThread(threadId, replaceUrl) {
    if (!threadId) {
      return;
    }

    var root = document.querySelector("[data-testid=\"sollicitaties-berichten-tab\"]");
    if (!root) {
      return;
    }

    root.querySelectorAll(".sollic-berichten-ti__select").forEach(function (link) {
      var id = link.getAttribute("data-sollic-thread-id");
      link.classList.toggle("on", id === threadId);
    });

    root.querySelectorAll(".sollic-berichten-tmain-panel").forEach(function (panel) {
      var id = panel.getAttribute("data-sollic-thread-id");
      var show = id === threadId;
      panel.hidden = !show;
    });

    if (replaceUrl) {
      var link = root.querySelector(".sollic-berichten-ti__select[data-sollic-thread-id=\"" + threadId + "\"]");
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

  function onThreadClick(ev) {
    if (!DESKTOP.matches) {
      return;
    }

    var link = ev.target && ev.target.closest && ev.target.closest(".sollic-berichten-ti__select");
    if (!link) {
      return;
    }

    var threadId = link.getAttribute("data-sollic-thread-id") || parseThreadId(link.getAttribute("href"));
    if (!threadId) {
      return;
    }

    ev.preventDefault();
    selectThread(threadId, true);
  }

  function onComposeSubmit(ev) {
    var form = ev.target;
    if (!form || !form.matches || !form.matches("[data-sollic-compose]")) {
      return;
    }
    ev.preventDefault();

    var appId = form.getAttribute("data-application-id");
    var input = form.querySelector("input[name=\"body\"]");
    var body = input && input.value ? input.value.trim() : "";
    if (!appId || !body) {
      return;
    }

    var submit = form.querySelector("button[type=\"submit\"]");
    if (submit) {
      submit.disabled = true;
    }

    fetch("/api/applications/" + encodeURIComponent(appId) + "/after-match/messages", {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest"
      },
      body: JSON.stringify({ body: body, confirmSensitive: false })
    })
      .then(function (res) {
        if (!res.ok) {
          return res.text().then(function (t) {
            throw new Error(t || "send failed");
          });
        }
        window.location.reload();
      })
      .catch(function () {
        if (submit) {
          submit.disabled = false;
        }
        window.alert("Bericht versturen is niet gelukt. Probeer het opnieuw.");
      });
  }

  function boot() {
    var root = document.querySelector("[data-testid=\"sollicitaties-berichten-tab\"]");
    if (!root) {
      return;
    }

    document.addEventListener("click", onThreadClick, true);
    document.addEventListener("submit", onComposeSubmit, true);

    if (DESKTOP.matches) {
      var active = root.querySelector(".sollic-berichten-ti__select.on");
      var id = active && active.getAttribute("data-sollic-thread-id");
      if (id) {
        selectThread(id, false);
      }
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
