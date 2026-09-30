/* Public landing shell: Esc closes mobile menu / lang details; focus returns to toggle. */
(function () {
  "use strict";

  function closestDetails(el) {
    return el && el.closest ? el.closest("details.pub-menu, details.pub-lang") : null;
  }

  document.addEventListener("keydown", function (ev) {
    if (ev.key !== "Escape") return;
    var open = document.querySelectorAll(".pub-theme details.pub-menu[open], .pub-theme details.pub-lang[open]");
    if (!open.length) return;
    var last = open[open.length - 1];
    var summary = last.querySelector("summary");
    last.removeAttribute("open");
    if (summary && typeof summary.focus === "function") {
      summary.focus();
    }
  });

  // Open language menu from footer / audience "Nieuw in Nederland" (#pub-lang).
  document.addEventListener("click", function (ev) {
    var a = ev.target && ev.target.closest ? ev.target.closest('a[href="#pub-lang"]') : null;
    if (!a) return;
    var details = document.getElementById("pub-lang");
    if (!details) return;
    ev.preventDefault();
    details.setAttribute("open", "");
    var summary = details.querySelector("summary");
    if (summary && typeof summary.focus === "function") {
      summary.focus();
    }
  });
})();
