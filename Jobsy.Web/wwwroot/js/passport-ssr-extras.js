(function () {
  "use strict";

  function bindReadAloud(root) {
    root.querySelectorAll("[data-read-aloud-text]").forEach(function (btn) {
      if (btn.getAttribute("data-read-aloud-bound") === "1") return;
      btn.setAttribute("data-read-aloud-bound", "1");
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var text = btn.getAttribute("data-read-aloud-text") || "";
        if (!text || !window.lobsyReadAloud || typeof window.lobsyReadAloud.speak !== "function") return;
        var lang = document.documentElement.lang || "nl";
        window.lobsyReadAloud.speak(text, lang);
      });
    });
  }

  function bindRefTip(root) {
    root.querySelectorAll("[data-ref-verif-tip-dismiss]").forEach(function (btn) {
      if (btn.getAttribute("data-ref-tip-bound") === "1") return;
      btn.setAttribute("data-ref-tip-bound", "1");
      btn.addEventListener("click", function () {
        var key = btn.getAttribute("data-ref-verif-tip-dismiss") || "";
        if (key && window.jobsyRefVerificationTip) {
          window.jobsyRefVerificationTip.dismiss(key);
        }
        var tip = btn.closest(".ref-verif-tip");
        if (tip) tip.hidden = true;
      });
    });
  }

  function triggerPdfDownload(href) {
    if (!href) {
      return;
    }
    try {
      var url = new URL(href, window.location.href);
      if (url.origin !== window.location.origin) {
        window.location.assign(url.href);
        return;
      }
    } catch (eUrl) {
      window.location.assign(href);
      return;
    }
    fetch(href, { credentials: "same-origin", headers: { Accept: "application/pdf" } })
      .then(function (res) {
        if (!res.ok) {
          throw new Error("pdf");
        }
        return res.blob();
      })
      .then(function (blob) {
        var objectUrl = URL.createObjectURL(blob);
        var temp = document.createElement("a");
        temp.href = objectUrl;
        temp.download = "";
        temp.rel = "noopener";
        document.body.appendChild(temp);
        temp.click();
        temp.remove();
        setTimeout(function () {
          URL.revokeObjectURL(objectUrl);
        }, 60_000);
      })
      .catch(function () {
        window.location.assign(href);
      });
  }

  function bindPassportPdfLinks(root) {
    root.querySelectorAll("[data-testid=passport-download-pdf-mobile], [data-testid=passport-download-pdf]").forEach(function (link) {
      if (link.getAttribute("data-passport-pdf-bound") === "1") {
        return;
      }
      link.setAttribute("data-passport-pdf-bound", "1");
      link.addEventListener("click", function (ev) {
        var href = link.getAttribute("href");
        if (!href) {
          return;
        }
        ev.preventDefault();
        ev.stopPropagation();
        triggerPdfDownload(href);
      });
    });
  }

  function init() {
    var root = document.querySelector("[data-passport-ssr]");
    if (!root) return;
    bindReadAloud(root);
    bindRefTip(root);
    bindPassportPdfLinks(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
