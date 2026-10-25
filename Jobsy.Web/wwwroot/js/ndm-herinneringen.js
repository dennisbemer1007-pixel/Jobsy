(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-herinneringen-ssr]");
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

  function toggleSwitch(btn) {
    btn.classList.toggle("on");
    var on = btn.classList.contains("on");
    btn.setAttribute("aria-pressed", on ? "true" : "false");
  }

  function bind(container) {
    var settings = container.querySelector("[data-ndm-reminder-settings]");
    if (!settings || !settings.hasAttribute("data-ndm-reminder-editable")) return;
    var list = settings.querySelector("[data-ndm-reminder-list]");
    if (!list) return;
    var appId = container.getAttribute("data-application-id");
    if (!appId) return;

    list.addEventListener("click", function (ev) {
      var btn = ev.target.closest("[data-ndm-reminder]");
      if (!btn || !list.contains(btn)) return;
      toggleSwitch(btn);
    });

    var save = settings.querySelector("[data-ndm-reminder-save]");
    if (!save) return;
    save.addEventListener("click", function () {
      save.disabled = true;
      postReminders(appId, readPrefs(list))
        .catch(function () {})
        .finally(function () {
          save.disabled = false;
        });
    });
  }

  function init() {
    var el = root();
    if (el) bind(el);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
