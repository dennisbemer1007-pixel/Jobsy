(function () {
  "use strict";

  function config() {
    return document.getElementById("lobsy-assistant-ssr-config");
  }

  function cfg(key, fallback) {
    var el = config();
    if (!el) return fallback || "";
    var val = el.getAttribute(key);
    return val == null ? (fallback || "") : val;
  }

  function syncClearance() {
    var dock = document.querySelector(".lobsy-coach-dock[data-ssr-coach]");
    if (!dock) return;
    var btn = dock.querySelector("#lobsy-coach-btn") || dock;
    var box = btn.getBoundingClientRect();
    if (!box || box.width < 8) return;
    var gap = 12;
    var clear = Math.ceil(window.innerWidth - box.left + gap);
    if (clear < 72) clear = 72;
    document.documentElement.style.setProperty("--lobsy-coach-clear-inline", clear + "px");
  }

  function hasBottomNav() {
    return document.querySelector(".app-shell.has-bottom-nav, .app-shell:has(.bottom-nav)");
  }

  function ensurePanel() {
    var existing = document.getElementById("lobsy-assistant-panel");
    if (existing) return existing;

    var shell = document.createElement("div");
    shell.className = "lobsy-assistant lobsy-assistant--coach lobsy-assistant--skin-warm";
    shell.id = "lobsy-assistant-ssr-shell";
    shell.hidden = true;
    if (hasBottomNav()) shell.classList.add("has-bottom-nav");

    shell.innerHTML =
      '<div id="lobsy-assistant-panel" class="lobsy-assistant__panel" role="dialog" aria-modal="false" aria-labelledby="lobsy-assistant-title" tabindex="-1">' +
      '<header class="lobsy-assistant__header">' +
      '<div class="lobsy-assistant__brand">' +
      '<img class="lobsy-assistant__avatar" src="/brand/svg/lobsy-merkteken.svg" alt="" width="40" height="40" decoding="async" />' +
      "<div><h2 id=\"lobsy-assistant-title\"></h2><p class=\"lobsy-assistant__lead\"></p></div>" +
      "</div>" +
      '<div class="lobsy-assistant__header-actions">' +
      '<button type="button" class="lobsy-assistant__icon-btn" data-ssr-assistant-new aria-label="">+</button>' +
      '<button type="button" class="lobsy-assistant__icon-btn" data-ssr-assistant-close aria-label="">×</button>' +
      "</div></header>" +
      '<p class="lobsy-assistant__context" data-ssr-assistant-context hidden role="status"></p>' +
      '<div class="lobsy-assistant__chat" data-ssr-assistant-chat></div>' +
      '<div class="lobsy-assistant__chips" data-ssr-assistant-chips></div>' +
      '<div class="lobsy-assistant__actions" data-ssr-assistant-action-links></div>' +
      '<form class="lobsy-assistant__compose" data-ssr-assistant-form>' +
      '<label class="visually-hidden" for="lobsy-assistant-input"></label>' +
      '<div class="lobsy-assistant__compose-row">' +
      '<textarea id="lobsy-assistant-input" rows="1" maxlength="2000"></textarea>' +
      '<button type="submit" class="lobsy-assistant__send" aria-label="">→</button>' +
      "</div></form>" +
      '<p class="lobsy-assistant__disclaimer"></p>' +
      "</div>";

    document.body.appendChild(shell);

    shell.querySelector("#lobsy-assistant-title").textContent = cfg("data-title", "Lobsy");
    shell.querySelector(".lobsy-assistant__lead").textContent = cfg("data-subtitle", "");
    shell.querySelector("[data-ssr-assistant-close]").setAttribute("aria-label", cfg("data-close", "Close"));
    shell.querySelector("[data-ssr-assistant-new]").setAttribute("aria-label", cfg("data-new-chat", "New chat"));
    shell.querySelector(".lobsy-assistant__send").textContent = cfg("data-send", "Send");
    shell.querySelector("#lobsy-assistant-input").placeholder = cfg("data-placeholder", "");
    shell.querySelector(".lobsy-assistant__disclaimer").textContent = cfg("data-disclaimer", "");

    var closeBtn = shell.querySelector("[data-ssr-assistant-close]");
    if (closeBtn && closeBtn.dataset.ssrCloseBound !== "1") {
      closeBtn.dataset.ssrCloseBound = "1";
      closeBtn.addEventListener("click", function (ev) {
        ev.preventDefault();
        ev.stopPropagation();
        setOpen(false);
      });
    }

    return shell.querySelector("#lobsy-assistant-panel");
  }

  var state = {
    open: false,
    busy: false,
    started: false,
    messages: []
  };

  function setOpen(on) {
    state.open = !!on;
    var btn = document.getElementById("lobsy-coach-btn");
    if (btn) btn.setAttribute("aria-expanded", state.open ? "true" : "false");
    if (state.open) {
      var panel = ensurePanel();
      var shell = document.getElementById("lobsy-assistant-ssr-shell");
      if (shell) {
        shell.hidden = false;
        shell.classList.add("is-open");
      }
      panel.focus();
      return;
    }
    var shell = document.getElementById("lobsy-assistant-ssr-shell");
    if (shell) {
      shell.hidden = true;
      shell.classList.remove("is-open");
    }
  }

  function renderMessages() {
    var chat = document.querySelector("[data-ssr-assistant-chat]");
    if (!chat) return;
    chat.innerHTML = "";
    var you = cfg("data-you", "You");
    var bot = cfg("data-title", "Lobsy");
    state.messages.forEach(function (msg) {
      var bubble = document.createElement("div");
      bubble.className = "lobsy-assistant__bubble lobsy-assistant__bubble--" + msg.role;
      if (msg.role === "assistant") {
        bubble.innerHTML =
          '<div class="lobsy-assistant__bubble-head">' +
          '<img class="lobsy-assistant__bubble-logo" src="/brand/svg/lobsy-merkteken.svg" alt="" width="32" height="32" />' +
          "<span>" + bot + "</span></div>";
      } else {
        bubble.innerHTML = '<span class="lobsy-assistant__bubble-role">' + you + "</span>";
      }
      var p = document.createElement("p");
      p.textContent = msg.content;
      bubble.appendChild(p);
      chat.appendChild(bubble);
    });
    if (state.busy) {
      var typing = document.createElement("div");
      typing.className = "lobsy-assistant__bubble lobsy-assistant__bubble--assistant lobsy-assistant__bubble--typing";
      typing.innerHTML = "<p>" + cfg("data-typing", "…") + "</p>";
      chat.appendChild(typing);
    }
    chat.scrollTop = chat.scrollHeight;
  }

  function renderChips(chips) {
    var host = document.querySelector("[data-ssr-assistant-chips]");
    if (!host) return;
    host.innerHTML = "";
    (chips || []).forEach(function (chip) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "lobsy-assistant__chip";
      btn.textContent = chip.label;
      btn.dataset.ssrChipMessage = chip.message;
      host.appendChild(btn);
    });
  }

  function renderActionLinks(actions) {
    var host = document.querySelector("[data-ssr-assistant-action-links]");
    if (!host) return;
    host.innerHTML = "";
    var seen = {};
    var count = 0;
    (actions || []).forEach(function (action) {
      if (count >= 4) return;
      var url = action && (action.url || action.Url);
      if (!url || seen[url]) return;
      seen[url] = true;
      count += 1;
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "engagement-btn lobsy-assistant__action";
      var label = action.label || action.Label || url || "→";
      btn.textContent = String(label).trim();
      btn.dataset.ssrActionUrl = url;
      host.appendChild(btn);
    });
  }

  function postChat(payload) {
    return fetch(cfg("data-chat-url", "/api/assistant-client/chat"), {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).then(function (res) {
      if (!res.ok) throw new Error("chat failed");
      return res.json();
    });
  }

  function showLocalGreeting() {
    state.started = true;
    var greeting = cfg("data-greeting", cfg("data-subtitle", ""));
    if (greeting) {
      state.messages.push({ role: "assistant", content: greeting });
    }
    renderMessages();
  }

  function startChat() {
    state.busy = true;
    renderMessages();
    return postChat({
      messages: [],
      pagePath: window.location.pathname + window.location.search,
      warm: true
    })
      .then(function (data) {
        var ctx = document.querySelector("[data-ssr-assistant-context]");
        if (ctx) {
          if (data.pageContextLine) {
            ctx.textContent = data.pageContextLine;
            ctx.hidden = false;
          } else {
            ctx.hidden = true;
          }
        }
        if (state.messages.length === 0) {
          state.messages.push({ role: "assistant", content: data.reply || "" });
        }
        renderChips(data.chips || []);
        renderActionLinks(data.actions || []);
      })
      .catch(function () {
        if (state.messages.length === 0) {
          state.messages.push({ role: "assistant", content: cfg("data-greeting", cfg("data-subtitle", "")) });
        }
      })
      .finally(function () {
        state.busy = false;
        state.started = true;
        renderMessages();
      });
  }

  function sendMessage(text) {
    if (state.busy || !text) return;
    state.messages.push({ role: "user", content: text });
    state.busy = true;
    renderChips([]);
    renderActionLinks([]);
    renderMessages();
    postChat({
      messages: state.messages,
      pagePath: window.location.pathname + window.location.search,
      warm: false
    })
      .then(function (data) {
        state.messages.push({ role: "assistant", content: data.reply || "" });
        var ctx = document.querySelector("[data-ssr-assistant-context]");
        if (ctx && data.pageContextLine) {
          ctx.textContent = data.pageContextLine;
          ctx.hidden = false;
        }
        if (data.chips && data.chips.length) {
          renderChips(data.chips);
        } else {
          renderChips([]);
        }
        renderActionLinks(data.actions || []);
      })
      .catch(function () {
        state.messages.pop();
      })
      .finally(function () {
        state.busy = false;
        renderMessages();
      });
  }

  function bind() {
    if (document.documentElement.dataset.ssrCoachBound === "1") return;
    document.documentElement.dataset.ssrCoachBound = "1";

    document.body.addEventListener("click", function (ev) {
      var open = ev.target.closest("[data-ssr-coach-open]");
      if (open) {
        ev.preventDefault();
        if (state.open) {
          setOpen(false);
          return;
        }
        setOpen(true);
        if (!state.started) startChat();
        return;
      }

      var close = ev.target.closest("[data-ssr-assistant-close]");
      if (close) {
        ev.preventDefault();
        setOpen(false);
        return;
      }
      var fresh = ev.target.closest("[data-ssr-assistant-new]");
      if (fresh) {
        ev.preventDefault();
        state.messages = [];
        state.started = false;
        startChat();
      }
    });

    document.addEventListener("keydown", function (ev) {
      if (ev.key === "Escape" && state.open) setOpen(false);
    });

    document.addEventListener(
      "submit",
      function (ev) {
        var form = ev.target;
        if (!form || !form.matches || !form.matches("[data-ssr-assistant-form]")) return;
        ev.preventDefault();
        var input = document.getElementById("lobsy-assistant-input");
        if (!input) return;
        var text = (input.value || "").trim();
        if (!text) return;
        input.value = "";
        sendMessage(text);
      },
      true
    );

    document.body.addEventListener("click", function (ev) {
      var chip = ev.target.closest("[data-ssr-assistant-chips] .lobsy-assistant__chip");
      if (chip && !chip.disabled) {
        ev.preventDefault();
        var msg = chip.dataset.ssrChipMessage || (chip.textContent || "").trim();
        if (msg) sendMessage(msg);
        return;
      }
      var link = ev.target.closest("[data-ssr-assistant-action-links] .lobsy-assistant__action");
      if (!link || link.disabled) return;
      ev.preventDefault();
      var url = link.dataset.ssrActionUrl;
      if (url) window.location.assign(url);
    });

    syncClearance();
    window.addEventListener("resize", syncClearance);
  }

  function boot() {
    bind();
    if (!document.querySelector("[data-ssr-coach]")) {
      var retry = 0;
      var timer = setInterval(function () {
        retry += 1;
        if (document.querySelector("[data-ssr-coach]") || retry > 40) {
          clearInterval(timer);
          syncClearance();
        }
      }, 250);
    } else {
      syncClearance();
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
