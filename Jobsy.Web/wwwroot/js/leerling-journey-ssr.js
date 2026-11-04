(function () {
  "use strict";

  var answerSaveChain = Promise.resolve();
  var pauseFlushMs = 2000;
  var reisDocumentGeneration = 0;

  function flushAnswerSaveChain() {
    return Promise.race([
      answerSaveChain.catch(function () {
        /* keep queue alive after failure */
      }),
      new Promise(function (resolve) {
        setTimeout(resolve, pauseFlushMs);
      }),
    ]);
  }

  function readAntiforgeryFromForm(form) {
    var itemId = form.querySelector('input[name="ItemId"]');
    var inputs = form.querySelectorAll('input[type="hidden"]');
    for (var i = 0; i < inputs.length; i++) {
      var inp = inputs[i];
      var name = inp.getAttribute("name") || "";
      if (name === "ItemId" || name === "Value") continue;
      if (itemId && inp === itemId) continue;
      return { field: name, token: inp.value || "" };
    }
    return { field: "", token: "" };
  }

  function writeAntiforgeryToForm(form, field, token) {
    if (!field || !token) return;
    var inputs = form.querySelectorAll('input[type="hidden"]');
    for (var i = 0; i < inputs.length; i++) {
      var inp = inputs[i];
      if ((inp.getAttribute("name") || "") === field) {
        inp.value = token;
        return;
      }
    }
    var hidden = document.createElement("input");
    hidden.type = "hidden";
    hidden.name = field;
    hidden.value = token;
    form.insertBefore(hidden, form.firstChild);
  }

  function applyG78AnswerSelection(form, btn, val) {
    var hidden = form.querySelector('input[name="Value"]');
    if (hidden) hidden.value = val;
    form.querySelectorAll("[data-ll-answer-value]").forEach(function (b) {
      if (!b || !b.classList) return;
      var sel = b === btn;
      b.classList.toggle("sel", sel);
      b.setAttribute("aria-checked", sel ? "true" : "false");
    });
  }

  function swapGroep78ShellMain(html) {
    var parser = new DOMParser();
    var doc = parser.parseFromString(html, "text/html");
    var nextShell = doc.querySelector(".ll-g78-shell");
    var shell = document.querySelector(".ll-g78-shell");
    if (!nextShell || !shell) {
      window.location.assign("/leerling/reis");
      return false;
    }
    var header = shell.querySelector("header.top");
    var footer = shell.querySelector("footer");
    var nextHeader = nextShell.querySelector("header.top");
    var nextFooter = nextShell.querySelector("footer");
    if (!header || !footer || !nextHeader || !nextFooter) {
      window.location.assign("/leerling/reis");
      return false;
    }
    var node = header.nextElementSibling;
    while (node && node !== footer) {
      var remove = node;
      node = node.nextElementSibling;
      remove.remove();
    }
    var insertBefore = footer;
    var incoming = nextHeader.nextElementSibling;
    while (incoming && incoming !== nextFooter) {
      shell.insertBefore(document.importNode(incoming, true), insertBefore);
      incoming = incoming.nextElementSibling;
    }
    document.title = doc.title || document.title;
    bindReadAloud(shell);
    bindAnswerScale(shell);
    bindChips(shell);
    bindVakkenScale(shell);
    bindWereld(shell);
    bindPuzzleIntro(shell);
    bindSchelpenPatternPick(shell);
    bindPuzzle(shell);
    bindIslandSteps(shell);
    bindJobPicks(shell);
    bindMhboScale(shell);
    bindPauseForms(shell);
    return true;
  }

  function fetchReisDocument() {
    var url = "/leerling/reis?_=" + Date.now();
    return fetch(url, {
      method: "GET",
      credentials: "same-origin",
      redirect: "manual",
      headers: { Accept: "text/html", "X-Requested-With": "XMLHttpRequest" },
    }).then(function (res) {
      if (res.type === "opaqueredirect" || (res.status >= 300 && res.status < 400)) {
        var loc = res.headers.get("Location");
        if (loc) {
          var err = new Error("reis_fetch_redirect");
          err.redirectPath = loc;
          throw err;
        }
      }
      if (!res.ok) throw new Error("reis_fetch_" + res.status);
      return res.text();
    });
  }

  function postReisAnswer(form, itemId, value) {
    var af = readAntiforgeryFromForm(form);
    var body = new URLSearchParams();
    if (af.field && af.token) {
      body.set(af.field, af.token);
    }
    body.set("ItemId", itemId);
    body.set("Value", String(value));
    return fetch(form.getAttribute("action") || "/leerling/reis/answer", {
      method: "POST",
      credentials: "same-origin",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/x-www-form-urlencoded",
        "X-Requested-With": "XMLHttpRequest",
      },
      body: body.toString(),
    }).then(function (res) {
      return res.json().then(function (data) {
        if (!data || typeof data !== "object") {
          throw new Error("reis_save_non_json");
        }
        var ok = data.ok === true || data.Ok === true || (data.ok === undefined && data.Ok === undefined && res.ok);
        if (!ok) {
          var err = new Error(data.error || data.Error || "reis_save_failed");
          err.status = data.status || data.Status || res.status;
          err.redirectPath = data.redirectPath || data.RedirectPath || null;
          throw err;
        }
        var afField = data.antiforgeryField || data.AntiforgeryField;
        var afToken = data.antiforgeryToken || data.AntiforgeryToken;
        if (afField && afToken) {
          writeAntiforgeryToForm(form, afField, afToken);
        }
        return {
          redirectPath: data.redirectPath || data.RedirectPath || "/leerling/reis",
        };
      });
    });
  }

  function setAnswerScaleBusy(form, busy) {
    if (!form) return;
    form.setAttribute("data-ll-save-state", busy ? "pending" : "idle");
    form.querySelectorAll("[data-ll-answer-value]").forEach(function (btn) {
      if (busy) {
        btn.setAttribute("aria-disabled", "true");
        btn.setAttribute("data-ll-save-blocked", "1");
      } else {
        btn.removeAttribute("aria-disabled");
        btn.removeAttribute("data-ll-save-blocked");
      }
    });
  }

  function navigateAfterReisSave(_form, data) {
    var target = data.redirectPath || "/leerling/reis";
    var plainReis =
      target === "/leerling/reis" ||
      target.indexOf("/leerling/reis?") === 0;
    if (!plainReis) {
      window.location.assign(target);
      return Promise.resolve();
    }
    var generation = ++reisDocumentGeneration;
    return fetchReisDocument()
      .then(function (html) {
        if (generation !== reisDocumentGeneration) {
          return;
        }
        if (swapGroep78ShellMain(html)) {
          return;
        }
        window.location.assign(target);
      })
      .catch(function (err) {
        if (generation !== reisDocumentGeneration) {
          return;
        }
        var recovery = (err && err.redirectPath) || target;
        window.location.assign(recovery);
      });
  }

  function bindReadAloud(root) {
    if (window.lobsyReadAloud && typeof window.lobsyReadAloud.bindTextButtons === "function") {
      window.lobsyReadAloud.bindTextButtons(root);
      return;
    }
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
      var itemInput = form.querySelector('input[name="ItemId"]');
      var hasAsyncButtons = form.querySelector("[data-ll-answer-value]");
      if (!hasAsyncButtons) {
        return;
      }

      form.querySelectorAll("[data-ll-answer-value]").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          if (form.getAttribute("data-ll-save-state") === "pending") {
            return;
          }
          if (btn.getAttribute("data-ll-save-blocked") === "1") {
            return;
          }
          var val = btn.getAttribute("data-ll-answer-value");
          if (!itemInput || !val) return;
          var itemId = itemInput.value;
          if (!itemId) return;
          applyG78AnswerSelection(form, btn, val);
          setAnswerScaleBusy(form, true);
          answerSaveChain = answerSaveChain
            .catch(function () {
              /* keep queue alive after failure */
            })
            .then(function () {
              return postReisAnswer(form, itemId, val);
            })
            .then(function (data) {
              return navigateAfterReisSave(form, data);
            })
            .then(function () {
              var live = document.querySelector("form[data-ll-answer-scale]");
              setAnswerScaleBusy(live, false);
            })
            .catch(function (err) {
              setAnswerScaleBusy(form, false);
              form.setAttribute("data-ll-save-state", "error");
              var alert = form.parentElement
                ? form.parentElement.querySelector(".ll-error")
                : null;
              if (!alert) {
                alert = document.createElement("p");
                alert.className = "ll-error";
                alert.setAttribute("role", "alert");
                alert.textContent = "Opslaan mislukt. Probeer opnieuw.";
                form.insertAdjacentElement("afterend", alert);
              }
              var status = err && err.status;
              var recovery = (err && err.redirectPath) || "/leerling/reis";
              if (status === 409 || status === 502 || status === 400 || status === 500) {
                window.setTimeout(function () {
                  window.location.assign(recovery);
                }, 400);
              }
            });
        });
      });

      form.addEventListener("submit", function (e) {
        if (form.querySelector("[data-ll-answer-value]")) {
          e.preventDefault();
        }
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

      function syncHiddenFields() {
        likes.value = Array.from(likeSet).filter(Boolean).join(",");
        dislikes.value = Array.from(dislikeSet).filter(Boolean).join(",");
      }

      function syncChipButton(btn, key, isDislike) {
        var on = isDislike ? dislikeSet.has(key) : likeSet.has(key);
        btn.classList.toggle("is-on", on);
        btn.classList.toggle("is-like", !isDislike && on);
        btn.classList.toggle("is-dislike", isDislike && on);
        btn.classList.toggle("on", on);
        btn.classList.toggle("like", !isDislike && on);
        btn.classList.toggle("dislike", isDislike && on);
        btn.setAttribute("aria-pressed", on ? "true" : "false");
      }

      function syncAllChipButtons() {
        form.querySelectorAll("[data-ll-chip-like]").forEach(function (btn) {
          var key = btn.getAttribute("data-ll-chip-like");
          if (key) syncChipButton(btn, key, false);
        });
        form.querySelectorAll("[data-ll-chip-dislike]").forEach(function (btn) {
          var key = btn.getAttribute("data-ll-chip-dislike");
          if (key) syncChipButton(btn, key, true);
        });
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
          syncAllChipButtons();
          syncHiddenFields();
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
          syncAllChipButtons();
          syncHiddenFields();
        });
      });

      form.querySelectorAll("[data-ll-other-toggle]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          var target = btn.getAttribute("data-ll-other-toggle");
          var panel = form.querySelector('[data-ll-other-panel="' + target + '"]');
          if (panel) panel.hidden = !panel.hidden;
        });
      });

      form.addEventListener("submit", function (e) {
        syncHiddenFields();
        if (form.querySelector("[data-ll-island-step]")) {
          if (form.getAttribute("data-ll-island-submitting") === "1") {
            e.preventDefault();
            return;
          }
          form.setAttribute("data-ll-island-submitting", "1");
        }
      });

      syncAllChipButtons();
    });
  }

  function vakkenScoreOk(val) {
    var n = parseInt(val, 10);
    return n >= 1 && n <= 5;
  }

  function syncVakkenForm(form) {
    var cards = form.querySelectorAll(".ll-vt-card");
    if (!cards.length) return;
    var done = 0;
    cards.forEach(function (card) {
      var leuk = null;
      var goed = null;
      card.querySelectorAll('input[type="hidden"]').forEach(function (inp) {
        var n = inp.getAttribute("name") || "";
        if (n.indexOf("Leuk.") === 0) leuk = inp;
        if (n.indexOf("Goed.") === 0) goed = inp;
      });
      var complete = leuk && goed && vakkenScoreOk(leuk.value) && vakkenScoreOk(goed.value);
      card.classList.toggle("is-done", !!complete);
      var ok = card.querySelector(".ll-vt-card__ok");
      if (complete && !ok) {
        ok = document.createElement("span");
        ok.className = "ll-vt-card__ok";
        ok.setAttribute("aria-hidden", "true");
        ok.textContent = "✓";
        var head = card.querySelector(".ll-vt-card__head");
        if (head) head.appendChild(ok);
      } else if (!complete && ok) {
        ok.remove();
      }
      if (complete) done += 1;
    });
    var total = cards.length;
    var remaining = total - done;
    var pct = total === 0 ? 0 : (done * 100) / total;
    var progHost = form.closest(".ll-vakken-g78-card, .ll-vt-main");
    var prog = progHost ? progHost.querySelector("[data-ll-vt-prog]") : null;
    if (prog) {
      var bar = prog.querySelector(".ll-vt-prog__bar i");
      if (bar) bar.style.width = pct + "%";
      var track = prog.querySelector(".ll-vt-prog__bar");
      if (track) {
        track.setAttribute("aria-valuenow", String(done));
        track.setAttribute("aria-valuemax", String(total));
      }
      var count = prog.querySelector("[data-ll-vt-count]");
      if (count) count.textContent = done + "/" + total;
      var rem = prog.querySelector("[data-ll-vt-remaining]");
      if (rem && rem.getAttribute("data-ll-vt-remaining-template")) {
        rem.textContent = rem
          .getAttribute("data-ll-vt-remaining-template")
          .replace("{0}", String(remaining));
      }
    }
    var bubbleHost = form.closest(".ll-vakken-g78-card, .ll-vt-main") || form.parentElement;
    var bubble = bubbleHost ? bubbleHost.querySelector("[data-ll-vt-bubble]") : null;
    if (bubble) {
      var bubbleLine = bubble.querySelector(".bt") || bubble;
      if (remaining <= 0 && bubble.getAttribute("data-ll-vt-bubble-done")) {
        bubbleLine.textContent = bubble.getAttribute("data-ll-vt-bubble-done");
      } else if (bubble.getAttribute("data-ll-vt-bubble-progress")) {
        bubbleLine.textContent = bubble
          .getAttribute("data-ll-vt-bubble-progress")
          .replace("{0}", String(remaining));
      }
    }
    var next = form.querySelector("[data-testid=ll-vt-next], .ll-vt-next");
    if (next) next.disabled = done !== total || total === 0;
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
          syncVakkenForm(form);
        });
      });
      syncVakkenForm(form);
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

  function bindPuzzleIntro(root) {
    root.querySelectorAll("[data-ll-puzzle-start]").forEach(function (btn) {
      if (btn.getAttribute("data-ll-bound") === "1") return;
      btn.setAttribute("data-ll-bound", "1");
      btn.addEventListener("click", function () {
        var card = btn.closest(".ll-puzzle-g78");
        if (!card) return;
        var intro = card.querySelector('[data-ll-puzzle-phase="intro"]');
        var play = card.querySelector('[data-ll-puzzle-phase="play"]');
        if (intro) {
          intro.hidden = true;
          intro.setAttribute("hidden", "");
        }
        if (play) {
          play.hidden = false;
          play.removeAttribute("hidden");
        }
        var kind = card.getAttribute("data-ll-puzzle-kind") || "";
        if (kind === "vuurtorenlampen") {
          card.removeAttribute("data-ll-bound");
          card.removeAttribute("data-ll-lamp-game-ready");
          bindPuzzle(card);
        }
      });
    });
  }

  function setIslandActiveStep(form, step) {
    var s1 = form.querySelector('[data-ll-island-step="1"]');
    var s2 = form.querySelector('[data-ll-island-step="2"]');
    var on1 = step === 1;
    var on2 = step === 2;
    if (s1) {
      s1.hidden = !on1;
      if (on1) s1.removeAttribute("hidden");
      else s1.setAttribute("hidden", "");
    }
    if (s2) {
      s2.hidden = !on2;
      if (on2) s2.removeAttribute("hidden");
      else s2.setAttribute("hidden", "");
    }
    form.setAttribute("data-ll-island-active-step", String(step));
    var shell = form.closest(".ll-g78-shell") || document.body;
    shell.querySelectorAll("[data-ll-island-jmeta]").forEach(function (el) {
      var show = el.getAttribute("data-ll-island-jmeta") === String(step);
      el.hidden = !show;
      if (show) el.removeAttribute("hidden");
      else el.setAttribute("hidden", "");
    });
    shell.querySelectorAll("[data-ll-island-coach-step]").forEach(function (el) {
      var show = el.getAttribute("data-ll-island-coach-step") === String(step);
      el.hidden = !show;
      if (show) el.removeAttribute("hidden");
      else el.setAttribute("hidden", "");
    });
  }

  function bindIslandSteps(root) {
    root.querySelectorAll("form[data-ll-chips-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-island-bound") === "1") return;
      if (!form.querySelector("[data-ll-island-step]")) return;
      form.setAttribute("data-ll-island-bound", "1");
      form.querySelectorAll("[data-ll-island-next]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          setIslandActiveStep(form, 2);
        });
      });
      form.querySelectorAll("[data-ll-island-prev]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          setIslandActiveStep(form, 1);
        });
      });
      var s2 = form.querySelector('[data-ll-island-step="2"]');
      setIslandActiveStep(form, s2 && !s2.hidden ? 2 : 1);
    });
  }

  function bindJobPicks(root) {
    root.querySelectorAll("form[data-ll-job-pick-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      var noneNote = form.querySelector("[data-ll-jpick-none-note]");
      function syncNoneNote() {
        if (!noneNote) return;
        // G78 mock: keep “Dan ga je naar Check je droombaan…” visible above the footer.
        noneNote.removeAttribute("hidden");
        noneNote.classList.add("on");
      }
      form.querySelectorAll('input[name="DreamJob"][type="radio"]').forEach(function (radio) {
        radio.addEventListener("change", function () {
          form.querySelectorAll(".jpick").forEach(function (card) {
            if (!card || !card.classList) return;
            var on = card.contains(radio) && radio.checked;
            card.classList.toggle("sel", on);
          });
          syncNoneNote();
        });
      });
      form.querySelectorAll("[data-ll-job-pick]").forEach(function (btn) {
        if (btn.querySelector('input[name="DreamJob"][type="radio"]')) {
          return;
        }
        btn.addEventListener("click", function () {
          form.querySelectorAll(".jpick").forEach(function (b) {
            if (!b || !b.classList) return;
            var sel = b === btn;
            b.classList.toggle("sel", sel);
            b.setAttribute("aria-pressed", sel ? "true" : "false");
          });
          syncNoneNote();
        });
      });
      syncNoneNote();
    });
  }

  function bindSchelpenPatternPick(root) {
    if (root.getAttribute("data-ll-schelp-pattern-bound") === "1") return;
    root.setAttribute("data-ll-schelp-pattern-bound", "1");
    root.addEventListener("click", function (evt) {
      var btn = evt.target.closest("[data-ll-schelp-choice]");
      if (!btn) return;
      var card = btn.closest('.ll-puzzle-g78[data-ll-puzzle-mode="pattern"]');
      if (!card || card.getAttribute("data-ll-schelp-solved") === "1") return;
      var form = btn.closest("form[data-ll-puzzle-form]");
      if (!form) return;
      var playSection = btn.closest("[data-ll-puzzle-play-section]") || card;
      var answer = parseInt(card.getAttribute("data-ll-puzzle-answer") || "0", 10);
      var choice = parseInt(btn.getAttribute("data-ll-schelp-choice") || "0", 10);
      playSection.querySelectorAll("[data-ll-schelp-choice]").forEach(function (b) {
        b.classList.remove("sel");
        b.setAttribute("aria-pressed", "false");
      });
      btn.classList.add("sel");
      btn.setAttribute("aria-pressed", "true");
      if (choice !== answer) {
        btn.classList.add("is-wrong");
        setTimeout(function () {
          btn.classList.remove("is-wrong");
        }, 450);
        return;
      }
      card.setAttribute("data-ll-schelp-solved", "1");
      var outcome = form.querySelector('input[name="Outcome"]');
      if (outcome) outcome.value = "done";
      var qSlot = playSection.querySelector("[data-ll-puzzle-slot-q]");
      if (qSlot) {
        qSlot.classList.remove("q");
        qSlot.innerHTML =
          '<img src="/leerling/werelden/img/schelp-' +
          choice +
          '.png" alt="" width="56" height="56" loading="lazy" decoding="async" />';
      }
      var success = playSection.querySelector("[data-ll-puzzle-success]");
      if (success) success.hidden = false;
      var continueBtn = playSection.querySelector("[data-ll-puzzle-continue]");
      if (continueBtn) {
        continueBtn.disabled = false;
        continueBtn.removeAttribute("aria-disabled");
      }
    });
  }

  function shuffleInPlace(list) {
    for (var i = list.length - 1; i > 0; i--) {
      var j = Math.floor(Math.random() * (i + 1));
      var tmp = list[i];
      list[i] = list[j];
      list[j] = tmp;
    }
    return list;
  }

  function openPuzzleCard(btn) {
    if (!btn || !btn.classList) return;
    btn.classList.add("is-open");
    var src = btn.getAttribute("data-ll-match-src");
    if (src) {
      btn.style.backgroundImage = "none";
      if (!btn.querySelector("img")) {
        btn.innerHTML = '<img src="' + src + '" alt="" loading="lazy" decoding="async" />';
      }
    }
  }

  function closePuzzleCard(btn) {
    if (!btn || !btn.classList) return;
    btn.classList.remove("is-open");
    if (!btn.getAttribute("data-ll-match-src")) return;
    var back = btn.getAttribute("data-ll-card-back");
    if (back) btn.style.backgroundImage = "url('" + back + "')";
    btn.innerHTML = "";
  }

  function isPuzzlePlayVisible(section) {
    var play = section.querySelector('[data-ll-puzzle-phase="play"]');
    return !play || (!play.hidden && !play.hasAttribute("hidden"));
  }

  function bindPuzzle(root) {
    var puzzleRoots =
      root.matches && root.matches(".ll-puzzle-g78[data-ll-puzzle-kind]")
        ? [root]
        : root.querySelectorAll("[data-ll-puzzle-root], .ll-puzzle-g78[data-ll-puzzle-kind]");
    puzzleRoots.forEach(function (section) {
      if (section.getAttribute("data-ll-bound") === "1") return;
      section.setAttribute("data-ll-bound", "1");
      var form = section.closest("form") || section.querySelector("form[data-ll-puzzle-form]");
      if (!form) return;
      var outcome = form.querySelector('input[name="Outcome"]');
      var kind = section.getAttribute("data-ll-puzzle-kind") || "";
      var playSection = section.querySelector("[data-ll-puzzle-play-section]") || section;
      var continueBtn = playSection.querySelector("[data-ll-puzzle-continue]");

      function syncContinue() {
        if (!continueBtn) return;
        var ready = outcome && outcome.value;
        continueBtn.disabled = !ready;
        if (ready) {
          continueBtn.removeAttribute("aria-disabled");
        } else {
          continueBtn.setAttribute("aria-disabled", "true");
        }
      }

      function markSolved() {
        if (outcome) outcome.value = "done";
        var success = playSection.querySelector("[data-ll-puzzle-success]");
        if (success) success.hidden = false;
        syncContinue();
      }

      syncContinue();

      if (kind === "schelpenrij" && section.getAttribute("data-ll-puzzle-mode") === "pattern") {
        /* Pattern picks handled by bindSchelpenPatternPick on document. */
      } else if (kind === "schelpenrij") {
        var order = [1, 3, 2, 4];
        var pick = null;
        playSection.querySelectorAll("[data-ll-shell-index]").forEach(function (btn) {
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
              markSolved();
            }
          });
        });
      }

      if (kind === "schatkaart" && section.getAttribute("data-ll-schatkaart-bound") !== "1") {
        section.setAttribute("data-ll-schatkaart-bound", "1");
        var cardBtns = Array.from(playSection.querySelectorAll("[data-ll-card-index]"));
        if (cardBtns.length > 0 && cardBtns[0].getAttribute("data-ll-match-src")) {
          cardBtns.forEach(function (btn) {
            var style = btn.getAttribute("style") || "";
            var m = style.match(/url\(['"]?([^'")]+)/);
            if (m) btn.setAttribute("data-ll-card-back", m[1]);
          });
          shuffleInPlace(cardBtns).forEach(function (btn, idx) {
            btn.setAttribute("data-ll-card-index", String(idx));
          });
          var grid = playSection.querySelector(".ll-puzzle__cards");
          if (grid) {
            cardBtns.forEach(function (btn) {
              grid.appendChild(btn);
            });
          }
        }

        var open = new Set();
        var matched = new Set();
        var first = null;
        playSection.querySelectorAll("[data-ll-card-index]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            var idx = parseInt(btn.getAttribute("data-ll-card-index"), 10);
            var face = btn.getAttribute("data-ll-card-face") || "";
            if (matched.has(face) || open.has(idx)) return;
            if (first === null) {
              first = idx;
              open.add(idx);
              openPuzzleCard(btn);
              return;
            }
            open.add(idx);
            openPuzzleCard(btn);
            var firstBtn = playSection.querySelector('[data-ll-card-index="' + first + '"]');
            var firstFace = firstBtn ? firstBtn.getAttribute("data-ll-card-face") : "";
            if (firstFace === face) {
              matched.add(face);
              open.delete(first);
              open.delete(idx);
              if (firstBtn && firstBtn.classList) firstBtn.classList.add("is-matched");
              if (btn && btn.classList) btn.classList.add("is-matched");
              first = null;
              if (matched.size >= 3) {
                markSolved();
              }
            } else {
              setTimeout(function () {
                open.delete(first);
                open.delete(idx);
                if (firstBtn) closePuzzleCard(firstBtn);
                closePuzzleCard(btn);
                first = null;
              }, 650);
            }
          });
        });
      }

      if (kind === "vuurtorenlampen" && isPuzzlePlayVisible(section) && section.getAttribute("data-ll-lamp-game-ready") !== "1") {
        section.setAttribute("data-ll-lamp-game-ready", "1");
        var pattern = [];
        for (var pi = 0; pi < 5; pi++) pattern.push(Math.floor(Math.random() * 3));
        playSection.setAttribute("data-ll-lamp-pattern", pattern.join(","));
        var step = 0;
        var playMode = false;
        var ready = false;
        var watchEl = playSection.querySelector("[data-ll-lamp-watch]");
        var turnEl = playSection.querySelector("[data-ll-lamp-turn]");
        var replayBtn = playSection.querySelector("[data-ll-puzzle-replay]");

        function setLampUi(state) {
          if (watchEl) watchEl.hidden = state !== "watch";
          if (turnEl) turnEl.hidden = state !== "turn";
          if (replayBtn) replayBtn.hidden = state !== "turn";
        }

        function playPattern() {
          playMode = true;
          ready = false;
          step = 0;
          setLampUi("watch");
          playSection.querySelectorAll("[data-ll-lamp-index]").forEach(function (b) {
            b.classList.remove("is-lit");
          });
          var i = 0;
          function nextLamp() {
            if (i >= pattern.length) {
              playMode = false;
              ready = true;
              setLampUi("turn");
              return;
            }
            var lamp = pattern[i];
            var el = playSection.querySelector('[data-ll-lamp-index="' + lamp + '"]');
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
        if (replayBtn) {
          replayBtn.addEventListener("click", function (e) {
            e.preventDefault();
            playPattern();
          });
        }

        playSection.querySelectorAll("[data-ll-lamp-index]").forEach(function (btn) {
          btn.addEventListener("click", function () {
            if (playMode || !ready) return;
            var lamp = parseInt(btn.getAttribute("data-ll-lamp-index"), 10);
            btn.classList.add("is-lit");
            setTimeout(function () {
              btn.classList.remove("is-lit");
            }, 350);
            if (pattern[step] !== lamp) {
              step = 0;
              playPattern();
              return;
            }
            step++;
            if (step >= pattern.length) {
              setLampUi("done");
              markSolved();
            }
          });
        });
      }

      playSection.querySelectorAll("[data-ll-puzzle-skip]").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          if (outcome) outcome.value = "skipped";
          form.submit();
        });
      });

      playSection.querySelectorAll("[data-ll-puzzle-continue]").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
          e.preventDefault();
          if (btn.disabled) return;
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

  function bindSessionKeepalive() {
    if (window.__lobsyLeerlingSessionKeepalive) {
      return;
    }
    window.__lobsyLeerlingSessionKeepalive = true;
    var touchMs = 3 * 60 * 1000;
    window.setInterval(function () {
      if (document.hidden) {
        return;
      }
      var path = window.location.pathname || "";
      if (path.indexOf("/leerling") !== 0 || path === "/leerling" || path.indexOf("/leerling/stop") === 0) {
        return;
      }
      fetch("/leerling/reis?_=" + Date.now(), {
        method: "GET",
        credentials: "same-origin",
        redirect: "manual",
        headers: {
          Accept: "text/html",
          "X-Requested-With": "XMLHttpRequest",
        },
      }).catch(function () {
        /* offline blips are retried on the next answer save */
      });
    }, touchMs);
  }

  function bindPauseForms(root) {
    root.querySelectorAll("form[data-ll-pause-form]").forEach(function (form) {
      if (form.getAttribute("data-ll-bound") === "1") return;
      form.setAttribute("data-ll-bound", "1");
      form.addEventListener("submit", function (e) {
        if (form.getAttribute("data-ll-pause-submitting") === "1") {
          return;
        }
        e.preventDefault();
        flushAnswerSaveChain().then(function () {
          form.setAttribute("data-ll-pause-submitting", "1");
          HTMLFormElement.prototype.submit.call(form);
        });
      });
    });
  }

  function init() {
    var root = document.body;
    bindSessionKeepalive();
    bindReadAloud(root);
    bindAnswerScale(root);
    bindChips(root);
    bindVakkenScale(root);
    bindWereld(root);
    bindPuzzleIntro(root);
    bindSchelpenPatternPick(root);
    bindPuzzle(root);
    bindIslandSteps(root);
    bindJobPicks(root);
    bindMhboScale(root);
    bindPauseForms(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
