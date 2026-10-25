(function () {
  "use strict";

  function root() {
    return document.querySelector("[data-ndm-employer-propose-ssr]");
  }

  function parseUtc(iso) {
    return new Date(iso);
  }

  function slotsForDay(dayUtc) {
    var local = new Date(dayUtc);
    local.setHours(0, 0, 0, 0);
    var hours = [9, 10, 11, 14, 15];
    var out = [];
    hours.forEach(function (h) {
      var d = new Date(local);
      d.setHours(h, 0, 0, 0);
      out.push(d.toISOString());
    });
    return out;
  }

  function weekDays(weekStartUtc) {
    var start = parseUtc(weekStartUtc);
    var days = [];
    for (var i = 0; i < 5; i++) {
      var d = new Date(start);
      d.setUTCDate(start.getUTCDate() + i);
      days.push(d.toISOString());
    }
    return days;
  }

  function formatWeekLabel(weekStartUtc) {
    var start = parseUtc(weekStartUtc);
    var end = new Date(start);
    end.setUTCDate(start.getUTCDate() + 4);
    var fmt = { day: "numeric", month: "short" };
    return (
      start.toLocaleDateString("nl-NL", fmt) +
      " – " +
      end.toLocaleDateString("nl-NL", { day: "numeric", month: "short", year: "numeric" })
    );
  }

  function selectedSet(container) {
    var picked = container.querySelectorAll("[data-ndm-picked-slot]");
    var set = new Set();
    picked.forEach(function (el) {
      set.add(el.getAttribute("data-ndm-picked-slot"));
    });
    return set;
  }

  function syncWeekChips(container, selected) {
    container.querySelectorAll("[data-ndm-week-slot]").forEach(function (btn) {
      var iso = btn.getAttribute("data-ndm-week-slot");
      if (selected.has(iso)) btn.classList.add("on");
      else btn.classList.remove("on");
    });
  }

  function renderPickedList(container, selected) {
    var list = container.querySelector("[data-ndm-picked-slots]");
    if (!list) return;
    list.innerHTML = "";
    Array.from(selected)
      .sort()
      .forEach(function (iso) {
        var start = parseUtc(iso);
        var end = new Date(start.getTime() + 30 * 60000);
        var row = document.createElement("div");
        row.className = "slot on";
        row.setAttribute("data-ndm-picked-slot", iso);
        row.innerHTML =
          '<span class="ico navy"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="5" width="16" height="15" rx="2"/></svg></span>' +
          '<div class="sp"><b>' +
          start.toLocaleDateString("nl-NL", { weekday: "long", day: "numeric", month: "long" }) +
          '</b><span class="small muted">' +
          start.toLocaleTimeString("nl-NL", { hour: "2-digit", minute: "2-digit" }) +
          " tot " +
          end.toLocaleTimeString("nl-NL", { hour: "2-digit", minute: "2-digit" }) +
          '</span></div><button type="button" class="xbtn" data-ndm-remove-slot aria-label="Verwijderen">×</button>';
        list.appendChild(row);
      });
    bindRemoveSlots(container);
    updateSubmit(container);
  }

  function renderWeekGrid(container, weekStartUtc, selected) {
    var cal = container.querySelector("[data-ndm-week-calendar]");
    if (!cal) return;
    cal.setAttribute("data-week-start", weekStartUtc);
    var label = cal.querySelector("[data-ndm-week-label]");
    if (label) label.textContent = formatWeekLabel(weekStartUtc);
    var grid = cal.querySelector("[data-ndm-week-grid]");
    if (!grid) return;
    grid.innerHTML = "";
    weekDays(weekStartUtc).forEach(function (dayIso) {
      var day = parseUtc(dayIso);
      var col = document.createElement("div");
      col.className = "ndm-week__col";
      var lbl = document.createElement("span");
      lbl.className = "lbl";
      lbl.textContent = day.toLocaleDateString("nl-NL", { weekday: "short", day: "numeric", month: "short" });
      col.appendChild(lbl);
      slotsForDay(dayIso).forEach(function (slotIso) {
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "chip" + (selected.has(slotIso) ? " on" : "");
        btn.setAttribute("data-ndm-week-slot", slotIso);
        btn.textContent = parseUtc(slotIso).toLocaleTimeString("nl-NL", { hour: "2-digit", minute: "2-digit" });
        col.appendChild(btn);
      });
      grid.appendChild(col);
    });
    bindWeekSlots(container);
  }

  function updateSubmit(container) {
    var btn = container.querySelector("[data-ndm-submit-propose]");
    if (!btn) return;
    var n = container.querySelectorAll("[data-ndm-picked-slot]").length;
    btn.disabled = n < 2 || n > 3;
  }

  function toggleSlot(container, iso) {
    var selected = selectedSet(container);
    if (selected.has(iso)) selected.delete(iso);
    else if (selected.size < 3) selected.add(iso);
    renderPickedList(container, selected);
    syncWeekChips(container, selected);
  }

  function bindWeekSlots(container) {
    container.querySelectorAll("[data-ndm-week-slot]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        toggleSlot(container, btn.getAttribute("data-ndm-week-slot"));
      });
    });
  }

  function bindRemoveSlots(container) {
    container.querySelectorAll("[data-ndm-remove-slot]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var row = btn.closest("[data-ndm-picked-slot]");
        if (!row) return;
        var iso = row.getAttribute("data-ndm-picked-slot");
        var selected = selectedSet(container);
        selected.delete(iso);
        renderPickedList(container, selected);
        syncWeekChips(container, selected);
      });
    });
  }

  function bindModeChips(container) {
    var manualHint = container.querySelector("[data-ndm-pick-hint]");
    var picked = container.querySelector("[data-ndm-picked-slots]");
    var agendaNote = container.querySelector("[data-ndm-propose-agenda-note]");
    container.querySelectorAll("[data-ndm-propose-mode]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        var mode = chip.getAttribute("data-ndm-propose-mode");
        container.querySelectorAll("[data-ndm-propose-mode]").forEach(function (c) {
          c.classList.toggle("on", c === chip);
        });
        var agenda = mode === "agenda";
        if (agendaNote) agendaNote.hidden = !agenda;
        if (manualHint) manualHint.hidden = agenda;
        if (picked) picked.hidden = agenda;
      });
    });
  }

  function bindFormatChips(container) {
    container.querySelectorAll("[data-ndm-format]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        container.querySelectorAll("[data-ndm-format]").forEach(function (c) {
          c.classList.toggle("on", c === chip);
        });
      });
    });
  }

  function bindWeekNav(container) {
    var cal = container.querySelector("[data-ndm-week-calendar]");
    if (!cal) return;
    var prev = cal.querySelector("[data-ndm-week-prev]");
    var next = cal.querySelector("[data-ndm-week-next]");
    function shift(days) {
      var start = parseUtc(cal.getAttribute("data-week-start"));
      start.setUTCDate(start.getUTCDate() + days);
      renderWeekGrid(container, start.toISOString(), selectedSet(container));
    }
    if (prev) prev.addEventListener("click", function () { shift(-7); });
    if (next) next.addEventListener("click", function () { shift(7); });
  }

  function bindSubmit(container) {
    var btn = container.querySelector("[data-ndm-submit-propose]");
    if (!btn) return;
    var appId = container.getAttribute("data-application-id");
    btn.addEventListener("click", function () {
      if (btn.disabled || !appId) return;
      var formatEl = container.querySelector("[data-ndm-format].on");
      var format = formatEl ? parseInt(formatEl.getAttribute("data-ndm-format"), 10) : 0;
      var location = container.querySelector("[data-ndm-location]");
      var withWhom = container.querySelector("[data-ndm-with-whom]");
      var note = container.querySelector("[data-ndm-note]");
      var slots = Array.from(container.querySelectorAll("[data-ndm-picked-slot]")).map(function (el) {
        var iso = el.getAttribute("data-ndm-picked-slot");
        var start = parseUtc(iso);
        var end = new Date(start.getTime() + 30 * 60000);
        return { startUtc: start.toISOString(), endUtc: end.toISOString() };
      });
      btn.disabled = true;
      fetch("/api/ndm-employer-propose-client/slots?applicationId=" + encodeURIComponent(appId), {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          format: format,
          locationOrLink: location ? location.value : null,
          withWhom: withWhom ? withWhom.value : null,
          note: note ? note.value : null,
          slots: slots,
        }),
      })
        .then(function (res) {
          if (!res.ok) throw new Error("propose failed");
          return res.json();
        })
        .then(function (data) {
          if (data && data.redirectUrl) window.location.assign(data.redirectUrl);
        })
        .catch(function () {
          btn.disabled = false;
          updateSubmit(container);
        });
    });
  }

  function init() {
    var el = root();
    if (!el) return;
    bindModeChips(el);
    bindFormatChips(el);
    bindWeekSlots(el);
    bindRemoveSlots(el);
    bindWeekNav(el);
    bindSubmit(el);
    updateSubmit(el);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
