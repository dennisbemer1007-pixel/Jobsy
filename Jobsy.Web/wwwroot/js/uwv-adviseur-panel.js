(function () {
  "use strict";

  function readBoot() {
    var el = document.getElementById("uwv-adviseur-boot");
    if (!el || !el.textContent) {
      return null;
    }
    try {
      return JSON.parse(el.textContent);
    } catch (e) {
      return null;
    }
  }

  function selectedSections(root) {
    var keys = [];
    root.querySelectorAll("[data-uwv-section-toggle]").forEach(function (input) {
      if (input.checked) {
        keys.push(input.getAttribute("data-uwv-section-toggle"));
      }
    });
    return keys;
  }

  function syncPreview(root) {
    var selected = selectedSections(root);
    root.querySelectorAll("[data-uwv-section]").forEach(function (block) {
      var key = block.getAttribute("data-uwv-section");
      if (selected.indexOf(key) >= 0) {
        block.removeAttribute("hidden");
      } else {
        block.setAttribute("hidden", "hidden");
      }
    });
  }

  function init() {
    var root = document.querySelector("[data-uwv-adviseur-panel=\"1\"]");
    if (!root) {
      return;
    }
    var boot = readBoot();

    root.addEventListener(
      "change",
      function (ev) {
        if (ev.target.matches("[data-uwv-section-toggle]")) {
          syncPreview(root);
        }
      },
      true
    );

    root.addEventListener(
      "click",
      function (ev) {
        if (ev.target.closest("[data-uwv-adviseur-print]")) {
          ev.preventDefault();
          window.print();
          return;
        }
        var shareBtn = ev.target.closest("[data-uwv-adviseur-share]");
        if (!shareBtn || !boot) {
          return;
        }
        ev.preventDefault();
        var sections = selectedSections(root);
        var errEl = root.querySelector(".uwv-adviseur__share-error");
        var urlWrap = root.querySelector(".uwv-adviseur__share-url");
        var urlInput = root.querySelector("[data-uwv-share-url-input]");
        if (sections.length === 0) {
          if (errEl) {
            errEl.textContent = boot.pickOneMessage;
            errEl.hidden = false;
          }
          return;
        }
        if (errEl) {
          errEl.hidden = true;
        }
        shareBtn.disabled = true;
        fetch(boot.shareUrl, {
          method: "POST",
          credentials: "same-origin",
          headers: { "Content-Type": "application/json", Accept: "application/json" },
          body: JSON.stringify({ sections: sections, language: boot.lang }),
        })
          .then(function (res) {
            if (!res.ok) {
              throw new Error("share failed");
            }
            return res.json();
          })
          .then(function (data) {
            if (!data || !data.token) {
              throw new Error("no token");
            }
            var link = window.location.origin + "/deel/uwv-adviseur/" + data.token;
            if (urlInput) {
              urlInput.value = link;
            }
            if (urlWrap) {
              urlWrap.hidden = false;
            }
            if (urlInput && navigator.clipboard && navigator.clipboard.writeText) {
              navigator.clipboard.writeText(link).catch(function () {});
            }
          })
          .catch(function () {
            if (errEl) {
              errEl.textContent = boot.errorMessage;
              errEl.hidden = false;
            }
          })
          .finally(function () {
            shareBtn.disabled = false;
          });
      },
      true
    );

    syncPreview(root);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
