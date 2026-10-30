(function () {
  "use strict";

  function page() {
    return document.querySelector("[data-testid=\"saved-page-ssr\"]");
  }

  function token() {
    var el = document.getElementById("saved-ssr-token");
    return el ? el.value : "";
  }

  function markRemoved(root, vacancyId, removed) {
    root.querySelectorAll("[data-saved-vacancy-id=\"" + vacancyId + "\"]").forEach(function (el) {
      if (removed) {
        el.setAttribute("data-saved-removed", "1");
        el.hidden = true;
      } else {
        el.removeAttribute("data-saved-removed");
        el.hidden = false;
      }
    });
  }

  function toast() {
    return document.querySelector("[data-saved-toast]");
  }

  function showUndoToast(root, vacancyId, onUndo) {
    var box = toast();
    if (!box) {
      return;
    }
    var textEl = box.querySelector("[data-saved-toast-text]");
    var undoBtn = box.querySelector("[data-saved-undo]");
    if (textEl) {
      textEl.textContent = root.getAttribute("data-saved-toast-template") || "";
    }
    if (undoBtn) {
      undoBtn.hidden = false;
      undoBtn.onclick = function () {
        onUndo();
      };
    }
    box.hidden = false;
    document.documentElement.classList.add("kb-saved-toast-open");
    window.clearTimeout(showUndoToast._t);
    showUndoToast._t = window.setTimeout(function () {
      box.hidden = true;
      document.documentElement.classList.remove("kb-saved-toast-open");
      if (undoBtn) {
        undoBtn.hidden = true;
        undoBtn.onclick = null;
      }
    }, 8000);
  }

  function postForm(url, fields) {
    var body = new URLSearchParams();
    Object.keys(fields).forEach(function (key) {
      body.set(key, fields[key]);
    });
    return fetch(url, {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/x-www-form-urlencoded",
        "X-Requested-With": "XMLHttpRequest"
      },
      body: body.toString(),
      credentials: "same-origin"
    }).then(function (res) {
      return res
        .json()
        .catch(function () {
          return { ok: false, status: res.status, code: "non_json" };
        })
        .then(function (data) {
          if (data && typeof data.status !== "number") {
            data.status = res.status;
          }
          return data;
        });
    });
  }

  function unsave(root, vacancyId) {
    var path = root.getAttribute("data-saved-unsave-path") || "/api/saved-client/unsave";
    var tok = token();
    var fields = { vacancyId: vacancyId };
    if (tok) {
      fields.__RequestVerificationToken = tok;
    }
    return postForm(path, fields);
  }

  function relike(root, vacancyId) {
    var path = root.getAttribute("data-saved-save-path") || "/api/banenkaart-client/save";
    var tok = token();
    var fields = { vacancyId: vacancyId, liked: "1" };
    if (tok) {
      fields.__RequestVerificationToken = tok;
    }
    return postForm(path, fields);
  }

  function bind(root) {
    root.querySelectorAll("[data-saved-unsave]").forEach(function (btn) {
      if (btn.getAttribute("data-saved-unsave-bound") === "1") {
        return;
      }
      btn.setAttribute("data-saved-unsave-bound", "1");
      btn.addEventListener("click", function () {
        var vacancyId = btn.getAttribute("data-vacancy-id");
        if (!vacancyId) {
          return;
        }
        btn.disabled = true;
        unsave(root, vacancyId)
          .then(function (data) {
            if (!data || !data.ok) {
              window.alert(root.getAttribute("data-saved-error") || "Error");
              return;
            }
            markRemoved(root, vacancyId, true);
            root.dispatchEvent(new CustomEvent("saved-ssr:visibility-changed", { bubbles: true }));
            showUndoToast(root, vacancyId, function () {
              relike(root, vacancyId)
                .then(function (data) {
                  if (!data || data.ok === false) {
                    window.alert(root.getAttribute("data-saved-error") || "Error");
                    return;
                  }
                  markRemoved(root, vacancyId, false);
                  root.dispatchEvent(new CustomEvent("saved-ssr:visibility-changed", { bubbles: true }));
                  var box = toast();
                  if (box) {
                    box.hidden = true;
                  }
                  document.documentElement.classList.remove("kb-saved-toast-open");
                })
                .catch(function () {
                  window.alert(root.getAttribute("data-saved-error") || "Error");
                });
            });
          })
          .catch(function () {
            window.alert(root.getAttribute("data-saved-error") || "Error");
          })
          .finally(function () {
            btn.disabled = false;
          });
      });
    });
  }

  function init() {
    var root = page();
    if (!root) {
      return;
    }
    bind(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
