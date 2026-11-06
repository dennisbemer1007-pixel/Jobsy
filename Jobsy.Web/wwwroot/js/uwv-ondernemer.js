(function () {
  "use strict";

  function readBoot() {
    var el = document.getElementById("uwv-ondernemer-boot");
    if (!el || !el.textContent) {
      return null;
    }
    try {
      return JSON.parse(el.textContent);
    } catch (e) {
      return null;
    }
  }

  function formatQuestionOf(fmt, n, total) {
    if (!fmt) {
      return n + " / " + total;
    }
    return fmt.replace("{0}", String(n)).replace("{1}", String(total));
  }

  function renderQuestion(mount, boot, state) {
    var qNum = state.index + 1;
    var total = boot.questions.length;
    var html = "";
    html += '<div class="test-page-shell test-page-shell--question">';
    html += "<h1 class=\"visually-hidden\">" + boot.testName + "</h1>";
    html += '<div class="test-flow test-flow--compact uwv-ondernemer-test__flow">';
    html += '<div class="test-flow__progress-head"><p class="test-flow__counter">';
    html += formatQuestionOf(boot.questionOfFormat, qNum, total);
    html += "</p></div><div class=\"test-flow__bar uwv-ondernemer-test__bar\" role=\"group\" aria-label=\"";
    html += formatQuestionOf(boot.questionOfFormat, qNum, total);
    html += '">';
    for (var seg = 0; seg < total; seg++) {
      var cls = seg < state.index ? "is-done" : seg === state.index ? "is-current" : "is-todo";
      html += '<span class="test-flow__seg ' + cls + '" aria-hidden="true"></span>';
    }
    html += "</div></div>";
    if (state.index > 0) {
      html += '<p class="test-flow__back"><button type="button" class="btn btn-link" data-uwv-ondern-back="1">';
      html += boot.backLabel;
      html += "</button></p>";
    } else {
      html += '<p class="test-flow__back"><a href="' + boot.introBackHref + '" data-enhance-nav="false">';
      html += boot.introBackLabel;
      html += "</a></p>";
    }
    html += '<p class="uwv-test-q">' + boot.questions[state.index] + "</p>";
    html += '<div class="uwv-test-scale" role="group" aria-label="Likert">';
    for (var score = 1; score <= 5; score++) {
      html += '<button type="button" class="uwv-test-scale__btn" data-uwv-ondern-score="' + score + '">';
      html += boot.scaleLabels[score - 1];
      html += "</button>";
    }
    html += "</div></div>";
    if (state.busy) {
      html += '<p class="state-message" role="status">' + boot.errorMessage + "</p>";
    }
    mount.innerHTML = html;
  }

  function initQuestionPage() {
    var root = document.querySelector("[data-uwv-ondernemer-question=\"1\"]");
    if (!root) {
      return;
    }
    var boot = readBoot();
    var mount = root.querySelector("[data-uwv-ondernemer-mount]");
    if (!boot || !mount) {
      return;
    }
    var state = { index: 0, answers: [], busy: false };

    function paint() {
      renderQuestion(mount, boot, state);
    }

    root.addEventListener(
      "click",
      function (ev) {
        var back = ev.target.closest("[data-uwv-ondern-back]");
        if (back) {
          ev.preventDefault();
          if (state.index <= 0 || state.answers.length === 0) {
            return;
          }
          state.answers.pop();
          state.index--;
          paint();
          return;
        }
        var scoreBtn = ev.target.closest("[data-uwv-ondern-score]");
        if (!scoreBtn || state.busy) {
          return;
        }
        ev.preventDefault();
        var score = parseInt(scoreBtn.getAttribute("data-uwv-ondern-score"), 10);
        if (!score || score < 1 || score > 5) {
          return;
        }
        state.answers.push(score);
        if (state.answers.length < boot.questions.length) {
          state.index++;
          paint();
          return;
        }
        state.busy = true;
        paint();
        fetch(boot.saveUrl, {
          method: "POST",
          credentials: "same-origin",
          headers: { "Content-Type": "application/json", Accept: "application/json" },
          body: JSON.stringify({ answers: state.answers }),
        })
          .then(function (res) {
            if (!res.ok) {
              throw new Error("save failed");
            }
            window.location.assign(boot.resultHref);
          })
          .catch(function () {
            state.busy = false;
            window.location.assign(boot.resultHref);
          });
      },
      true
    );

    paint();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initQuestionPage);
  } else {
    initQuestionPage();
  }
})();
