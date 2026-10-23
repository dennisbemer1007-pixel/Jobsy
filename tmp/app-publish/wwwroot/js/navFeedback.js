/* Instant bottom-nav pressed state + thin route progress (CSS bar).
   Runs before Blazor finishes navigation so taps feel immediate on slow 4G. */
(function () {
    "use strict";

    var ACTIVE = "is-active";
    var progressEl = null;
    var previousItem = null;
    var failTimer = 0;
    var startHref = "";
    var enhancedBound = false;

    function ensureProgress() {
        if (progressEl && progressEl.isConnected) {
            return progressEl;
        }
        progressEl = document.createElement("div");
        progressEl.className = "nav-route-progress";
        progressEl.setAttribute("aria-hidden", "true");
        (document.body || document.documentElement).appendChild(progressEl);
        return progressEl;
    }

    function showProgress() {
        ensureProgress().classList.add("is-active");
    }

    function hideProgress() {
        if (progressEl) {
            progressEl.classList.remove("is-active");
        }
    }

    function clearFailTimer() {
        if (failTimer) {
            clearTimeout(failTimer);
            failTimer = 0;
        }
    }

    function setActive(item) {
        var nav = item.closest(".bottom-nav");
        if (!nav) {
            return;
        }
        previousItem = nav.querySelector(".bottom-nav__item.is-active, .bottom-nav__item[aria-current='page']");
        var items = nav.querySelectorAll(".bottom-nav__item");
        for (var i = 0; i < items.length; i++) {
            items[i].classList.remove(ACTIVE);
            items[i].removeAttribute("aria-current");
        }
        item.classList.add(ACTIVE);
        item.setAttribute("aria-current", "page");
    }

    function restorePrevious() {
        if (!previousItem || !previousItem.isConnected) {
            previousItem = null;
            return;
        }
        var nav = previousItem.closest(".bottom-nav");
        if (!nav) {
            previousItem = null;
            return;
        }
        var items = nav.querySelectorAll(".bottom-nav__item");
        for (var i = 0; i < items.length; i++) {
            items[i].classList.remove(ACTIVE);
            items[i].removeAttribute("aria-current");
        }
        previousItem.classList.add(ACTIVE);
        previousItem.setAttribute("aria-current", "page");
        previousItem = null;
    }

    function onNavigated() {
        clearFailTimer();
        hideProgress();
        previousItem = null;
        startHref = "";
    }

    function bindEnhancedLoad() {
        if (enhancedBound) {
            return;
        }
        if (window.Blazor && typeof Blazor.addEventListener === "function") {
            Blazor.addEventListener("enhancedload", onNavigated);
            enhancedBound = true;
        }
    }

    function onPointerDown(ev) {
        if (ev.button != null && ev.button !== 0) {
            return;
        }
        var target = ev.target;
        if (!target || !target.closest) {
            return;
        }
        var item = target.closest("a.bottom-nav__item");
        if (!item || !item.getAttribute("href")) {
            return;
        }
        if (item.classList.contains(ACTIVE) || item.getAttribute("aria-current") === "page") {
            return;
        }

        bindEnhancedLoad();
        setActive(item);
        showProgress();
        startHref = location.href;
        clearFailTimer();
        failTimer = setTimeout(function () {
            failTimer = 0;
            if (startHref && location.href === startHref) {
                restorePrevious();
                hideProgress();
            }
        }, 8000);
    }

    document.addEventListener("pointerdown", onPointerDown, { capture: true, passive: true });

    // Location settled (Blazor NavigationManager / full load / back-forward).
    window.addEventListener("pageshow", onNavigated);
    window.addEventListener("popstate", onNavigated);
    document.addEventListener("enhancedload", onNavigated);

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bindEnhancedLoad);
    } else {
        bindEnhancedLoad();
    }
    // Blazor.start is async — retry briefly until addEventListener is available.
    var tries = 0;
    var retry = setInterval(function () {
        bindEnhancedLoad();
        tries += 1;
        if (enhancedBound || tries > 40) {
            clearInterval(retry);
        }
    }, 250);
})();
