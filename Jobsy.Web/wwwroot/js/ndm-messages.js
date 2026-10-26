(function () {
  "use strict";

  var MOBILE = window.matchMedia("(max-width: 600px)");
  var MARK_READ_PATH = "/api/sollicitaties-client/berichten/mark-read";
  var READ_ACK_KEY = "sollicitaties-threads-read-ack-v1";
  var READ_PENDING_KEY = "sollicitaties-threads-read-pending-v1";

  function root() {
    return document.querySelector("[data-ndm-messages-ssr]");
  }

  function readIdSet(key) {
    try {
      var raw = sessionStorage.getItem(key);
      if (!raw) {
        return [];
      }
      var parsed = JSON.parse(raw);
      return Array.isArray(parsed)
        ? parsed.filter(function (id) {
            return typeof id === "string" && id;
          })
        : [];
    } catch (_e) {
      return [];
    }
  }

  function writeIdSet(key, ids) {
    try {
      sessionStorage.setItem(key, JSON.stringify(ids));
    } catch (_e2) {
      /* ignore */
    }
  }

  function markThreadReadAcked(threadId) {
    if (!threadId) {
      return;
    }
    var ack = readIdSet(READ_ACK_KEY);
    if (ack.indexOf(threadId) < 0) {
      ack.push(threadId);
      writeIdSet(READ_ACK_KEY, ack);
    }
    writeIdSet(
      READ_PENDING_KEY,
      readIdSet(READ_PENDING_KEY).filter(function (id) {
        return id !== threadId;
      })
    );
  }

  function setThreadOpen(open) {
    var container = root();
    if (!container) {
      return;
    }
    container.classList.toggle("ndm-messages-thread-open", !!open);
  }

  function clearUnreadBadge(threadId) {
    if (!threadId) {
      return;
    }
    var container = root();
    if (!container) {
      return;
    }
    container.querySelectorAll('[data-ndm-thread-select][data-ndm-thread-id="' + threadId + '"] .pill.warm').forEach(function (badge) {
      badge.remove();
    });
  }

  function applyStoredReadState() {
    var container = root();
    if (!container) {
      return;
    }
    var ack = readIdSet(READ_ACK_KEY);
    ack.forEach(function (threadId) {
      clearUnreadBadge(threadId);
    });
    try {
      var match = window.location.pathname.match(
        /\/candidate\/applications\/([0-9a-f-]{36})\/after-match/i
      );
      if (match && match[1]) {
        clearUnreadBadge(match[1]);
      }
    } catch (_e) {
      /* ignore */
    }
  }

  function updateBreadcrumbCompany(company) {
    var el = document.querySelector("[data-ndm-bc-company]");
    if (el && company) {
      el.textContent = company;
    }
  }

  function messagesUrlForThread(threadId) {
    return "/candidate/applications/" + encodeURIComponent(threadId) + "/after-match?panel=messages";
  }

  function selectThread(threadId, options) {
    var container = root();
    if (!container || !threadId) {
      return;
    }

    var link = container.querySelector('[data-ndm-thread-select][data-ndm-thread-id="' + threadId + '"]');
    var company = link && link.getAttribute("data-ndm-company");

    container.querySelectorAll("[data-ndm-thread-panel]").forEach(function (panel) {
      panel.hidden = panel.getAttribute("data-ndm-thread-id") !== threadId;
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

    updateBreadcrumbCompany(company || "");
    container.setAttribute("data-context-application-id", threadId);

    if (MOBILE.matches) {
      setThreadOpen(true);
    }

    if (options && (options.history === "push" || options.history === "replace")) {
      try {
        var url = messagesUrlForThread(threadId);
        var fn = options.history === "push" ? "pushState" : "replaceState";
        window.history[fn]({ ndmThread: threadId }, "", url);
      } catch (_e) {
        /* ignore */
      }
    }

    clearUnreadBadge(threadId);
    markThreadRead(threadId);
  }

  function markThreadRead(threadId) {
    if (!threadId) {
      return;
    }
    markThreadReadAcked(threadId);
    fetch(MARK_READ_PATH + "?applicationId=" + encodeURIComponent(threadId), {
      method: "POST",
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" },
    })
      .then(function (res) {
        if (res.ok) {
          markThreadReadAcked(threadId);
        }
      })
      .catch(function () {
        /* ignore */
      });
  }

  function onThreadClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest("[data-ndm-thread-select]");
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
    container.addEventListener("submit", onComposeSubmit);

    window.addEventListener("popstate", function () {
      try {
        var match = window.location.pathname.match(
          /\/candidate\/applications\/([0-9a-f-]{36})\/after-match/i
        );
        if (match && match[1]) {
          selectThread(match[1], {});
        }
      } catch (_e) {
        /* ignore */
      }
    });

    window.addEventListener("pageshow", function (ev) {
      if (ev && ev.persisted) {
        applyStoredReadState();
      }
    });

    if (typeof MOBILE.addEventListener === "function") {
      MOBILE.addEventListener("change", syncMobileLayout);
    } else if (typeof MOBILE.addListener === "function") {
      MOBILE.addListener(syncMobileLayout);
    }

    applyStoredReadState();
    syncMobileLayout();
    var active = container.querySelector("[data-ndm-thread-panel]:not([hidden])");
    if (active) {
      var threadId = active.getAttribute("data-ndm-thread-id");
      clearUnreadBadge(threadId);
      markThreadRead(threadId);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
