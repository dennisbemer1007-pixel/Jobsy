(function () {
  "use strict";

  function bindReadAloud(root) {
    root.querySelectorAll("[data-read-aloud-text]").forEach(function (btn) {
      if (btn.getAttribute("data-read-aloud-bound") === "1") return;
      btn.setAttribute("data-read-aloud-bound", "1");
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var text = btn.getAttribute("data-read-aloud-text") || "";
        if (!text || !window.lobsyReadAloud || typeof window.lobsyReadAloud.speak !== "function") return;
        var lang = document.documentElement.lang || "nl";
        window.lobsyReadAloud.speak(text, lang);
      });
    });
  }

  function bindAnswerScale(root) {
    root.querySelectorAll("form[data-ll-answer-scale]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      var hidden = form.querySelector('input[name="Value"]');
      form.querySelectorAll("[data-ll-answer-value]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var val = btn.getAttribute("data-ll-answer-value");
          if (!hidden || !val) return;
          hidden.value = val;
          form.submit();
        });
      });
    });
  }

  function bindChips(root) {
    root.querySelectorAll("form[data-ll-chips-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      var likes = form.querySelector('input[name="Likes"]');
      var dislikes = form.querySelector('input[name="Dislikes"]');
      if (!likes || !dislikes) return;
      var likeSet = new Set(likes.value ? likes.value.split(",") : []);
      var dislikeSet = new Set(dislikes.value ? dislikes.value.split(",") : []);

      function sync() {
        likes.value = Array.from(likeSet).filter(Boolean).join(",");
        dislikes.value = Array.from(dislikeSet).filter(Boolean).join(",");
      }

      form.querySelectorAll("[data-ll-chip-like]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var key = btn.getAttribute("data-ll-chip-like");
          if (!key) return;
          if (likeSet.has(key)) likeSet.delete(key);
          else {
            likeSet.add(key);
            dislikeSet.delete(key);
          }
          btn.classList.toggle("is-on", likeSet.has(key));
          btn.classList.toggle("is-like", likeSet.has(key));
          sync();
        });
      });

      form.querySelectorAll("[data-ll-chip-dislike]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var key = btn.getAttribute("data-ll-chip-dislike");
          if (!key) return;
          if (dislikeSet.has(key)) dislikeSet.delete(key);
          else {
            dislikeSet.add(key);
            likeSet.delete(key);
          }
          btn.classList.toggle("is-on", dislikeSet.has(key));
          btn.classList.toggle("is-dislike", dislikeSet.has(key));
          sync();
        });
      });

      form.querySelectorAll("[data-ll-other-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var target = btn.getAttribute("data-ll-other-toggle");
          var panel = form.querySelector('[data-ll-other-panel="' + target + '"]');
          if (panel) panel.hidden = !panel.hidden;
        });
      });
    });
  }

  function bindVakkenScale(root) {
    root.querySelectorAll("form[data-ll-vakken-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      form.querySelectorAll("[data-ll-vt-scale]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var subject = btn.getAttribute("data-ll-vt-subject");
          var field = btn.getAttribute("data-ll-vt-field");
          var val = btn.getAttribute("data-ll-vt-scale");
          if (!subject || !field || !val) return;
          var hidden = form.querySelector('input[name="' + field + "." + subject + '"]');
          if (!hidden) return;
          hidden.value = val;
          var group = btn.closest(".ll-vt-sc, .ll-vakken__scale-btns");
          if (group) {
            group.querySelectorAll("[data-ll-vt-scale]").forEach(function (b) {
              b.classList.toggle("is-on", b === btn);
              b.setAttribute("aria-checked", b === btn ? "true" : "false");
            });
          }
        });
      });
    });
  }

  function bindWereld(root) {
    root.querySelectorAll("[data-ll-wereld-card]").forEach(function (btn) {
      if (btn.getAttribute("data-ll-bound") === "1") return;
      btn.setAttribute("data-ll-bound", "1");
      btn.addEventListener("click", function () {
        var form = btn.closest("form");
        if (form) form.submit();
      });
    });
  }

  function bindPuzzle(root) {
    root.querySelectorAll("[data-ll-puzzle-root]").forEach(function (section) {
      if (section.getAttribute("data-ll-bound") === "1") return;
      section.setAttribute("data-ll-bound", "1");
      var form = section.closest("form") || section.querySelector("form[data-ll-puzzle-form]");
      if (!form) return;
      var outcome = form.querySelector('input[name="Outcome"]');
      var kind = section.getAttribute("data-ll-puzzle-kind") || "";

      if (kind === "schelpenrij") {
        var order = [1, 3, 2, 4];
        var pick = null;
        section.querySelectorAll("[data-ll-shell-index]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            var idx = parseInt(btn.getAttribute("data-ll-shell-index"), 10);
            if (pick === null) {
              pick = idx;
              btn.classList.add("is-pick");
              return;
            }
            if (pick === idx) {
              pick = null;
              btn.classList.remove("is-pick");
              return;
            }
            var tmp = order[pick];
            order[pick] = order[idx];
            order[idx] = tmp;
            pick = null;
            section.querySelectorAll("[data-ll-shell-index]").forEach(function (b) {
              b.classList.remove("is-pick");
            });
            if (order[0] === 1 && order[1] === 2 && order[2] === 3 && order[3] === 4) {
              section.querySelectorAll(".ll-puzzle__shell").forEach(function (s) {
                s.classList.add("is-done");
              });
              if (outcome) outcome.value = "done";
            }
          });
        });
      }

      if (kind === "schatkaart") {
        var open = new Set();
        var matched = new Set();
        var first = null;
        section.querySelectorAll("[data-ll-card-index]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            var idx = parseInt(btn.getAttribute("data-ll-card-index"), 10);
            var face = btn.getAttribute("data-ll-card-face") || "";
            if (matched.has(face) || open.has(idx)) return;
            if (first === null) {
              first = idx;
              open.add(idx);
              btn.classList.add("is-open");
              return;
            }
            open.add(idx);
            btn.classList.add("is-open");
            var firstBtn = section.querySelector('[data-ll-card-index="' + first + '"]');
            var firstFace = firstBtn ? firstBtn.getAttribute("data-ll-card-face") : "";
            if (firstFace === face) {
              matched.add(face);
              first = null;
              if (matched.size >= 3) {
                if (outcome) outcome.value = "done";
              }
            } else {
              setTimeout(function () {
                open.delete(first);
                open.delete(idx);
                if (firstBtn) firstBtn.classList.remove("is-open");
                btn.classList.remove("is-open");
                first = null;
              }, 650);
            }
          });
        });
      }

      if (kind === "vuurtorenlampen") {
        var pattern = [];
        for (var i = 0; i < 5; i++) pattern.push(Math.floor(Math.random() * 3));
        var step = 0;
        var playMode = false;
        var ready = false;

        function playPattern() {
          playMode = true;
          step = 0;
          var i = 0;
          function nextLamp() {
            if (i >= pattern.length) {
              playMode = false;
              ready = true;
              return;
            }
            var lamp = pattern[i];
            var el = section.querySelector('[data-ll-lamp-index="' + lamp + '"]');
            if (el) el.classList.add("is-lit");
            setTimeout(function () {
              if (el) el.classList.remove("is-lit");
              i++;
              setTimeout(nextLamp, 400);
            }, 950);
          }
          setTimeout(nextLamp, 700);
        }

        playPattern();
        section.querySelectorAll("[data-ll-lamp-index]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            if (playMode || !ready) return;
            var lamp = parseInt(btn.getAttribute("data-ll-lamp-index"), 10);
            btn.classList.add("is-lit");
            if (pattern[step] !== lamp) {
              step = 0;
              playPattern();
              return;
            }
            step++;
            if (step >= pattern.length) {
              if (outcome) outcome.value = "done";
            }
          });
        });
      }

      section.querySelectorAll("[data-ll-puzzle-skip]").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          if (outcome) outcome.value = "skipped";
          form.submit();
        });
      });

      section.querySelectorAll("[data-ll-puzzle-continue]").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          if (outcome) outcome.value = "done";
          form.submit();
        });
      });
    });
  }

  function bindMhboScale(root) {
    root.querySelectorAll("form[data-mhbo-scale-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      form.querySelectorAll("[data-mhbo-scale-value]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var field = btn.getAttribute("data-mhbo-scale-field");
          var val = btn.getAttribute("data-mhbo-scale-value");
          if (!field || !val) return;
          var hidden = form.querySelector('input[name="' + field + '"]');
          if (hidden) hidden.value = val;
          var group = btn.parentElement;
          if (group) {
            group.querySelectorAll("[data-mhbo-scale-value]").forEach(function (b) {
              b.classList.toggle("is-on", b === btn);
            });
          }
        });
      });
    });
  }

  function init() {
    var root = document.body;
    bindReadAloud(root);
    bindAnswerScale(root);
    bindChips(root);
    bindVakkenScale(root);
    bindWereld(root);
    bindPuzzle(root);
    bindMhboScale(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
