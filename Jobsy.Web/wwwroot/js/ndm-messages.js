(function () {
  "use strict";

  var MOBILE = window.matchMedia("(max-width: 600px)");
  var MARK_READ_PATH = "/api/sollicitaties-client/berichten/mark-read";

  function root() {
    return document.querySelector("[data-ndm-messages-ssr]");
  }

  function panelFor(threadId) {
    var container = root();
    if (!container || !threadId) {
      return null;
    }
    return container.querySelector('[data-ndm-thread-panel][data-ndm-thread-id="' + threadId + '"]');
  }

  function listItemFor(threadId) {
    var container = root();
    if (!container || !threadId) {
      return null;
    }
    return container.querySelector('[data-ndm-thread-select][data-ndm-thread-id="' + threadId + '"]');
  }

  function setThreadOpen(open) {
    var container = root();
    if (!container) {
      return;
    }
    container.classList.toggle("ndm-messages-thread-open", !!open);
  }

  function selectThread(threadId, options) {
    var container = root();
    if (!container || !threadId) {
      return;
    }

    container.querySelectorAll("[data-ndm-thread-panel]").forEach(function (panel) {
      var on = panel.getAttribute("data-ndm-thread-id") === threadId;
      panel.hidden = !on;
    });

    container.querySelectorAll("[data-ndm-thread-select]").forEach(function (item) {
      var on = item.getAttribute("data-ndm-thread-id") === threadId;
      item.classList.toggle("on", on);
      if (on) {
        item.setAttribute("aria-current", "true");
      } else {
        item.removeAttribute("aria-current");
      }
    });

    if (MOBILE.matches) {
      setThreadOpen(true);
    }

    if (options && options.history === "push") {
      try {
        var base = container.getAttribute("data-messages-base") || window.location.pathname + window.location.search;
        var url = new URL(base, window.location.origin);
        url.searchParams.set("thread", threadId);
        window.history.pushState({ ndmThread: threadId }, "", url.pathname + url.search + url.hash);
      } catch (_e) {
        /* ignore */
      }
    } else if (options && options.history === "replace") {
      try {
        var base2 = container.getAttribute("data-messages-base") || window.location.pathname + window.location.search;
        var url2 = new URL(base2, window.location.origin);
        url2.searchParams.set("thread", threadId);
        window.history.replaceState({ ndmThread: threadId }, "", url2.pathname + url2.search + url2.hash);
      } catch (_e2) {
        /* ignore */
      }
    }

    markThreadRead(threadId);
  }

  function markThreadRead(threadId) {
    if (!threadId) {
      return;
    }
    fetch(MARK_READ_PATH + "?applicationId=" + encodeURIComponent(threadId), {
      method: "POST",
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" },
    }).catch(function () {
      /* ignore */
    });
  }

  function closeThreadList(options) {
    setThreadOpen(false);
    if (options && options.history) {
      try {
        var container = root();
        var base = (container && container.getAttribute("data-messages-base")) || window.location.pathname + window.location.search;
        var url = new URL(base, window.location.origin);
        url.searchParams.delete("thread");
        window.history.pushState({}, "", url.pathname + url.search + url.hash);
      } catch (_e) {
        /* ignore */
      }
    }
  }

  function onThreadClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest("[data-ndm-thread-select]");
    if (!link || link.getAttribute("data-enhance-nav") === "false") {
      /* still intercept for in-page switch */
    }
    if (!link) {
      return;
    }
    var threadId = link.getAttribute("data-ndm-thread-id");
    if (!threadId) {
      return;
    }
    ev.preventDefault();
    selectThread(threadId, { history: "push" });
  }

  function onBackClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest("[data-ndm-messages-back]");
    if (!link) {
      return;
    }
    ev.preventDefault();
    closeThreadList({ history: true });
  }

  function onComposeSubmit(ev) {
    var form = ev.target;
    if (!form || !form.matches || !form.matches("[data-ndm-compose]")) {
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
        "X-Requested-With": "XMLHttpRequest",
      },
      body: JSON.stringify({ body: body, confirmSensitive: false }),
    })
      .then(function (res) {
        if (!res.ok) {
          throw new Error("send failed");
        }
        window.location.reload();
      })
      .catch(function () {
        if (submit) {
          submit.disabled = false;
        }
      });
  }

  function syncMobileLayout() {
    var container = root();
    if (!container) {
      return;
    }
    if (!MOBILE.matches) {
      setThreadOpen(false);
      return;
    }
    var openPanel = container.querySelector("[data-ndm-thread-panel]:not([hidden])");
    setThreadOpen(!!openPanel);
  }

  function init() {
    var container = root();
    if (!container || container.getAttribute("data-ndm-messages-bound") === "1") {
      return;
    }
    container.setAttribute("data-ndm-messages-bound", "1");

    container.addEventListener("click", onThreadClick);
    container.addEventListener("click", onBackClick);
    container.addEventListener("submit", onComposeSubmit);

    window.addEventListener("popstate", function () {
      try {
        var url = new URL(window.location.href);
        var thread = url.searchParams.get("thread");
        if (thread) {
          selectThread(thread, {});
        } else {
          closeThreadList({});
        }
      } catch (_e) {
        /* ignore */
      }
    });

    if (typeof MOBILE.addEventListener === "function") {
      MOBILE.addEventListener("change", syncMobileLayout);
    } else if (typeof MOBILE.addListener === "function") {
      MOBILE.addListener(syncMobileLayout);
    }

    syncMobileLayout();
    var active = container.querySelector("[data-ndm-thread-panel]:not([hidden])");
    if (active) {
      markThreadRead(active.getAttribute("data-ndm-thread-id"));
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
