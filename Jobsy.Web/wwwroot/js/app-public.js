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

    function readCookieValue() {
        var escaped = KEY.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        var match = document.cookie.match(new RegExp("(?:^|; )" + escaped + "=([^;]*)"));
        if (!match || !match[1]) {
            return "";
        }
        try {
            return decodeURIComponent(match[1]);
        } catch (e2) {
            return match[1];
        }
    }

    function consentStored() {
        try {
            if (localStorage.getItem(KEY)) {
                return true;
            }
        } catch (e) { }
        return !!readCookieValue();
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
                var ls = localStorage.getItem(KEY);
                if (ls) {
                    return ls;
                }
            } catch (e) { }
            return readCookieValue();
        },
        set: function (value) {
            try {
                localStorage.setItem(KEY, value || "necessary");
                var maxAge = 60 * 60 * 24 * 365;
                document.cookie = KEY + "=" + encodeURIComponent(value || "necessary")
                    + "; Path=/; Max-Age=" + maxAge
                    + "; SameSite=Lax"
                    + (location.protocol === "https:" ? "; Secure" : "");
                applyKnownClass();
                return true;
            } catch (e) {
                return false;
            }
        },
        clear: function () {
            try {
                localStorage.removeItem(KEY);
            } catch (e) { }
            try {
                document.cookie = KEY + "=; Path=/; Max-Age=0; SameSite=Lax"
                    + (location.protocol === "https:" ? "; Secure" : "");
            } catch (e2) { }
            applyKnownClass();
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
