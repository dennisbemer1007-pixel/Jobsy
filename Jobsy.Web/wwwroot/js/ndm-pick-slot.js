(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-pick-slot-ssr]");
  }

  function selectedSlotId(container) {
    var on = container.querySelector(".cal3 .slot.on[data-slot-id]");
    return on ? on.getAttribute("data-slot-id") : null;
  }

  function selectSlotButton(list, container, btn) {
    list.querySelectorAll(".slot").forEach(function (el) {
      el.classList.remove("on");
      el.setAttribute("aria-pressed", "false");
      var radio = el.querySelector(".radio");
      if (radio) radio.classList.remove("on");
    });
    btn.classList.add("on");
    btn.setAttribute("aria-pressed", "true");
    var radioOn = btn.querySelector(".radio");
    if (radioOn) radioOn.classList.add("on");
    var confirm = container.querySelector("[data-ndm-confirm-slot]");
    if (confirm) confirm.disabled = false;
    var err = container.querySelector("[data-ndm-pick-slot-error]");
    if (err) err.hidden = true;
  }

  function bindSlots(container) {
    var list = container.querySelector("[data-ndm-slot-list]");
    if (!list) return;

    list.addEventListener("click", function (ev) {
      var btn = ev.target.closest(".slot[data-slot-id]");
      if (!btn || !list.contains(btn)) return;
      selectSlotButton(list, container, btn);
    });
  }

  function scrollPropose(container) {
    var link = container.querySelector("[data-ndm-scroll-propose]");
    var form = container.querySelector("#ndm-propose-form");
    if (!link || !form) return;
    link.addEventListener("click", function () {
      form.scrollIntoView({ behavior: "smooth", block: "start" });
      var first = form.querySelector("input, textarea");
      if (first) first.focus({ preventScroll: true });
    });
  }

  function postJson(url, body) {
    return fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "same-origin",
      body: JSON.stringify(body),
    });
  }

  function bindConfirm(container) {
    var btn = container.querySelector("[data-ndm-confirm-slot]");
    if (!btn) return;
    var appId = container.getAttribute("data-application-id");
    btn.addEventListener("click", function () {
      var slotId = selectedSlotId(container);
      if (!slotId || !appId) return;
      btn.disabled = true;
      postJson(
        "/api/ndm-pick-slot-client/select-slot?applicationId=" +
          encodeURIComponent(appId),
        { slotId: slotId }
      )
        .then(function (res) {
          if (!res.ok) throw new Error("select failed");
          return res.json();
        })
        .then(function (data) {
          if (data && data.redirectUrl) {
            window.location.assign(data.redirectUrl);
          }
        })
        .catch(function () {
          btn.disabled = false;
          var err = container.querySelector("[data-ndm-pick-slot-error]");
          if (err) err.hidden = false;
        });
    });
  }

  function bindPropose(container) {
    var btn = container.querySelector("[data-ndm-propose-submit]");
    if (!btn) return;
    var appId = btn.getAttribute("data-application-id");
    btn.addEventListener("click", function () {
      var form = container.querySelector("#ndm-propose-form");
      if (!form || !appId) return;
      var dateEl = form.querySelector("[data-ndm-propose-date]");
      var timeEl = form.querySelector("[data-ndm-propose-time]");
      var noteEl = form.querySelector("[data-ndm-propose-note]");
      if (!dateEl || !timeEl || !noteEl) return;
      if (!dateEl.value.trim() || !timeEl.value.trim() || !noteEl.value.trim()) return;
      btn.disabled = true;
      postJson(
        "/api/ndm-pick-slot-client/propose-slot?applicationId=" +
          encodeURIComponent(appId),
        {
          dateLabel: dateEl.value.trim(),
          timeWindow: timeEl.value.trim(),
          note: noteEl.value.trim(),
        }
      )
        .then(function (res) {
          if (!res.ok) throw new Error("propose failed");
          return res.json();
        })
        .then(function (data) {
          if (data && data.redirectUrl) {
            window.location.assign(data.redirectUrl);
          }
        })
        .catch(function () {
          btn.disabled = false;
        });
    });
  }

  function bindAskLobsy(container) {
    var btn = container.querySelector("[data-ndm-ask-lobsy]");
    if (!btn) return;
    btn.addEventListener("click", function () {
      var coach = document.querySelector(".lobsy-coach-cta, .lobsy-coach-dock button");
      if (coach) coach.click();
    });
  }

  function init() {
    var container = root();
    if (!container) return;
    bindSlots(container);
    scrollPropose(container);
    bindConfirm(container);
    bindPropose(container);
    bindAskLobsy(container);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
