(function () {
  "use strict";

  function bindRowNavigation() {
    document.addEventListener(
      "click",
      function (event) {
        var target = event.target;
        if (!target || !target.closest) {
          return;
        }
        var row = target.closest("tr[data-sch-row-href]");
        if (!row) {
          return;
        }
        if (target.closest("a, button, input, select, textarea, label, form")) {
          return;
        }
        var href = row.getAttribute("data-sch-row-href");
        if (href) {
          window.location.assign(href);
        }
      },
      true
    );
  }

  function bindConfirmForms() {
    document.addEventListener(
      "submit",
      function (event) {
        var form = event.target;
        if (!form || !form.getAttribute) {
          return;
        }
        var message = form.getAttribute("data-sch-confirm");
        if (!message) {
          return;
        }
        if (!window.confirm(message)) {
          event.preventDefault();
        }
      },
      true
    );
  }

  function bindAutoSubmitSelects() {
    document.addEventListener("change", function (event) {
      var select = event.target;
      if (!select || !select.matches || !select.matches("select[data-sch-auto-submit]")) {
        return;
      }
      if (select.form) {
        select.form.submit();
      }
    });
  }

  function init() {
    bindRowNavigation();
    bindConfirmForms();
    bindAutoSubmitSelects();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
