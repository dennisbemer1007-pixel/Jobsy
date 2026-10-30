(function () {
  "use strict";

  function usesMobileHalteLayout(root) {
    var mobileRoute = root.querySelector(".journey-route__mobile-only");
    if (!mobileRoute) {
      return false;
    }
    return window.matchMedia("(max-width: 899px)").matches;
  }

  function panelMatchesLayout(panel, mobileLayout) {
    var inline = panel.classList.contains("journey-route-mobile__inline");
    return mobileLayout ? inline : !inline;
  }

  function activatePhase(root, phase) {
    if (!phase) {
      return;
    }

    var mobileLayout = usesMobileHalteLayout(root);

    if (mobileLayout) {
      root.querySelectorAll(".discovery-mobile-stop").forEach(function (stop) {
        var stopPhase = stop.getAttribute("data-phase");
        var open = stopPhase === phase;
        stop.classList.toggle("is-open", open);
      });

      root.querySelectorAll(".discovery-mobile-stop .journey-route__vignette").forEach(function (vig) {
        var link = vig.closest(".discovery-halte-link");
        var linkPhase = link && link.getAttribute("data-phase");
        var isCurrent = linkPhase === phase;
        vig.classList.toggle("is-current", isCurrent);
        if (isCurrent) {
          vig.setAttribute("data-testid", "discovery-active-vignette");
        } else {
          vig.removeAttribute("data-testid");
        }
      });
    }

    root.querySelectorAll(".discovery-phase-panel").forEach(function (panel) {
      if (!panelMatchesLayout(panel, mobileLayout)) {
        return;
      }
      var panelPhase = panel.getAttribute("data-phase");
      var active = panelPhase === phase;
      panel.classList.toggle("is-active", active);
      if (active) {
        panel.removeAttribute("hidden");
      } else {
        panel.setAttribute("hidden", "");
      }
    });

    if (!mobileLayout) {
      root.querySelectorAll(".journey-route__desktop-only .journey-route__stop-link").forEach(function (link) {
      var linkPhase = link.getAttribute("data-phase");
      var isCurrent = linkPhase === phase;
      link.classList.toggle("is-current", isCurrent);
      var vig = link.querySelector(".journey-route__vignette");
      if (vig) {
        vig.classList.toggle("is-current", isCurrent);
      }
      });
    }

    var url = new URL(window.location.href);
    url.searchParams.set("stap", "overview");
    url.searchParams.set("fase", phase);
    url.hash = "";
    window.history.replaceState(null, "", url.pathname + url.search);
    if (mobileLayout) {
      var anchor = document.getElementById("halte-" + phase);
      if (anchor) {
        anchor.scrollIntoView({ block: "nearest", behavior: "smooth" });
      }
    }
    var panelSelector = mobileLayout
      ? "#discovery-panel-" + phase
      : ".journey-route__desktop-only .discovery-phase-panel[data-phase=\"" + phase + "\"]";
    var panel = root.querySelector(panelSelector);
    if (panel && !panel.hasAttribute("hidden")) {
      panel.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
  }

  function initDiscoverySsrHaltes() {
    var root = document.querySelector('[data-discovery-ssr="1"]');
    if (!root || root.dataset.discoveryHaltesBound === "1") {
      return;
    }

    root.dataset.discoveryHaltesBound = "1";

    function handleHalteActivation(event) {
      var link = event.target.closest("a.discovery-halte-link[data-phase]");
      if (!link || !root.contains(link)) {
        return;
      }

      if (link.classList.contains("journey-route__adjust")) {
        return;
      }

      if (event.type === "click" && root.dataset.discoveryHalteTouchHandled === "1") {
        root.dataset.discoveryHalteTouchHandled = "0";
        event.preventDefault();
        return;
      }

      event.preventDefault();
      if (event.type === "pointerup" && event.pointerType === "touch") {
        root.dataset.discoveryHalteTouchHandled = "1";
      }

      activatePhase(root, link.getAttribute("data-phase"));
    }

    root.addEventListener("pointerup", handleHalteActivation, true);
    root.addEventListener("click", handleHalteActivation, true);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initDiscoverySsrHaltes);
  } else {
    initDiscoverySsrHaltes();
  }
})();
