(function () {
  "use strict";

  function page() {
    return document.querySelector("[data-testid=\"saved-page-ssr\"]");
  }

  function token() {
    var el = document.getElementById("saved-ssr-token");
    return el ? el.value : "";
  }

  function hideVacancy(root, vacancyId) {
    root.querySelectorAll("[data-saved-vacancy-id=\"" + vacancyId + "\"]").forEach(function (el) {
      el.hidden = true;
    });
  }

  function showVacancy(root, vacancyId) {
    root.querySelectorAll("[data-saved-vacancy-id=\"" + vacancyId + "\"]").forEach(function (el) {
      el.hidden = false;
    });
  }

  function toast(root) {
    return root.querySelector("[data-saved-toast]");
  }

  function showUndoToast(root, vacancyId, onUndo) {
    var box = toast(root);
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
    window.clearTimeout(showUndoToast._t);
    showUndoToast._t = window.setTimeout(function () {
      box.hidden = true;
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
      return res.json().catch(function () {
        return { ok: false };
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
            hideVacancy(root, vacancyId);
            root.dispatchEvent(new CustomEvent("saved-ssr:visibility-changed", { bubbles: true }));
            showUndoToast(root, vacancyId, function () {
              relike(root, vacancyId)
                .then(function (data) {
                  if (!data || data.ok === false) {
                    window.alert(root.getAttribute("data-saved-error") || "Error");
                    return;
                  }
                  showVacancy(root, vacancyId);
                  root.dispatchEvent(new CustomEvent("saved-ssr:visibility-changed", { bubbles: true }));
                  var box = toast(root);
                  if (box) {
                    box.hidden = true;
                  }
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
