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
            ".maplibregl-ctrl-attrib, .maplibregl-ctrl-logo, a.maplibregl-ctrl-logo, .maplibregl-compact"
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

    function syncStyleToggle(ctrl, key) {
        if (!ctrl) {
            return;
        }
        var btn = ctrl.querySelector(".job-map-style-switch__btn");
        if (!btn) {
            return;
        }
        var on = key === "bright";
        btn.classList.toggle("is-on", on);
        btn.setAttribute("aria-pressed", on ? "true" : "false");
        btn.title = on ? "3D uitzetten" : "3D aanzetten";
    }

    function StyleSwitchControl() { }

    StyleSwitchControl.prototype.onAdd = function (map) {
        var ctrl = document.createElement("div");
        ctrl.className = "maplibregl-ctrl maplibregl-ctrl-group job-map-style-switch";
        ctrl.innerHTML =
            "<button type=\"button\" class=\"job-map-style-switch__btn\" " +
            "aria-label=\"3D-kaart\" aria-pressed=\"false\" title=\"3D aanzetten\">3D</button>";
        syncStyleToggle(ctrl, map._jobsyStyleKey || "liberty");
        ctrl.addEventListener("click", function (ev) {
            var btn = ev.target && ev.target.closest
                ? ev.target.closest(".job-map-style-switch__btn")
                : null;
            if (!btn) {
                return;
            }
            ev.preventDefault();
            ev.stopPropagation();
            var next = (map._jobsyStyleKey === "bright") ? "liberty" : "bright";
            setStyle(map, next);
        });
        map._jobsyStyleSwitch = ctrl;
        return ctrl;
    };

    StyleSwitchControl.prototype.onRemove = function () { };

    function attachStyleSwitch(map, position) {
        var host = map.getContainer();
        if (host.querySelector(".job-map-style-switch")) {
            return host.querySelector(".job-map-style-switch");
        }
        map.addControl(new StyleSwitchControl(), position || "top-right");
        return map._jobsyStyleSwitch;
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
        syncStyleToggle(map._jobsyStyleSwitch, spec.key);
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

    function createMap(container, options) {
        if (typeof maplibregl === "undefined") {
            throw new Error("MapLibre GL JS (maplibregl) is not loaded");
        }
        options = options || {};
        var styleKey = options.styleKey || readStoredStyle();
        var spec = styleSpec(styleKey);
        var map = new maplibregl.Map({
            container: container,
            style: spec.url,
            center: options.center,
            zoom: options.zoom,
            attributionControl: false,
            maplibreLogo: false,
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
        lockTouch(map.getContainer());
        hideChrome(map);
        var controlsPosition = options.controlsPosition === "bottom-right"
            ? "bottom-right"
            : "top-right";
        map.addControl(new maplibregl.NavigationControl({
            showCompass: false,
            showZoom: true,
            visualizePitch: false
        }), controlsPosition);
        attachStyleSwitch(map, controlsPosition);

        map.on("load", function () {
            hideChrome(map);
        });
        map.on("styledata", function () {
            hideChrome(map);
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
        lockTouch: lockTouch
    };
})();
