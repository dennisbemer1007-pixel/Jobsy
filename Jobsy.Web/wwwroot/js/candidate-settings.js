(function () {
  "use strict";

  function init(root) {
    if (!root || root.dataset.settingsInit === "1") {
      return;
    }

    root.dataset.settingsInit = "1";

    root.querySelectorAll("[data-settings-danger]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var intent = btn.getAttribute("data-settings-danger");
        if (!intent) {
          return;
        }
        window.location.href =
          "/candidate/paspoort?tab=data&danger=" + encodeURIComponent(intent);
      });
    });

    var wa = root.querySelector("[data-whatsapp-optin]");
    var phone = root.querySelector("[data-whatsapp-phone]");
    if (wa && phone) {
      wa.addEventListener("change", function () {
        if (wa.checked) {
          phone.removeAttribute("hidden");
        } else {
          phone.setAttribute("hidden", "");
        }
        var label = wa.closest(".cand-toggle");
        if (label) {
          var switchEl = label.querySelector(".cand-toggle__switch");
          if (switchEl) {
            switchEl.classList.toggle("is-on", wa.checked);
          }
        }
      });
    }

    root.querySelectorAll(".cand-toggle__switch input[type=checkbox]").forEach(function (input) {
      if (input.hasAttribute("data-whatsapp-optin")) {
        return;
      }
      input.addEventListener("change", function () {
        var switchEl = input.closest(".cand-toggle__switch");
        if (switchEl) {
          switchEl.classList.toggle("is-on", input.checked);
        }
      });
    });
  }

  document.querySelectorAll("[data-testid=\"candidate-settings-ssr\"]").forEach(init);
})();
