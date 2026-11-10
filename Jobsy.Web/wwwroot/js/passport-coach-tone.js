(function () {
  "use strict";

  function readState(root) {
    var json = root.querySelector("[data-coach-prefs-json]");
    if (!json) {
      return { startIndex: 0, answers: {} };
    }
    try {
      return JSON.parse(json.textContent || "{}");
    } catch (e) {
      return { startIndex: 0, answers: {} };
    }
  }

  function syncHiddenFields(step, codes) {
    step.querySelectorAll("[data-coach-answer-field]").forEach(function (el) {
      el.remove();
    });
    var key = step.getAttribute("data-question-key");
    if (!key) {
      return;
    }
    (codes || []).forEach(function (code) {
      var input = document.createElement("input");
      input.type = "hidden";
      input.name = "answer_" + key;
      input.value = code;
      input.setAttribute("data-coach-answer-field", "1");
      step.appendChild(input);
    });
  }

  function getSelected(step) {
    return Array.prototype.map
      .call(step.querySelectorAll(".coach-prefs__tile.is-selected"), function (t) {
        return t.getAttribute("data-code");
      })
      .filter(Boolean);
  }

  function showStep(form, index) {
    form.querySelectorAll("[data-coach-step]").forEach(function (el) {
      var stepIndex = parseInt(el.getAttribute("data-coach-step") || "0", 10);
      el.hidden = stepIndex !== index;
    });
  }

  function bind(form) {
    var root = form.closest("[data-coach-prefs]");
    if (!root || root.getAttribute("data-coach-prefs-bound") === "1") {
      return;
    }
    root.setAttribute("data-coach-prefs-bound", "1");

    var state = readState(root);
    showStep(form, state.startIndex || 0);

    form.querySelectorAll("[data-coach-tile]").forEach(function (tile) {
      tile.addEventListener("click", function () {
        var step = tile.closest("[data-coach-step]");
        if (!step) {
          return;
        }
        var multi = tile.getAttribute("data-multi") === "1";
        if (!multi) {
          step.querySelectorAll(".coach-prefs__tile").forEach(function (t) {
            t.classList.remove("is-selected");
            t.setAttribute("aria-pressed", "false");
          });
        }
        var on = !tile.classList.contains("is-selected");
        if (on) {
          tile.classList.add("is-selected");
          tile.setAttribute("aria-pressed", "true");
        } else {
          tile.classList.remove("is-selected");
          tile.setAttribute("aria-pressed", "false");
        }
        syncHiddenFields(step, getSelected(step));
      });
    });

    function syncAllSteps() {
      form.querySelectorAll("[data-coach-step]").forEach(function (step) {
        syncHiddenFields(step, getSelected(step));
      });
    }

    function submitAction(action) {
      syncAllSteps();
      var actionField = form.querySelector("[data-coach-action]");
      if (actionField) {
        actionField.value = action;
      }
      form.submit();
    }

    form.querySelectorAll("[data-coach-btn]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var action = btn.getAttribute("data-coach-btn") || "save";
        var step = btn.closest("[data-coach-step]");
        var index = step ? parseInt(step.getAttribute("data-coach-step") || "0", 10) : 0;
        if (action === "next") {
          syncHiddenFields(step, getSelected(step));
          var steps = form.querySelectorAll("[data-coach-step]");
          if (index < steps.length - 1) {
            showStep(form, index + 1);
            var next = form.querySelector('[data-coach-step="' + (index + 1) + '"]');
            if (next) {
              next.scrollIntoView({ block: "nearest", behavior: "smooth" });
            }
            return;
          }
          action = "save";
        }
        submitAction(action);
      });
    });
  }

  function init() {
    document.querySelectorAll("[data-coach-prefs-form]").forEach(bind);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
