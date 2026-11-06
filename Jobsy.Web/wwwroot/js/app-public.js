/* app-public.js — culture + cookieConsent for NoBlazor public pages. */
/* === culture.js === */
window.jobsyCulture = {
  cookieName: "Jobsy.Culture",
  get: function () {
    var name = String(this.cookieName).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    var match = document.cookie.match(new RegExp("(?:^|; )" + name + "=([^;]*)"));
    return match ? decodeURIComponent(match[1]) : null;
  },
  set: function (code) {
    var maxAge = 60 * 60 * 24 * 365;
    document.cookie =
      this.cookieName +
      "=" +
      encodeURIComponent(code) +
      "; path=/; max-age=" +
      maxAge +
      "; SameSite=Lax" +
      (location.protocol === "https:" ? "; Secure" : "");
  },
  applyDocument: function (code, rtl) {
    document.documentElement.lang = code || "nl";
    document.documentElement.dir = rtl ? "rtl" : "ltr";
  }
};

/* === cookieConsent.js === */
(function () {
    "use strict";

    var KEY = "Jobsy.CookieConsent";

    function consentStored() {
        try {
            if (localStorage.getItem(KEY)) {
                return true;
            }
        } catch (e) { }
        var escaped = KEY.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var match = document.cookie.match(new RegExp("(?:^|; )" + escaped + "=([^;]*)"));
        return !!(match && match[1]);
    }

    function applyKnownClass() {
        try {
            document.documentElement.classList.toggle("cookie-consent-known", consentStored());
        } catch (e) {
            // private mode / blocked storage — show the banner
        }
    }

    function onEnhancedNav() {
        applyKnownClass();
    }

    window.jobsyCookieConsent = {
        get: function () {
            try {
                return localStorage.getItem(KEY) || "";
            } catch (e) {
                return "";
            }
        },
        set: function (value) {
            try {
                localStorage.setItem(KEY, value || "necessary");
                var maxAge = 60 * 60 * 24 * 365;
                document.cookie = KEY + "=" + encodeURIComponent(value || "necessary")
                    + "; Path=/; SameSite=Lax; Max-Age=" + maxAge
                    + (location.protocol === "https:" ? "; Secure" : "");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        allowsAnalytics: function () {
            var value = (window.jobsyCookieConsent.get() || "").toLowerCase();
            return value === "analytics" || value.indexOf("analytics.") === 0;
        }
    };

    window.jobsyViewport = {
        isWide: function () {
            // Design-system breakpoint (file 03): split view from 900px.
            return window.matchMedia("(min-width: 900px)").matches;
        },
        isKompasWide: function () {
            return window.matchMedia("(min-width: 1024px)").matches;
        },
        /** Subscribe to ≥1024px changes; returns { dispose() } for Blazor interop. */
        watchKompasWide: function (dotNetRef, methodName) {
            if (!dotNetRef || !methodName || !window.matchMedia) {
                return { dispose: function () { } };
            }
            var mql = window.matchMedia("(min-width: 1024px)");
            var handler = function (ev) {
                try {
                    dotNetRef.invokeMethodAsync(methodName, !!(ev && typeof ev.matches === "boolean" ? ev.matches : mql.matches));
                } catch (e) { }
            };
            if (typeof mql.addEventListener === "function") {
                mql.addEventListener("change", handler);
            } else if (typeof mql.addListener === "function") {
                mql.addListener(handler);
            }
            try {
                dotNetRef.invokeMethodAsync(methodName, !!mql.matches);
            } catch (e) { }
            return {
                dispose: function () {
                    if (typeof mql.removeEventListener === "function") {
                        mql.removeEventListener("change", handler);
                    } else if (typeof mql.removeListener === "function") {
                        mql.removeListener(handler);
                    }
                }
            };
        }
    };

    // Infinite-scroll sentinel for vacancy card lists (Zoeken).
    window.jobsyList = (function () {
        var observer = null;
        var currentEl = null;
        return {
            observeMore: function (el, dotNetRef, methodName) {
                if (!el || !dotNetRef || !methodName || typeof IntersectionObserver !== "function") {
                    return;
                }
                if (observer) {
                    try { observer.disconnect(); } catch (e) { }
                    observer = null;
                }
                currentEl = el;
                observer = new IntersectionObserver(function (entries) {
                    for (var i = 0; i < entries.length; i++) {
                        if (entries[i].isIntersecting) {
                            try {
                                dotNetRef.invokeMethodAsync(methodName);
                            } catch (e) { }
                            break;
                        }
                    }
                }, { root: null, rootMargin: "240px 0px", threshold: 0.01 });
                observer.observe(el);
            },
            disconnect: function () {
                if (observer) {
                    try { observer.disconnect(); } catch (e) { }
                    observer = null;
                }
                currentEl = null;
            }
        };
    })();

    // Static cookie banner (PublicLayout SSR): buttons use data-consent, no Blazor circuit.
    document.addEventListener("click", function (ev) {
        var btn = ev.target && ev.target.closest ? ev.target.closest("[data-consent]") : null;
        if (!btn || !btn.getAttribute) return;
        var choice = (btn.getAttribute("data-consent") || "").toLowerCase();
        if (choice !== "necessary" && choice !== "analytics") return;
        ev.preventDefault();

        function finish(value) {
            window.jobsyCookieConsent.set(value);
            var banner = btn.closest(".cookie-consent");
            if (banner && banner.parentNode) {
                banner.parentNode.removeChild(banner);
            }
        }

        if (choice === "necessary") {
            finish("necessary");
            return;
        }

        fetch("/account/cookie-consent/analytics-token", {
            method: "POST",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        })
            .then(function (res) {
                if (!res.ok) throw new Error("token");
                return res.json();
            })
            .then(function (body) {
                finish((body && body.token) || "analytics");
            })
            .catch(function () {
                finish("analytics");
            });
    });

    applyKnownClass();
    document.addEventListener("enhancedload", onEnhancedNav);
    try {
        if (window.Blazor && typeof Blazor.addEventListener === "function") {
            Blazor.addEventListener("enhancedload", onEnhancedNav);
        }
    } catch (e) { }
})();

/* Header menus on NoBlazorRuntime pages (banenkaart SSR): same contract as app-core.js. */
(function () {
    "use strict";
    if (window.__jobsyHeaderMenus) {
        return;
    }
    window.__jobsyHeaderMenus = true;

    function syncAria() {
        var open = document.documentElement.getAttribute("data-header-menu");
        document.querySelectorAll("[data-menu-id]").forEach(function (root) {
            var id = root.getAttribute("data-menu-id");
            var on = open === id;
            var trigger = root.querySelector("[data-menu-trigger]");
            if (trigger) {
                trigger.setAttribute("aria-expanded", on ? "true" : "false");
            }
            root.classList.toggle("is-open", on);
            root.classList.toggle("is-closed", !on);
        });
    }

    function setOpen(id) {
        if (id) {
            document.documentElement.setAttribute("data-header-menu", id);
        } else {
            document.documentElement.removeAttribute("data-header-menu");
        }
        syncAria();
    }

    document.addEventListener("click", function (event) {
        if (event.target.closest("[data-menu-close]")) {
            setOpen(null);
            return;
        }
        var trigger = event.target.closest("[data-menu-trigger]");
        if (trigger) {
            var root = trigger.closest("[data-menu-id]");
            if (!root) {
                return;
            }
            var id = root.getAttribute("data-menu-id");
            var current = document.documentElement.getAttribute("data-header-menu");
            setOpen(current === id ? null : id);
            return;
        }
        if (event.target.closest(".header-dropdown-backdrop")) {
            setOpen(null);
            return;
        }
        if (!event.target.closest("[data-menu-id]")) {
            if (document.documentElement.hasAttribute("data-header-menu")) {
                setOpen(null);
            }
        }
    }, true);

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        if (!document.documentElement.hasAttribute("data-header-menu")) {
            return;
        }
        setOpen(null);
    });
})();

/* Static candidate nav (NoBlazor SSR): active link + document title without Blazor circuit. */
(function () {
    "use strict";
    if (window.lobsyCandidateNav && window.lobsyCandidateNav.syncActiveClassesFromLocation) {
        return;
    }
    window.lobsyCandidateNav = {
        normalizePath: function (urlOrPath) {
            try {
                var u = typeof urlOrPath === "string" && urlOrPath.indexOf("http") === 0
                    ? new URL(urlOrPath)
                    : new URL(String(urlOrPath || ""), location.href);
                var path = u.pathname || "/";
                return path.length > 1 && path.charAt(path.length - 1) === "/"
                    ? path.slice(0, -1)
                    : path;
            } catch (e) {
                var s = String(urlOrPath || "/").split("?")[0].split("#")[0];
                if (s.length > 1 && s.charAt(s.length - 1) === "/") {
                    s = s.slice(0, -1);
                }
                return s || "/";
            }
        },
        navLinkMatchesPath: function (el, path) {
            var p = this.normalizePath(path || location.pathname);
            var href = el.getAttribute("href") || "";
            var h = this.normalizePath(href);
            if (h && p === h) {
                return true;
            }
            if (h && h !== "/" && p.indexOf(h + "/") === 0) {
                return true;
            }
            var prefixes = el.getAttribute("data-lobsy-active-prefixes");
            if (!prefixes) {
                return false;
            }
            var parts = prefixes.split(",");
            for (var i = 0; i < parts.length; i++) {
                var raw = parts[i].trim();
                if (!raw) {
                    continue;
                }
                if (raw === "/") {
                    if (p === "/") {
                        return true;
                    }
                    continue;
                }
                var pref = this.normalizePath(raw);
                if (p === pref || p.indexOf(pref + "/") === 0) {
                    return true;
                }
            }
            return false;
        },
        syncActiveClassesFromLocation: function (path) {
            var p = path || location.pathname;
            var links = document.querySelectorAll('[data-lobsy-candidate-nav="true"]');
            for (var i = 0; i < links.length; i++) {
                var el = links[i];
                var active = this.navLinkMatchesPath(el, p);
                el.classList.toggle("is-active", active);
                if (active) {
                    el.setAttribute("aria-current", "page");
                } else {
                    el.removeAttribute("aria-current");
                }
            }
        }
    };
    function syncNow() {
        window.lobsyCandidateNav.syncActiveClassesFromLocation(location.pathname);
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", syncNow);
    } else {
        syncNow();
    }
    window.addEventListener("pageshow", syncNow);
})();
