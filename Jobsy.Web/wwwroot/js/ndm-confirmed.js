(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-confirmed-ssr]");
  }

  function icsHref(container) {
    var row = container.querySelector("[data-ndm-calendar-row]");
    return row ? row.getAttribute("data-ics-href") : null;
  }

  function openIcs(container) {
    var href = icsHref(container);
    if (href) {
      window.location.href = href;
    }
  }

  function bindCalendar(container) {
    var outlook = container.querySelector("[data-ndm-cal-outlook]");
    var google = container.querySelector("[data-ndm-cal-google]");
    var apple = container.querySelector("[data-ndm-cal-apple]");
    [outlook, google, apple].forEach(function (btn) {
      if (!btn) return;
      btn.addEventListener("click", function () {
        openIcs(container);
      });
    });
  }

  function readPrefs(list) {
    var day = list.querySelector('[data-ndm-reminder="dayBefore18"]');
    var two = list.querySelector('[data-ndm-reminder="twoHoursBefore"]');
    var week = list.querySelector('[data-ndm-reminder="weekBefore"]');
    return {
      weekBefore: week && week.classList.contains("on"),
      dayBefore18: day && day.classList.contains("on"),
      twoHoursBefore: two && two.classList.contains("on"),
    };
  }

  function postReminders(appId, body) {
    return fetch("/api/ndm-confirmed-client/reminders?applicationId=" + encodeURIComponent(appId), {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
      },
      body: JSON.stringify(body),
    });
  }

  function bindReminders(container) {
    var list = container.querySelector("[data-ndm-reminder-list]");
    if (!list) return;
    var appId = container.getAttribute("data-application-id");
    if (!appId) return;

    list.addEventListener("click", function (ev) {
      var btn = ev.target.closest("[data-ndm-reminder]");
      if (!btn || !list.contains(btn)) return;
      btn.classList.toggle("on");
      var on = btn.classList.contains("on");
      btn.setAttribute("aria-pressed", on ? "true" : "false");
      postReminders(appId, readPrefs(list)).catch(function () {
        btn.classList.toggle("on");
        btn.setAttribute("aria-pressed", btn.classList.contains("on") ? "true" : "false");
      });
    });
  }

  function bindAskLobsy(container) {
    var ask = container.querySelector("[data-ndm-ask-lobsy]");
    if (!ask) return;
    ask.addEventListener("click", function () {
      window.location.href = "/assistant";
    });
  }

  function boot() {
    var container = root();
    if (!container) return;
    bindCalendar(container);
    bindReminders(container);
    bindAskLobsy(container);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
