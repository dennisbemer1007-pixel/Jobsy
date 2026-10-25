(function () {
  "use strict";

  var TZ = "Europe/Amsterdam";
  var SLOT_MS = 30 * 60000;

  function root() {
    return document.querySelector("[data-ndm-employer-ssr], [data-ndm-employer-reschedule-ssr]");
  }

  function parseUtc(iso) {
    return new Date(iso);
  }

  function capitalizeNl(s) {
    if (!s) return s;
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  function formatDayLabel(isoUtc) {
    var d = parseUtc(isoUtc);
    var wd = d.toLocaleDateString("nl-NL", { weekday: "long", day: "numeric", month: "long", timeZone: TZ });
    return capitalizeNl(wd);
  }

  function formatTimeRange(isoUtc) {
    var start = parseUtc(isoUtc);
    var end = new Date(start.getTime() + SLOT_MS);
    var fmt = { hour: "2-digit", minute: "2-digit", timeZone: TZ };
    return (
      start.toLocaleTimeString("nl-NL", fmt) +
      " tot " +
      end.toLocaleTimeString("nl-NL", fmt)
    );
  }

  function amsterdamWallToUtcIso(dateStr, timeStr) {
    if (!dateStr || !timeStr) return null;
    var probe = new Date(dateStr + "T" + timeStr + ":00Z");
    var utcGuess = probe.getTime();
    var fmt = new Intl.DateTimeFormat("en-US", {
      timeZone: TZ,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
    for (var i = 0; i < 3; i++) {
      var parts = fmt.formatToParts(new Date(utcGuess));
      var got = {};
      parts.forEach(function (p) {
        if (p.type !== "literal") got[p.type] = p.value;
      });
      var target = Date.UTC(
        parseInt(dateStr.slice(0, 4), 10),
        parseInt(dateStr.slice(5, 7), 10) - 1,
        parseInt(dateStr.slice(8, 10), 10),
        parseInt(timeStr.slice(0, 2), 10),
        parseInt(timeStr.slice(3, 5), 10)
      );
      var shown = Date.UTC(
        parseInt(got.year, 10),
        parseInt(got.month, 10) - 1,
        parseInt(got.day, 10),
        parseInt(got.hour, 10),
        parseInt(got.minute, 10)
      );
      utcGuess += target - shown;
    }
    return new Date(utcGuess).toISOString();
  }

  function minDateStrAmsterdam() {
    var now = new Date();
    return now.toLocaleDateString("en-CA", { timeZone: TZ });
  }

  function minTimeStrIfToday(dateStr) {
    if (dateStr !== minDateStrAmsterdam()) return "00:00";
    return new Date().toLocaleTimeString("en-GB", {
      timeZone: TZ,
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
  }

  function isPastSlot(isoUtc) {
    return parseUtc(isoUtc).getTime() < Date.now();
  }

  function baseAfterMatchUrl(el) {
    var appId = el.getAttribute("data-application-id");
    if (el.hasAttribute("data-ndm-employer-reschedule-ssr")) {
      return (
        "/werkgever/sollicitaties/" +
        encodeURIComponent(appId) +
        "/after-match/reschedule-ssr"
      );
    }
    return "/werkgever/sollicitaties/" + encodeURIComponent(appId) + "/after-match";
  }

  function panelFromUrl() {
    try {
      return new URL(window.location.href).searchParams.get("panel") || "";
    } catch (e) {
      return "";
    }
  }

  function setPanel(el, panel, push) {
    panel = panel || "";
    if (panel === "slots") panel = "propose";
    el.setAttribute("data-ndm-panel", panel);

    var flow = el.querySelector("[data-ndm-wg-flow]");
    var messagesPanel = el.querySelector("[data-ndm-messages-panel]");
    var proposePanel = el.querySelector("[data-ndm-propose-panel]");
    var cancelPanel = el.querySelector("[data-ndm-cancel-panel]");
    var shareAside = el.querySelector("[data-ndm-share-aside], [data-ndm-wg-aside]");
    var infoNote = el.querySelector("[data-ndm-propose-info-note]");
    var whatGrid = el.querySelector(".what");
    var actions = el.querySelector("[data-ndm-wg-main] .row.wrapflex");

    if (panel === "messages") {
      if (flow) flow.hidden = true;
      if (messagesPanel) messagesPanel.hidden = false;
      syncHero("messages");
      if (window.NdmEmployerMessagesInit) {
        window.NdmEmployerMessagesInit();
      }
    } else if (panel === "propose") {
      if (flow) flow.hidden = false;
      if (messagesPanel) messagesPanel.hidden = true;
      if (proposePanel) proposePanel.hidden = false;
      if (cancelPanel) cancelPanel.hidden = true;
      el.querySelectorAll("[data-ndm-share-aside], [data-ndm-wg-aside]").forEach(function (aside) {
        aside.hidden = true;
      });
      if (infoNote) infoNote.hidden = false;
      if (whatGrid) whatGrid.hidden = true;
      if (actions) actions.hidden = true;
      syncHero("propose");
    } else if (panel === "cancel") {
      if (flow) flow.hidden = false;
      if (messagesPanel) messagesPanel.hidden = true;
      if (proposePanel) proposePanel.hidden = true;
      if (cancelPanel) cancelPanel.hidden = false;
      el.querySelectorAll("[data-ndm-share-aside], [data-ndm-wg-aside]").forEach(function (aside) {
        aside.hidden = true;
      });
      if (infoNote) infoNote.hidden = true;
      if (whatGrid) whatGrid.hidden = true;
      if (actions) actions.hidden = true;
      syncHero("");
    } else {
      if (flow) flow.hidden = false;
      if (messagesPanel) messagesPanel.hidden = true;
      if (proposePanel) proposePanel.hidden = true;
      if (cancelPanel) cancelPanel.hidden = true;
      el.querySelectorAll("[data-ndm-share-aside], [data-ndm-wg-aside]").forEach(function (aside) {
        aside.hidden = false;
      });
      if (infoNote) infoNote.hidden = true;
      if (whatGrid) whatGrid.hidden = false;
      if (actions) actions.hidden = false;
      syncHero("");
    }

    if (push) {
      var url = baseAfterMatchUrl(el);
      if (panel) url += "?panel=" + encodeURIComponent(panel);
      history.pushState({ ndmPanel: panel }, "", url);
    }
  }

  function syncHero(panel) {
    var el = root();
    var titleEl = document.querySelector(".wtitle h1");
    var leadEl = document.querySelector(".wtitle p");
    if (!el || !titleEl || !leadEl) return;
    var titleKey =
      panel === "propose"
        ? "data-ndm-title-propose"
        : panel === "messages"
          ? "data-ndm-title-messages"
          : "data-ndm-title-home";
    var leadKey =
      panel === "propose"
        ? "data-ndm-lead-propose"
        : panel === "messages"
          ? "data-ndm-lead-messages"
          : "data-ndm-lead-home";
    var title = el.getAttribute(titleKey);
    var lead = el.getAttribute(leadKey);
    if (title) titleEl.textContent = title;
    if (lead) leadEl.textContent = lead;
  }

  function selectedSet(container) {
    var set = new Set();
    container.querySelectorAll("[data-ndm-picked-slot]").forEach(function (el) {
      set.add(el.getAttribute("data-ndm-picked-slot"));
    });
    return set;
  }

  function renderPickedList(container, selected) {
    var list = container.querySelector("[data-ndm-picked-slots]");
    if (!list) return;
    list.innerHTML = "";
    Array.from(selected)
      .sort()
      .forEach(function (iso) {
        var row = document.createElement("div");
        row.className = "slot on";
        row.setAttribute("data-ndm-picked-slot", iso);
        row.innerHTML =
          '<span class="ico navy"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="4" y="5" width="16" height="15" rx="2"/><path d="M4 10h16M9 3v4M15 3v4"/></svg></span>' +
          '<div class="sp"><b>' +
          formatDayLabel(iso) +
          '</b><span class="small muted">' +
          formatTimeRange(iso) +
          '</span></div><button type="button" class="xbtn" data-ndm-remove-slot aria-label="Verwijderen">×</button>';
        list.appendChild(row);
      });
    bindRemoveSlots(container);
    updateSubmit(container);
  }

  function updateSubmit(container) {
    var btn = container.querySelector("[data-ndm-submit-propose]");
    if (!btn) return;
    var n = container.querySelectorAll("[data-ndm-picked-slot]").length;
    btn.disabled = n < 2 || n > 3;
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
      });
    });
  }

  function bindModeChips(container) {
    var manual = container.querySelector("[data-ndm-manual-block]");
    var agenda = container.querySelector("[data-ndm-agenda-block]");
    container.querySelectorAll("[data-ndm-propose-mode]").forEach(function (chip) {
      chip.addEventListener("click", function () {
        var mode = chip.getAttribute("data-ndm-propose-mode");
        container.querySelectorAll("[data-ndm-propose-mode]").forEach(function (c) {
          c.classList.toggle("on", c === chip);
        });
        var isAgenda = mode === "agenda";
        if (manual) manual.hidden = isAgenda;
        if (agenda) agenda.hidden = !isAgenda;
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

  function bindAddSlot(container) {
    var addBtn = container.querySelector("[data-ndm-add-slot]");
    var form = container.querySelector("[data-ndm-add-slot-form]");
    var dateInp = container.querySelector("[data-ndm-add-date]");
    var timeInp = container.querySelector("[data-ndm-add-time]");
    var confirmBtn = container.querySelector("[data-ndm-confirm-add-slot]");
    if (!addBtn || !form || !dateInp || !timeInp) return;

    dateInp.min = minDateStrAmsterdam();

    addBtn.addEventListener("click", function () {
      form.hidden = !form.hidden;
      if (!form.hidden) {
        dateInp.min = minDateStrAmsterdam();
        if (!dateInp.value) dateInp.value = dateInp.min;
        timeInp.min = minTimeStrIfToday(dateInp.value);
      }
    });

    dateInp.addEventListener("change", function () {
      timeInp.min = minTimeStrIfToday(dateInp.value);
    });

    if (confirmBtn) {
      confirmBtn.addEventListener("click", function () {
        var selected = selectedSet(container);
        if (selected.size >= 3) return;
        var iso = amsterdamWallToUtcIso(dateInp.value, timeInp.value);
        if (!iso || isPastSlot(iso)) return;
        if (selected.has(iso)) return;
        selected.add(iso);
        renderPickedList(container, selected);
        form.hidden = true;
      });
    }
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
        var end = new Date(start.getTime() + SLOT_MS);
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

  function bindPanelNav(container) {
    container.querySelectorAll("[data-ndm-open-panel]").forEach(function (btn) {
      if (btn.getAttribute("data-ndm-panel-bound") === "1") {
        return;
      }
      btn.setAttribute("data-ndm-panel-bound", "1");
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        var panel = btn.getAttribute("data-ndm-open-panel");
        setPanel(container, panel, true);
      });
    });

    container.querySelectorAll("[data-ndm-close-panel]").forEach(function (btn) {
      if (btn.getAttribute("data-ndm-panel-bound") === "1") {
        return;
      }
      btn.setAttribute("data-ndm-panel-bound", "1");
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        setPanel(container, "", true);
      });
    });
  }

  function handlePanelClick(ev) {
    var openBtn = ev.target && ev.target.closest && ev.target.closest("[data-ndm-open-panel]");
    if (openBtn) {
      var el = root();
      if (!el) {
        return;
      }
      ev.preventDefault();
      ev.stopImmediatePropagation();
      setPanel(el, openBtn.getAttribute("data-ndm-open-panel"), true);
      return;
    }
    var closeBtn = ev.target && ev.target.closest && ev.target.closest("[data-ndm-close-panel]");
    if (closeBtn) {
      var container = root();
      if (!container) {
        return;
      }
      ev.preventDefault();
      ev.stopImmediatePropagation();
      setPanel(container, "", true);
    }
  }

  function bindEnhancedNavRecovery() {
    if (window.__ndmEmployerEnhancedBound) {
      return;
    }
    window.__ndmEmployerEnhancedBound = true;
    document.addEventListener("click", handlePanelClick, true);
    function onNav() {
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
  }

  function init() {
    var el = root();
    if (!el) return;

    var initial = el.getAttribute("data-ndm-panel") || panelFromUrl();
    setPanel(el, initial, false);

    bindPanelNav(el);
    bindModeChips(el);
    bindFormatChips(el);
    bindAddSlot(el);
    bindRemoveSlots(el);
    bindSubmit(el);
    updateSubmit(el);

    if (!window.__ndmEmployerPopstateBound) {
      window.__ndmEmployerPopstateBound = true;
      window.addEventListener("popstate", function () {
        var current = root();
        if (!current) {
          return;
        }
        setPanel(current, panelFromUrl(), false);
      });
    }
  }

  window.NdmEmployerInit = init;
  bindEnhancedNavRecovery();

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
