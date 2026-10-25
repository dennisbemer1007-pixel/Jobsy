(function () {
  "use strict";

  var MOBILE = window.matchMedia("(max-width: 600px)");
  var MARK_READ_PATH = "/api/werkgever-client/berichten/mark-read";
  var PREVIEW_PATH = "/api/werkgever-client/berichten/preview";
  var SEND_PATH = "/api/werkgever-client/berichten/send";
  var READ_ACK_KEY = "werkgever-threads-read-ack-v1";

  function root() {
    return document.querySelector("[data-ndm-employer-messages-ssr]");
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
  }

  function setThreadOpen(open) {
    var container = root();
    if (!container) {
      return;
    }
    container.classList.toggle("ndm-wg-messages-thread-open", !!open);
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

  function hostApplicationId(container) {
    container = container || root();
    if (container) {
      var host = container.getAttribute("data-ndm-messages-host-id");
      if (host) {
        return host;
      }
    }
    try {
      var match = window.location.pathname.match(
        /\/werkgever\/sollicitaties\/([0-9a-f-]{36})\/after-match/i
      );
      return match && match[1];
    } catch (_e) {
      return null;
    }
  }

  function messagesListUrl() {
    var hostId = hostApplicationId();
    if (!hostId) {
      return null;
    }
    return (
      "/werkgever/sollicitaties/" +
      encodeURIComponent(hostId) +
      "/after-match?panel=messages"
    );
  }

  function messagesUrlForThread(threadId) {
    var hostId = hostApplicationId();
    if (!hostId) {
      return null;
    }
    return (
      "/werkgever/sollicitaties/" +
      encodeURIComponent(hostId) +
      "/after-match?panel=messages&thread=" +
      encodeURIComponent(threadId)
    );
  }

  function selectThread(threadId, options) {
    var container = root();
    if (!container || !threadId) {
      return;
    }

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

    if (MOBILE.matches) {
      setThreadOpen(true);
    }

    if (options && (options.history === "push" || options.history === "replace")) {
      try {
        var url = messagesUrlForThread(threadId);
        if (url) {
          var fn = options.history === "push" ? "pushState" : "replaceState";
          window.history[fn]({ ndmWgThread: threadId }, "", url);
        }
      } catch (_e) {
        /* ignore */
      }
    }

    clearUnreadBadge(threadId);
    markThreadRead(threadId);
    syncSensitiveWarning(threadId);
  }

  function activePanel() {
    var container = root();
    if (!container) {
      return null;
    }
    return container.querySelector("[data-ndm-thread-panel]:not([hidden])");
  }

  function syncSensitiveWarning(threadId) {
    var panel = activePanel();
    if (!panel || panel.getAttribute("data-ndm-thread-id") !== threadId) {
      return;
    }
    var input = panel.querySelector(".ndm-wg-messages-compose-input");
    var warn = panel.querySelector("[data-ndm-sensitive-warning]");
    if (!input || !warn) {
      return;
    }
    previewDraft(threadId, input.value.trim(), warn);
  }

  var previewTimer = null;
  function previewDraft(appId, body, warnEl) {
    if (!appId || !warnEl) {
      return;
    }
    if (!body) {
      warnEl.hidden = true;
      return;
    }
    clearTimeout(previewTimer);
    previewTimer = setTimeout(function () {
      fetch(PREVIEW_PATH + "?applicationId=" + encodeURIComponent(appId), {
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
            throw new Error("preview failed");
          }
          return res.json();
        })
        .then(function (preview) {
          if (preview && preview.wouldBlock) {
            warnEl.hidden = false;
          } else {
            warnEl.hidden = true;
          }
        })
        .catch(function () {
          warnEl.hidden = true;
        });
    }, 250);
  }

  function messagesPanelVisible(container) {
    if (!container) {
      return false;
    }
    if (container.getAttribute("data-ndm-view") === "messages") {
      return true;
    }
    var wrap = container.closest("[data-ndm-messages-panel]");
    return !wrap || !wrap.hidden;
  }

  function markThreadRead(threadId) {
    if (!threadId) {
      return;
    }
    var container = root();
    if (container && !messagesPanelVisible(container)) {
      return;
    }
    markThreadReadAcked(threadId);
    fetch(MARK_READ_PATH + "?applicationId=" + encodeURIComponent(threadId), {
      method: "POST",
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" },
    }).catch(function () {
      /* ignore */
    });
  }

  function onThreadClick(ev) {
    var link = ev.target && ev.target.closest && ev.target.closest("[data-ndm-thread-select]");
    if (!link) {
      return false;
    }
    var container = root();
    if (!container || !container.contains(link) || !messagesPanelVisible(container)) {
      return false;
    }
    var threadId = link.getAttribute("data-ndm-thread-id");
    if (!threadId) {
      return false;
    }
    ev.preventDefault();
    selectThread(threadId, { history: "push" });
    return true;
  }

  function onQuickReply(ev) {
    var btn = ev.target && ev.target.closest && ev.target.closest("[data-ndm-quick-reply]");
    if (!btn) {
      return;
    }
    var panel = activePanel();
    if (!panel) {
      return;
    }
    var input = panel.querySelector(".ndm-wg-messages-compose-input");
    if (input) {
      input.value = btn.getAttribute("data-ndm-quick-reply") || "";
      input.focus();
      syncSensitiveWarning(panel.getAttribute("data-ndm-thread-id"));
    }
  }

  function onComposeInput(ev) {
    var input = ev.target;
    if (!input || !input.matches || !input.matches(".ndm-wg-messages-compose-input")) {
      return;
    }
    var form = input.closest("[data-ndm-compose]");
    var appId = form && form.getAttribute("data-application-id");
    var warn = form && form.parentElement && form.parentElement.querySelector("[data-ndm-sensitive-warning]");
    if (appId && warn) {
      previewDraft(appId, input.value.trim(), warn);
    }
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
    fetch(SEND_PATH + "?applicationId=" + encodeURIComponent(appId), {
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

  function shouldForceMobileList(container) {
    if (!container) {
      return false;
    }
    try {
      var params = new URL(window.location.href).searchParams;
      if (params.get("thread")) {
        return false;
      }
      if (params.get("panel") === "messages") {
        return true;
      }
      if (container.getAttribute("data-ndm-messages-list-first") === "1") {
        return true;
      }
      var page = container.closest("[data-ndm-employer-ssr]");
      return !!(page && page.getAttribute("data-ndm-panel") === "messages");
    } catch (_e) {
      return false;
    }
  }

  function showMobileListOnly(container) {
    if (!container || !MOBILE.matches) {
      return;
    }
    setThreadOpen(false);
    container.querySelectorAll("[data-ndm-thread-panel]").forEach(function (panel) {
      panel.hidden = true;
    });
    container.querySelectorAll("[data-ndm-thread-select]").forEach(function (item) {
      item.classList.remove("on");
      item.removeAttribute("aria-current");
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
    if (shouldForceMobileList(container)) {
      showMobileListOnly(container);
      return;
    }
    var openPanel = container.querySelector("[data-ndm-thread-panel]:not([hidden])");
    setThreadOpen(!!openPanel);
  }

  window.NdmEmployerMessagesResetMobileList = function () {
    showMobileListOnly(root());
  };

  window.NdmEmployerMessagesSyncLayout = syncMobileLayout;

  function isMobileThreadOpen() {
    var container = root();
    return !!(container && MOBILE.matches && container.classList.contains("ndm-wg-messages-thread-open"));
  }

  function backToMobileList() {
    var container = root();
    if (!container || !MOBILE.matches) {
      return false;
    }
    try {
      var params = new URL(window.location.href).searchParams;
      if (params.get("thread")) {
        history.back();
        return true;
      }
    } catch (_e) {
      /* ignore */
    }
    showMobileListOnly(container);
    try {
      var listUrl = messagesListUrl();
      if (listUrl) {
        history.replaceState({ ndmWgPanel: "messages" }, "", listUrl);
      }
    } catch (_e2) {
      /* ignore */
    }
    return true;
  }

  window.NdmEmployerMessagesBackToList = backToMobileList;
  window.NdmEmployerMessagesIsMobileThreadOpen = isMobileThreadOpen;

  function bindPopstateOnce() {
    if (window.__ndmEmployerMessagesPopstateBound) {
      return;
    }
    window.__ndmEmployerMessagesPopstateBound = true;
    window.addEventListener("popstate", function () {
      var container = root();
      if (!container) {
        return;
      }
      try {
        var params = new URL(window.location.href).searchParams;
        var threadId = params.get("thread");
        if (shouldForceMobileList(container)) {
          showMobileListOnly(container);
          return;
        }
        if (!threadId) {
          var match = window.location.pathname.match(
            /\/werkgever\/sollicitaties\/([0-9a-f-]{36})\/after-match/i
          );
          threadId = match && match[1];
        }
        if (threadId) {
          selectThread(threadId, {});
        }
      } catch (_e) {
        /* ignore */
      }
    });
  }

  function init() {
    var container = root();
    if (!container) {
      return;
    }
    if (!messagesPanelVisible(container)) {
      return;
    }

    var firstBind = container.getAttribute("data-ndm-employer-messages-bound") !== "1";
    if (firstBind) {
      container.setAttribute("data-ndm-employer-messages-bound", "1");
      container.addEventListener("click", onQuickReply);
      container.addEventListener("input", onComposeInput);
      container.addEventListener("submit", onComposeSubmit);
      bindPopstateOnce();
      if (typeof MOBILE.addEventListener === "function") {
        MOBILE.addEventListener("change", syncMobileLayout);
      } else if (typeof MOBILE.addListener === "function") {
        MOBILE.addListener(syncMobileLayout);
      }
    }

    syncMobileLayout();
    var params;
    try {
      params = new URL(window.location.href).searchParams;
    } catch (_e2) {
      params = null;
    }
    var explicitThread = params && params.get("thread");
    var active = container.querySelector("[data-ndm-thread-panel]:not([hidden])");
    if (active && explicitThread) {
      var threadId = active.getAttribute("data-ndm-thread-id");
      clearUnreadBadge(threadId);
      markThreadRead(threadId);
    }
  }

  function bindEnhancedNavRecovery() {
    function onNav() {
      var container = root();
      if (container) {
        container.removeAttribute("data-ndm-employer-messages-bound");
      }
      init();
    }
    document.addEventListener("enhancedload", onNav);
    try {
      if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
        window.Blazor.addEventListener("enhancedload", onNav);
      }
    } catch (_e) {
      /* ignore */
    }
    if (window.__ndmEmployerMessagesCaptureBound) {
      return;
    }
    window.__ndmEmployerMessagesCaptureBound = true;
    document.addEventListener("click", onThreadClick, true);
  }

  window.NdmEmployerMessagesInit = init;
  bindEnhancedNavRecovery();

  function tryAutoInit() {
    var container = root();
    if (container && messagesPanelVisible(container)) {
      if (shouldForceMobileList(container)) {
        showMobileListOnly(container);
      }
      init();
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", tryAutoInit);
  } else {
    tryAutoInit();
  }
})();
