(function () {
  "use strict";

  var STORAGE_KEY = "jobsy.matchSsr.v1";

  function readBoot() {
    var el = document.getElementById("match-ssr-boot");
    if (!el || !el.textContent) {
      return null;
    }
    try {
      return JSON.parse(el.textContent);
    } catch (_e) {
      return null;
    }
  }

  function readToken() {
    var el = document.getElementById("match-ssr-token");
    return el ? el.value : "";
  }

  function root() {
    return document.querySelector("[data-match-ssr=\"1\"]");
  }

  function toastEl(page) {
    return page.querySelector("[data-match-toast]");
  }

  function showToast(page, message) {
    var el = toastEl(page);
    if (!el || !message) {
      return;
    }
    el.textContent = message;
    el.hidden = false;
    window.clearTimeout(showToast._t);
    showToast._t = window.setTimeout(function () {
      el.hidden = true;
    }, 1800);
  }

  function stages(page) {
    return Array.prototype.slice.call(page.querySelectorAll("[data-match-stage]"));
  }

  function sides(page) {
    return Array.prototype.slice.call(page.querySelectorAll("[data-match-side]"));
  }

  function visibleIndex(stagesList) {
    for (var i = 0; i < stagesList.length; i++) {
      if (!stagesList[i].hidden) {
        var idx = parseInt(stagesList[i].getAttribute("data-match-stage"), 10);
        return isNaN(idx) ? i : idx;
      }
    }
    return 0;
  }

  function syncView(page, index, boot) {
    var stageEls = stages(page);
    var sideEls = sides(page);
    stageEls.forEach(function (stage) {
      var idx = parseInt(stage.getAttribute("data-match-stage"), 10);
      var show = idx === index || idx === index + 1;
      stage.hidden = !show;
      var card = stage.querySelector("[data-match-card]");
      if (card) {
        card.classList.toggle("swipe-card--backdrop", idx === index + 1);
        card.classList.toggle("swipe-card--enter", idx === index);
      }
    });
    sideEls.forEach(function (side) {
      var idx = parseInt(side.getAttribute("data-match-side-index"), 10);
      side.hidden = idx !== index;
    });

    var progressText = page.querySelector("[data-match-progress-text]");
    if (progressText && boot && boot.count > 0) {
      var pos = Math.min(index + 1, boot.count);
      var template = page.getAttribute("data-progress-format");
      if (template) {
        var lead = template.replace("{0}", String(pos)).replace("{1}", String(boot.count));
        var suffix = progressText.querySelector("span");
        progressText.textContent = lead + " ";
        if (suffix) {
          progressText.appendChild(suffix);
        }
      }
    }

    var bar = page.querySelector("[data-match-progress-bar]");
    if (bar && boot && boot.count > 0) {
      var posBar = Math.min(index + 1, boot.count);
      bar.setAttribute("aria-valuenow", String(posBar));
      var fill = bar.querySelector(".match-page__bar-fill");
      if (fill) {
        var width = Math.round((100 * posBar) / boot.count);
        fill.setAttribute("width", String(width));
      }
    }

    try {
      sessionStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({ index: index, vacancyIds: boot.vacancyIds || [] })
      );
    } catch (_e2) {
      /* ignore */
    }
  }

  function currentVacancyId(page) {
    var card = page.querySelector("[data-match-card]:not(.swipe-card--backdrop)");
    return card ? card.getAttribute("data-vacancy-id") : null;
  }

  function likeVacancy(boot, vacancyId) {
    var token = readToken();
    var body = new URLSearchParams();
    body.set("vacancyId", vacancyId);
    body.set("liked", "1");
    if (token) {
      body.set("__RequestVerificationToken", token);
    }
    return fetch(boot.savePath, {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/x-www-form-urlencoded",
        "X-Requested-With": "XMLHttpRequest"
      },
      body: body.toString(),
      credentials: "same-origin"
    });
  }

  function advance(page, boot, state, deferredOnce) {
    var index = state.index;
    var vacancyId = boot.vacancyIds[index];
    if (index + 1 >= boot.count) {
      window.location.reload();
      return;
    }
    state.index = index + 1;
    syncView(page, state.index, boot);
    if (typeof deferredOnce === "object" && vacancyId) {
      deferredOnce[vacancyId] = true;
    }
  }

  function deferCard(page, boot, state, deferredOnce) {
    var index = state.index;
    var vacancyId = boot.vacancyIds[index];
    if (!vacancyId) {
      advance(page, boot, state, deferredOnce);
      return;
    }
    if (deferredOnce[vacancyId]) {
      advance(page, boot, state, deferredOnce);
      return;
    }
    deferredOnce[vacancyId] = true;
    boot.vacancyIds.splice(index, 1);
    boot.vacancyIds.push(vacancyId);
    var stage = page.querySelector('[data-match-stage="' + index + '"]');
    var stack = page.querySelector("[data-match-stack]");
    if (stage && stack) {
      stack.appendChild(stage);
    }
    syncView(page, index, boot);
  }

  function bindActions(page, boot, state, deferredOnce) {
    page.querySelectorAll("[data-match-action]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var action = btn.getAttribute("data-match-action");
        var vacancyId = currentVacancyId(page);
        if (action === "more" && vacancyId) {
          window.location.href = "/vacancies/" + vacancyId;
          return;
        }
        if (action === "interest" && vacancyId) {
          likeVacancy(boot, vacancyId)
            .catch(function () {
              showToast(page, page.getAttribute("data-toast-error"));
            })
            .finally(function () {
              showToast(page, page.getAttribute("data-toast-liked"));
              advance(page, boot, state, deferredOnce);
            });
          return;
        }
        if (action === "reject") {
          showToast(page, page.getAttribute("data-toast-skipped"));
          deferCard(page, boot, state, deferredOnce);
        }
      });
    });
  }

  function restoreIndex(boot) {
    try {
      var raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return boot.index || 0;
      }
      var saved = JSON.parse(raw);
      if (!saved || !saved.vacancyIds || !saved.vacancyIds.length) {
        return boot.index || 0;
      }
      if (JSON.stringify(saved.vacancyIds) !== JSON.stringify(boot.vacancyIds)) {
        return boot.index || 0;
      }
      return typeof saved.index === "number" ? saved.index : 0;
    } catch (_e) {
      return boot.index || 0;
    }
  }

  function init() {
    var page = root();
    var boot = readBoot();
    if (!page || !boot || !boot.count) {
      return;
    }

    var state = { index: restoreIndex(boot) };
    if (state.index >= boot.count) {
      window.location.reload();
      return;
    }

    var deferredOnce = {};
    syncView(page, state.index, boot);
    bindActions(page, boot, state, deferredOnce);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
