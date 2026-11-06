(function () {
  "use strict";

  function fitRoot() {
    return document.querySelector("[data-passport-fit]");
  }

  function navigateFit(jobTitle) {
    var u = new URL(window.location.href);
    u.searchParams.set("tab", "fit");
    var title = String(jobTitle || "").trim();
    if (title.length >= 2) {
      u.searchParams.set("check", title);
    } else {
      u.searchParams.delete("check");
    }
    window.location.assign(u.pathname + u.search + u.hash);
  }

  function postRoleFit(jobTitle) {
    return fetch("/api/me/role-fit", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ jobTitle: jobTitle, vacancyId: null })
    });
  }

  function bindForm(root) {
    var form = root.querySelector("[data-passport-fit-form]");
    if (!form || form.getAttribute("data-passport-fit-bound") === "1") return;
    form.setAttribute("data-passport-fit-bound", "1");
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var input = form.querySelector("[data-passport-fit-job]");
      var title = input ? String(input.value || "").trim() : "";
      if (title.length < 2) return;
      var btn = form.querySelector('button[type="submit"]');
      if (btn) btn.disabled = true;
      postRoleFit(title)
        .then(function (res) {
          if (!res.ok) throw new Error("fit");
          navigateFit(title);
        })
        .catch(function () {
          if (btn) btn.disabled = false;
          var msg = root.querySelector("[data-passport-fit-error]");
          if (msg) msg.hidden = false;
        });
    });
  }

  function bindSimilar(root) {
    root.querySelectorAll("[data-passport-fit-similar]").forEach(function (btn) {
      if (btn.getAttribute("data-passport-fit-bound") === "1") return;
      btn.setAttribute("data-passport-fit-bound", "1");
      btn.addEventListener("click", function () {
        var title = btn.getAttribute("data-passport-fit-similar") || "";
        if (!title) return;
        btn.disabled = true;
        postRoleFit(title)
          .then(function (res) {
            if (!res.ok) throw new Error("fit");
            navigateFit(title);
          })
          .catch(function () {
            btn.disabled = false;
          });
      });
    });
  }

  function bindFutureCheck(root) {
    root.querySelectorAll("[data-passport-fit-future-check]").forEach(function (btn) {
      if (btn.getAttribute("data-passport-fit-bound") === "1") return;
      btn.setAttribute("data-passport-fit-bound", "1");
      btn.addEventListener("click", function () {
        var title = btn.getAttribute("data-passport-fit-future-check") || "";
        if (!title) return;
        btn.disabled = true;
        postRoleFit(title)
          .then(function (res) {
            if (!res.ok) throw new Error("fit");
            navigateFit(title);
          })
          .catch(function () {
            btn.disabled = false;
          });
      });
    });
  }

  function init() {
    var root = fitRoot();
    if (!root) return;
    bindForm(root);
    bindSimilar(root);
    bindFutureCheck(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
