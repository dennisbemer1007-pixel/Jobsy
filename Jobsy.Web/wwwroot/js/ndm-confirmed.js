(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-confirmed-ssr]");
  }

  function calendarRow(container) {
    return container.querySelector("[data-ndm-calendar-row]");
  }

  function openHref(href) {
    if (!href) return;
    window.open(href, "_blank", "noopener,noreferrer");
  }

  function bindCalendar(container) {
    var row = calendarRow(container);
    if (!row) return;
    var icsHref = row.getAttribute("data-ics-href");
    var googleHref = row.getAttribute("data-google-href");
    var outlookHref = row.getAttribute("data-outlook-href");
    var outlook = container.querySelector("[data-ndm-cal-outlook]");
    var google = container.querySelector("[data-ndm-cal-google]");
    var apple = container.querySelector("[data-ndm-cal-apple]");
    if (outlook) {
      outlook.addEventListener("click", function () {
        openHref(outlookHref);
      });
    }
    if (google) {
      google.addEventListener("click", function () {
        openHref(googleHref);
      });
    }
    if (apple) {
      apple.addEventListener("click", function () {
        if (icsHref) window.location.href = icsHref;
      });
    }
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

  function toggleReminder(btn) {
    btn.classList.toggle("on");
    var on = btn.classList.contains("on");
    btn.setAttribute("aria-pressed", on ? "true" : "false");
    return on;
  }

  function bindReminders(container) {
    var list = container.querySelector("[data-ndm-reminder-list]");
    if (!list) return;
    var appId = container.getAttribute("data-application-id");
    if (!appId) return;

    function onToggle(btn) {
      var wasOn = btn.classList.contains("on");
      toggleReminder(btn);
      postReminders(appId, readPrefs(list)).catch(function () {
        if (wasOn) {
          btn.classList.add("on");
        } else {
          btn.classList.remove("on");
        }
        btn.setAttribute("aria-pressed", btn.classList.contains("on") ? "true" : "false");
      });
    }

    list.addEventListener("click", function (ev) {
      var row = ev.target.closest("[data-ndm-reminder-row]");
      if (!row || !list.contains(row)) return;
      var btn = row.querySelector("[data-ndm-reminder]");
      if (!btn) return;
      onToggle(btn);
    });

    list.addEventListener("keydown", function (ev) {
      if (ev.key !== "Enter" && ev.key !== " ") return;
      var row = ev.target.closest("[data-ndm-reminder-row]");
      if (!row || !list.contains(row)) return;
      ev.preventDefault();
      var btn = row.querySelector("[data-ndm-reminder]");
      if (btn) onToggle(btn);
    });
  }

  function bindAskLobsy(container) {
    var ask = container.querySelector("[data-ndm-ask-lobsy]");
    if (!ask) return;
    ask.addEventListener("click", function () {
      var coach = document.querySelector(".lobsy-coach-cta, .lobsy-coach-dock button, #lobsy-coach-btn");
      if (coach) {
        coach.click();
        return;
      }
      var fallback = ask.getAttribute("data-assistant-fallback");
      if (fallback) window.location.href = fallback;
    });
  }

  function scrollCancelPanel(container) {
    var params = new URLSearchParams(window.location.search);
    if (params.get("panel") !== "cancel" && !window.location.hash) return;
    var panel = container.querySelector("#ndm-cancel-panel");
    if (!panel) return;
    if (params.get("panel") === "cancel" || window.location.hash === "#ndm-cancel-panel") {
      panel.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }

  function bindCancel(container) {
    var btn = container.querySelector("[data-ndm-cancel-submit]");
    var reasonEl = container.querySelector("[data-ndm-cancel-reason]");
    if (!btn || !reasonEl) return;
    var appId = container.getAttribute("data-application-id");
    if (!appId) return;
    btn.addEventListener("click", function () {
      var reason = (reasonEl.value || "").trim();
      if (!reason) {
        reasonEl.focus();
        return;
      }
      btn.disabled = true;
      fetch("/api/ndm-confirmed-client/cancel?applicationId=" + encodeURIComponent(appId), {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
        body: JSON.stringify({ reason: reason }),
      })
        .then(function (resp) {
          if (!resp.ok) throw new Error("cancel failed");
          window.location.reload();
        })
        .catch(function () {
          btn.disabled = false;
        });
    });
  }

  function boot() {
    var container = root();
    if (!container) return;
    bindCalendar(container);
    bindReminders(container);
    bindAskLobsy(container);
    bindCancel(container);
    scrollCancelPanel(container);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
