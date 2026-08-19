window.jobsyMapLibre = (function () {
    "use strict";

    var LIBERTY_URL = "https://tiles.openfreemap.org/styles/liberty";
    var BRIGHT_URL = "https://tiles.openfreemap.org/styles/bright";
    var STORAGE_KEY = "jobsy.mapStyle";

    function styleSpec(key) {
        if (key === "bright") {
            return {
                key: "bright",
                url: BRIGHT_URL,
                pitch: 45,
                maxPitch: 60,
                minPitch: 0
            };
        }
        return {
            key: "liberty",
            url: LIBERTY_URL,
            pitch: 0,
            maxPitch: 0,
            minPitch: 0
        };
    }

    function readStoredStyle() {
        try {
            var stored = localStorage.getItem(STORAGE_KEY);
            if (stored === "bright" || stored === "liberty") {
                return stored;
            }
        } catch (e) { }
        return "liberty";
    }

    function storeStyle(key) {
        try {
            localStorage.setItem(STORAGE_KEY, key);
        } catch (e) { }
    }

    function lockTouch(container) {
        if (!container) {
            return;
        }
        container.style.touchAction = "none";
        container.style.overscrollBehavior = "contain";
        container.classList.add("job-map--one-finger");
        if (container.dataset.jobsyTouchLock) {
            return;
        }
        container.dataset.jobsyTouchLock = "1";
        // One-finger pan on the map must not scroll the rest of the page.
        container.addEventListener("touchmove", function (ev) {
            // One finger pans the map; two fingers may pinch-zoom. Never scroll the page.
            if (ev.touches && ev.touches.length > 1) {
                return;
            }
            if (ev.cancelable) {
                ev.preventDefault();
            }
        }, { passive: false });
    }

    function hideChrome(map) {
        if (!map) {
            return;
        }
        var el = map.getContainer();
        el.classList.add("job-map--minimal");
        var junk = el.querySelectorAll(
            ".maplibregl-ctrl-attrib, .maplibregl-ctrl-logo, a.maplibregl-ctrl-logo, .maplibregl-compact, .maplibregl-ctrl-attrib-inner, .maplibregl-ctrl-attrib-button"
        );
        for (var i = 0; i < junk.length; i++) {
            junk[i].remove();
        }
    }

    function applyCameraForStyle(map, spec) {
        if (!map || !spec) {
            return;
        }
        if (typeof map.setMinPitch === "function") {
            map.setMinPitch(spec.minPitch);
        }
        if (typeof map.setMaxPitch === "function") {
            map.setMaxPitch(spec.maxPitch);
        }
        if (spec.key === "bright") {
            if (map.dragRotate && typeof map.dragRotate.enable === "function") {
                map.dragRotate.enable();
            }
            if (map.touchPitch && typeof map.touchPitch.enable === "function") {
                map.touchPitch.enable();
            }
            map.easeTo({ pitch: spec.pitch, duration: 450 });
        } else {
            if (map.dragRotate && typeof map.dragRotate.disable === "function") {
                map.dragRotate.disable();
            }
            if (map.touchPitch && typeof map.touchPitch.disable === "function") {
                map.touchPitch.disable();
            }
            if (typeof map.resetNorthPitch === "function") {
                map.resetNorthPitch({ duration: 450 });
            } else {
                map.easeTo({ pitch: 0, bearing: 0, duration: 450 });
            }
        }
    }

    function syncStyleButtons(ctrl, key) {
        if (!ctrl) {
            return;
        }
        var buttons = ctrl.querySelectorAll("[data-map-style]");
        for (var i = 0; i < buttons.length; i++) {
            var active = buttons[i].getAttribute("data-map-style") === key;
            buttons[i].classList.toggle("is-active", active);
            buttons[i].setAttribute("aria-pressed", active ? "true" : "false");
        }
    }

    function attachStyleSwitch(map) {
        var host = map.getContainer();
        if (host.querySelector(".job-map-style-switch")) {
            return host.querySelector(".job-map-style-switch");
        }
        var ctrl = document.createElement("div");
        ctrl.className = "job-map-style-switch";
        ctrl.setAttribute("role", "group");
        ctrl.setAttribute("aria-label", "Kaartstijl");
        ctrl.innerHTML =
            "<button type=\"button\" class=\"job-map-style-switch__btn\" data-map-style=\"liberty\">Liberty</button>" +
            "<button type=\"button\" class=\"job-map-style-switch__btn\" data-map-style=\"bright\">3D / Bright</button>";
        host.appendChild(ctrl);
        syncStyleButtons(ctrl, map._jobsyStyleKey || "liberty");
        ctrl.addEventListener("click", function (ev) {
            var btn = ev.target && ev.target.closest
                ? ev.target.closest("[data-map-style]")
                : null;
            if (!btn) {
                return;
            }
            ev.preventDefault();
            ev.stopPropagation();
            setStyle(map, btn.getAttribute("data-map-style"));
        });
        map._jobsyStyleSwitch = ctrl;
        return ctrl;
    }

    function setStyle(map, key) {
        if (!map || typeof map.setStyle !== "function") {
            return;
        }
        var spec = styleSpec(key);
        if (map._jobsyStyleKey === spec.key && typeof map.isStyleLoaded === "function" && map.isStyleLoaded()) {
            return;
        }
        map._jobsyStyleKey = spec.key;
        storeStyle(spec.key);
        syncStyleButtons(map._jobsyStyleSwitch, spec.key);
        var onRestore = map._jobsyOnStyleRestored;
        map.setStyle(spec.url);
        map.once("style.load", function () {
            applyCameraForStyle(map, spec);
            hideChrome(map);
            lockTouch(map.getContainer());
            if (typeof onRestore === "function") {
                onRestore(spec);
            }
        });
    }

    function pinReservedBox(container) {
        if (!container || container.id !== "job-map") {
            return;
        }
        var wide = false;
        try {
            wide = window.matchMedia("(min-width: 769px)").matches;
        } catch (e) { }
        if (!wide) {
            try {
                wide = (window.innerWidth || 0) >= 769;
            } catch (e) { }
        }
        if (wide) {
            container.style.position = "absolute";
            container.style.inset = "0";
            container.style.top = "0";
            container.style.right = "0";
            container.style.bottom = "0";
            container.style.left = "0";
            container.style.width = "100%";
            container.style.height = "100%";
            container.style.minHeight = "0";
            container.style.minWidth = "0";
            container.style.display = "block";
            var stage = container.parentElement;
            var pane = container.closest ? container.closest(".map-pane") : null;
            if (stage && stage.classList && stage.classList.contains("map-stage")) {
                stage.style.position = "relative";
                stage.style.height = "100%";
                stage.style.minHeight = "0";
                stage.style.width = "100%";
            }
            if (pane) {
                pane.style.height = "100%";
                pane.style.minHeight = "0";
                pane.style.position = "relative";
            }
            return;
        }
        var height = container.style.height;
        var minHeight = container.style.minHeight;
        if (!height || height === "0" || height === "0px") {
            container.style.height = "300px";
        }
        if (!minHeight || minHeight === "0" || minHeight === "0px") {
            container.style.minHeight = "300px";
        }
        container.style.width = container.style.width || "100%";
        container.style.display = "block";
        container.style.minWidth = container.style.minWidth || "100%";
        if (container.style.position === "absolute") {
            container.style.position = "relative";
            container.style.inset = "auto";
            container.style.top = "";
            container.style.right = "";
            container.style.bottom = "";
            container.style.left = "";
        }
    }

    function createMap(container, options) {
        if (typeof maplibregl === "undefined") {
            throw new Error("MapLibre GL JS (maplibregl) is not loaded");
        }
        options = options || {};
        pinReservedBox(container);
        var styleKey = options.styleKey || readStoredStyle();
        var spec = styleSpec(styleKey);
        var map = new maplibregl.Map({
            container: container,
            style: spec.url,
            center: options.center,
            zoom: options.zoom,
            attributionControl: false,
            cooperativeGestures: false,
            fadeDuration: 0,
            pitch: spec.pitch,
            maxPitch: spec.maxPitch,
            minPitch: spec.minPitch,
            dragRotate: spec.key === "bright",
            touchPitch: spec.key === "bright",
            pitchWithRotate: spec.key === "bright",
            scrollZoom: options.scrollZoom !== false,
            dragPan: true,
            touchZoomRotate: true,
            renderWorldCopies: false,
            maxZoom: 19,
            minZoom: 4,
            locale: {
                "AttributionControl.ToggleAttribution": "",
                "Map.Title": ""
            }
        });

        map._jobsyStyleKey = spec.key;
        pinReservedBox(map.getContainer());
        lockTouch(map.getContainer());
        hideChrome(map);
        map.addControl(new maplibregl.NavigationControl({
            showCompass: false,
            showZoom: true,
            visualizePitch: false
        }), "top-right");
        attachStyleSwitch(map);

        map.on("load", function () {
            pinReservedBox(map.getContainer());
            hideChrome(map);
            applyCameraForStyle(map, spec);
        });
        map.on("styledata", function () {
            pinReservedBox(map.getContainer());
            hideChrome(map);
        });
        map.on("resize", function () {
            pinReservedBox(map.getContainer());
        });

        return map;
    }

    return {
        STYLES: {
            liberty: LIBERTY_URL,
            bright: BRIGHT_URL
        },
        styleSpec: styleSpec,
        readStoredStyle: readStoredStyle,
        createMap: createMap,
        setStyle: setStyle,
        hideChrome: hideChrome,
        lockTouch: lockTouch,
        pinReservedBox: pinReservedBox
    };
})();
