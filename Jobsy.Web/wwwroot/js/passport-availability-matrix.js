(function () {
  "use strict";

  function syncCell(label) {
    var input = label.querySelector('input[type="checkbox"]');
    if (!input) return;
    var on = input.checked;
    label.classList.toggle("is-on", on);
    label.setAttribute("aria-pressed", on ? "true" : "false");
    var mark = label.querySelector(".availability-matrix__mark");
    if (on && !mark) {
      mark = document.createElement("span");
      mark.className = "availability-matrix__mark";
      mark.setAttribute("aria-hidden", "true");
      mark.style.fontWeight = "700";
      mark.style.fontSize = "1.125rem";
      mark.style.lineHeight = "1";
      mark.textContent = "✓";
      label.appendChild(mark);
    } else if (!on && mark) {
      mark.remove();
    }
  }

  function init() {
    document.querySelectorAll("[data-passport-matrix-cell]").forEach(function (label) {
      if (label.getAttribute("data-passport-matrix-bound") === "1") return;
      label.setAttribute("data-passport-matrix-bound", "1");
      syncCell(label);
      label.addEventListener("click", function (ev) {
        if (ev.target && ev.target.tagName === "INPUT") return;
        var input = label.querySelector('input[type="checkbox"]');
        if (!input) return;
        ev.preventDefault();
        input.checked = !input.checked;
        syncCell(label);
      });
      var input = label.querySelector('input[type="checkbox"]');
      if (input) {
        input.addEventListener("change", function () {
          syncCell(label);
        });
      }
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
