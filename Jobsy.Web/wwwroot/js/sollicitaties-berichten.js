(function () {
  "use strict";

  var DESKTOP = window.matchMedia("(min-width: 901px)");
  var MOBILE = window.matchMedia("(max-width: 900px)");
  var BASE = "/candidate/applications";

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

  function hrefForBerichten(threadId) {
    var q = "tab=berichten";
    if (threadId) {
      q += "&thread=" + encodeURIComponent(threadId);
    }
    return BASE + "?" + q;
  }

  function listHref() {
    return hrefForBerichten(null);
  }

  function root() {
    return document.querySelector("[data-testid=\"sollicitaties-berichten-tab\"]");
  }

  function pushSollicHistory(threadId) {
    var state = {
      tab: "berichten",
      filter: "all",
      sel: null,
      thread: threadId || null
    };
    try {
      window.history.pushState({ sollic: state }, "", hrefForBerichten(threadId));
    } catch (_e) {
      /* ignore */
    }
  }

  function replaceSollicHistory(threadId) {
    var state = {
      tab: "berichten",
      filter: "all",
      sel: null,
      thread: threadId || null
    };
    try {
      window.history.replaceState({ sollic: state }, "", hrefForBerichten(threadId));
    } catch (_e2) {
      /* ignore */
    }
  }

  function clearUnreadForThread(threadId) {
    if (!threadId) {
      return;
    }

    var r = root();
    if (r) {
      r.querySelectorAll(
        ".sollic-berichten-ti__select[data-sollic-thread-id=\"" + threadId + "\"] .n"
      ).forEach(function (badge) {
        badge.remove();
      });
    }

    document.querySelectorAll(".sollic-card[data-sollic-app-id=\"" + threadId + "\"] .sollic-card__msg").forEach(function (link) {
      link.remove();
    });

    document.querySelectorAll(".sollic-detail-panel").forEach(function (panel) {
      var card = document.querySelector(
        ".sollic-card[data-sollic-app-id=\"" + threadId + "\"]"
      );
      if (!card) {
        return;
      }
      var rowId = card.getAttribute("data-sollic-row-id");
      if (panel.getAttribute("data-sollic-row-id") === rowId) {
        panel.querySelectorAll(".sollic-detail__msg-badge").forEach(function (b) {
          b.remove();
        });
      }
    });

    updateBerichtenTabBadge();
  }

  function markThreadReadServer(threadId) {
    if (!threadId) {
      return;
    }
    fetch("/api/me/applications/" + encodeURIComponent(threadId) + "/after-match/messages/read", {
      method: "POST",
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" }
    }).catch(function () {
      /* optimistic UI already cleared badges */
    });
  }

  function updateBerichtenTabBadge() {
    var r = root();
    var total = 0;
    if (r) {
      total = r.querySelectorAll(".sollic-berichten-ti__select .n").length;
    }

    var tab = document.querySelector(".sollicitaties-segment__tab[data-sollic-tab=\"berichten\"]");
    if (!tab) {
      return;
    }
    var existing = tab.querySelector(".sollicitaties-segment__badge");
    if (total > 0) {
      if (existing) {
        existing.textContent = String(total);
      } else {
        var span = document.createElement("span");
        span.className = "sollicitaties-segment__badge";
        span.setAttribute("aria-hidden", "true");
        span.textContent = String(total);
        tab.appendChild(span);
      }
    } else if (existing) {
      existing.remove();
    }
  }

  function setMobileThreadOpen(open) {
    var r = root();
    if (!r) {
      return;
    }
    r.classList.toggle("sollic-berichten--thread-open", !!open);
  }

  function selectThread(threadId, options) {
    var opts = options || {};
    if (!threadId) {
      return;
    }

    var r = root();
    if (!r) {
      return;
    }

    r.querySelectorAll(".sollic-berichten-ti__select").forEach(function (link) {
      var id = link.getAttribute("data-sollic-thread-id");
      link.classList.toggle("on", id === threadId);
    });

    r.querySelectorAll(".sollic-berichten-tmain-panel").forEach(function (panel) {
      var id = panel.getAttribute("data-sollic-thread-id");
      var show = id === threadId;
      panel.hidden = !show;
    });

    if (MOBILE.matches) {
      setMobileThreadOpen(true);
    }

    clearUnreadForThread(threadId);
    markThreadReadServer(threadId);

    if (opts.history === "push") {
      pushSollicHistory(threadId);
    } else if (opts.history === "replace") {
      replaceSollicHistory(threadId);
    } else if (opts.history === "list") {
      replaceSollicHistory(null);
    }
  }

  function closeThreadList(options) {
    var opts = options || {};
    var r = root();
    if (!r) {
      return;
    }

    setMobileThreadOpen(false);
    r.querySelectorAll(".sollic-berichten-tmain-panel").forEach(function (panel) {
      panel.hidden = true;
    });
    r.querySelectorAll(".sollic-berichten-ti__select").forEach(function (link) {
      link.classList.remove("on");
    });

    if (opts.history !== false) {
      replaceSollicHistory(null);
    }
  }

  function shouldIntercept(ev, link) {
    if (!link || ev.defaultPrevented) {
      return false;
    }
    if (ev.button !== 0 || ev.metaKey || ev.ctrlKey || ev.shiftKey || ev.altKey) {
      return false;
    }
    if (link.target && link.target !== "_self") {
      return false;
    }
    return true;
  }

  function onThreadClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest(".sollic-berichten-ti__select");
    if (!link || !shouldIntercept(ev, link)) {
      return;
    }

    var threadId = link.getAttribute("data-sollic-thread-id") || parseThreadId(link.getAttribute("href"));
    if (!threadId) {
      return;
    }

    ev.preventDefault();
    selectThread(threadId, { history: "push" });
  }

  function onBackClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest("[data-sollic-berichten-back]");
    if (!link || !shouldIntercept(ev, link)) {
      return;
    }
    ev.preventDefault();
    closeThreadList({ history: true });
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

  window.__sollicBerichtenSelectThread = function (threadId, replaceUrl) {
    if (!threadId) {
      closeThreadList({ history: !!replaceUrl });
      return;
    }
    selectThread(threadId, { history: replaceUrl ? "replace" : false });
  };

  window.__sollicBerichtenSyncFromHistory = function (state) {
    if (!state || state.tab !== "berichten") {
      setMobileThreadOpen(false);
      return;
    }
    if (state.thread) {
      selectThread(state.thread, { history: false });
    } else {
      closeThreadList({ history: false });
    }
  };

  function boot() {
    var r = root();
    if (!r) {
      return;
    }

    document.addEventListener("click", onThreadClick, true);
    document.addEventListener("click", onBackClick, true);
    document.addEventListener("submit", onComposeSubmit, true);

    var initialThread = parseThreadId(window.location.href);
    if (initialThread) {
      selectThread(initialThread, { history: false });
    } else if (DESKTOP.matches) {
      var active = r.querySelector(".sollic-berichten-ti__select.on");
      var id = active && active.getAttribute("data-sollic-thread-id");
      if (id) {
        selectThread(id, { history: false });
      }
    } else {
      closeThreadList({ history: false });
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
