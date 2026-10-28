(function () {
  "use strict";

  function lees(b) {
    var t =
      b.getAttribute("data-t") ||
      (b.parentNode && b.parentNode.querySelector("p") && b.parentNode.querySelector("p").textContent);
    if (!t || !window.speechSynthesis) {
      return;
    }
    if (window.speechSynthesis.speaking) {
      window.speechSynthesis.cancel();
      b.classList.remove("on");
      return;
    }
    var u = new SpeechSynthesisUtterance(t);
    u.lang = "nl-NL";
    u.rate = 0.92;
    var voices = window.speechSynthesis.getVoices();
    var v = voices.find(function (x) {
      return x.lang && x.lang.toLowerCase().indexOf("nl") === 0;
    });
    if (v) {
      u.voice = v;
    }
    u.onend = function () {
      b.classList.remove("on");
    };
    b.classList.add("on");
    window.speechSynthesis.speak(u);
  }

  function bind(root) {
    root.querySelectorAll(".speak, .mspk").forEach(function (btn) {
      if (btn.getAttribute("data-plain878-speak-bound") === "1") {
        return;
      }
      btn.setAttribute("data-plain878-speak-bound", "1");
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        lees(btn);
      });
    });
  }

  function init() {
    var root = document.querySelector(".cp-plain878");
    if (root) {
      bind(root);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
