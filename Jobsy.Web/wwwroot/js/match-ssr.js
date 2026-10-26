(function () {
  "use strict";

  var STORAGE_KEY = "jobsy.matchSsr.v1";
  var SWIPE_MS = 280;
  var THRESHOLD = 100;

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

  function activeCard(page) {
    var stage = page.querySelector("[data-match-stage]:not([hidden])");
    if (!stage) {
      return null;
    }
    return stage.querySelector("[data-match-card]:not(.swipe-card--backdrop)");
  }

  function syncView(page, index, boot) {
    var stageEls = stages(page);
    var sideEls = sides(page);
    stageEls.forEach(function (stage) {
      var idx = parseInt(stage.getAttribute("data-match-stage"), 10);
      var show = idx === index;
      stage.hidden = !show;
      var card = stage.querySelector("[data-match-card]");
      if (card) {
        card.classList.toggle("swipe-card--backdrop", false);
        card.classList.toggle("swipe-card--enter", idx === index);
        if (idx === index) {
          card.style.transform = "";
          card.classList.remove("swipe-card--out-left", "swipe-card--out-right", "is-dragging");
        }
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
    var card = activeCard(page);
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
    if (index + 1 >= boot.count) {
      window.location.reload();
      return;
    }
    state.index = index + 1;
    syncView(page, state.index, boot);
    if (typeof deferredOnce === "object") {
      var vacancyId = boot.vacancyIds[index];
      if (vacancyId) {
        deferredOnce[vacancyId] = true;
      }
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

  function animateOut(card, direction, done) {
    if (!card) {
      done();
      return;
    }
    card.classList.remove("is-dragging");
    card.style.transform = "";
    card.classList.add(direction === "left" ? "swipe-card--out-left" : "swipe-card--out-right");
    var stampNo = card.querySelector(".swipe-card__stamp--no");
    var stampYes = card.querySelector(".swipe-card__stamp--yes");
    if (direction === "left" && stampNo) {
      stampNo.classList.add("is-on");
    }
    if (direction === "right" && stampYes) {
      stampYes.classList.add("is-on");
    }
    window.setTimeout(done, SWIPE_MS);
  }

  function runReject(page, boot, state, deferredOnce) {
    var card = activeCard(page);
    animateOut(card, "left", function () {
      showToast(page, page.getAttribute("data-toast-skipped"));
      advance(page, boot, state, deferredOnce);
    });
  }

  function runInterest(page, boot, state, deferredOnce) {
    var vacancyId = currentVacancyId(page);
    var card = activeCard(page);
    if (!vacancyId) {
      return;
    }
    animateOut(card, "right", function () {
      likeVacancy(boot, vacancyId)
        .catch(function () {
          showToast(page, page.getAttribute("data-toast-error"));
        })
        .finally(function () {
          showToast(page, page.getAttribute("data-toast-liked"));
          advance(page, boot, state, deferredOnce);
        });
    });
  }

  function bindActions(page, boot, state, deferredOnce) {
    page.querySelectorAll("[data-match-action]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var action = btn.getAttribute("data-match-action");
        if (action === "more") {
          var vacancyId = currentVacancyId(page);
          if (vacancyId) {
            window.location.href = "/vacancies/" + vacancyId;
          }
          return;
        }
        if (action === "interest") {
          runInterest(page, boot, state, deferredOnce);
          return;
        }
        if (action === "reject") {
          runReject(page, boot, state, deferredOnce);
        }
      });
    });
  }

  function bindSwipe(page, boot, state, deferredOnce) {
    var drag = null;

    function resetCard(card) {
      if (!card) {
        return;
      }
      card.classList.remove("is-dragging");
      card.style.transform = "";
      var stampNo = card.querySelector(".swipe-card__stamp--no");
      var stampYes = card.querySelector(".swipe-card__stamp--yes");
      if (stampNo) {
        stampNo.classList.remove("is-on");
      }
      if (stampYes) {
        stampYes.classList.remove("is-on");
      }
    }

    function onMove(clientX, clientY) {
      if (!drag) {
        return;
      }
      var dx = clientX - drag.startX;
      var dy = (clientY - drag.startY) * 0.35;
      drag.card.classList.add("is-dragging");
      var rotate = Math.max(-12, Math.min(12, dx / 14));
      drag.card.style.transform =
        "translate3d(" + dx + "px, " + dy + "px, 0) rotate(" + rotate + "deg)";
      var stampNo = drag.card.querySelector(".swipe-card__stamp--no");
      var stampYes = drag.card.querySelector(".swipe-card__stamp--yes");
      if (stampNo) {
        stampNo.classList.toggle("is-on", dx < -40);
      }
      if (stampYes) {
        stampYes.classList.toggle("is-on", dx > 40);
      }
    }

    function onEnd() {
      if (!drag) {
        return;
      }
      var dx = drag.lastX - drag.startX;
      var card = drag.card;
      drag = null;
      if (Math.abs(dx) >= THRESHOLD) {
        if (dx < 0) {
          runReject(page, boot, state, deferredOnce);
        } else {
          runInterest(page, boot, state, deferredOnce);
        }
        return;
      }
      resetCard(card);
    }

    page.addEventListener(
      "pointerdown",
      function (ev) {
        if (ev.button !== 0) {
          return;
        }
        var stage = page.querySelector("[data-match-stage]:not([hidden])");
        if (!stage) {
          return;
        }
        var card = ev.target.closest("[data-match-card]");
        if (!card || !stage.contains(card) || card.classList.contains("swipe-card--backdrop")) {
          return;
        }
        drag = {
          card: card,
          startX: ev.clientX,
          startY: ev.clientY,
          lastX: ev.clientX,
          lastY: ev.clientY,
          pointerId: ev.pointerId
        };
        card.setPointerCapture(ev.pointerId);
      },
      true
    );
    page.addEventListener(
      "pointermove",
      function (ev) {
        if (!drag || ev.pointerId !== drag.pointerId) {
          return;
        }
        drag.lastX = ev.clientX;
        drag.lastY = ev.clientY;
        onMove(ev.clientX, ev.clientY);
      },
      true
    );
    page.addEventListener(
      "pointerup",
      function (ev) {
        if (!drag || ev.pointerId !== drag.pointerId) {
          return;
        }
        var card = drag.card;
        try {
          card.releasePointerCapture(ev.pointerId);
        } catch (_e) {
          /* ignore */
        }
        onEnd();
      },
      true
    );
    page.addEventListener(
      "pointercancel",
      function () {
        if (drag) {
          resetCard(drag.card);
          drag = null;
        }
      },
      true
    );
  }

  function bindKeys(page, boot, state, deferredOnce) {
    page.addEventListener("keydown", function (ev) {
      if (ev.target && /^(INPUT|TEXTAREA|SELECT|BUTTON)$/i.test(ev.target.tagName)) {
        return;
      }
      if (ev.key === "ArrowLeft") {
        ev.preventDefault();
        runReject(page, boot, state, deferredOnce);
      } else if (ev.key === "ArrowRight" || ev.key === "Enter") {
        ev.preventDefault();
        runInterest(page, boot, state, deferredOnce);
      }
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
    bindSwipe(page, boot, state, deferredOnce);
    bindKeys(page, boot, state, deferredOnce);
    page.focus({ preventScroll: true });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
