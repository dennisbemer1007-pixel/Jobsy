window.vacancyWizard = {
    isMobilePreview: function () {
        return window.matchMedia("(max-width: 1023px)").matches;
    },
    init: function () {
        var root = document.documentElement;
        root.classList.add("has-vacancy-wizard");

        function measureCookie() {
            var banner = document.querySelector(".cookie-consent");
            var height = banner && banner.offsetParent !== null ? banner.offsetHeight : 0;
            root.style.setProperty("--vac-wizard-cookie-offset", height + "px");
        }

        measureCookie();
        window.addEventListener("resize", measureCookie, { passive: true });

        if (typeof MutationObserver !== "undefined") {
            var cookieClassObserver = new MutationObserver(measureCookie);
            cookieClassObserver.observe(root, { attributes: true, attributeFilter: ["class"] });
            var banner = document.querySelector(".cookie-consent");
            if (banner) {
                cookieClassObserver.observe(banner, { attributes: true, childList: true, subtree: true });
            }
        }

        if (window.visualViewport) {
            window.visualViewport.addEventListener("resize", function () {
                var keyboardOpen = window.visualViewport.height < window.innerHeight * 0.75;
                root.classList.toggle("vac-wizard-keyboard-open", keyboardOpen);
            }, { passive: true });
        }

        document.addEventListener(
            "click",
            function (ev) {
                var btn = ev.target && ev.target.closest
                    ? ev.target.closest("[data-consent], .cookie-consent button")
                    : null;
                if (btn) {
                    setTimeout(measureCookie, 80);
                }
            },
            true
        );
    }
};
