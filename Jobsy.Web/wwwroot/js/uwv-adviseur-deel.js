(function () {
  "use strict";

  document.addEventListener(
    "click",
    function (ev) {
      var btn = ev.target.closest("[data-uwv-adviseur-print]");
      if (!btn) {
        return;
      }
      ev.preventDefault();
      window.print();
    },
    true
  );
})();
